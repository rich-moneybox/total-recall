namespace Moneybox.Payments.FamilyPayments.Services;

using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Moneybox.Payments.FamilyPayments.Interfaces;
using Moneybox.Payments.FamilyPayments.Models;

/// <summary>
/// Generates unique payment links for the family member journey.
/// The link is a public-facing URL with no authentication — pen testing
/// required before launch (two-week lead time with the security team).
/// </summary>
public class PaymentLinkService : IPaymentLinkService
{
    private readonly ILogger<PaymentLinkService> _logger;

    public PaymentLinkService(ILogger<PaymentLinkService> logger)
    {
        _logger = logger;
    }

    public Task<PaymentLink> GenerateLinkAsync(
        Guid paymentId,
        FamilyPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = GenerateSecureToken();

        var link = new PaymentLink
        {
            PaymentId = paymentId,
            Token = token,
            Url = $"https://pay.moneybox.app/family/{token}",
            ExpiresAt = DateTime.UtcNow.AddHours(72)
        };

        _logger.LogInformation(
            "Generated payment link for {PaymentId}, expires {ExpiresAt}",
            paymentId, link.ExpiresAt);

        // In production this would persist the link to a data store
        return Task.FromResult(link);
    }

    public Task<PaymentLink?> ValidateLinkAsync(string token, CancellationToken cancellationToken = default)
    {
        // Stub — would look up and validate the token from the data store
        return Task.FromResult<PaymentLink?>(null);
    }

    private static string GenerateSecureToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
