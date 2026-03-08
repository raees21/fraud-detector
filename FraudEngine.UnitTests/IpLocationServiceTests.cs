using FraudEngine.Application.Interfaces;
using FraudEngine.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace FraudEngine.UnitTests;

public class IpLocationServiceTests
{
    [Fact]
    public async Task ResolveAsync_KnownMappedIp_ReturnsConfiguredCountry()
    {
        var options = Options.Create(new ConfiguredIpLocationService.IpLocationOptions
        {
            Mappings =
            [
                new ConfiguredIpLocationService.IpLocationMappingOptions
                {
                    Cidr = "203.0.113.0/24",
                    CountryCode = "ZA",
                    CountryName = "South Africa"
                }
            ]
        });
        var sut = new ConfiguredIpLocationService(options);

        IpLocationResult result = await sut.ResolveAsync("203.0.113.10");

        Assert.Equal("ZA", result.CountryCode);
        Assert.Equal("South Africa", result.CountryName);
        Assert.True(result.IsReliable);
    }

    [Fact]
    public async Task ResolveAsync_UnmappedIp_ReturnsUnknownLocation()
    {
        var sut = new ConfiguredIpLocationService(Options.Create(
            new ConfiguredIpLocationService.IpLocationOptions()));

        IpLocationResult result = await sut.ResolveAsync("8.8.8.8");

        Assert.Null(result.CountryCode);
        Assert.Null(result.CountryName);
        Assert.False(result.IsReliable);
    }
}
