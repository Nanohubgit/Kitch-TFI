using Kitch.Application.DTOs.Planificador;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;
using System.Linq.Expressions;

namespace Kitch.Application.Services;

public class PlanificadorService : IPlanificadorService
{
    private readonly IRepository<ComidaPlanificada> _repository;
    private readonly IRepository<Receta> _recetaRepository;
    private readonly IRepository<IngredienteReceta> _ingredienteRecetaRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<ItemListaCompra> _listaCompraRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;
    private readonly IRepository<Usuario> _usuarioRepository;

    public PlanificadorService(
        IRepository<ComidaPlanificada> repository,
        IRepository<Receta> recetaRepository,
        IRepository<IngredienteReceta> ingredienteRecetaRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<ItemListaCompra> listaCompraRepository,
        IRepository<Ingrediente> ingredienteRepository,
        IRepository<Usuario> usuarioRepository)
    {
        _repository = repository;
        _recetaRepository = recetaRepository;
        _ingredienteRecetaRepository = ingredienteRecetaRepository;
        _stockRepository = stockRepository;
        _listaCompraRepository = listaCompraRepository;
        _ingredienteRepository = ingredienteRepository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<IEnumerable<ComidaPlanificadaResponseDto>> GetByUsuarioIdAsync(int usuarioId)
    {
        var comidas = await ObtenerComidasConRecetaAsync(comida => comida.UsuarioId == usuarioId);
        return comidas.Select(comida => comida.ToResponseDto());
    }

    public async Task<IEnumerable<ComidaPlanificadaResponseDto>> GetByFechaAsync(int usuarioId, DateTime fecha)
    {
        var fechaInicio = fecha.Date;
        var fechaFin = fechaInicio.AddDays(1);

        var comidas = await ObtenerComidasConRecetaAsync(comida =>
            comida.UsuarioId == usuarioId &&
            comida.FechaAsignada >= fechaInicio &&
            comida.FechaAsignada < fechaFin);

        return comidas.Select(comida => comida.ToResponseDto());
    }

    public async Task<ComidaPlanificadaResponseDto?> GetByIdAsync(int id, int usuarioId)
    {
        var comidas = await ObtenerComidasConRecetaAsync(comida =>
            comida.Id == id && comida.UsuarioId == usuarioId);

        return comidas.FirstOrDefault()?.ToResponseDto();
    }

    public async Task<ComidaPlanificadaResponseDto> CreateAsync(ComidaPlanificadaCreateDto comida)
    {
        var resultado = await PlanificarAsync(comida);
        return resultado.Comida;
    }

    public async Task<PlanificacionResultadoDto> PlanificarAsync(ComidaPlanificadaCreateDto comida)
    {
        var receta = await ObtenerRecetaAsync(comida.RecetaId);
        await ValidarHorizontePlanificacionAsync(comida.UsuarioId, comida.FechaAsignada);
        await AsegurarCupoComidasPlanificadasAsync(comida.UsuarioId);

        if (await ExisteConflictoAsync(comida.UsuarioId, comida.FechaAsignada, comida.Turno))
        {
            throw new InvalidOperationException("Ya existe una comida planificada para ese usuario, fecha y turno.");
        }

        var entity = new ComidaPlanificada
        {
            UsuarioId = comida.UsuarioId,
            RecetaId = receta.Id,
            FechaAsignada = comida.FechaAsignada,
            Turno = comida.Turno.Trim()
        };

        var created = await _repository.AddAsync(entity);
        created.Receta = receta;

        var agregados = await AgregarFaltantesAListaCompraAsync(comida.UsuarioId, comida.RecetaId);

        return new PlanificacionResultadoDto
        {
            Comida = created.ToResponseDto(),
            IngredientesAgregadosALista = agregados
        };
    }

    private async Task<List<string>> AgregarFaltantesAListaCompraAsync(int usuarioId, int recetaId)
    {
        var agregados = new List<string>();

        var ingredientesReceta = await _ingredienteRecetaRepository.FindAsync(
            ingrediente => ingrediente.RecetaId == recetaId);

        if (ingredientesReceta.Count == 0)
        {
            return agregados;
        }

        var stock = await _stockRepository.FindAsync(item => item.UsuarioId == usuarioId);
        var stockPorIngrediente = stock.ToDictionary(item => item.IngredienteId, item => item.Cantidad);

        var faltantes = new Dictionary<int, decimal>();
        foreach (var ingrediente in ingredientesReceta)
        {
            stockPorIngrediente.TryGetValue(ingrediente.IngredienteId, out var enStock);
            var faltante = ingrediente.Cantidad - enStock;
            if (faltante > 0)
            {
                faltantes.TryGetValue(ingrediente.IngredienteId, out var acumulado);
                faltantes[ingrediente.IngredienteId] = acumulado + faltante;
            }
        }

        if (faltantes.Count == 0)
        {
            return agregados;
        }

        var ingredienteIds = faltantes.Keys.ToList();
        var ingredientes = await _ingredienteRepository.FindAsync(
            ingrediente => ingredienteIds.Contains(ingrediente.Id));
        var nombrePorIngrediente = ingredientes.ToDictionary(
            ingrediente => ingrediente.Id,
            ingrediente => ingrediente.Nombre);

        var itemsExistentes = await _listaCompraRepository.FindAsync(item => item.UsuarioId == usuarioId);

        foreach (var (ingredienteId, cantidadFaltante) in faltantes)
        {
            var nombre = nombrePorIngrediente.TryGetValue(ingredienteId, out var n)
                ? n
                : $"Ingrediente #{ingredienteId}";

            var existente = itemsExistentes.FirstOrDefault(item =>
                string.Equals(item.NombreArticulo, nombre, StringComparison.OrdinalIgnoreCase));

            if (existente is not null)
            {
                existente.CantidadFaltante += (float)cantidadFaltante;
                await _listaCompraRepository.UpdateAsync(existente);
            }
            else
            {
                await _listaCompraRepository.AddAsync(new ItemListaCompra
                {
                    UsuarioId = usuarioId,
                    NombreArticulo = nombre,
                    CantidadFaltante = (float)cantidadFaltante,
                    EstaComprado = false
                });
            }

            agregados.Add(nombre);
        }

        return agregados;
    }

    public async Task<bool> UpdateAsync(int id, ComidaPlanificadaUpdateDto comida, int usuarioId)
    {
        var existingComida = await _repository.GetByIdAsync(id);

        if (existingComida is null || existingComida.UsuarioId != usuarioId)
        {
            return false;
        }

        await ObtenerRecetaAsync(comida.RecetaId);
        await ValidarHorizontePlanificacionAsync(usuarioId, comida.FechaAsignada);

        if (await _repository.AnyAsync(existing =>
                existing.Id != id &&
                existing.UsuarioId == usuarioId &&
                existing.FechaAsignada.Date == comida.FechaAsignada.Date &&
                existing.Turno == comida.Turno.Trim()))
        {
            throw new InvalidOperationException("Ya existe una comida planificada para ese usuario, fecha y turno.");
        }

        existingComida.UsuarioId = usuarioId;
        existingComida.RecetaId = comida.RecetaId;
        existingComida.FechaAsignada = comida.FechaAsignada;
        existingComida.Turno = comida.Turno.Trim();

        await _repository.UpdateAsync(existingComida);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, int usuarioId)
    {
        var comida = await _repository.GetByIdAsync(id);

        if (comida is null || comida.UsuarioId != usuarioId)
        {
            return false;
        }

        await _repository.DeleteAsync(comida);

        return true;
    }

    private async Task AsegurarCupoComidasPlanificadasAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId)
            ?? throw new InvalidOperationException("El usuario no existe.");

