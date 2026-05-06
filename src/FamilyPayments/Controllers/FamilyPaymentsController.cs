namespace Moneybox.Payments.FamilyPayments.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Moneybox.Payments.FamilyPayments.Interfaces;
using Moneybox.Payments.FamilyPayments.Models;

[ApiController]
[Route("api/payments/family")]
public class FamilyPaymentsController : ControllerBase
{
    private readonly IFamilyPaymentService _familyPaymentService;
    private readonly IFeatureManager _featureManager;

    public FamilyPaymentsController(
        IFamilyPaymentService familyPaymentService,
        IFeatureManager featureManager)
    {
        _familyPaymentService = familyPaymentService;
        _featureManager = featureManager;
    }

    [HttpPost]
    public async Task<IActionResult> InitiatePayment(
        [FromBody] FamilyPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _featureManager.IsEnabledAsync(FeatureFlags.FamilyPayments))
        {
            return NotFound();
        }

        if (request.Amount <= 0)
        {
            return BadRequest("Payment amount must be greater than zero.");
        }

        var result = await _familyPaymentService.InitiatePaymentAsync(request, cancellationToken);

        return result.Status switch
        {
            FamilyPaymentStatus.Accepted => Ok(result),
            FamilyPaymentStatus.Rejected => UnprocessableEntity(result),
            _ => StatusCode(500, result)
        };
    }

    [HttpGet("{paymentId:guid}")]
    public async Task<IActionResult> GetPayment(Guid paymentId, CancellationToken cancellationToken)
    {
        if (!await _featureManager.IsEnabledAsync(FeatureFlags.FamilyPayments))
        {
            return NotFound();
        }

        var payment = await _familyPaymentService.GetPaymentAsync(paymentId, cancellationToken);

        if (payment is null)
        {
            return NotFound();
        }

        return Ok(payment);
    }
}
