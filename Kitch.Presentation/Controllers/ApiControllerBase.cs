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
}
