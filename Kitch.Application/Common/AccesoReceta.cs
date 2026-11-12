using System.Linq.Expressions;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;

namespace Kitch.Application.Common;

public static class AccesoReceta
{
    public const string MensajeNoEncontrada = "La receta no existe o no pertenece a tu cuenta.";

    public const string MensajeSoloAutor =
        "Solo el autor de la receta o un administrador pueden modificarla o eliminarla.";

    public static bool PuedeVer(Receta receta, int usuarioId, string? rol) =>
        PuedeVer(receta.UsuarioId, usuarioId, rol);

    public static bool PuedeVer(int? duenioId, int usuarioId, string? rol) =>
        rol == RolUsuario.Admin || duenioId is null || duenioId == usuarioId;

    public static bool PuedeEditar(Receta receta, int usuarioId, string? rol) =>
        rol == RolUsuario.Admin || receta.UsuarioId == usuarioId;

    public static Expression<Func<Receta, bool>> Filtro(int usuarioId, string? rol) =>
        rol == RolUsuario.Admin
            ? receta => true
            : receta => receta.UsuarioId == null || receta.UsuarioId == usuarioId;
}
