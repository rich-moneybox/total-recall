namespace Moneybox.Payments.FamilyPayments.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Moneybox.Payments.FamilyPayments.Clients;
using Moneybox.Payments.FamilyPayments.Interfaces;
using Moneybox.Payments.FamilyPayments.Services;

public static class FamilyPaymentsServiceRegistration
{
    /// <summary>
    /// Registers the family payments feature within the existing payments service.
    /// All dependencies are behind interfaces to maintain a clean boundary
    /// for future extraction into a standalone microservice.
    /// </summary>
    public static IServiceCollection AddFamilyPayments(
        this IServiceCollection services,
        string isaServiceBaseUrl)
    {
        services.AddScoped<IFamilyPaymentService, FamilyPaymentService>();

        services.AddHttpClient<IIsaAllowanceService, IsaAllowanceServiceClient>(client =>
        {
            client.BaseAddress = new Uri(isaServiceBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        // AML check service — currently embedded in the payments service.
        // Should be extracted to a shared library per ADR-047.
        // services.AddScoped<IAmlCheckService, AmlCheckService>();

        return services;
    }
}
