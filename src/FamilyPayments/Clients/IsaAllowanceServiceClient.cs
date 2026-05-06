namespace Moneybox.Payments.FamilyPayments.Clients;

using System.Net;
using System.Net.Http.Json;
using Moneybox.Payments.FamilyPayments.Interfaces;
using Moneybox.Payments.FamilyPayments.Models;

/// <summary>
/// HTTP client to the core ISA service for allowance validation and conditional deduction.
/// Uses optimistic locking: check allowance, then confirm deduction with a conditional write.
/// If the balance has changed between those two points, the write fails and the payment
/// is rejected — the family member must re-initiate.
/// </summary>
public class IsaAllowanceServiceClient : IIsaAllowanceService
{
    private readonly HttpClient _httpClient;

    public IsaAllowanceServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IsaAllowanceDetails> GetAllowanceAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/isa/accounts/{accountId}/allowance", cancellationToken);
        response.EnsureSuccessStatusCode();

        var allowance = await response.Content.ReadFromJsonAsync<IsaAllowanceDetails>(cancellationToken: cancellationToken);

        return allowance ?? throw new InvalidOperationException($"No allowance data returned for account {accountId}");
    }

    public async Task<AllowanceDeductionResult> ConfirmDeductionAsync(
        Guid accountId,
        decimal amount,
        decimal expectedUsedAllowance,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            Amount = amount,
            ExpectedUsedAllowance = expectedUsedAllowance
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/api/isa/accounts/{accountId}/allowance/deduct", payload, cancellationToken);

        // 409 Conflict = optimistic lock failure — another payment changed the balance
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return new AllowanceDeductionResult
            {
                Succeeded = false,
                ConflictReason = "Allowance was modified by a concurrent payment. Please re-initiate."
            };
        }

        response.EnsureSuccessStatusCode();
        return new AllowanceDeductionResult { Succeeded = true };
    }
}
