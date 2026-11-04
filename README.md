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