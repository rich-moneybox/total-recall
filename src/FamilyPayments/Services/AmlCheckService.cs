namespace Moneybox.Payments.FamilyPayments.Services;

using Moneybox.Payments.FamilyPayments.Interfaces;

/// <summary>
/// Stub AML check service — to be replaced with the shared library implementation.
/// In production this would call the existing AML/KYC provider.
/// </summary>
public class AmlCheckService : IAmlCheckService
{
    public Task<AmlCheckResult> RunCheckAsync(
        string name,
        string sortCode,
        string accountNumber,
        CancellationToken cancellationToken = default)
    {
        // Stub: always passes. Real implementation would call the AML provider.
        return Task.FromResult(new AmlCheckResult { Passed = true });
    }
}
