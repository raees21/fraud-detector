using System.Net;
using FraudEngine.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace FraudEngine.Infrastructure.Services;

/// <summary>
/// Resolves IP addresses to coarse country-level locations using configured CIDR mappings.
/// </summary>
internal sealed class ConfiguredIpLocationService : IIpLocationService
{
    private readonly IReadOnlyList<ResolvedMapping> _mappings;

    public ConfiguredIpLocationService(IOptions<IpLocationOptions> options)
    {
        _mappings = options.Value.Mappings
            .Select(ResolvedMapping.TryCreate)
            .Where(mapping => mapping is not null)
            .Cast<ResolvedMapping>()
            .ToList();
    }

    public Task<IpLocationResult> ResolveAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (!IPAddress.TryParse(ipAddress, out IPAddress? parsedAddress))
            return Task.FromResult(new IpLocationResult(null, null, false));

        foreach (ResolvedMapping mapping in _mappings)
        {
            if (mapping.Contains(parsedAddress))
            {
                return Task.FromResult(new IpLocationResult(
                    mapping.CountryCode,
                    mapping.CountryName,
                    mapping.IsReliable));
            }
        }

        return Task.FromResult(new IpLocationResult(null, null, false));
    }

    internal sealed class IpLocationOptions
    {
        public List<IpLocationMappingOptions> Mappings { get; set; } = new();
    }

    internal sealed class IpLocationMappingOptions
    {
        public string Cidr { get; init; } = string.Empty;

        public string CountryCode { get; init; } = string.Empty;

        public string CountryName { get; init; } = string.Empty;

        public bool IsReliable { get; init; } = true;
    }

    private sealed class ResolvedMapping
    {
        private ResolvedMapping(byte[] networkBytes, int prefixLength, string countryCode, string countryName,
            bool isReliable)
        {
            NetworkBytes = networkBytes;
            PrefixLength = prefixLength;
            CountryCode = countryCode;
            CountryName = countryName;
            IsReliable = isReliable;
        }

        public string CountryCode { get; }

        public string CountryName { get; }

        public bool IsReliable { get; }

        private byte[] NetworkBytes { get; }

        private int PrefixLength { get; }

        public bool Contains(IPAddress address)
        {
            byte[] addressBytes = address.GetAddressBytes();
            if (addressBytes.Length != NetworkBytes.Length)
                return false;

            int fullBytes = PrefixLength / 8;
            int remainderBits = PrefixLength % 8;

            for (int index = 0; index < fullBytes; index++)
            {
                if (addressBytes[index] != NetworkBytes[index])
                    return false;
            }

            if (remainderBits == 0)
                return true;

            int mask = 0xFF << (8 - remainderBits);
            return (addressBytes[fullBytes] & mask) == (NetworkBytes[fullBytes] & mask);
        }

        public static ResolvedMapping? TryCreate(IpLocationMappingOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Cidr) ||
                string.IsNullOrWhiteSpace(options.CountryCode) ||
                string.IsNullOrWhiteSpace(options.CountryName))
                return null;

            string[] parts = options.Cidr.Split('/', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 ||
                !IPAddress.TryParse(parts[0], out IPAddress? networkAddress) ||
                !int.TryParse(parts[1], out int prefixLength))
                return null;

            byte[] networkBytes = networkAddress.GetAddressBytes();
            int maxPrefix = networkBytes.Length * 8;
            if (prefixLength < 0 || prefixLength > maxPrefix)
                return null;

            return new ResolvedMapping(
                networkBytes,
                prefixLength,
                options.CountryCode.Trim().ToUpperInvariant(),
                options.CountryName.Trim(),
                options.IsReliable);
        }
    }
}
