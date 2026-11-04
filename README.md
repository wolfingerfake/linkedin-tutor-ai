# linkedin-tutor-ai

Tutor de IA para LinkedIn (Blazor Server)

Aplicación web construida con **Blazor Server** sobre **.NET 10**.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Un editor como Visual Studio, VS Code o JetBrains Rider

## Estructura

```
linkedin-tutor-ai/
├── Data/           # Servicios y modelos de datos
├── Pages/          # Páginas Razor y layout host
├── Shared/         # Componentes compartidos (layout, nav, etc.)
├── wwwroot/        # Archivos estáticos (css, favicon)
├── App.razor       # Componente raíz
├── Program.cs      # Punto de entrada y configuración de servicios
└── linkedin-tutor-ai.csproj
```

## Ejecutar en local

```bash
dotnet restore
dotnet run --project linkedin-tutor-ai.csproj
```

Luego abre `https://localhost:5001` (o el puerto indicado en consola).

## Configuración

Los ajustes de la aplicación viven en `appsettings.json`. Usa
`appsettings.Development.json` para sobrescribir valores locales
(este último está ignorado por git).

### Azure AI Foundry (`AiFoundry`)

La sección `AiFoundry` se bindea a `FoundryOptions` (tipada y validada con
`IOptions<FoundryOptions>` + `ValidateOnStart`: fail-fast al arrancar si
`Endpoint`, `AgentId` o `ModelDeployment` faltan). El registro se hace en
`Program.cs` con una sola línea:

```csharp
builder.Services.AddAiFoundry(builder.Configuration);
```

`AddAiFoundry` (en `LinkedInTutorAI.Infrastructure`) enlaza la sección,
registra `IAiFoundryAgentService`, `ITutorSessionRepository` e
`IChatOrchestrator`, y valida las opciones al inicio.

**Ningún archivo contiene secretos reales: solo placeholders.**
Sustituye los valores por los de tu proyecto de Foundry.

| Clave | ¿Requerido? | Descripción |
|---|---|---|
| `Endpoint` | sí | URL del proyecto de Foundry |
| `AgentId` | sí | ID del agente existente en Foundry |
| `ModelDeployment` | sí | Nombre del despliegue del modelo |
| `AuthMode` | no | `DefaultCredential` (Entra) o `ApiKey` |
| `ApiKey` | solo si `AuthMode=ApiKey` | Clave de API — **secreto** |
| `MaxRetries` | no | Reintentos ante 429/5xx (default 3) |
| `TimeoutSeconds` | no | Timeout por llamada (default 60) |

### Por entorno

- **Development** (`appsettings.Development.json`, ignorado por git):
  `AuthMode=DefaultCredential` usa tu login de Azure CLI (`az login`), sin
  secreto. Si prefieres `ApiKey`, guárdala con User Secrets y cambia el modo:
  ```bash
  dotnet user-secrets set "AiFoundry:ApiKey" "<tu-clave>"
  ```
  y pon `"AuthMode": "ApiKey"` en `appsettings.Development.json`.
- **Production** (`appsettings.Production.json`, commiteado con placeholders):
  sobrescribe con **variables de entorno** (o Azure Key Vault). El `__`
  separa la sección anidada en env vars (p. ej. `AiFoundry:Endpoint` →
  `AiFoundry__Endpoint`):

  | Variable de entorno | Clave de config |
  |---|---|
  | `AiFoundry__Endpoint` | `AiFoundry:Endpoint` |
  | `AiFoundry__AgentId` | `AiFoundry:AgentId` |
  | `AiFoundry__ModelDeployment` | `AiFoundry:ModelDeployment` |
  | `AiFoundry__AuthMode` | `AiFoundry:AuthMode` (`DefaultCredential` \| `ApiKey`) |
  | `AiFoundry__ApiKey` | `AiFoundry:ApiKey` — **secreto**: env var o Key Vault, nunca en git |
  | `AiFoundry__MaxRetries` | `AiFoundry:MaxRetries` |
  | `AiFoundry__TimeoutSeconds` | `AiFoundry:TimeoutSeconds` |

  Ejemplo (Bash):
  ```bash
  export AiFoundry__Endpoint="https://MIFOUNDRY.services.ai.azure.com/api/projects/MIPROYECTO"
  export AiFoundry__AgentId="asst_MIAGENTE"
  export AiFoundry__ModelDeployment="gpt-4o-mini"
  export AiFoundry__AuthMode="DefaultCredential"
  ```

### Validación

`FoundryOptions.Validate()` ejecuta las `DataAnnotations` y una regla condicional:
`ApiKey` es obligatoria cuando `AuthMode=ApiKey`. Con `ValidateOnStart`,
una config incompleta lanza `OptionsValidationException` al arrancar (o al
primer uso del agente), no en silencio.