using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FraudEngine.Application.DTOs;

namespace FraudEngine.IntegrationTests;

public sealed class TransactionScenarios : IClassFixture<ApiIntegrationFactory>
{
    private readonly ApiIntegrationFactory _factory;

    public TransactionScenarios(ApiIntegrationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_AllowsAnonymousAccess()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement payload = await ReadJsonAsync(response);
        Assert.Equal("Healthy", payload.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetRules_WithoutAuth_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/rules");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        JsonElement payload = await ReadJsonAsync(response);
        Assert.Equal("Unauthorized", payload.GetProperty("error").GetString());
        Assert.Contains("X-Client-Id", payload.GetProperty("message").GetString());
    }

    [Fact]
    public async Task PostTransaction_WithOptionalTelemetryOmitted_ReturnsAllowAndIsQueryable()
    {
        using HttpClient client = CreateAuthenticatedClient();
        string accountId = $"ACC-{Guid.NewGuid():N}";
        var transaction = new TransactionDto(
            accountId,
            149.99m,
            "ZAR",
            "Contoso",
            "RETAIL",
            null,
            null,
            365,
            DateTimeOffset.UtcNow);

        HttpResponseMessage submitResponse = await client.PostAsJsonAsync("/api/v1/transactions", transaction);

        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        FraudEvaluationResultDto? evaluation = await submitResponse.Content.ReadFromJsonAsync<FraudEvaluationResultDto>();
        Assert.NotNull(evaluation);
        Assert.Equal("ALLOW", evaluation!.Decision);
        Assert.NotEqual(Guid.Empty, evaluation.TransactionId);

        HttpResponseMessage historyResponse = await client.GetAsync(
            $"/api/v1/transactions?accountId={Uri.EscapeDataString(accountId)}");

        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        JsonElement historyPayload = await ReadJsonAsync(historyResponse);
        Assert.Equal(1, historyPayload.GetProperty("page").GetInt32());
        Assert.Equal(20, historyPayload.GetProperty("pageSize").GetInt32());

        JsonElement[] transactions = historyPayload.GetProperty("data").EnumerateArray().ToArray();
        JsonElement matchingTransaction = Assert.Single(transactions);
        Assert.Equal(evaluation.TransactionId, matchingTransaction.GetProperty("transactionId").GetGuid());
    }

    [Fact]
    public async Task PostTransaction_WithInvalidCurrency_ReturnsBadRequest()
    {
        using HttpClient client = CreateAuthenticatedClient();
        var transaction = new TransactionDto(
            $"ACC-{Guid.NewGuid():N}",
            149.99m,
            "US",
            "Contoso",
            "RETAIL",
            null,
            null,
            365,
            DateTimeOffset.UtcNow);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/transactions", transaction);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement payload = await ReadJsonAsync(response);
        Assert.Equal("Validation.InvalidInput", payload.GetProperty("error").GetString());
        Assert.Contains("Currency", payload.GetProperty("message").GetString());
    }

    [Fact]
    public async Task GetEvaluations_FilteredByDecision_ReturnsSubmittedTransaction()
    {
        using HttpClient client = CreateAuthenticatedClient();
        string accountId = $"ACC-{Guid.NewGuid():N}";
        var transaction = new TransactionDto(
            accountId,
            99.99m,
            "ZAR",
            "Contoso",
            "RETAIL",
            null,
            null,
            365,
            DateTimeOffset.UtcNow);

        HttpResponseMessage submitResponse = await client.PostAsJsonAsync("/api/v1/transactions", transaction);
        FraudEvaluationResultDto? evaluation = await submitResponse.Content.ReadFromJsonAsync<FraudEvaluationResultDto>();

        Assert.NotNull(evaluation);
        Assert.Equal("ALLOW", evaluation!.Decision);

        HttpResponseMessage response = await client.GetAsync("/api/v1/evaluations?decision=ALLOW");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement payload = await ReadJsonAsync(response);
        bool containsTransaction = payload.GetProperty("data")
            .EnumerateArray()
            .Any(item => item.GetProperty("transactionId").GetGuid() == evaluation.TransactionId);

        Assert.True(containsTransaction);
    }

    [Fact]
    public async Task GetRules_ReturnsSanitizedRuleDefinitions()
    {
        using HttpClient client = CreateAuthenticatedClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/rules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement[] rules = (await ReadJsonAsync(response)).EnumerateArray().ToArray();

        Assert.NotEmpty(rules);
        JsonElement firstRule = rules[0];
        Assert.True(firstRule.TryGetProperty("ruleName", out _));
        Assert.True(firstRule.TryGetProperty("description", out _));
        Assert.True(firstRule.TryGetProperty("isActive", out _));
        Assert.False(firstRule.TryGetProperty("workflowJson", out _));
        Assert.False(firstRule.TryGetProperty("scoreContribution", out _));
    }

    private HttpClient CreateAuthenticatedClient()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Client-Id", "fraud-ops");
        client.DefaultRequestHeaders.Add("X-Api-Key", "fraud-ops-dev-local-2026");
        return client;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        JsonElement payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload;
    }
}
