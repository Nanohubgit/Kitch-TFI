using Kitch.Application.DTOs.Recetas;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;
using System.Linq.Expressions;

namespace Kitch.Application.Services;

public class RecetaService : IRecetaService
{
    private readonly IRepository<Receta> _repository;
    private readonly IRepository<Usuario> _usuarioRepository;

    public RecetaService(
        IRepository<Receta> repository,
        IRepository<Usuario> usuarioRepository)
    {
        _repository = repository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<IEnumerable<RecetaResponseDto>> GetAllAsync(int usuarioId)
    {
        var rol = await ObtenerRolAsync(usuarioId);
        var recetas = await CargarRecetasAsync(_ => true);
        return FiltrarPorPlan(recetas, rol).Select(receta => receta.ToResponseDto());
    }

    public async Task<RecetaResponseDto?> GetByIdAsync(int id, int usuarioId)
    {
        var receta = await CargarRecetaCompletaAsync(id);
        if (receta is null)
        {
            return null;
        }

        var rol = await ObtenerRolAsync(usuarioId);
        if (!LimitesPlan.PuedeUsarDificultad(rol, receta.Dificultad))
        {
            throw new ForbiddenException(LimitesPlan.MensajeDificultadPremium);
        }

        return receta.ToResponseDto();
    }

    public async Task<RecetaResponseDto> CreateAsync(RecetaCreateDto receta, int usuarioId)
    {
        ValidateReceta(receta);
        await AsegurarDificultadPermitidaAsync(usuarioId, receta.Dificultad);
        var created = await _repository.AddAsync(ToEntity(receta));
        var completa = await CargarRecetaCompletaAsync(created.Id);
        return (completa ?? created).ToResponseDto();
    }

    public async Task<bool> UpdateAsync(int id, RecetaUpdateDto receta, int usuarioId)
    {
        if (!await _repository.AnyAsync(existing => existing.Id == id))
        {
            return false;
        }

        ValidateReceta(receta);
        await AsegurarDificultadPermitidaAsync(usuarioId, receta.Dificultad);
        var entity = ToEntity(receta);
        entity.Id = id;
        await _repository.UpdateAsync(entity);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, int solicitanteId)
    {
        await ValidarPermisosAdminAsync(solicitanteId);

        var receta = await _repository.GetByIdAsync(id);

        if (receta is null)
        {
            return false;
        }

        await _repository.DeleteAsync(receta);
        return true;
    }

    public async Task<IEnumerable<RecetaResponseDto>> GetByDificultadAsync(
        DificultadReceta dificultad,
        int usuarioId)
    {
        var rol = await ObtenerRolAsync(usuarioId);
        if (!LimitesPlan.PuedeUsarDificultad(rol, dificultad))
        {
            throw new ForbiddenException(LimitesPlan.MensajeDificultadPremium);
        }

        var recetas = await CargarRecetasAsync(receta => receta.Dificultad == dificultad);
        return recetas.Select(receta => receta.ToResponseDto());
    }

    private Task<IReadOnlyList<Receta>> CargarRecetasAsync(Expression<Func<Receta, bool>> predicate) =>
        _repository.FindWithIncludePathsAsync(
            predicate,
            $"{nameof(Receta.IngredientesReceta)}.{nameof(IngredienteReceta.Ingrediente)}",
            nameof(Receta.Preparaciones));

    private async Task<Receta?> CargarRecetaCompletaAsync(int id)
    {
        var recetas = await CargarRecetasAsync(receta => receta.Id == id);
        return recetas.FirstOrDefault();
    }

    private async Task ValidarPermisosAdminAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        if (usuario is null)
        {
            throw new UnauthorizedAccessException();
        }

        if (usuario.Rol != RolUsuario.Admin)
        {
            throw new ForbiddenException(
                "Acceso denegado. Se requieren permisos de administrador para visualizar esta información.");
        }
    }

    private async Task<string?> ObtenerRolAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        return usuario?.Rol;
    }

    private async Task AsegurarDificultadPermitidaAsync(int usuarioId, DificultadReceta dificultad)
    {
        var rol = await ObtenerRolAsync(usuarioId);
        if (!LimitesPlan.PuedeUsarDificultad(rol, dificultad))
        {
            throw new ForbiddenException(LimitesPlan.MensajeDificultadPremium);
        }
    }

    private static IEnumerable<Receta> FiltrarPorPlan(IEnumerable<Receta> recetas, string? rolUsuario)
    {
        if (RolUsuario.TieneAccesoPremium(rolUsuario))
        {
            return recetas;
        }

        return recetas.Where(r => LimitesPlan.PuedeUsarDificultad(rolUsuario, r.Dificultad));
    }

    private static void ValidateReceta(RecetaCreateDto receta)
    {
        if (receta.Ingredientes is null || receta.Ingredientes.Count == 0)
        {
            throw new InvalidOperationException("La receta debe tener al menos un ingrediente.");
        }

        if (receta.Preparaciones is null || receta.Preparaciones.Count == 0)
        {
            throw new InvalidOperationException("La receta debe tener al menos un paso de preparacion.");
        }

        if (receta.TiempoPreparacionMinutos <= 0)
        {
            throw new InvalidOperationException("El tiempo de preparacion debe ser mayor a cero.");
        }

        if (receta.Porciones <= 0)
        {
            throw new InvalidOperationException("La cantidad de porciones debe ser mayor a cero.");
        }

        if (receta.Ingredientes.Any(ingrediente => ingrediente.Cantidad <= 0))
        {
            throw new InvalidOperationException("Cada ingrediente debe tener una cantidad mayor a cero.");
        }

        if (receta.Preparaciones.Any(preparacion => preparacion.NumeroPaso <= 0))
        {
            throw new InvalidOperationException("Cada paso debe tener un numero mayor a cero.");
        }

        if (receta.Ingredientes.GroupBy(ingrediente => ingrediente.IngredienteId).Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException("No se puede repetir el mismo ingrediente en una receta.");
        }

        if (receta.Preparaciones.GroupBy(preparacion => preparacion.NumeroPaso).Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException("No se puede repetir el numero de paso en una receta.");
        }
    }

    private static Receta ToEntity(RecetaCreateDto receta) => new()
    {
        Titulo = receta.Titulo.Trim(),
        CaloriasEstimadas = receta.CaloriasEstimadas,
        Descripcion = receta.Descripcion.Trim(),
        TiempoPreparacionMinutos = receta.TiempoPreparacionMinutos,
        Porciones = receta.Porciones,
        Dificultad = receta.Dificultad,
        Categoria = CategoriasReceta.Normalizar(receta.Categoria),
        IngredientesReceta = receta.Ingredientes
            .Select(ingrediente => new IngredienteReceta
            {
                IngredienteId = ingrediente.IngredienteId,
                Cantidad = ingrediente.Cantidad,
                UnidadMedida = ingrediente.UnidadMedida.Trim()
            })
            .ToList(),
        Preparaciones = receta.Preparaciones
            .OrderBy(preparacion => preparacion.NumeroPaso)
            .Select(preparacion => new PreparacionReceta
            {
                NumeroPaso = preparacion.NumeroPaso,
                DescripcionPaso = preparacion.DescripcionPaso.Trim()
            })
            .ToList()
    };
}
