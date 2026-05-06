namespace Moneybox.Payments.FamilyPayments.Interfaces;

using Moneybox.Payments.FamilyPayments.Models;

/// <summary>
/// Clean interface boundary to the core ISA service.
/// Synchronous call — we cannot allow over-subscription on a regulatory limit.
/// </summary>
public interface IIsaAllowanceService
{
    Task<IsaAllowanceDetails> GetAllowanceAsync(Guid accountId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface boundary for AML checks — extracted so it can be moved to a shared library later.
/// </summary>
public interface IAmlCheckService
{
    Task<AmlCheckResult> RunCheckAsync(string name, string sortCode, string accountNumber, CancellationToken cancellationToken = default);
}

public record AmlCheckResult
{
    public bool Passed { get; init; }
    public string? RejectionReason { get; init; }
}

/// <summary>
/// Interface for the family payments feature — kept behind a clean boundary
/// so it can be physically separated into its own microservice in phase two.
/// </summary>
public interface IFamilyPaymentService
{
    Task<FamilyPaymentResponse> InitiatePaymentAsync(FamilyPaymentRequest request, CancellationToken cancellationToken = default);
    Task<FamilyPaymentResponse?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
}
