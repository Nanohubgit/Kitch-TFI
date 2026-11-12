using Kitch.Application.Common;
using Kitch.Application.DTOs.Recetas;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Linq.Expressions;

namespace Kitch.Application.Services;

public class RecetaService : IRecetaService
{
    public const string CacheCatalogoKey = "recetas:catalogo";
    private static readonly TimeSpan CacheCatalogoTtl = TimeSpan.FromMinutes(5);
    private const int MaxNombreIngrediente = 100;

    private readonly IRepository<Receta> _repository;
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;
    private readonly IIngredienteNormalizerService _normalizer;
    private readonly IListaCompraService _listaCompraService;
    private readonly IMemoryCache _cache;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    public RecetaService(
        IRepository<Receta> repository,
        IRepository<Usuario> usuarioRepository,
        IRepository<Ingrediente> ingredienteRepository,
        IIngredienteNormalizerService normalizer,
        IListaCompraService listaCompraService,
        IMemoryCache cache,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _repository = repository;
        _usuarioRepository = usuarioRepository;
        _ingredienteRepository = ingredienteRepository;
        _normalizer = normalizer;
        _listaCompraService = listaCompraService;
        _cache = cache;
    }

    public async Task<IEnumerable<RecetaResponseDto>> GetAllAsync(int usuarioId)
    {
        var rol = await ObtenerRolAsync(usuarioId);
        var catalogo = await ObtenerCatalogoDtoAsync();
        return catalogo.Where(receta => Visible(receta, usuarioId, rol));
    }

    public async Task<RecetaResponseDto?> GetByIdAsync(int id, int usuarioId)
    {
        var receta = await CargarRecetaCompletaAsync(id);
        var rol = await ObtenerRolAsync(usuarioId);
        if (receta is null || !AccesoReceta.PuedeVer(receta, usuarioId, rol))
        {
            return null;
        }

        if (!LimitesPlan.PuedeUsarDificultad(rol, receta.Dificultad))
        {
            throw new ForbiddenException(LimitesPlan.MensajeDificultadPremium);
        }

        return receta.ToResponseDto();
    }

    public async Task<RecetaResponseDto> CreateAsync(RecetaCreateDto receta, int usuarioId)
    {
        var rol = await ObtenerRolAsync(usuarioId)
            ?? throw new UnauthorizedAccessException();

        ValidateReceta(receta);
        await ResolverIngredientesAsync(receta);

        var nueva = ToEntity(receta);
        if (!LimitesPlan.PuedeUsarDificultad(rol, nueva.Dificultad))
        {
            throw new ForbiddenException(LimitesPlan.MensajeDificultadPremium);
        }

        nueva.UsuarioId = usuarioId;
        var created = await _repository.AddAsync(nueva);
        _cache.Remove(CacheCatalogoKey);
        var completa = await CargarRecetaCompletaAsync(created.Id);
        return (completa ?? created).ToResponseDto();
    }

    public async Task<bool> UpdateAsync(int id, RecetaUpdateDto receta, int usuarioId)
    {
        var actualizada = await _unidadDeTrabajo.EjecutarAsync(() => UpdateEnTransaccionAsync(id, receta, usuarioId));
        _cache.Remove(CacheCatalogoKey);
        return actualizada;
    }

