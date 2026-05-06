namespace Moneybox.Payments.FamilyPayments.Services;

using Microsoft.Extensions.Logging;
using Moneybox.Payments.FamilyPayments.Interfaces;
using Moneybox.Payments.FamilyPayments.Models;

/// <summary>
/// Family payment service — lives within the existing payments service but behind
/// a clean interface boundary so it can be extracted to a standalone microservice later.
/// 
/// Architecture decision: extend existing payments service for tax-year deadline,
/// with phase-two extraction planned. See ADR-047 for trade-off documentation.
/// 
/// Concurrency: uses optimistic locking on the ISA allowance. We check allowance on
/// initiation, then do a conditional write at confirmation. If the balance changed
/// between those two points (concurrent payment), we reject and ask the family member
/// to re-initiate. See May 6 build planning discussion.
/// </summary>
public class FamilyPaymentService : IFamilyPaymentService
{
    private readonly IIsaAllowanceService _isaAllowanceService;
    private readonly IAmlCheckService _amlCheckService;
    private readonly IPaymentLinkService _paymentLinkService;
    private readonly ILogger<FamilyPaymentService> _logger;

    public FamilyPaymentService(
        IIsaAllowanceService isaAllowanceService,
        IAmlCheckService amlCheckService,
        IPaymentLinkService paymentLinkService,
        ILogger<FamilyPaymentService> logger)
    {
        _isaAllowanceService = isaAllowanceService;
        _amlCheckService = amlCheckService;
        _paymentLinkService = paymentLinkService;
        _logger = logger;
    }

    public async Task<FamilyPaymentResponse> InitiatePaymentAsync(
        FamilyPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var paymentId = Guid.NewGuid();

        _logger.LogInformation(
            "Initiating family payment {PaymentId} for ISA account {AccountId}, amount {Amount}",
            paymentId, request.IsaAccountId, request.Amount);

        // Step 1: AML check on the family member
        var amlResult = await _amlCheckService.RunCheckAsync(
            request.FamilyMemberName,
            request.FamilyMemberSortCode,
            request.FamilyMemberAccountNumber,
            cancellationToken);

        if (!amlResult.Passed)
        {
            _logger.LogWarning(
                "AML check failed for family payment {PaymentId}: {Reason}",
                paymentId, amlResult.RejectionReason);

            return CreateResponse(paymentId, request, FamilyPaymentStatus.Rejected);
        }

        // Step 2: Check ISA allowance (read — no deduction yet)
        // This is the "check" phase of optimistic locking
        var allowance = await _isaAllowanceService.GetAllowanceAsync(
            request.IsaAccountId, cancellationToken);

        if (request.Amount > allowance.RemainingAllowance)
        {
            _logger.LogWarning(
                "Family payment {PaymentId} exceeds ISA allowance. Requested: {Amount}, Remaining: {Remaining}",
                paymentId, request.Amount, allowance.RemainingAllowance);

            return CreateResponse(paymentId, request, FamilyPaymentStatus.Rejected);
        }

        // Step 3: Conditional write — deduct allowance only if balance hasn't changed
        // This prevents race conditions where concurrent payments both pass validation
        var deduction = await _isaAllowanceService.ConfirmDeductionAsync(
            request.IsaAccountId,
            request.Amount,
            allowance.UsedAllowance,
            cancellationToken);

        if (!deduction.Succeeded)
        {
            _logger.LogWarning(
                "Family payment {PaymentId} failed optimistic lock — allowance was modified concurrently. {Reason}",
                paymentId, deduction.ConflictReason);

            return CreateResponse(paymentId, request, FamilyPaymentStatus.AllowanceConflict);
        }

        _logger.LogInformation(
            "Family payment {PaymentId} confirmed. Allowance remaining after payment: {Remaining}",
            paymentId, allowance.RemainingAllowance - request.Amount);

        // Step 4: Generate payment link for the family member journey
        var link = await _paymentLinkService.GenerateLinkAsync(paymentId, request, cancellationToken);

        return CreateResponse(paymentId, request, FamilyPaymentStatus.Accepted, link.Url);
    }

    public Task<FamilyPaymentResponse?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        // Stub — would query the data store
        return Task.FromResult<FamilyPaymentResponse?>(null);
    }

    private static FamilyPaymentResponse CreateResponse(
        Guid paymentId, FamilyPaymentRequest request, FamilyPaymentStatus status, string? paymentLinkUrl = null)
    {
        return new FamilyPaymentResponse
        {
            PaymentId = paymentId,
            IsaAccountId = request.IsaAccountId,
            Amount = request.Amount,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            PaymentLinkUrl = paymentLinkUrl
        };
    }
}
