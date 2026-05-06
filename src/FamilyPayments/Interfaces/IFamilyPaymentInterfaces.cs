namespace Moneybox.Payments.FamilyPayments.Interfaces;

using Moneybox.Payments.FamilyPayments.Models;

/// <summary>
/// Clean interface boundary to the core ISA service.
/// Synchronous call — we cannot allow over-subscription on a regulatory limit.
/// Uses optimistic locking: check allowance on initiation, conditional write at confirmation.
/// If balance changed between check and confirm, reject and ask family member to re-initiate.
/// </summary>
public interface IIsaAllowanceService
{
    Task<IsaAllowanceDetails> GetAllowanceAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Conditional write — deducts from the ISA allowance only if the current used amount
    /// still matches <paramref name="expectedUsedAllowance"/>. Returns false on conflict.
    /// </summary>
    Task<AllowanceDeductionResult> ConfirmDeductionAsync(
        Guid accountId,
        decimal amount,
        decimal expectedUsedAllowance,
        CancellationToken cancellationToken = default);
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

public record AllowanceDeductionResult
{
    public bool Succeeded { get; init; }
    public string? ConflictReason { get; init; }
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

/// <summary>
/// Generates a unique payment link for the family member to complete the payment.
/// The link is a public-facing URL — pen testing required before launch.
/// </summary>
public interface IPaymentLinkService
{
    Task<PaymentLink> GenerateLinkAsync(Guid paymentId, FamilyPaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentLink?> ValidateLinkAsync(string token, CancellationToken cancellationToken = default);
}

public record PaymentLink
{
    public Guid PaymentId { get; init; }
    public string Token { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
