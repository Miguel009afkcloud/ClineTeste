using BancaRevistas.Web.Components;
using Microsoft.JSInterop;
using BancaRevistas.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Componentes Blazor (renderização no servidor + interatividade)
// ---------------------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ---------------------------------------------------------------------
// Cliente da API da banca
// ---------------------------------------------------------------------
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5001";

builder.Services.AddHttpClient<BancaApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// O AuthService compartilha o mesmo HttpClient para realizar o login.
builder.Services.AddScoped<AuthService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return new AuthService(factory.CreateClient("BancaApi"), sp.GetRequiredService<IJSRuntime>());
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
