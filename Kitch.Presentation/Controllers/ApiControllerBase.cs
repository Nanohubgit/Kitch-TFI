using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected bool TryGetUsuarioId(out int usuarioId)
    {
        var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(usuarioIdClaim, out usuarioId);
    }

    protected int GetUsuarioIdOrThrow(string? mensaje = null)
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            throw new UnauthorizedAccessException(
                mensaje ?? "No se pudo identificar al usuario a partir del token.");
        }

        return usuarioId;
    }

    protected ActionResult BadRequestMessage(string message) =>
        BadRequest(new { message });

    protected ActionResult UnauthorizedMessage(string message) =>
        Unauthorized(new { message });
}
