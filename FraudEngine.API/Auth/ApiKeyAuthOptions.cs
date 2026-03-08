namespace FraudEngine.API.Auth;

/// <summary>
/// Configures the partner API clients that can authenticate against this service.
/// </summary>
public class ApiKeyAuthOptions
{
    public List<ApiKeyClientOptions> Clients { get; set; } = new();
}

/// <summary>
/// Represents a single partner or internal integration client.
/// </summary>
public class ApiKeyClientOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ApiKeyHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<string> Scopes { get; set; } = new();
}
