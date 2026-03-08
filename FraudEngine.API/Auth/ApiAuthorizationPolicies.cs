namespace FraudEngine.API.Auth;

/// <summary>
/// Authorization policy names for partner and internal API capabilities.
/// </summary>
public static class ApiAuthorizationPolicies
{
    public const string SubmitTransactions = "transactions.submit";
    public const string ReadTransactions = "transactions.read";
    public const string ReadEvaluations = "evaluations.read";
    public const string ReadRules = "rules.read";
    public const string ManageRules = "rules.write";
}
