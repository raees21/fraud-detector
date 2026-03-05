using FraudEngine.API.Middleware;
using FraudEngine.Application;
using FraudEngine.Infrastructure;
using FraudEngine.Infrastructure.Data;
using FraudEngine.Infrastructure.Persistence;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Clean Architecture Layers
builder.Services.AddApplication();
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
app.UseAuthorization();
app.MapControllers();

// Apply Migrations and Seed Database
using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Log.Information("Applying Database Migrations...");
        // Ensure the database is created (for local dev/initial creation as requested)
        await context.Database.EnsureCreatedAsync();

        // Seed the initial rules if the database exists and we can connect
        if (await context.Database.CanConnectAsync()) await AppDbContextSeed.SeedAsync(context);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred during database seeding/migration checks.");
    }
}

app.Run();

/// <summary>
/// Partial Program class to expose it to the integration tests project.
/// </summary>
public partial class Program { }
