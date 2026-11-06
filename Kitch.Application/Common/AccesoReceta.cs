using System.Linq.Expressions;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;

namespace Kitch.Application.Common;

public static class AccesoReceta
{
    public const string MensajeNoEncontrada = "La receta no existe o no pertenece a tu cuenta.";

    public static bool Permitido(Receta receta, int usuarioId, string? rol) =>
        rol == RolUsuario.Admin || receta.UsuarioId == usuarioId;

    public static Expression<Func<Receta, bool>> Filtro(int usuarioId, string? rol) =>
        rol == RolUsuario.Admin
            ? receta => true
            : receta => receta.UsuarioId == usuarioId;
}
