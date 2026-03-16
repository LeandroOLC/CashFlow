using Blazored.LocalStorage;
using CashFlow.WebApp;
using CashFlow.WebApp.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var cfg = builder.Configuration;

// Cada serviço usa seu próprio HttpClient apontando para a API correta
var authBase = cfg["ApiEndpoints:Auth"] ?? "http://localhost:5001";
var transactionsBase = cfg["ApiEndpoints:Transactions"] ?? "http://localhost:5002";
var consolidationBase = cfg["ApiEndpoints:Consolidation"] ?? "http://localhost:5003";

builder.Services.AddHttpClient("auth", c => c.BaseAddress = new Uri(authBase));
builder.Services.AddHttpClient("transactions", c => c.BaseAddress = new Uri(transactionsBase));
builder.Services.AddHttpClient("consolidation", c => c.BaseAddress = new Uri(consolidationBase));

//builder.Services.AddMudServices();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.ShowTransitionDuration = 200;
    config.SnackbarConfiguration.HideTransitionDuration = 200;
    config.SnackbarConfiguration.VisibleStateDuration = 3000;
    config.SnackbarConfiguration.MaxDisplayedSnackbars = 3;
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IConsolidationService, ConsolidationService>();
//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
