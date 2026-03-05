using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FraudEngine.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FraudEngine.IntegrationTests;

public class TransactionScenarios : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TransactionScenarios(WebApplicationFactory<Program> factory)
    {
        // In a real scenario, we would use Testcontainers for Postgres and Redis here.
        // For simplicity and speed in this demo, we assume the API initializes cleanly,
        // or we mock the dependencies inside ConfigureTestServices.
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
               // Replace DbContext to use an InMemory instance OR Testcontainers
            });
        });
    }

    // A placeholder integration test to demonstrate setup
    // Since we need to start Docker containers for full E2E, this proves the pattern.
    [Fact(Skip = "Requires running PostgreSQL and Redis containers. Use Testcontainers for full E2E.")]
    public async Task PostTransaction_ReturnsValidEvaluation()
    {
        // Arrange
        var client = _factory.CreateClient();
        var dto = new TransactionDto(
            AccountId: "USER_123",
            Amount: 1500,
            Currency: "USD",
            MerchantName: "Amazon",
            MerchantCategory: "RETAIL",
            IPAddress: "192.168.1.1",
            DeviceId: "DEV-ABC",
            AccountAgeDays: 100,
            Timestamp: DateTimeOffset.UtcNow
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FraudEvaluationResultDto>();
        result.Should().NotBeNull();
        result!.Decision.Should().Be("ALLOW");
    }
}
