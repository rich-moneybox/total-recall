namespace Moneybox.Payments.FamilyPayments.Tests;

using Moneybox.Payments.FamilyPayments.Interfaces;
using Moneybox.Payments.FamilyPayments.Models;
using Moneybox.Payments.FamilyPayments.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class FamilyPaymentServiceTests
{
    private readonly Mock<IIsaAllowanceService> _isaAllowanceService = new();
    private readonly Mock<IAmlCheckService> _amlCheckService = new();
    private readonly Mock<IPaymentLinkService> _paymentLinkService = new();
    private readonly Mock<ILogger<FamilyPaymentService>> _logger = new();
    private readonly FamilyPaymentService _sut;

    public FamilyPaymentServiceTests()
    {
        // Default: deduction succeeds and link is generated
        _isaAllowanceService.Setup(x => x.ConfirmDeductionAsync(
                It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AllowanceDeductionResult { Succeeded = true });

        _paymentLinkService.Setup(x => x.GenerateLinkAsync(
                It.IsAny<Guid>(), It.IsAny<FamilyPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentLink
            {
                PaymentId = Guid.NewGuid(),
                Token = "test-token",
                Url = "https://pay.moneybox.app/family/test-token",
                ExpiresAt = DateTime.UtcNow.AddHours(72)
            });

        _sut = new FamilyPaymentService(
            _isaAllowanceService.Object,
            _amlCheckService.Object,
            _paymentLinkService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task InitiatePayment_WithinAllowance_ReturnsAccepted()
    {
        // Arrange
        var request = CreateRequest(amount: 1000m);

        _amlCheckService.Setup(x => x.RunCheckAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AmlCheckResult { Passed = true });

        _isaAllowanceService.Setup(x => x.GetAllowanceAsync(request.IsaAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IsaAllowanceDetails
            {
                AccountId = request.IsaAccountId,
                AnnualLimit = 20000m,
                UsedAllowance = 5000m,
                TaxYear = 2026
            });

        // Act
        var result = await _sut.InitiatePaymentAsync(request);

        // Assert
        Assert.Equal(FamilyPaymentStatus.Accepted, result.Status);
        Assert.Equal(1000m, result.Amount);
        Assert.NotNull(result.PaymentLinkUrl);
    }

    [Fact]
    public async Task InitiatePayment_ExceedsAllowance_ReturnsRejected()
    {
        // Arrange
        var request = CreateRequest(amount: 18000m);

        _amlCheckService.Setup(x => x.RunCheckAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AmlCheckResult { Passed = true });

        _isaAllowanceService.Setup(x => x.GetAllowanceAsync(request.IsaAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IsaAllowanceDetails
            {
                AccountId = request.IsaAccountId,
                AnnualLimit = 20000m,
                UsedAllowance = 5000m, // Only 15000 remaining
                TaxYear = 2026
            });

        // Act
        var result = await _sut.InitiatePaymentAsync(request);

        // Assert
        Assert.Equal(FamilyPaymentStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task InitiatePayment_AmlCheckFails_ReturnsRejected()
    {
        // Arrange
        var request = CreateRequest(amount: 500m);

        _amlCheckService.Setup(x => x.RunCheckAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AmlCheckResult { Passed = false, RejectionReason = "Identity verification failed" });

        // Act
        var result = await _sut.InitiatePaymentAsync(request);

        // Assert
        Assert.Equal(FamilyPaymentStatus.Rejected, result.Status);

        // Should NOT call ISA service if AML fails
        _isaAllowanceService.Verify(
            x => x.GetAllowanceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InitiatePayment_ExactRemainingAllowance_ReturnsAccepted()
    {
        // Arrange — boundary condition: exact remaining allowance
        var request = CreateRequest(amount: 15000m);

        _amlCheckService.Setup(x => x.RunCheckAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AmlCheckResult { Passed = true });

        _isaAllowanceService.Setup(x => x.GetAllowanceAsync(request.IsaAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IsaAllowanceDetails
            {
                AccountId = request.IsaAccountId,
                AnnualLimit = 20000m,
                UsedAllowance = 5000m,
                TaxYear = 2026
            });

        // Act
        var result = await _sut.InitiatePaymentAsync(request);

        // Assert
        Assert.Equal(FamilyPaymentStatus.Accepted, result.Status);
    }

    private static FamilyPaymentRequest CreateRequest(decimal amount) => new()
    {
        IsaAccountId = Guid.NewGuid(),
        FamilyMemberName = "Jane Doe",
        FamilyMemberEmail = "jane.doe@example.com",
        FamilyMemberSortCode = "12-34-56",
        FamilyMemberAccountNumber = "12345678",
        Amount = amount,
        Reference = "Birthday gift"
    };

    [Fact]
    public async Task InitiatePayment_ConcurrentPaymentConflict_ReturnsAllowanceConflict()
    {
        // Arrange — allowance check passes, but conditional write fails (another payment landed first)
        var request = CreateRequest(amount: 5000m);

        _amlCheckService.Setup(x => x.RunCheckAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AmlCheckResult { Passed = true });

        _isaAllowanceService.Setup(x => x.GetAllowanceAsync(request.IsaAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IsaAllowanceDetails
            {
                AccountId = request.IsaAccountId,
                AnnualLimit = 20000m,
                UsedAllowance = 5000m,
                TaxYear = 2026
            });

        // Simulate optimistic lock failure — balance changed between check and confirm
        _isaAllowanceService.Setup(x => x.ConfirmDeductionAsync(
                request.IsaAccountId, request.Amount, 5000m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AllowanceDeductionResult
            {
                Succeeded = false,
                ConflictReason = "Allowance was modified by a concurrent payment."
            });

        // Act
        var result = await _sut.InitiatePaymentAsync(request);

        // Assert
        Assert.Equal(FamilyPaymentStatus.AllowanceConflict, result.Status);

        // Should NOT generate a payment link on conflict
        _paymentLinkService.Verify(
            x => x.GenerateLinkAsync(It.IsAny<Guid>(), It.IsAny<FamilyPaymentRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
