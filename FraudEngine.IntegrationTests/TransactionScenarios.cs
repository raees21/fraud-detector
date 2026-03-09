using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FraudEngine.Application.DTOs;
using FraudEngine.Domain.Enums;

namespace FraudEngine.IntegrationTests;

public sealed class TransactionScenarios : IClassFixture<ApiIntegrationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

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
    public async Task PostTransaction_WithRequiredTelemetry_IsAcceptedAndCompletesAsAllow()
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

        Assert.Equal(HttpStatusCode.Accepted, submitResponse.StatusCode);
        TransactionSubmissionAcceptedDto? accepted =
            await submitResponse.Content.ReadFromJsonAsync<TransactionSubmissionAcceptedDto>();
        Assert.NotNull(accepted);
        Assert.Equal(accountId, accepted!.AccountId);
        Assert.Equal("PENDING", accepted.Status);

        TransactionStatusDto completed = await WaitForTerminalStatusAsync(client, accepted.TransactionId);

        Assert.Equal("COMPLETED", completed.Status);
        Assert.Equal("ALLOW", completed.Decision);
        Assert.Contains("TRANSACTION_TYPE_EFT_RULE", completed.TriggeredRules);

        HttpResponseMessage historyResponse = await client.GetAsync(
            $"/api/v1/transactions?accountId={Uri.EscapeDataString(accountId)}");

        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        JsonElement historyPayload = await ReadJsonAsync(historyResponse);
        Assert.Equal(1, historyPayload.GetProperty("page").GetInt32());
        Assert.Equal(20, historyPayload.GetProperty("pageSize").GetInt32());

        JsonElement[] transactions = historyPayload.GetProperty("data").EnumerateArray().ToArray();
        JsonElement matchingTransaction = Assert.Single(transactions);
        Assert.Equal(accepted.TransactionId, matchingTransaction.GetProperty("transactionId").GetGuid());
        Assert.Equal("EFT", matchingTransaction.GetProperty("transactionType").GetString());
        Assert.Equal("COMPLETED", matchingTransaction.GetProperty("status").GetString());
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
        TransactionSubmissionAcceptedDto? accepted =
            await submitResponse.Content.ReadFromJsonAsync<TransactionSubmissionAcceptedDto>();

        Assert.NotNull(accepted);
        TransactionStatusDto completed = await WaitForTerminalStatusAsync(client, accepted!.TransactionId);
        Assert.Equal("ALLOW", completed.Decision);

        HttpResponseMessage response = await client.GetAsync("/api/v1/evaluations?decision=ALLOW");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement payload = await ReadJsonAsync(response);
        bool containsTransaction = payload.GetProperty("data")
            .EnumerateArray()
            .Any(item => item.GetProperty("transactionId").GetGuid() == accepted.TransactionId);

        Assert.True(containsTransaction);
    }

    [Fact]
    public async Task PostTransaction_WhenRecentActivityIsFromDifferentCountry_CompletesAsReview()
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

        Assert.Equal(HttpStatusCode.Accepted, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, secondResponse.StatusCode);

        TransactionSubmissionAcceptedDto? secondAccepted =
            await secondResponse.Content.ReadFromJsonAsync<TransactionSubmissionAcceptedDto>();

        Assert.NotNull(secondAccepted);
        TransactionStatusDto secondEvaluation = await WaitForTerminalStatusAsync(client, secondAccepted!.TransactionId);
        Assert.Equal("REVIEW", secondEvaluation.Decision);
        Assert.Contains("RECENT_LOCATION_CHANGE_RULE", secondEvaluation.TriggeredRules);
    }

    [Fact]
    public async Task PostTransaction_WhenThreeRecentBlockedAttemptsExist_CompletesAsBlock()
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

            Assert.Equal(HttpStatusCode.Accepted, blockedResponse.StatusCode);
            TransactionSubmissionAcceptedDto? blockedAccepted =
                await blockedResponse.Content.ReadFromJsonAsync<TransactionSubmissionAcceptedDto>();
            Assert.NotNull(blockedAccepted);

            TransactionStatusDto blockedEvaluation =
                await WaitForTerminalStatusAsync(client, blockedAccepted!.TransactionId);
            Assert.Equal("BLOCK", blockedEvaluation.Decision);
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
        TransactionSubmissionAcceptedDto? accepted =
            await response.Content.ReadFromJsonAsync<TransactionSubmissionAcceptedDto>();

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(accepted);

        TransactionStatusDto evaluation = await WaitForTerminalStatusAsync(client, accepted!.TransactionId);
        Assert.Equal("BLOCK", evaluation.Decision);
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

    private static async Task<TransactionStatusDto> WaitForTerminalStatusAsync(HttpClient client, Guid transactionId)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            HttpResponseMessage response = await client.GetAsync($"/api/v1/transactions/{transactionId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            TransactionStatusDto? transaction =
                await response.Content.ReadFromJsonAsync<TransactionStatusDto>(JsonOptions);
            Assert.NotNull(transaction);

            if (transaction!.Status is "COMPLETED" or "FAILED")
                return transaction;

            await Task.Delay(50);
        }

        throw new TimeoutException($"Transaction {transactionId} did not reach a terminal status in time.");
    }
}
