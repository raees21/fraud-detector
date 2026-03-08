namespace FraudEngine.API.Auth;

/// <summary>
/// Authorization policy names for partner and internal API capabilities.
/// </summary>
internal static class ApiAuthorizationPolicies
{
    internal const string SubmitTransactions = "transactions.submit";
    internal const string ReadTransactions = "transactions.read";
    internal const string ReadEvaluations = "evaluations.read";
    internal const string ReadRules = "rules.read";
    internal const string ManageRules = "rules.write";
}
