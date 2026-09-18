using System.Security.Claims;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kitch.Presentation.Filters;

/// <summary>
/// En cada request autenticado, si el Profesional ya no tiene suscripción vigente, baja el rol a Básico.
/// </summary>
public class AsegurarRolVigenteFilter : IAsyncActionFilter
{
    private readonly ISuscripcionVigenciaService _suscripcionVigencia;

    public AsegurarRolVigenteFilter(ISuscripcionVigenciaService suscripcionVigencia)
    {
        _suscripcionVigencia = suscripcionVigencia;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var permitirAnonimo = context.ActionDescriptor.EndpointMetadata
            .OfType<IAllowAnonymous>()
            .Any();

        if (!permitirAnonimo &&
            context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            var usuarioIdClaim = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(usuarioIdClaim, out var usuarioId))
            {
                await _suscripcionVigencia.AsegurarRolVigenteAsync(usuarioId);
            }
        }

        await next();
    }
}
