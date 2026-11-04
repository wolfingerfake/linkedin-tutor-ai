using Azure;
using LinkedInTutorAI.Application.Exceptions;

namespace LinkedInTutorAI.Infrastructure.AiFoundry;

/// <summary>
/// Traduce excepciones del SDK de Azure (<c>Azure.RequestFailedException</c>) a la jerarquía
/// de excepciones de aplicación (<see cref="AiFoundryException"/> y derivadas) en la frontera,
/// para que la UI nunca reciba tipos del SDK.
/// </summary>
public static class AgentExceptionMapper
{
    /// <summary>Traduce <paramref name="ex"/> a una <see cref="AiFoundryException"/>.</summary>
    public static Exception Map(Exception ex)
    {
        if (ex is RequestFailedException rfe)
        {
            return rfe.Status switch
            {
                401 or 403 => new AiFoundryAuthException(
                    "No se pudo autenticar/autorizar con el agente. Revisa la configuración y los permisos RBAC.", rfe),
                408 or 504 => new AiFoundryTimeoutException("El agente tardó demasiado. Intenta de nuevo.", rfe),
                429 => new AiFoundryThrottledException("Demasiadas solicitudes. Espera unos segundos.", rfe),
                >= 500 => new AiFoundryUnavailableException("El agente no está disponible. Intenta más tarde.", rfe),
                _ => new AiFoundryException($"Error al llamar al agente (HTTP {rfe.Status}): {rfe.Message}", rfe)
            };
        }

        return new AiFoundryException("Error inesperado al llamar al agente.", ex);
    }
}