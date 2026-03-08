using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace FraudEngine.API.Auth;

/// <summary>
/// Authenticates machine-to-machine callers using partner client IDs and API keys.
/// </summary>
internal sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IOptionsMonitor<ApiKeyAuthOptions> _apiKeyOptions;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptionsMonitor<ApiKeyAuthOptions> apiKeyOptions)
        : base(options, logger, encoder)
    {
        _apiKeyOptions = apiKeyOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        ApiKeyAuthOptions authOptions = _apiKeyOptions.CurrentValue;

        bool hasClientHeader = Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.ClientIdHeaderName,
            out StringValues clientIdValues);
        bool hasApiKeyHeader = Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.ApiKeyHeaderName,
            out StringValues apiKeyValues);

        if (!hasClientHeader && !hasApiKeyHeader)
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!hasClientHeader || StringValues.IsNullOrEmpty(clientIdValues))
            return Task.FromResult(AuthenticateResult.Fail(
                $"Missing required header '{ApiKeyAuthenticationDefaults.ClientIdHeaderName}'."));

        if (!hasApiKeyHeader || StringValues.IsNullOrEmpty(apiKeyValues))
            return Task.FromResult(AuthenticateResult.Fail(
                $"Missing required header '{ApiKeyAuthenticationDefaults.ApiKeyHeaderName}'."));

        string clientId = clientIdValues.ToString().Trim();
        string apiKey = apiKeyValues.ToString().Trim();

        ApiKeyClientOptions? client = authOptions.Clients.FirstOrDefault(candidate =>
            string.Equals(candidate.ClientId, clientId, StringComparison.OrdinalIgnoreCase));

        if (client is null || !client.IsActive || !ApiKeyHasher.Verify(apiKey, client.ApiKeyHash))
            return Task.FromResult(AuthenticateResult.Fail(
                $"Invalid client ID or API key. Send the raw API key secret in '{ApiKeyAuthenticationDefaults.ApiKeyHeaderName}', not the stored SHA-256 hash."));

        var claims = new List<Claim>
        {
            new(ApiKeyAuthenticationDefaults.ClientIdClaimType, client.ClientId),
            new(ClaimTypes.NameIdentifier, client.ClientId),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(client.DisplayName) ? client.ClientId : client.DisplayName)
        };

        claims.AddRange(client.Scopes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(scope => new Claim(ApiKeyAuthenticationDefaults.ScopeClaimType, scope)));

        var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationDefaults.AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        AuthenticateResult authenticateResult = await HandleAuthenticateOnceSafeAsync();

        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = ApiKeyAuthenticationDefaults.AuthenticationScheme;
        await Response.WriteAsJsonAsync(new
        {
            Error = "Unauthorized",
            Message = GetChallengeMessage(authenticateResult)
        });
    }

    internal static string GetChallengeMessage(AuthenticateResult authenticateResult)
    {
        if (authenticateResult.Failure is not null && !string.IsNullOrWhiteSpace(authenticateResult.Failure.Message))
            return authenticateResult.Failure.Message;

        return
            $"Missing authentication headers. Provide '{ApiKeyAuthenticationDefaults.ClientIdHeaderName}' and '{ApiKeyAuthenticationDefaults.ApiKeyHeaderName}'.";
    }
}