    private async Task<bool> UpdateEnTransaccionAsync(int id, RecetaUpdateDto receta, int usuarioId)
    {
        var rol = await ObtenerRolAsync(usuarioId)
            ?? throw new UnauthorizedAccessException();

        var existente = await CargarRecetaCompletaAsync(id);
        if (existente is null || !AccesoReceta.PuedeVer(existente, usuarioId, rol))
        {
            return false;
        }

        if (!AccesoReceta.PuedeEditar(existente, usuarioId, rol))
        {
            throw new ForbiddenException(AccesoReceta.MensajeSoloAutor);
        }

        ValidateReceta(receta);
        await ResolverIngredientesAsync(receta);
        var datos = ToEntity(receta);

        if (!LimitesPlan.PuedeUsarDificultad(rol, datos.Dificultad))
        {
            throw new ForbiddenException(LimitesPlan.MensajeDificultadPremium);
        }

        existente.Titulo = datos.Titulo;
        existente.CaloriasEstimadas = datos.CaloriasEstimadas;
        existente.Descripcion = datos.Descripcion;
        existente.TiempoPreparacionMinutos = datos.TiempoPreparacionMinutos;
        existente.Porciones = datos.Porciones;
        existente.Dificultad = datos.Dificultad;
        existente.Categoria = datos.Categoria;

        var nuevosIngredientes = datos.IngredientesReceta.ToDictionary(item => item.IngredienteId);
        foreach (var actual in existente.IngredientesReceta.ToList())
        {
            if (nuevosIngredientes.Remove(actual.IngredienteId, out var nuevo))
            {
                actual.Cantidad = nuevo.Cantidad;
                actual.UnidadMedida = nuevo.UnidadMedida;
            }
            else
            {
                existente.IngredientesReceta.Remove(actual);
            }
        }

        foreach (var nuevo in nuevosIngredientes.Values)
        {
            existente.IngredientesReceta.Add(nuevo);
        }

        var nuevosPasos = datos.Preparaciones.ToDictionary(paso => paso.NumeroPaso);
        foreach (var actual in existente.Preparaciones.ToList())
        {
            if (nuevosPasos.Remove(actual.NumeroPaso, out var nuevo))
            {
                actual.DescripcionPaso = nuevo.DescripcionPaso;
            }
            else
            {
                existente.Preparaciones.Remove(actual);
            }
        }

        foreach (var nuevo in nuevosPasos.Values)
        {
            existente.Preparaciones.Add(nuevo);
        }

        await _repository.UpdateAsync(existente);
        _cache.Remove(CacheCatalogoKey);
        await SincronizarListaDuenioAsync(existente.UsuarioId);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, int solicitanteId)
    {
        var eliminada = await _unidadDeTrabajo.EjecutarAsync(() => DeleteEnTransaccionAsync(id, solicitanteId));
        _cache.Remove(CacheCatalogoKey);
        return eliminada;
    }

    private async Task<bool> DeleteEnTransaccionAsync(int id, int solicitanteId)
    {
        var rol = await ObtenerRolAsync(solicitanteId)
            ?? throw new UnauthorizedAccessException();

        var receta = await _repository.GetByIdAsync(id);

        if (receta is null || !AccesoReceta.PuedeVer(receta, solicitanteId, rol))
        {
            return false;
        }

        if (!AccesoReceta.PuedeEditar(receta, solicitanteId, rol))
        {
            throw new ForbiddenException(AccesoReceta.MensajeSoloAutor);
        }

        await _repository.DeleteAsync(receta);
        _cache.Remove(CacheCatalogoKey);
        await SincronizarListaDuenioAsync(receta.UsuarioId);
        return true;
    }

    private async Task SincronizarListaDuenioAsync(int? usuarioId)
    {
        if (usuarioId is int id)
        {
            await _listaCompraService.SincronizarFaltantesAsync(id);
        }
    }

    public async Task<IEnumerable<RecetaResponseDto>> GetByDificultadAsync(
        string dificultad,
        int usuarioId)
    {
        if (!Enum.TryParse<DificultadReceta>(dificultad, true, out var valor))
        {
            throw new ArgumentException("La dificultad no es válida. Usá Facil, Medio o Dificil.");
        }

        var rol = await ObtenerRolAsync(usuarioId);
        if (!LimitesPlan.PuedeUsarDificultad(rol, valor))
        {
            throw new ForbiddenException(LimitesPlan.MensajeDificultadPremium);
        }

        var recetas = await ObtenerCatalogoDtoAsync();
        return recetas.Where(receta => receta.Dificultad == valor && Visible(receta, usuarioId, rol));
    }

    public async Task<IEnumerable<RecetaResponseDto>> GetByCategoriaAsync(string categoria, int usuarioId)
    {
        var clave = categoria?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!CategoriasReceta.Permitidas.Contains(clave))
        {
            throw new ArgumentException(
                "La categoría no es válida. Usá pastas, carnes, pollo, ensaladas, sopas, pescados, pizzas, postres, tartas, guisos o general.");
        }

