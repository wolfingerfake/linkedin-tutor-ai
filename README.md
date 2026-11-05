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

## Configuración en Azure (plataforma)

Pasos para crear la infraestructura en Azure. Necesitas una suscripción de Azure con permisos para crear recursos.

### 1. Crear el recurso y el proyecto de Azure AI Foundry
1. Entra en [Azure AI Foundry](https://ai.azure.com) con tu cuenta de Azure.
2. Si no tienes un **hub** (recurso de Foundry), créalo: *Manage* → *All hubs* → *New hub* (o usa el que se cree por defecto). Anota el **nombre del recurso** (p. ej. `mi-recurso`).
3. Crea un **proyecto** dentro del hub: *New project* → nómbralo (p. ej. `miproyecto`). Anota el **endpoint del proyecto**, con la forma `https://<recurso>.services.ai.azure.com/api/projects/<proyecto>`.
4. En el proyecto, despliega un **modelo** (p. ej. `gpt-4o-mini`) desde *Models* si tu agente lo requiere.

> El **modo de acceso** (RBAC de Entra vs claves) se fija al **crear el hub** y no es editable después. Para invocar al agente con una identidad de Entra (recomendado), el hub debe permitir acceso Entra. Si se creó en modo "solo RBAC", la API key **no puede** invocar al agente (ver *Problemas* más abajo).

### 2. Identidad con permisos (RBAC)
La operación de invocar al agente (`agents/write`) exige RBAC de Entra. Asigna uno de estos roles a tu identidad sobre el **recurso de Foundry** (la cuenta de Cognitive Services, en Azure Portal → IAM), **no** sobre el proyecto:
- **Foundry Agent Consumer** — mínimo privilegio, solo invocar el endpoint del agente (recomendado para service principals).
- **Foundry User** — lectura + acciones de datos.
- **Foundry Owner** — acceso total.

> ⚠️ El rol *Azure AI Developer* **no** cubre los agentes hospedados en Foundry (su scope es workspaces de Azure ML / hubs, no proyectos Foundry). Usa los roles *Foundry*.

### 3. Crear el agente en Foundry
1. En [ai.azure.com](https://ai.azure.com), dentro de tu proyecto, ve a *Agents* → *New agent* (o reutiliza uno existente).
2. Configura el agente: modelo, instrucciones/system prompt, herramientas (opcional). **Anota el nombre del agente** (p. ej. `agent-miagente`) — es el `AgentName` de la app.
3. Pruébalo en el *playground* de Foundry para confirmar que responde.

Anota también el **endpoint del proyecto** (`https://<recurso>.services.ai.azure.com/api/projects/<proyecto>`) — son los dos valores (`Endpoint` + `AgentName`) que necesita la app.

### 4. Autenticación — elige un modo


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

## Publicar en un subdirectorio (IIS / subcarpeta / subdominio)

Si la app **no** cuelga de la raíz del host (p. ej. `https://grumexcoapps.com/foundry/`), indica el prefijo con `Hosting:PathBase` para que rutas, estáticos y el endpoint de Blazor resuelvan bajo él. En la raíz se deja vacío (sin config).

- **Env var** (recomendado para IIS): `Hosting__PathBase=/foundry` (ajústala en el Application Pool del sitio).
- **appsettings.json**: `"Hosting": { "PathBase": "/foundry" }`.

El `<base href>` y el `app.UsePathBase` se ajustan automáticamente a ese valor. Para raíz, omítelo o déjalo vacío.

```bash
# Ejemplo: publicar y arrancar en un subdirectorio /foundry
dotnet publish linkedin-tutor-ai.csproj -c Release -o ./publish
Hosting__PathBase=/foundry dotnet ./publish/linkedin-tutor-ai.dll --urls http://localhost:5000
```

> Si publicas en IIS, copia el contenido de `publish/` al directorio del sitio (p. ej. `C:\inetpub\foundry`) y crea el sitio/aplicación apuntando ahí con `Hosting__PathBase=/foundry` en el Application Pool.

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

## Problemas encontrados y soluciones

Registro de incidentes reales durante el desarrollo (por si a alguien le sirve):

1. **403 RBAC al invocar al agente con API key.** La API key pasa la autenticación del workspace pero la operación `agents/write` exige RBAC de Entra; una clave no es un principal de Entra (`object id` vacío → `403 Forbidden`). *Solución:* autenticación con **service principal de Entra** (`ClientSecretCredential`) + rol **Foundry** asignado a ese service principal sobre la cuenta de Foundry.
2. **Rol equivocado (`Azure AI Developer`).** Aunque suene bien, ese rol está scoped a workspaces de Azure ML / hubs, **no** a agentes de Foundry. *Solución:* usar los roles **Foundry** (Agent Consumer / User / Owner) sobre el recurso de Foundry. [Doc oficial](https://learn.microsoft.com/azure/foundry/concepts/rbac-foundry).
3. **No había API pública para leer los deltas del stream.** En `OpenAI 2.14.0` / `Azure.AI.Extensions.OpenAI 3.0.0-beta.6` se eliminó el acumulador `StreamingResponse` y `StreamingResponseUpdate` solo expone `Kind`/`Patch`(opaco)/`SequenceNumber`. *Solución:* serializar cada update con `ModelReaderWriter.Write(update)` (API pública) y leer el campo `"delta"`, filtrando por `update.Kind == ResponseOutputTextDelta` y `"type" == "response.output_text.delta"` (para no mezclar deltas de reasoning/function-call).
4. **`yield` dentro de un `try` con `catch`.** En el `AskStreamAsync` (`IAsyncEnumerable`) no se puede hacer `yield` dentro de un `try-catch` (restricción de C#). *Solución:* enumerar con `IAsyncEnumerator` manual y envolver solo `MoveNextAsync` en el `try-catch`, mapeando errores con `AgentExceptionMapper`.
5. **IIS Production: la app no conectaba al agente.** `appsettings.Production.json` (commiteado con placeholders) pisaba al `appsettings.json` real en Production → `AuthMode=DefaultCredential` + endpoint placeholder → `DefaultAzureCredential` fallaba en el app pool de IIS (sin `az login`). *Solución:* quitar la sección `AiFoundry` de `appsettings.Production.json`; en Production los valores reales van por variables de entorno / Key Vault.
6. **IIS en subdirectorio (`/foundry/`): estilos y páginas no cargaban.** El `<base href="/">` hacía que el navegador pidiera assets en la raíz del host en vez de bajo `/foundry/`. *Solución:* `Hosting:PathBase` (env var `Hosting__PathBase`) → `<base href>` dinámico + `app.UsePathBase(pathBase)`. Vacío para raíz.
7. **Iconos SVG enormes.** Los `<svg>` sin `width`/`height` caían al tamaño por defecto (300×150) si el bundle CSS scoped no cargaba. *Solución:* `width`/`height` explícitos en cada SVG (13–20px) + tamaños proporcionales al texto adyacente.
8. **Layout descuadrado en escritorio.** Faltaba el `@media (min-width:641px)` que pone el `.page` en `flex-direction: row` y el sidebar sticky a altura completa. *Solución:* añadir el media query.
9. **Texto del menú en azul sobre sidebar marino.** El `<a>` de cada item lo renderiza `NavLink` (componente hijo), fuera del scope de `NavMenu.razor.css`, así que ganaba el `.nav-link` de Bootstrap (azul). *Solución:* usar `::deep` para reachedlos y fijar color blanco.

---

## Estado y roadmap

- ✅ Generación con streaming token-a-token (Responses API + `ModelReaderWriter.Write`)
- ✅ Auth dual: service principal (Entra) y `DefaultAzureCredential` (`ApiKey` queda para otras ops, no invoca al agente)
- ✅ Diseño Microsoft Azure / Fluent: sidebar marino colapsable, Ejecutar/Cancelar con deshabilitación, página Acerca de premium, iconos SVG
- ✅ Publicación en IIS (subdirectorio vía `Hosting:PathBase`) y config Production sin pisar la real
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
