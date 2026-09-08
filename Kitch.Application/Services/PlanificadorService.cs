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
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IListaCompraService _listaCompraService;

    public PlanificadorService(
        IRepository<ComidaPlanificada> repository,
        IRepository<Receta> recetaRepository,
        IRepository<IngredienteReceta> ingredienteRecetaRepository,
        IRepository<Usuario> usuarioRepository,
        IListaCompraService listaCompraService)
    {
        _repository = repository;
        _recetaRepository = recetaRepository;
        _ingredienteRecetaRepository = ingredienteRecetaRepository;
        _usuarioRepository = usuarioRepository;
        _listaCompraService = listaCompraService;
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
        await AsegurarRecetaVisibleAsync(comida.UsuarioId, receta);
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

        var agregados = await ObtenerNombresAgregadosDeRecetaAsync(comida.UsuarioId, receta.Id);

        return new PlanificacionResultadoDto
        {
            Comida = created.ToResponseDto(),
            IngredientesAgregadosALista = agregados
        };
    }

    private async Task<List<string>> ObtenerNombresAgregadosDeRecetaAsync(int usuarioId, int recetaId)
    {
        var ingredientesReceta = await _ingredienteRecetaRepository.FindAsync(
            ingrediente => ingrediente.RecetaId == recetaId);
        var idsReceta = ingredientesReceta
            .Select(ingrediente => ingrediente.IngredienteId)
            .ToHashSet();

        var pendientes = await _listaCompraService.SincronizarFaltantesAsync(usuarioId);

        return pendientes
            .Where(item => item.IngredienteId is int id && idsReceta.Contains(id))
            .Select(item => item.NombreArticulo)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<bool> UpdateAsync(int id, ComidaPlanificadaUpdateDto comida, int usuarioId)
    {
        var existingComida = await _repository.GetByIdAsync(id);

        if (existingComida is null || existingComida.UsuarioId != usuarioId)
        {
            return false;
        }

        var receta = await ObtenerRecetaAsync(comida.RecetaId);
        await AsegurarRecetaVisibleAsync(usuarioId, receta);
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
        await _listaCompraService.SincronizarFaltantesAsync(usuarioId);

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
        await _listaCompraService.SincronizarFaltantesAsync(usuarioId);

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
            throw new ForbiddenException(LimitesPlan.MensajeLimitePlanner);
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
            throw new ForbiddenException(LimitesPlan.MensajeHorizontePlanner);
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

    private async Task AsegurarRecetaVisibleAsync(int usuarioId, Receta receta)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId)
            ?? throw new InvalidOperationException("El usuario no existe.");

        if (!LimitesPlan.PuedeUsarDificultad(usuario.Rol, receta.Dificultad))
        {
            throw new ForbiddenException(LimitesPlan.MensajeDificultadPremium);
        }
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
