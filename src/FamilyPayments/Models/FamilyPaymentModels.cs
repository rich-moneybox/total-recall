namespace Moneybox.Payments.FamilyPayments.Models;

public record FamilyPaymentRequest
{
    public Guid IsaAccountId { get; init; }
    public string FamilyMemberName { get; init; } = string.Empty;
    public string FamilyMemberEmail { get; init; } = string.Empty;
    public string FamilyMemberSortCode { get; init; } = string.Empty;
    public string FamilyMemberAccountNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Reference { get; init; } = string.Empty;
}

public record FamilyPaymentResponse
{
    public Guid PaymentId { get; init; }
    public Guid IsaAccountId { get; init; }
    public decimal Amount { get; init; }
    public FamilyPaymentStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? PaymentLinkUrl { get; init; }
}

public enum FamilyPaymentStatus
{
    Pending,
    AmlCheckInProgress,
    AllowanceValidated,
    Accepted,
    Rejected,
    AllowanceConflict,
    Failed
}

public record IsaAllowanceDetails
{
    public Guid AccountId { get; init; }
    public decimal AnnualLimit { get; init; }
    public decimal UsedAllowance { get; init; }
    public decimal RemainingAllowance => AnnualLimit - UsedAllowance;
    public int TaxYear { get; init; }
    /// <summary>Version/ETag for optimistic locking on conditional writes.</summary>
    public string? ETag { get; init; }
}
