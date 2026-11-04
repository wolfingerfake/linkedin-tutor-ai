namespace LinkedInTutorAI.Application.Exceptions;

/// <summary>
/// Clase base para los errores al dialogar con el agente de Azure AI Foundry.
/// <see cref="Infrastructure"/> traduce <c>Azure.RequestFailedException</c> a estas excepciones
/// en la frontera, de modo que <c>Web</c>/la UI nunca recibe tipos del SDK de Azure.
/// </summary>
public class AiFoundryException : Exception
{
    /// <summary>Crea una <see cref="AiFoundryException"/> con un mensaje.</summary>
    /// <param name="message">Descripción del error.</param>
    public AiFoundryException(string message) : base(message) { }

    /// <summary>Crea una <see cref="AiFoundryException"/> con mensaje y causa interna.</summary>
    /// <param name="message">Descripción del error.</param>
    /// <param name="inner">Excepción causal (p. ej. <c>Azure.RequestFailedException</c>).</param>
    public AiFoundryException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Error de autenticación/autorización (401/403) contra Azure AI Foundry.</summary>
public sealed class AiFoundryAuthException : AiFoundryException
{
    public AiFoundryAuthException(string message) : base(message) { }
    public AiFoundryAuthException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Timeout agotado (408/504) en la llamada al agente.</summary>
public sealed class AiFoundryTimeoutException : AiFoundryException
{
    public AiFoundryTimeoutException(string message) : base(message) { }
    public AiFoundryTimeoutException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Límite de tasa excedido (429). Reintentable con backoff.</summary>
public sealed class AiFoundryThrottledException : AiFoundryException
{
    public AiFoundryThrottledException(string message) : base(message) { }
    public AiFoundryThrottledException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>El servicio no está disponible (5xx). Reintentable.</summary>
public sealed class AiFoundryUnavailableException : AiFoundryException
{
    public AiFoundryUnavailableException(string message) : base(message) { }
    public AiFoundryUnavailableException(string message, Exception inner) : base(message, inner) { }
}