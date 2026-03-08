using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FraudEngine.Application.DTOs;
using FraudEngine.Domain.Enums;

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
    public async Task PostTransaction_WithRequiredTelemetry_ReturnsAllowAndIsQueryable()
    {
        using HttpClient client = CreateAuthenticatedClient();
        string accountId = $"ACC-{Guid.NewGuid():N}";
        var transaction = new
        {
            accountId,
            amount = 149.99m,
            currency = "ZAR",
            merchantName = "Contoso",
            merchantCategory = "RETAIL",
            transactionType = "EFT",
            ipAddress = "203.0.113.10",
            deviceId = "DEVICE-001",
            accountAgeDays = 365,
            timestamp = DateTimeOffset.UtcNow
        };

        HttpResponseMessage submitResponse = await client.PostAsJsonAsync("/api/v1/transactions", transaction);

        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        FraudEvaluationResultDto? evaluation = await submitResponse.Content.ReadFromJsonAsync<FraudEvaluationResultDto>();
        Assert.NotNull(evaluation);
        Assert.Equal("ALLOW", evaluation!.Decision);
        Assert.NotEqual(Guid.Empty, evaluation.TransactionId);
        Assert.Equal(accountId, evaluation.AccountId);
        Assert.Contains("TRANSACTION_TYPE_EFT_RULE", evaluation.TriggeredRules);

        HttpResponseMessage historyResponse = await client.GetAsync(
            $"/api/v1/transactions?accountId={Uri.EscapeDataString(accountId)}");

        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        JsonElement historyPayload = await ReadJsonAsync(historyResponse);
        Assert.Equal(1, historyPayload.GetProperty("page").GetInt32());
        Assert.Equal(20, historyPayload.GetProperty("pageSize").GetInt32());

        JsonElement[] transactions = historyPayload.GetProperty("data").EnumerateArray().ToArray();
        JsonElement matchingTransaction = Assert.Single(transactions);
        Assert.Equal(evaluation.TransactionId, matchingTransaction.GetProperty("transactionId").GetGuid());
        Assert.Equal("EFT", matchingTransaction.GetProperty("transactionType").GetString());
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
            TransactionType.CARD,
            "203.0.113.10",
            "DEVICE-001",
            365,
            DateTimeOffset.UtcNow);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/transactions", transaction);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement payload = await ReadJsonAsync(response);
        Assert.Equal("Validation.InvalidInput", payload.GetProperty("error").GetString());
        Assert.Contains("Currency", payload.GetProperty("message").GetString());
    }

    [Fact]
    public async Task PostTransaction_WithoutRequiredTelemetry_ReturnsBadRequest()
    {
        using HttpClient client = CreateAuthenticatedClient();
        var transaction = new
        {
            accountId = $"ACC-{Guid.NewGuid():N}",
            amount = 149.99m,
            currency = "ZAR",
            merchantName = "Contoso",
            merchantCategory = "RETAIL",
            transactionType = "CARD",
            ipAddress = string.Empty,
            deviceId = string.Empty,
            accountAgeDays = 365,
            timestamp = DateTimeOffset.UtcNow
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/transactions", transaction);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement payload = await ReadJsonAsync(response);
        Assert.Contains("IPAddress is required", payload.GetProperty("message").GetString());
        Assert.Contains("DeviceId is required", payload.GetProperty("message").GetString());
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
            TransactionType.CARD,
            "203.0.113.10",
            "DEVICE-001",
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
    public async Task PostTransaction_WhenRecentActivityIsFromDifferentCountry_ReturnsReview()
    {
        using HttpClient client = CreateAuthenticatedClient();
        string accountId = $"ACC-{Guid.NewGuid():N}";
        var firstTransaction = new TransactionDto(
            accountId,
            99.99m,
            "ZAR",
            "Contoso",
            "RETAIL",
            TransactionType.CARD,
            "203.0.113.10",
            "DEVICE-001",
            365,
            DateTimeOffset.UtcNow.AddMinutes(-1));
        var secondTransaction = new TransactionDto(
            accountId,
            129.99m,
            "ZAR",
            "Contoso",
            "RETAIL",
            TransactionType.CARD,
            "198.51.100.10",
            "DEVICE-002",
            365,
            DateTimeOffset.UtcNow);

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/v1/transactions", firstTransaction);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/v1/transactions", secondTransaction);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        FraudEvaluationResultDto? secondEvaluation =
            await secondResponse.Content.ReadFromJsonAsync<FraudEvaluationResultDto>();

        Assert.NotNull(secondEvaluation);
        Assert.Equal("REVIEW", secondEvaluation!.Decision);
        Assert.Contains("RECENT_LOCATION_CHANGE_RULE", secondEvaluation.TriggeredRules);
    }

    [Fact]
    public async Task PostTransaction_WhenThreeRecentBlockedAttemptsExist_ReturnsBlock()
    {
        using HttpClient client = CreateAuthenticatedClient();
        string accountId = $"ACC-{Guid.NewGuid():N}";

        for (int attempt = 0; attempt < 3; attempt++)
        {
            var blockedAttempt = new TransactionDto(
                accountId,
                12000m + attempt,
                "USD",
                $"Contoso-{attempt}",
                "RETAIL",
                TransactionType.CARD,
                "203.0.113.10",
                $"DEVICE-BLOCK-{attempt}",
                1,
                DateTimeOffset.UtcNow.AddMinutes(-(attempt + 1)));

            HttpResponseMessage blockedResponse =
                await client.PostAsJsonAsync("/api/v1/transactions", blockedAttempt);
            FraudEvaluationResultDto? blockedEvaluation =
                await blockedResponse.Content.ReadFromJsonAsync<FraudEvaluationResultDto>();

            Assert.Equal(HttpStatusCode.OK, blockedResponse.StatusCode);
            Assert.NotNull(blockedEvaluation);
            Assert.Equal("BLOCK", blockedEvaluation!.Decision);
        }

        var followUpTransaction = new TransactionDto(
            accountId,
            150m,
            "ZAR",
            "Contoso",
            "RETAIL",
            TransactionType.CARD,
            "203.0.113.10",
            "DEVICE-FOLLOWUP",
            365,
            DateTimeOffset.UtcNow);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/transactions", followUpTransaction);
        FraudEvaluationResultDto? evaluation = await response.Content.ReadFromJsonAsync<FraudEvaluationResultDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(evaluation);
        Assert.Equal("BLOCK", evaluation!.Decision);
        Assert.Contains("REPEATED_DECLINED_TRANSACTION_RULE", evaluation.TriggeredRules);
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
