using Microsoft.FeatureManagement;
using Moneybox.Payments.FamilyPayments.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddFeatureManagement();
builder.Services.AddFamilyPayments(
    isaServiceBaseUrl: builder.Configuration.GetValue<string>("IsaService:BaseUrl") ?? "https://localhost:5001");

var app = builder.Build();

app.MapControllers();
app.Run();
