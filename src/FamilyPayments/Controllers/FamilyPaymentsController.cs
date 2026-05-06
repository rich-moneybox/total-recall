namespace Moneybox.Payments.FamilyPayments.Controllers;

using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moneybox.Payments.FamilyPayments.Interfaces;
using Moneybox.Payments.FamilyPayments.Models;

[ApiController]
[Route("api/payments/family")]
public class FamilyPaymentsController : ControllerBase
{
    private readonly IFamilyPaymentService _familyPaymentService;
    private readonly ILdClient _ldClient;

    public FamilyPaymentsController(
        IFamilyPaymentService familyPaymentService,
        ILdClient ldClient)
    {
        _familyPaymentService = familyPaymentService;
        _ldClient = ldClient;
    }

    [HttpPost]
    public async Task<IActionResult> InitiatePayment(
        [FromBody] FamilyPaymentRequest request,
        CancellationToken cancellationToken)
    {
        // LaunchDarkly feature flag — staged rollout: 5% → 25% → 100%
        var context = Context.Builder(request.IsaAccountId.ToString())
            .Kind("user")
            .Build();

        if (!_ldClient.BoolVariation(FeatureFlags.FamilyPayments, context, defaultValue: true))
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
            FamilyPaymentStatus.AllowanceConflict => Conflict(result),
            FamilyPaymentStatus.Rejected => UnprocessableEntity(result),
            _ => StatusCode(500, result)
        };
    }

    [HttpGet("{paymentId:guid}")]
    public async Task<IActionResult> GetPayment(Guid paymentId, CancellationToken cancellationToken)
    {
        // Use anonymous context for reads — flag still gates the endpoint
        var context = Context.Builder("anonymous").Kind("user").Build();

        if (!_ldClient.BoolVariation(FeatureFlags.FamilyPayments, context, defaultValue: true))
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
