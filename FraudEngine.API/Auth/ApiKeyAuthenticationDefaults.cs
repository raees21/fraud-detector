namespace FraudEngine.API.Auth;

/// <summary>
/// Constants shared by the API key authentication and authorization flow.
/// </summary>
internal static class ApiKeyAuthenticationDefaults
{
    internal const string AuthenticationScheme = "PartnerApiKey";
    internal const string ClientIdHeaderName = "X-Client-Id";
    internal const string ApiKeyHeaderName = "X-Api-Key";
    internal const string ClientIdClaimType = "client_id";
    internal const string ScopeClaimType = "scope";
}
