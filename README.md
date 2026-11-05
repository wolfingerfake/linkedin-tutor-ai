# linkedin-tutor-ai

Tutor de IA para crear y mejorar publicaciones de LinkedIn, construido sobre un agente de **Azure AI Foundry** con **ASP.NET Core 10 + Blazor Interactive Server** y **Clean Architecture** en 4 capas.

El portal te describe una idea y el agente redacta un post listo para publicar —con título, cuerpo, hashtags y recomendaciones— mostrándose en pantalla **token a token** según se genera.

> Proyecto académico. El contenido generado es una sugerencia: revísalo antes de publicarlo.

---

## Características

- **Generador de publicaciones** guiado por un agente de Azure AI Foundry (Responses API).
- **Streaming token-a-token en vivo**: el post se va redactando en pantalla a medida que el agente genera.
- **Formulario completo**: Tema (obligatorio), Rol, Audiencia, Tipo de publicación, Tono (Profesional / Cercano / Inspirador / Directo / Narrativo), Longitud (50–3000 caracteres), Incluir hashtags, Incluir pregunta final.
- **Ejecutar y Cancelar**: envía con el botón **Ejecutar** o pulsando **Enter** en cualquier campo. Mientras se ejecuta, los campos y el botón se **deshabilitan** para impedir envíos dobles; el botón **Cancelar** detiene la consulta en curso.
- **Salida estructurada**: el agente devuelve título, publicación, hashtags y recomendaciones (el parser soporta JSON o texto por secciones `TÍTULO` / `PUBLICACIÓN` / `HASHTAGS` / `RECOMENDACIONES`).
- **Copiar publicación** al portapapeles con un clic.
- **Sidebar colapsable/expandible** con un toggle (en escritorio contrae a solo iconos; en móvil es un overlay).
- **Página Acerca de** con información del proyecto y el stack.
- **Diseño Microsoft Azure / Fluent**: paleta Azure blue, neutros Fluent, tipografía Segoe UI, iconos SVG inline (sin emoticones ni librerías de iconos).
- **Persistencia de sesiones** (en memoria) por usuario, con historial de mensajes.

---

## Stack técnico

| Capa | Tecnología |
|---|---|
| Web | ASP.NET Core 10, Blazor Interactive Server, Bootstrap 5 |
| AI | `Azure.AI.Projects` + `Azure.AI.Extensions.OpenAI` (Responses API) |
| Auth | `Azure.Identity` (`ClientSecretCredential` / `DefaultAzureCredential`) |
| Arquitectura | Clean Architecture (Domain / Application / Infrastructure / Web) |

---

## Arquitectura

```
linkedin-tutor-ai/                  # Web (Blazor): Pages, Shared, Program.cs, wwwroot
├── LinkedInTutorAI.Domain/         # Entidades, enums, abstracciones (sin dependencias)
├── LinkedInTutorAI.Application/     # DTOs, opciones, orquestador, mappings, validación
└── LinkedInTutorAI.Infrastructure/   # Cliente de Foundry, credenciales, persistencia, DI
```

Flujo de una generación:

```
LinkedInGenerator.razor (Blazor)
        │  GeneratePostStreamAsync
        ▼
IChatOrchestrator (Application)  ── valida, resuelve sesión, mapea, persiste
        │  AskStreamAsync
        ▼
IAiFoundryAgentService (Infrastructure)  ── CreateResponseStreamingAsync
        │  deltas (response.output_text.delta)
        ▼
   token a token  ──►  UI (StreamingPost)
```

---

## Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Un editor (Visual Studio, VS Code, JetBrains Rider)
- Una suscripción de Azure con:
  - un **proyecto de Azure AI Foundry** y un **agente** desplegado en él
  - una **identidad** (usuario o service principal) con un rol de Foundry sobre la cuenta (ver abajo)

---

## Configuración de Azure AI Foundry

