using System.Security.Claims;
using FraudEngine.API.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace FraudEngine.UnitTests;

public class ApiKeyAuthenticationTests
{
    [Fact]
    public void Verify_ReturnsTrue_ForMatchingApiKey()
    {
        const string apiKey = "super-secret-partner-key";
        string hash = ApiKeyHasher.ComputeHash(apiKey);

        bool isValid = ApiKeyHasher.Verify(apiKey, hash);

        Assert.True(isValid);
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongApiKey()
    {
        string hash = ApiKeyHasher.ComputeHash("expected-key");

        bool isValid = ApiKeyHasher.Verify("wrong-key", hash);

        Assert.False(isValid);
    }

    [Fact]
    public void GetClientIdentifier_PrefersAuthenticatedClientId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[ApiKeyAuthenticationDefaults.ClientIdHeaderName] = "header-client";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ApiKeyAuthenticationDefaults.ClientIdClaimType, "authenticated-client") },
            ApiKeyAuthenticationDefaults.AuthenticationScheme));

        string clientIdentifier = Program.GetClientIdentifier(context);

        Assert.Equal("client:authenticated-client", clientIdentifier);
    }

    [Fact]
    public void GetClientIdentifier_FallsBackToClientIdHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[ApiKeyAuthenticationDefaults.ClientIdHeaderName] = "partner-sandbox";

        string clientIdentifier = Program.GetClientIdentifier(context);

        Assert.Equal("client:partner-sandbox", clientIdentifier);
    }

    [Fact]
    public void GetChallengeMessage_UsesAuthenticationFailureMessage()
    {
        AuthenticateResult result = AuthenticateResult.Fail(
            $"Invalid client ID or API key. Send the raw API key secret in '{ApiKeyAuthenticationDefaults.ApiKeyHeaderName}', not the stored SHA-256 hash.");

        string message = ApiKeyAuthenticationHandler.GetChallengeMessage(result);

        Assert.Contains("not the stored SHA-256 hash", message);
    }

    [Fact]
    public void GetChallengeMessage_UsesMissingHeaderMessage_WhenNoFailureExists()
    {
        string message = ApiKeyAuthenticationHandler.GetChallengeMessage(AuthenticateResult.NoResult());

        Assert.Contains("Missing authentication headers", message);
        Assert.Contains(ApiKeyAuthenticationDefaults.ClientIdHeaderName, message);
        Assert.Contains(ApiKeyAuthenticationDefaults.ApiKeyHeaderName, message);
    }
}
