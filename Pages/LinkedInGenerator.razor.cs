using System.ComponentModel.DataAnnotations;
using LinkedInTutorAI.Application.Abstractions;
using LinkedInTutorAI.Application.Dtos;
using LinkedInTutorAI.Application.Exceptions;
using LinkedInTutorAI.Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace linkedin_tutor_ai.Pages;

/// <summary>
/// Modelo del formulario de generación. Las validaciones se declaran aquí para
/// que <c>DataAnnotationsValidator</c> las muestre en línea en la UI.
/// </summary>
public sealed class LinkedInGeneratorModel
{
    [Required(ErrorMessage = "El tema es obligatorio.")]
    [StringLength(300, ErrorMessage = "El tema no puede superar 300 caracteres.")]
    public string Tema { get; set; } = "";

    [StringLength(120, ErrorMessage = "El rol no puede superar 120 caracteres.")]
    public string? Rol { get; set; }

    [StringLength(300, ErrorMessage = "La audiencia no puede superar 300 caracteres.")]
    public string? Audiencia { get; set; }

    [StringLength(80, ErrorMessage = "El tipo de publicación no puede superar 80 caracteres.")]
    public string? TipoPublicacion { get; set; }

    public PostTone Tono { get; set; } = PostTone.Professional;

    [Range(50, 3000, ErrorMessage = "La longitud debe estar entre 50 y 3000.")]
    public int Longitud { get; set; } = 1300;

    public bool IncluirHashtags { get; set; } = true;

    public bool IncluirPreguntaFinal { get; set; } = true;
}

/// <summary>
/// Página Blazor Server para generar publicaciones de LinkedIn con el agente de IA.
/// Lógica en code-behind separado; maquetación en <c>LinkedInGenerator.razor</c>
/// y estilos en <c>LinkedInGenerator.razor.css</c>.
/// </summary>
public partial class LinkedInGenerator
{
    /// <summary>Orquestador del caso de uso (inyectado por DI).</summary>
    [Inject]
    private IChatOrchestrator Orchestrator { get; set; } = default!;

    /// <summary>Acceso a JS para el portapapeles (copiar).</summary>
    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    // TODO: reemplazar por el usuario autenticado cuando el wiring de auth (Web layer) esté listo.
    private const string DemoUserId = "demo-user";

    private readonly LinkedInGeneratorModel Model = new();

    private bool IsGenerating;
    private LinkedInPostResponse? Result;
    private string StreamingPost = "";
    private string? ErrorMessage;
    private string? StatusMessage;
    private bool copied;

    /// <summary>CTS de la generación en curso; permite cancelar con el botón Cancelar.</summary>
    private CancellationTokenSource? _cts;

    /// <summary>True si la cancelación la inició el usuario (vs timeout del CTS).</summary>
    private bool _userCancelled;

    /// <summary>Título a mostrar en el resultado (con respaldo si el agente no lo devolvió).</summary>
    private string TitleDisplay =>
        string.IsNullOrEmpty(Result?.Title) ? "Publicación generada" : Result!.Title;

    /// <summary>
    /// Maneja el envío válido del formulario (también al pulsar Enter en un campo):
    /// construye el <see cref="LinkedInPostRequest"/>, valida, llama al orquestador en streaming
    /// y gestiona errores, cancelación y estado. Mientras se ejecuta, los campos y el botón
    /// Ejecutar quedan deshabilitados para impedir envíos dobles.
    /// </summary>
    private async Task GenerateAsync(EditContext editContext)
    {
        IsGenerating = true;
        ErrorMessage = null;
        StatusMessage = null;
        Result = null;
        StreamingPost = "";
        copied = false;
        StateHasChanged();

        _cts?.Dispose();
        _cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        _userCancelled = false;

        try
        {
            var request = new LinkedInPostRequest
            {
                PostDraft = Model.Tema,
                Goal = PostGoal.IncreaseReach,
                Tone = Model.Tono,
                TargetAudience = Model.Audiencia,
                MaxLength = Model.Longitud,
                Language = "es",
                Rol = Model.Rol,
                TipoPublicacion = Model.TipoPublicacion,
                IncluirHashtags = Model.IncluirHashtags,
                IncluirPreguntaFinal = Model.IncluirPreguntaFinal
            };

            var validation = request.Validate();
            if (!validation.IsValid)
            {
                ErrorMessage = string.Join(" · ", validation.Errors.Select(e => e.Message));
                return;
            }

            await foreach (var chunk in Orchestrator.GeneratePostStreamAsync(request, DemoUserId, _cts.Token))
            {
                if (!string.IsNullOrEmpty(chunk.Delta))
                {
                    StreamingPost += chunk.Delta;
                    StateHasChanged();
                }

                if (chunk.IsFinal && chunk.Response is not null)
                {
                    Result = chunk.Response;
                    StateHasChanged();
                }
            }
        }
        catch (OperationCanceledException) when (_cts is { IsCancellationRequested: true })
        {
            if (_userCancelled)
            {
                StatusMessage = "Generación cancelada.";
            }
            else
            {
                ErrorMessage = "La generación tardó demasiado. Intenta de nuevo.";
            }
        }
        catch (AiFoundryException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = "Ocurrió un error inesperado al generar: " + ex.Message;
        }
        finally
        {
            IsGenerating = false;
            _cts?.Dispose();
            _cts = null;
            StateHasChanged();
        }
    }

    /// <summary>Cancela la generación en curso (botón Cancelar).</summary>
    private Task CancelAsync()
    {
        _userCancelled = true;
        if (_cts is { IsCancellationRequested: false })
        {
            _cts.Cancel();
        }

        return Task.CompletedTask;
    }

    /// <summary>Copia la publicación generada al portapapeles.</summary>
    private async Task CopyAsync()
    {
        if (Result is null || string.IsNullOrWhiteSpace(Result.GeneratedPost))
        {
            return;
        }

        try
        {
            await Js.InvokeVoidAsync("navigator.clipboard.writeText", Result.GeneratedPost);
            copied = true;
        }
        catch
        {
            // El portapapeles puede no estar disponible en algunos visores; fallo silencioso.
        }
    }
}
