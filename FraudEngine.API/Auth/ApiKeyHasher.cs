using System.Security.Cryptography;
using System.Text;

namespace FraudEngine.API.Auth;

/// <summary>
/// Computes and verifies API key hashes without storing plaintext partner secrets.
/// </summary>
public static class ApiKeyHasher
{
    public static string ComputeHash(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key can not be empty.", nameof(apiKey));

        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static bool IsValidHash(string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash) || hash.Length != 64)
            return false;

        try
        {
            _ = Convert.FromHexString(hash);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool Verify(string providedApiKey, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(providedApiKey) || !IsValidHash(expectedHash))
            return false;

        byte[] providedHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(providedApiKey));
        byte[] expectedHashBytes = Convert.FromHexString(expectedHash);

        return CryptographicOperations.FixedTimeEquals(providedHashBytes, expectedHashBytes);
    }
}
