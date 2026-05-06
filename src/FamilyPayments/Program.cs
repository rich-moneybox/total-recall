using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Interfaces;
using Moneybox.Payments.FamilyPayments.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// CORS — allow the frontend to call the API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// LaunchDarkly — staged rollout: 5% initially, 25% after 24h observation, then 100%
// Rollback runbook must be ready to disable within 15 minutes
var ldClient = new LdClient(
    builder.Configuration.GetValue<string>("LaunchDarkly:SdkKey") ?? "sdk-key-placeholder");
builder.Services.AddSingleton<ILdClient>(ldClient);

builder.Services.AddFamilyPayments(
    isaServiceBaseUrl: builder.Configuration.GetValue<string>("IsaService:BaseUrl") ?? "https://localhost:5001");

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.Run();
