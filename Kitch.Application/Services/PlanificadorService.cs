using Kitch.Application.Common;
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
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    public PlanificadorService(
        IRepository<ComidaPlanificada> repository,
        IRepository<Receta> recetaRepository,
        IRepository<IngredienteReceta> ingredienteRecetaRepository,
        IRepository<Usuario> usuarioRepository,
        IListaCompraService listaCompraService,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
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

    public async Task<IEnumerable<ComidaPlanificadaResponseDto>> GetByRangoFechasAsync(
        int usuarioId,
        DateTime desde,
        DateTime hasta)
    {
        if (hasta.Date < desde.Date)
        {
            throw new ArgumentException("La fecha 'hasta' no puede ser anterior a 'desde'.");
        }

        var fechaInicio = desde.Date;
        var fechaFin = hasta.Date.AddDays(1);

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
        comida.Turno = TurnosComida.Normalizar(comida.Turno);
        comida.FechaAsignada = comida.FechaAsignada.Date;
        var habiaConflicto = await ExisteConflictoAsync(comida.UsuarioId, comida.FechaAsignada, comida.Turno);
        var resultado = await PlanificarAsync(comida);
        return CompletarRespuestaPlanificacion(resultado, habiaConflicto && comida.ReemplazarSiConflicto);
    }

    public Task<ComidaPlanificadaResponseDto> AgendarAsync(int usuarioId, AgendarRecetaRequestDto request)
    {
        return CreateAsync(new ComidaPlanificadaCreateDto
        {
            UsuarioId = usuarioId,
            RecetaId = request.RecetaId,
            FechaAsignada = request.FechaAsignada,
            Turno = request.Turno,
            ReemplazarSiConflicto = request.ReemplazarSiConflicto
        });
    }

    public Task<PlanificacionResultadoDto> PlanificarAsync(ComidaPlanificadaCreateDto comida) =>
        EnTransaccionAsync(comida.UsuarioId, () => PlanificarEnTransaccionAsync(comida));

    private Task<T> EnTransaccionAsync<T>(int usuarioId, Func<Task<T>> operacion) =>
        _unidadDeTrabajo.EjecutarAsync(async () =>
        {
            await _unidadDeTrabajo.BloquearRecursoAsync($"kitch:stock:usuario:{usuarioId}");
            return await operacion();
        });

    private async Task<PlanificacionResultadoDto> PlanificarEnTransaccionAsync(ComidaPlanificadaCreateDto comida)
    {
        comida.Turno = TurnosComida.Normalizar(comida.Turno);
        comida.FechaAsignada = comida.FechaAsignada.Date;
        var receta = await ObtenerRecetaAsync(comida.RecetaId);
        await AsegurarRecetaVisibleAsync(comida.UsuarioId, receta);
        await ValidarHorizontePlanificacionAsync(comida.UsuarioId, comida.FechaAsignada);

        if (await ExisteConflictoAsync(comida.UsuarioId, comida.FechaAsignada, comida.Turno))
        {
            if (!comida.ReemplazarSiConflicto)
            {
                throw new ConflictException(
                    "Ya tenés una comida asignada en ese día y turno. ¿Querés reemplazarla?");
            }

            await EliminarConflictoAsync(comida.UsuarioId, comida.FechaAsignada, comida.Turno);
        }

        await AsegurarCupoComidasPlanificadasAsync(comida.UsuarioId);

        var entity = new ComidaPlanificada
        {
            UsuarioId = comida.UsuarioId,
            RecetaId = receta.Id,
            FechaAsignada = comida.FechaAsignada,
            Turno = comida.Turno
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

    public Task<bool> UpdateAsync(int id, ComidaPlanificadaUpdateDto comida, int usuarioId) =>
        EnTransaccionAsync(usuarioId, () => UpdateEnTransaccionAsync(id, comida, usuarioId));

    private async Task<bool> UpdateEnTransaccionAsync(int id, ComidaPlanificadaUpdateDto comida, int usuarioId)
    {
        var existingComida = await _repository.GetByIdAsync(id);

        if (existingComida is null || existingComida.UsuarioId != usuarioId)
        {
            return false;
        }

        var turno = TurnosComida.Normalizar(comida.Turno);
        var fecha = comida.FechaAsignada.Date;
        var fechaFin = fecha.AddDays(1);

        var receta = await ObtenerRecetaAsync(comida.RecetaId);
        await AsegurarRecetaVisibleAsync(usuarioId, receta);
        await ValidarHorizontePlanificacionAsync(usuarioId, fecha);

        if (await _repository.AnyAsync(existing =>
                existing.Id != id &&
                existing.UsuarioId == usuarioId &&
                existing.FechaAsignada >= fecha &&
                existing.FechaAsignada < fechaFin &&
                existing.Turno == turno))
        {
            throw new ConflictException("Ya existe una comida planificada para ese usuario, fecha y turno.");
        }

        var hoy = FechaLocal.Hoy();
        if (existingComida.FechaAsignada < hoy && fecha >= hoy)
        {
            await AsegurarCupoComidasPlanificadasAsync(usuarioId);
        }

        existingComida.UsuarioId = usuarioId;
        existingComida.RecetaId = comida.RecetaId;
        existingComida.FechaAsignada = fecha;
        existingComida.Turno = turno;

        await _repository.UpdateAsync(existingComida);
        await _listaCompraService.SincronizarFaltantesAsync(usuarioId);

        return true;
    }

    public Task<bool> DeleteAsync(int id, int usuarioId) =>
        EnTransaccionAsync(usuarioId, () => DeleteEnTransaccionAsync(id, usuarioId));

    private async Task<bool> DeleteEnTransaccionAsync(int id, int usuarioId)
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

        var hoy = FechaLocal.Hoy();
        var cantidad = await _repository.CountAsync(comida =>
            comida.UsuarioId == usuarioId && comida.FechaAsignada >= hoy);
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

        var hoy = FechaLocal.Hoy();
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

        if (!AccesoReceta.Permitido(receta, usuarioId, usuario.Rol))
        {
            throw new KeyNotFoundException(AccesoReceta.MensajeNoEncontrada);
        }

        if (!LimitesPlan.PuedeUsarDificultad(usuario.Rol, receta.Dificultad))
        {
            throw new ForbiddenException(LimitesPlan.MensajeDificultadPremium);
        }
    }

    private Task<IReadOnlyList<ComidaPlanificada>> ObtenerComidasConRecetaAsync(
        Expression<Func<ComidaPlanificada, bool>> predicate) =>
        _repository.FindWithIncludesAsync(predicate, comida => comida.Receta);

    private async Task EliminarConflictoAsync(int usuarioId, DateTime fecha, string turno)
    {
        var fechaInicio = fecha.Date;
        var fechaFin = fechaInicio.AddDays(1);
        var turnoNormalizado = turno.Trim();
        var existentes = await _repository.FindAsync(comida =>
            comida.UsuarioId == usuarioId &&
            comida.FechaAsignada >= fechaInicio &&
            comida.FechaAsignada < fechaFin &&
            comida.Turno == turnoNormalizado);

        foreach (var existente in existentes)
        {
            await _repository.DeleteAsync(existente);
        }
    }

    private static ComidaPlanificadaResponseDto CompletarRespuestaPlanificacion(
        PlanificacionResultadoDto resultado,
        bool reemplazoRealizado)
    {
        var dto = resultado.Comida;
        dto.IngredientesAgregadosALista = resultado.IngredientesAgregadosALista;
        dto.TeniaTodoEnAlacena = resultado.IngredientesAgregadosALista.Count == 0;
        dto.ReemplazoRealizado = reemplazoRealizado;
        dto.MensajeLista = dto.TeniaTodoEnAlacena
            ? "¡Tenés todo lo necesario en tu alacena!"
            : string.Join(", ", resultado.IngredientesAgregadosALista);
        return dto;
    }

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
