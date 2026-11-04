using LinkedInTutorAI.Application.Abstractions;
using LinkedInTutorAI.Application.Options;
using LinkedInTutorAI.Application.Orchestrators;
using LinkedInTutorAI.Domain.Abstractions;
using LinkedInTutorAI.Infrastructure.AiFoundry;
using LinkedInTutorAI.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LinkedInTutorAI.Infrastructure.Configuration;

/// <summary>
/// Extensiones de DI para registrar la capa de Infrastructure.
/// <c>AddAiFoundry</c> enlaza <see cref="FoundryOptions"/> desde la sección
/// <c>AiFoundry</c> con validación al inicio (fail-fast) y registra el cliente del
/// agente, la fábrica de credenciales, el repositorio y el orquestador.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra los servicios de Infrastructure y valida <see cref="FoundryOptions"/> al arrancar.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración (sección <c>AiFoundry</c>).</param>
    /// <returns>La colección de servicios, para encadenable.</returns>
    public static IServiceCollection AddAiFoundry(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FoundryOptions>()
            .Bind(configuration.GetSection(FoundryOptions.SectionName))
            .Validate(o => o.Validate().IsValid, "Las opciones de Azure AI Foundry son inválidas.")
            .ValidateOnStart();

        services.AddSingleton<AiFoundryCredentialFactory>();
        services.AddSingleton<IAiFoundryAgentService, AgentsAiFoundryAgentService>();
        services.AddSingleton<ITutorSessionRepository, InMemoryTutorSessionRepository>();
        services.AddSingleton<IChatOrchestrator, ChatOrchestrator>();

        return services;
    }
}