        var rol = await ObtenerRolAsync(usuarioId);
        var recetas = await ObtenerCatalogoDtoAsync();
        return recetas.Where(receta => receta.Categoria == clave && Visible(receta, usuarioId, rol));
    }

    public IReadOnlyList<string> GetCategorias() => CategoriasReceta.Permitidas;

    private static bool Visible(RecetaResponseDto receta, int usuarioId, string? rol) =>
        AccesoReceta.PuedeVer(receta.UsuarioId, usuarioId, rol) &&
        LimitesPlan.PuedeUsarDificultad(rol, receta.Dificultad);

    private async Task ResolverIngredientesAsync(RecetaCreateDto receta)
    {
        foreach (var ingrediente in receta.Ingredientes)
        {
            if (ingrediente.IngredienteId > 0)
            {
                if (!await _ingredienteRepository.AnyAsync(item => item.Id == ingrediente.IngredienteId))
                {
                    throw new InvalidOperationException(
                        $"El ingrediente con id {ingrediente.IngredienteId} no existe. Indicalo por nombre.");
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(ingrediente.NombreIngrediente))
            {
                throw new InvalidOperationException("Cada ingrediente debe indicarse por id o por nombre.");
            }

            var nombre = _normalizer.Normalizar(ingrediente.NombreIngrediente);
            if (nombre.Length > MaxNombreIngrediente)
            {
                nombre = nombre[..MaxNombreIngrediente].TrimEnd();
            }

            var existente = await _ingredienteRepository.FirstOrDefaultAsync(item => item.Nombre == nombre);
            ingrediente.IngredienteId = existente?.Id ?? (await _ingredienteRepository.AddAsync(new Ingrediente
            {
                Nombre = nombre,
                Categoria = "Varios"
            })).Id;
        }

        if (receta.Ingredientes.GroupBy(ingrediente => ingrediente.IngredienteId).Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException("No se puede repetir el mismo ingrediente en una receta.");
        }
    }

    private async Task<IReadOnlyList<RecetaResponseDto>> ObtenerCatalogoDtoAsync()
    {
        if (_cache.TryGetValue(CacheCatalogoKey, out IReadOnlyList<RecetaResponseDto>? cached) && cached is not null)
        {
            return cached;
        }

        var recetas = await CargarRecetasAsync(_ => true);
        var dtos = recetas.Select(receta => receta.ToResponseDto()).ToList();
        _cache.Set(CacheCatalogoKey, dtos, CacheCatalogoTtl);
        return dtos;
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

    private async Task<string?> ObtenerRolAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        return usuario?.Rol;
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

        if (receta.Preparaciones.GroupBy(preparacion => preparacion.NumeroPaso).Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException("No se puede repetir el numero de paso en una receta.");
        }
    }

    private static DificultadReceta CalcularDificultadEfectiva(RecetaCreateDto receta)
    {
        var textos = new List<string?> { receta.Titulo, receta.Descripcion };
        textos.AddRange(receta.Preparaciones.Select(preparacion => preparacion.DescripcionPaso));

        return ComplejidadReceta.Efectiva(
            receta.Dificultad,
            receta.TiempoPreparacionMinutos,
            receta.Preparaciones.Count,
            receta.Ingredientes.Count,
            textos);
    }

    private static Receta ToEntity(RecetaCreateDto receta) => new()
    {
        Titulo = receta.Titulo.Trim(),
        CaloriasEstimadas = receta.CaloriasEstimadas,
        Descripcion = receta.Descripcion.Trim(),
        TiempoPreparacionMinutos = receta.TiempoPreparacionMinutos,
        Porciones = receta.Porciones,
        Dificultad = CalcularDificultadEfectiva(receta),
        Categoria = CategoriasReceta.Normalizar(receta.Categoria),
        IngredientesReceta = receta.Ingredientes
            .Select(ingrediente => new IngredienteReceta
            {
                IngredienteId = ingrediente.IngredienteId,
                Cantidad = ingrediente.Cantidad,
                UnidadMedida = string.IsNullOrWhiteSpace(ingrediente.UnidadMedida)
                    ? UnidadesMedida.Unidad
                    : ingrediente.UnidadMedida.Trim()
            })
            .ToList(),
        Preparaciones = receta.Preparaciones
            .OrderBy(preparacion => preparacion.NumeroPaso)
            .Select((preparacion, indice) => new PreparacionReceta
            {
                NumeroPaso = indice + 1,
                DescripcionPaso = preparacion.DescripcionPaso.Trim()
            })
            .ToList()
    };
}