        if (RolUsuario.TieneAccesoPremium(usuario.Rol))
        {
            return;
        }

        var cantidad = await _repository.CountAsync(comida => comida.UsuarioId == usuarioId);
        if (cantidad >= LimitesPlan.MaxComidasPlanificadasBasico)
        {
            throw new ForbiddenException(
                $"Límite alcanzado. El plan Básico permite hasta {LimitesPlan.MaxComidasPlanificadasBasico} recetas planificadas. Mejorá tu plan a Profesional para planificar más.");
        }
    }

    private async Task ValidarHorizontePlanificacionAsync(int usuarioId, DateTime fechaAsignada)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId)
            ?? throw new InvalidOperationException("El usuario no existe.");

        var diasMax = RolUsuario.TieneAccesoPremium(usuario.Rol)
            ? LimitesPlan.DiasPlanificacionProfesional
            : LimitesPlan.DiasPlanificacionBasico;

        var hoy = DateTime.UtcNow.Date;
        var limite = hoy.AddDays(diasMax);

        if (fechaAsignada.Date < hoy)
        {
            throw new InvalidOperationException("No podés planificar comidas en fechas pasadas.");
        }

        if (fechaAsignada.Date > limite)
        {
            throw new ForbiddenException(
                $"Tu plan permite agendar hasta {diasMax} días adelante. Actualizá a Profesional para planificar a 30 días.");
        }
    }

    private async Task<Receta> ObtenerRecetaAsync(int recetaId)
    {
        var receta = await _recetaRepository.GetByIdAsync(recetaId);
        if (receta is null)
        {
            throw new KeyNotFoundException(
                $"La receta con id {recetaId} no existe. Generá y guardá una receta primero para obtener su id.");
        }

        return receta;
    }

    private Task<IReadOnlyList<ComidaPlanificada>> ObtenerComidasConRecetaAsync(
        Expression<Func<ComidaPlanificada, bool>> predicate) =>
        _repository.FindWithIncludesAsync(predicate, comida => comida.Receta);

    private async Task<bool> ExisteConflictoAsync(int usuarioId, DateTime fecha, string turno)
    {
        var fechaInicio = fecha.Date;
        var fechaFin = fechaInicio.AddDays(1);
        var turnoNormalizado = turno.Trim();

        return await _repository.AnyAsync(comida =>
            comida.UsuarioId == usuarioId &&
            comida.FechaAsignada >= fechaInicio &&
            comida.FechaAsignada < fechaFin &&
            comida.Turno == turnoNormalizado);
    }
}
