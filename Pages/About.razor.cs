using Microsoft.AspNetCore.Components;

namespace linkedin_tutor_ai.Pages;

/// <summary>
/// Página "Acerca de". Code-behind separado; solo expone la fecha actual para mostrarla.
/// </summary>
public partial class About
{
    /// <summary>Fecha y hora actual (UTC offset local) para mostrarla en la página.</summary>
    private DateTimeOffset Now => DateTimeOffset.Now;
}
