using Blazored.LocalStorage;
using CashFlow.WebApp;
using CashFlow.WebApp.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var authBase          = builder.Configuration["ApiEndpoints:Auth"]          ?? "http://localhost:5001";
var transactionsBase  = builder.Configuration["ApiEndpoints:Transactions"]  ?? "http://localhost:5002";
var consolidationBase = builder.Configuration["ApiEndpoints:Consolidation"] ?? "http://localhost:5003";

builder.Services.AddHttpClient<IAuthService, AuthService>(
    c => c.BaseAddress = new Uri(authBase));

builder.Services.AddHttpClient<ITransactionService, TransactionService>(
    c => c.BaseAddress = new Uri(transactionsBase));

builder.Services.AddHttpClient<IConsolidationService, ConsolidationService>(
    c => c.BaseAddress = new Uri(consolidationBase));

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass          = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.ShowTransitionDuration = 200;
    config.SnackbarConfiguration.HideTransitionDuration = 200;
    config.SnackbarConfiguration.VisibleStateDuration   = 3000;
    config.SnackbarConfiguration.MaxDisplayedSnackbars  = 3;
});

await builder.Build().RunAsync();
