namespace Moneybox.Payments.FamilyPayments.Clients;

using Moneybox.Payments.FamilyPayments.Interfaces;
using Moneybox.Payments.FamilyPayments.Models;

/// <summary>
/// Synchronous HTTP client to the core ISA service for allowance validation.
/// As discussed in the architecture review: eventual consistency on a regulatory
/// limit is not acceptable — this must be a synchronous call.
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
}
