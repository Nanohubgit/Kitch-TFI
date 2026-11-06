using System.Security.Claims;
using Kitch.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;

namespace Kitch.Presentation.Filters;

public class RolVigenteClaimsTransformation : IClaimsTransformation
{
    public const string ClaimRolVerificado = "kitch:rol_verificado";
    public const string ItemUsuarioBloqueado = "kitch:usuario_bloqueado";

    private readonly ISuscripcionVigenciaService _suscripcionVigencia;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RolVigenteClaimsTransformation(
        ISuscripcionVigenciaService suscripcionVigencia,
        IHttpContextAccessor httpContextAccessor)
    {
        _suscripcionVigencia = suscripcionVigencia;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identidadOriginal ||
            principal.HasClaim(c => c.Type == ClaimRolVerificado))
        {
            return principal;
        }

        if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            return principal;
        }

        var rolVigente = await _suscripcionVigencia.ObtenerRolVigenteAsync(usuarioId);
        if (rolVigente is null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is not null)
            {
                httpContext.Items[ItemUsuarioBloqueado] = true;
            }

            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var identidad = new ClaimsIdentity(
            identidadOriginal.Claims.Where(c => c.Type != identidadOriginal.RoleClaimType),
            identidadOriginal.AuthenticationType,
            identidadOriginal.NameClaimType,
            identidadOriginal.RoleClaimType);

        identidad.AddClaim(new Claim(identidadOriginal.RoleClaimType, rolVigente));
        identidad.AddClaim(new Claim(ClaimRolVerificado, "1"));

        return new ClaimsPrincipal(identidad);
    }
}
