using Kitch.Application.DTOs.Recetas;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class RecetaService : IRecetaService
{
    private readonly IRepository<Receta> _repository;

    public RecetaService(IRepository<Receta> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<RecetaResponseDto>> GetAllAsync(string? rolUsuario = null)
    {
        var recetas = await _repository.GetAllAsync();
        return FiltrarPorPlan(recetas, rolUsuario).Select(receta => receta.ToResponseDto());
    }

    public async Task<RecetaResponseDto?> GetByIdAsync(int id, string? rolUsuario = null)
    {
        var receta = await _repository.GetByIdAsync(id);
        if (receta is null)
        {
            return null;
        }

        if (!PuedeVerDificultad(rolUsuario, receta.Dificultad))
        {
            throw new ForbiddenException(
                "Las recetas de dificultad Avanzada (Difícil) requieren plan Profesional.");
        }

        return receta.ToResponseDto();
    }

    public async Task<RecetaResponseDto> CreateAsync(RecetaCreateDto receta)
    {
        ValidateReceta(receta);
        var created = await _repository.AddAsync(ToEntity(receta));
        return created.ToResponseDto();
    }

    public async Task<bool> UpdateAsync(int id, RecetaUpdateDto receta)
    {
        if (!await _repository.AnyAsync(existing => existing.Id == id))
        {
            return false;
        }

        ValidateReceta(receta);
        var entity = ToEntity(receta);
        entity.Id = id;
        await _repository.UpdateAsync(entity);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
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
        string? rolUsuario = null)
    {
        if (!PuedeVerDificultad(rolUsuario, dificultad))
        {
            throw new ForbiddenException(
                "Las recetas de dificultad Avanzada (Difícil) requieren plan Profesional.");
        }

        var recetas = await _repository.FindAsync(receta => receta.Dificultad == dificultad);
        return recetas.Select(receta => receta.ToResponseDto());
    }

    private static IEnumerable<Receta> FiltrarPorPlan(IEnumerable<Receta> recetas, string? rolUsuario)
    {
        if (RolUsuario.TieneAccesoPremium(rolUsuario))
        {
            return recetas;
        }

        // Básico: Fácil e Intermedia (Medio). Sin Difícil/Avanzada.
        return recetas.Where(r => r.Dificultad is DificultadReceta.Facil or DificultadReceta.Medio);
    }

    private static bool PuedeVerDificultad(string? rolUsuario, DificultadReceta dificultad)
    {
        if (RolUsuario.TieneAccesoPremium(rolUsuario))
        {
            return true;
        }

        return dificultad is DificultadReceta.Facil or DificultadReceta.Medio;
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
