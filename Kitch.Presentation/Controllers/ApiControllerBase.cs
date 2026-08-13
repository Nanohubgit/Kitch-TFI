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

    protected string? GetRolOrNull() =>
        User.FindFirstValue(ClaimTypes.Role);

    protected ActionResult ForbiddenMessage(string message) =>
        StatusCode(StatusCodes.Status403Forbidden, new { message });

    protected ActionResult BadRequestMessage(string message) =>
        BadRequest(new { message });

    protected ActionResult UnauthorizedMessage(string message) =>
        Unauthorized(new { message });
}
