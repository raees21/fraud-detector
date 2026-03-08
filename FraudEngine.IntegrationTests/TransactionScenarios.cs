using System.Net;
using System.Net.Http.Json;
using Xunit;
using FraudEngine.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

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
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Client-Id", "partner-sandbox");
        client.DefaultRequestHeaders.Add("X-Api-Key", "replace-with-client-secret");
        var dto = new TransactionDto(
            "USER_123",
            1500,
            "USD",
            "Amazon",
            "RETAIL",
            "192.168.1.1",
            "DEV-ABC",
            100,
            DateTimeOffset.UtcNow
        );

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/transactions", dto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        FraudEvaluationResultDto? result = await response.Content.ReadFromJsonAsync<FraudEvaluationResultDto>();
        Assert.NotNull(result);
        Assert.Equal("ALLOW", result!.Decision);
        Assert.NotEqual(default, result.EvaluatedAt);
    }
}
