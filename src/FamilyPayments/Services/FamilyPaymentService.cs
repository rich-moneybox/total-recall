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
/// </summary>
public class FamilyPaymentService : IFamilyPaymentService
{
    private readonly IIsaAllowanceService _isaAllowanceService;
    private readonly IAmlCheckService _amlCheckService;
    private readonly ILogger<FamilyPaymentService> _logger;

    public FamilyPaymentService(
        IIsaAllowanceService isaAllowanceService,
        IAmlCheckService amlCheckService,
        ILogger<FamilyPaymentService> logger)
    {
        _isaAllowanceService = isaAllowanceService;
        _amlCheckService = amlCheckService;
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

        // Step 2: Synchronous ISA allowance validation
        // Cannot use eventual consistency here — regulatory limit must be enforced strictly
        var allowance = await _isaAllowanceService.GetAllowanceAsync(
            request.IsaAccountId, cancellationToken);

        if (request.Amount > allowance.RemainingAllowance)
        {
            _logger.LogWarning(
                "Family payment {PaymentId} exceeds ISA allowance. Requested: {Amount}, Remaining: {Remaining}",
                paymentId, request.Amount, allowance.RemainingAllowance);

            return CreateResponse(paymentId, request, FamilyPaymentStatus.Rejected);
        }

        _logger.LogInformation(
            "Family payment {PaymentId} validated. Allowance remaining after payment: {Remaining}",
            paymentId, allowance.RemainingAllowance - request.Amount);

        // Step 3: Accept the payment
        // In production this would persist to the database and trigger downstream processing
        return CreateResponse(paymentId, request, FamilyPaymentStatus.Accepted);
    }

    public Task<FamilyPaymentResponse?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        // Stub — would query the data store
        return Task.FromResult<FamilyPaymentResponse?>(null);
    }

    private static FamilyPaymentResponse CreateResponse(
        Guid paymentId, FamilyPaymentRequest request, FamilyPaymentStatus status)
    {
        return new FamilyPaymentResponse
        {
            PaymentId = paymentId,
            IsaAccountId = request.IsaAccountId,
            Amount = request.Amount,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }
}
