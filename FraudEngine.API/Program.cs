using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FraudEngine.API.Auth;
using FraudEngine.API.Middleware;
using FraudEngine.Application;
using FraudEngine.Infrastructure;
using FraudEngine.Infrastructure.Data;
using FraudEngine.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;
using Asp.Versioning;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(ApiKeyAuthenticationDefaults.ClientIdHeaderName, new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = ApiKeyAuthenticationDefaults.ClientIdHeaderName,
        Type = SecuritySchemeType.ApiKey,
        Description = "Integration client identifier."
    });

    options.AddSecurityDefinition(ApiKeyAuthenticationDefaults.ApiKeyHeaderName, new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = ApiKeyAuthenticationDefaults.ApiKeyHeaderName,
        Type = SecuritySchemeType.ApiKey,
        Description = "Partner API key secret."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = ApiKeyAuthenticationDefaults.ClientIdHeaderName
            }
        }] = Array.Empty<string>(),
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = ApiKeyAuthenticationDefaults.ApiKeyHeaderName
            }
        }] = Array.Empty<string>()
    });
});
builder.Services.AddOptions<ApiKeyAuthOptions>()
    .Bind(builder.Configuration.GetSection("ApiKeyAuth"))
    .Validate(options => options.Clients.All(client =>
            !string.IsNullOrWhiteSpace(client.ClientId) &&
            ApiKeyHasher.IsValidHash(client.ApiKeyHash) &&
            client.Scopes.Count > 0),
        "Each API client must define a client ID, a valid SHA-256 API key hash, and at least one scope.")
    .Validate(options => options.Clients
            .Select(client => client.ClientId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == options.Clients.Count,
        "API client IDs must be unique.")
    .ValidateOnStart();
builder.Services.AddAuthentication(ApiKeyAuthenticationDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationDefaults.AuthenticationScheme,
        _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ApiAuthorizationPolicies.SubmitTransactions, policy =>
        policy.RequireAuthenticatedUser()
            .RequireClaim(ApiKeyAuthenticationDefaults.ScopeClaimType, "transactions:submit"));
    options.AddPolicy(ApiAuthorizationPolicies.ReadTransactions, policy =>
        policy.RequireAuthenticatedUser()
            .RequireClaim(ApiKeyAuthenticationDefaults.ScopeClaimType, "transactions:read"));
    options.AddPolicy(ApiAuthorizationPolicies.ReadEvaluations, policy =>
        policy.RequireAuthenticatedUser()
            .RequireClaim(ApiKeyAuthenticationDefaults.ScopeClaimType, "evaluations:read"));
    options.AddPolicy(ApiAuthorizationPolicies.ReadRules, policy =>
        policy.RequireAuthenticatedUser()
            .RequireClaim(ApiKeyAuthenticationDefaults.ScopeClaimType, "rules:read"));
    options.AddPolicy(ApiAuthorizationPolicies.ManageRules, policy =>
        policy.RequireAuthenticatedUser()
            .RequireClaim(ApiKeyAuthenticationDefaults.ScopeClaimType, "rules:write"));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetTokenBucketLimiter(
            GetClientIdentifier(context),
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 2000,
                TokensPerPeriod = 250,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("transaction-submissions", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            GetClientIdentifier(context),
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1000,
                TokensPerPeriod = 100,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddApplication();
if (builder.Environment.IsEnvironment("IntegrationTesting"))
    builder.Services.AddIntegrationTestInfrastructure();
else
    builder.Services.AddInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

// Apply Migrations and Seed Database
using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext? context = scope.ServiceProvider.GetService<AppDbContext>();
    if (context is not null)
    {
        try
        {
            Log.Information("Applying Database Migrations...");

            await context.Database.EnsureCreatedAsync();

            if (await context.Database.CanConnectAsync()) await AppDbContextSeed.SeedAsync(context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during database seeding/migration checks.");
        }
    }
}

app.Run();

/// <summary>
/// Partial Program class to expose it to the integration tests project.
/// </summary>
public partial class Program
{
    internal static string GetClientIdentifier(HttpContext context)
    {
        string? authenticatedClientId =
            context.User.FindFirst(ApiKeyAuthenticationDefaults.ClientIdClaimType)?.Value;
        if (!string.IsNullOrWhiteSpace(authenticatedClientId))
            return $"client:{authenticatedClientId}";

        string? requestedClientId = context.Request.Headers[ApiKeyAuthenticationDefaults.ClientIdHeaderName]
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(requestedClientId))
            return $"client:{requestedClientId.Trim()}";

        string? forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
