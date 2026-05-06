namespace Moneybox.Payments.FamilyPayments;

public static class FeatureFlags
{
    /// <summary>
    /// Controls access to the family payments feature.
    /// Must be behind a feature flag so it can be rolled out incrementally
    /// and killed quickly if issues arise near the tax-year deadline.
    /// </summary>
    public const string FamilyPayments = "FamilyPayments";
}
