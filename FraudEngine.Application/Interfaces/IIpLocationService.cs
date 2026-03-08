namespace FraudEngine.Application.Interfaces;

/// <summary>
/// Resolves coarse location information from an IP address for fraud evaluation.
/// </summary>
public interface IIpLocationService
{
    /// <summary>
    /// Resolves the provided IP address to a country-level location signal.
    /// </summary>
    /// <param name="ipAddress">The source IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved location result.</returns>
    public Task<IpLocationResult> ResolveAsync(string ipAddress, CancellationToken cancellationToken = default);
}

/// <summary>
/// Country-level location data derived from an IP address.
/// </summary>
/// <param name="CountryCode">A two-letter country code when known.</param>
/// <param name="CountryName">A country name when known.</param>
/// <param name="IsReliable">Whether the resolved location is suitable for fraud decisioning.</param>
public sealed record IpLocationResult(string? CountryCode, string? CountryName, bool IsReliable);
