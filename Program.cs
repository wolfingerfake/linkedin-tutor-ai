using linkedin_tutor_ai;
using linkedin_tutor_ai.Data;
using LinkedInTutorAI.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// LinkedIn Tutor AI: Azure AI Foundry (agente) + orquestador + sesiones.
builder.Services.AddAiFoundry(builder.Configuration);

builder.Services.AddSingleton<WeatherForecastService>();

var app = builder.Build();

// Sub-aplicación IIS (p. ej. https://host/foundry/): ajusta la path base para que
// rutas, estáticos y el endpoint de Blazor resuelvan bajo el prefijo.
// Configúralo con "Hosting:PathBase" (appsettings) o la env var
// Hosting__PathBase. Vacío/en blanco = la app cuelga de la raíz (sin config).
var pathBase = builder.Configuration["Hosting:PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
{
    app.UsePathBase(pathBase);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