### 1. El agente
Crea (o reutiliza) un agente en [Azure AI Foundry](https://ai.azure.com). Anota:
- el **endpoint del proyecto** (p. ej. `https://<recurso>.services.ai.azure.com/api/projects/<proyecto>`)
- el **nombre del agente** (p. ej. `agent-miagente`)

### 2. Rol RBAC (importante)
La operación de invocar al agente (`agents/write`) requiere **RBAC de Microsoft Entra ID**. Una API key **no sirve** para llamar al agente (no es un principal de Entra). Asigna uno de estos roles a tu identidad sobre el **recurso de Foundry** (la cuenta de Cognitive Services), **no** sobre el proyecto:

- **Foundry Agent Consumer** — mínimo privilegio, solo invocar el endpoint del agente (recomendado para service principals)
- **Foundry User** — lectura + acciones de datos
- **Foundry Owner** — acceso total

> ⚠️ El rol *Azure AI Developer* **no** cubre los agentes hospedados en Foundry (su scope es workspaces de Azure ML / hubs, no proyectos Foundry). Usa los roles *Foundry*.

### 3. Autenticación — elige un modo

| `AuthMode` | Cómo | Cuándo |
|---|---|---|
| `ServicePrincipal` (recomendado) | App Registration en Entra → `TenantId` + `ClientId` + `ClientSecret` + rol Foundry asignado al service principal | Producción / despliegue (no requiere login interactivo) |
| `DefaultCredential` | `az login` con un usuario que tenga el rol Foundry | Desarrollo local rápido |
| `ApiKey` | Cabecera `api-key` | **No funciona para invocar al agente** (RBAC). Se mantiene para otras operaciones del workspace. |

**Para `ServicePrincipal`** (producción):
1. Entra ID → **Registros de aplicaciones** → **Nuevo registro** → anota **Id. de aplicación (cliente)** (`ClientId`) e **Id. de directorio (inquilino)** (`TenantId`).
2. **Certificados y secretos** → crea un secreto de cliente → copia el **Valor** (`ClientSecret`).
3. Asigna el rol **Foundry Agent Consumer** (o superior) al service principal sobre el recurso de Foundry.

---

## Configuración de la aplicación

Los ajustes viven en la sección `AiFoundry` (se bindea a `FoundryOptions`, tipada y validada con `IOptions<FoundryOptions>` + `ValidateOnStart` → fail-fast al arrancar). El registro se hace en `Program.cs`:

```csharp
builder.Services.AddAiFoundry(builder.Configuration);
```

`AddAiFoundry` (en `LinkedInTutorAI.Infrastructure`) enlaza la sección, registra `IAiFoundryAgentService`, `ITutorSessionRepository` e `IChatOrchestrator`, y valida las opciones al inicio.

### `appsettings.json`

```json
{
  "AiFoundry": {
    "Endpoint": "https://<recurso>.services.ai.azure.com/api/projects/<proyecto>",
    "AgentName": "<nombre-del-agente>",
    "AuthMode": "ServicePrincipal",
    "TenantId": "<tenant-id>",
    "ClientId": "<client-id>",
    "ClientSecret": "<client-secret>",
    "MaxRetries": 3,
    "TimeoutSeconds": 60
  }
}
```

| Clave | ¿Requerido? | Descripción |
|---|---|---|
| `Endpoint` | sí | URL del proyecto de Foundry |
| `AgentName` | sí | Nombre del agente existente en Foundry |
| `AuthMode` | no | `ServicePrincipal` \| `DefaultCredential` \| `ApiKey` (default `DefaultCredential`) |
| `TenantId` | solo si `ServicePrincipal` | Tenant (directory) ID de Entra — **secreto** |
| `ClientId` | solo si `ServicePrincipal` | Application (client) ID — **secreto** |
| `ClientSecret` | solo si `ServicePrincipal` | Client secret — **secreto** |
| `ApiKey` | solo si `ApiKey` | Clave de API — **secreto** (no invoca al agente) |
| `MaxRetries` | no | Reintentos ante 429/5xx (default 3) |
| `TimeoutSeconds` | no | Timeout por llamada en segundos (default 60) |

> **Ningún archivo commiteado contiene secretos reales:** `appsettings.example.json` tiene solo placeholders. `appsettings.json` y `appsettings.Development.json` están ignorados por git.

### Por entorno

- **Development** — sobrescribe con `appsettings.Development.json` (ignorado por git) o [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets):
  ```bash
  dotnet user-secrets set "AiFoundry:ClientSecret" "<tu-secret>"
  ```
  Con `AuthMode=DefaultCredential`, haz `az login` (sin secretos) con un usuario que tenga el rol Foundry.
- **Production** — usa **variables de entorno** o Azure Key Vault. El `__` separa la sección anidada (`AiFoundry:Endpoint` → `AiFoundry__Endpoint`):

  | Variable de entorno | Clave |
  |---|---|
  | `AiFoundry__Endpoint` | `AiFoundry:Endpoint` |
  | `AiFoundry__AgentName` | `AiFoundry:AgentName` |
  | `AiFoundry__AuthMode` | `AiFoundry:AuthMode` |
  | `AiFoundry__TenantId` | `AiFoundry:TenantId` — **secreto** |
  | `AiFoundry__ClientId` | `AiFoundry:ClientId` — **secreto** |
  | `AiFoundry__ClientSecret` | `AiFoundry:ClientSecret` — **secreto** |

---

## Ejecutar en local

```bash
dotnet restore
dotnet run --project linkedin-tutor-ai.csproj
```

Abre **`https://localhost:7074`** (o `http://localhost:5078`) — la página de inicio es directamente el **Generador**.

---

## Cómo usar el portal

1. En el **Generador**, escribe un **Tema** (obligatorio) y ajusta opcionalmente rol, audiencia, tipo, tono, longitud, hashtags y pregunta final.
2. Pulsa **Ejecutar** (o **Enter**). El agente empieza a redactar y verás el texto aparecer **token a token**.
3. Si quieres detenerte, pulsa **Cancelar** — la consulta se cancela y los campos se rehabilitan.
4. Al terminar, el resultado limpio (título + publicación + hashtags + recomendaciones) sustituye al texto en streaming. Usa **Copiar publicación** para llevarlo al portapapeles.
5. El **sidebar** se contrae/expandide con su toggle. **Acerca de** tiene la info del proyecto.

---

## Seguridad

- **Nunca confirmes secretos en git.** `appsettings.json` y `appsettings.Development.json` están gitignored; usa User Secrets (dev) o variables de entorno / Key Vault (prod).
- **Rota el `ClientSecret`** periódicamente.
- El agente se invoca con una identidad de Entra con rol mínimo (Foundry Agent Consumer). No uses roles más amplios de los necesarios.

---

## Estructura del repositorio

```
linkedin-tutor-ai/
├── LinkedInTutorAI.Domain/          # Entidades, enums (AuthMode, PostTone, PostGoal...), abstracciones
├── LinkedInTutorAI.Application/      # DTOs, FoundryOptions, ChatOrchestrator, mappers, validación
├── LinkedInTutorAI.Infrastructure/    # AiFoundry (cliente, credenciales, mapper), Persistence, DI
├── Pages/                            # LinkedInGenerator, About (+ css/code-behind)
├── Shared/                           # MainLayout, NavMenu (+ css)
├── wwwroot/css/                      # Bootstrap, site.css (tokens Fluent)
├── App.razor / Program.cs
├── appsettings.example.json         # Plantilla (commiteada, placeholders)
└── linkedin-tutor-ai.slnx
```

---

## Estado y roadmap

- ✅ Generación con streaming token-a-token
- ✅ Auth dual: service principal (Entra) y `DefaultAzureCredential`
- ✅ Diseño Azure/Fluent, sidebar colapsable, Ejecutar/Cancelar
- ⏳ **Auth real**: usar el usuario autenticado en vez de `demo-user` (wiring de la capa Web pendiente)
- ⏳ **Persistencia real**: el repositorio es en memoria (`InMemoryTutorSessionRepository`) — swap por EF Core / otro store para producción
- ⏳ **CI** en GitHub Actions
- ⏳ Afinar RBAC (bajar de Foundry Owner a Foundry Agent Consumer)

---

## Licencia

[MIT](LICENSE).

---

## Autor

**Alejandro Pérez Maldonado** — alexandro6@hotmail.com

Proyecto académico · ASP.NET Core 10 + Azure AI Foundry.
