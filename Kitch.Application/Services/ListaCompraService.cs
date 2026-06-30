using Kitch.Application.DTOs.ListaCompra;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class ListaCompraService : IListaCompraService
{
    private readonly IRepository<ItemListaCompra> _itemRepository;
    private readonly IRepository<ComidaPlanificada> _comidaRepository;
    private readonly IRepository<IngredienteReceta> _ingredienteRecetaRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;

    public ListaCompraService(
        IRepository<ItemListaCompra> itemRepository,
        IRepository<ComidaPlanificada> comidaRepository,
        IRepository<IngredienteReceta> ingredienteRecetaRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<Ingrediente> ingredienteRepository)
    {
        _itemRepository = itemRepository;
        _comidaRepository = comidaRepository;
        _ingredienteRecetaRepository = ingredienteRecetaRepository;
        _stockRepository = stockRepository;
        _ingredienteRepository = ingredienteRepository;
    }

    public async Task<IEnumerable<ItemListaCompraResponseDto>> GetByUsuarioIdAsync(int usuarioId)
    {
        var items = await _itemRepository.FindAsync(item => item.UsuarioId == usuarioId);
        return items.Select(item => item.ToResponseDto());
    }

    public async Task<ItemListaCompraResponseDto?> GetByIdAsync(int id, int usuarioId)
    {
        var item = await _itemRepository.GetByIdAsync(id);

        // Si el ítem no es tuyo, lo tratamos como inexistente (no revelamos que existe).
        if (item is null || item.UsuarioId != usuarioId)
        {
            return null;
        }

        return item.ToResponseDto();
    }

    public async Task<ItemListaCompraResponseDto> CreateAsync(ItemListaCompraCreateDto item)
    {
        var entity = new ItemListaCompra
        {
            UsuarioId = item.UsuarioId,
            NombreArticulo = item.NombreArticulo.Trim(),
            CantidadFaltante = item.CantidadFaltante,
            EstaComprado = false
        };

        var created = await _itemRepository.AddAsync(entity);
        return created.ToResponseDto();
    }

    public async Task<bool> UpdateAsync(int id, ItemListaCompraUpdateDto item, int usuarioId)
    {
        var existingItem = await _itemRepository.GetByIdAsync(id);

        // Solo podés editar tus propios ítems.
        if (existingItem is null || existingItem.UsuarioId != usuarioId)
        {
            return false;
        }

        existingItem.NombreArticulo = item.NombreArticulo.Trim();
        existingItem.CantidadFaltante = item.CantidadFaltante;
        existingItem.EstaComprado = item.EstaComprado;

        await _itemRepository.UpdateAsync(existingItem);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, int usuarioId)
    {
        var item = await _itemRepository.GetByIdAsync(id);

        // Solo podés borrar tus propios ítems.
        if (item is null || item.UsuarioId != usuarioId)
        {
            return false;
        }

        await _itemRepository.DeleteAsync(item);

        return true;
    }

    public async Task<bool> MarcarComoCompradoAsync(int id, int usuarioId)
    {
        var item = await _itemRepository.GetByIdAsync(id);

        // Solo podés marcar como comprado tus propios ítems.
        if (item is null || item.UsuarioId != usuarioId)
        {
            return false;
        }

        item.EstaComprado = true;
        await _itemRepository.UpdateAsync(item);

        return true;
    }

    public async Task<IEnumerable<ItemListaCompraResponseDto>> GenerarListaFaltantesAsync(int usuarioId)
    {
        // La planificación (incluida la que hace el asistente de IA) persiste sus
        // faltantes en ItemListaCompra. Este endpoint debe incluirlos aunque no haya
        // comidas en la semana actual.
        var itemsPersistidos = (await _itemRepository.FindAsync(item =>
                item.UsuarioId == usuarioId && !item.EstaComprado))
            .ToList();

        // 1. Qué tengo que cocinar esta semana
        var comidasDeLaSemana = await ObtenerComidasDeLaSemanaAsync(usuarioId);

        if (comidasDeLaSemana.Count == 0)
        {
            return itemsPersistidos.Select(item => item.ToResponseDto());
        }

        // 2. Cuánto necesito de cada ingrediente (Requerimiento Total)
        var requerimientoTotal = await CalcularRequerimientoTotalAsync(comidasDeLaSemana);

        // 3. Cuánto ya tengo en mi alacena (Stock Actual)
        var stockActual = await ObtenerStockActualAsync(usuarioId);

        // 4. Resto Requerimiento - Stock y me quedo solo con lo que da positivo
        var cantidadesFaltantes = CalcularFaltantes(requerimientoTotal, stockActual);

        if (cantidadesFaltantes.Count == 0)
        {
            return itemsPersistidos.Select(item => item.ToResponseDto());
        }

        // 5. Armo la lista final con el nombre real de cada ingrediente

        var faltantesCalculados = await ArmarListaDeCompraAsync(usuarioId, cantidadesFaltantes);

        // Los faltantes de una receta planificada por la IA ya están persistidos.
        // Evitamos mostrarlos dos veces al combinar ambas fuentes.
        var nombresPersistidos = itemsPersistidos
            .Select(item => item.NombreArticulo.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var faltantesNoPersistidos = faltantesCalculados
            .Where(item => !nombresPersistidos.Contains(item.NombreArticulo.Trim()));

        return itemsPersistidos
            .Concat(faltantesNoPersistidos)
            .Select(item => item.ToResponseDto())
            .ToList();
    }

    private async Task<List<ComidaPlanificada>> ObtenerComidasDeLaSemanaAsync(int usuarioId)
    {
        var (inicioSemana, finSemana) = ObtenerRangoSemanaActual();

        var comidas = await _comidaRepository.FindAsync(comida =>
            comida.UsuarioId == usuarioId &&
            comida.FechaAsignada >= inicioSemana &&
            comida.FechaAsignada < finSemana);

        return comidas.ToList();
    }

    private async Task<Dictionary<int, decimal>> CalcularRequerimientoTotalAsync(
        IReadOnlyCollection<ComidaPlanificada> comidas)
    {
        var recetaIds = comidas
            .Select(comida => comida.RecetaId)
            .Distinct()
            .ToList();

        var ingredientesDeRecetas = await _ingredienteRecetaRepository.FindAsync(ingrediente =>
            recetaIds.Contains(ingrediente.RecetaId));

        var ingredientesPorReceta = ingredientesDeRecetas
            .GroupBy(ingrediente => ingrediente.RecetaId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList());

        var requerimientoTotal = new Dictionary<int, decimal>();

        // Recorro CADA comida planificada: si una receta aparece 2 veces en la semana,
        // sus ingredientes se suman 2 veces.
        foreach (var comida in comidas)
        {
            if (!ingredientesPorReceta.TryGetValue(comida.RecetaId, out var ingredientes))
            {
                continue;
            }

            foreach (var ingrediente in ingredientes)
            {
                requerimientoTotal.TryGetValue(ingrediente.IngredienteId, out var acumulado);
                requerimientoTotal[ingrediente.IngredienteId] = acumulado + ingrediente.Cantidad;
            }
        }

        return requerimientoTotal;
    }

    private async Task<Dictionary<int, decimal>> ObtenerStockActualAsync(int usuarioId)
    {
        var stock = await _stockRepository.FindAsync(item => item.UsuarioId == usuarioId);

        return stock.ToDictionary(item => item.IngredienteId, item => item.Cantidad);
    }

    private static Dictionary<int, decimal> CalcularFaltantes(
        Dictionary<int, decimal> requerimientoTotal,
        Dictionary<int, decimal> stockActual)
    {
        var faltantes = new Dictionary<int, decimal>();

        foreach (var (ingredienteId, cantidadRequerida) in requerimientoTotal)
        {
            stockActual.TryGetValue(ingredienteId, out var cantidadEnStock);

            var cantidadFaltante = cantidadRequerida - cantidadEnStock;

            if (cantidadFaltante > 0)
            {
                faltantes[ingredienteId] = cantidadFaltante;
            }
        }

        return faltantes;
    }

    private async Task<List<ItemListaCompra>> ArmarListaDeCompraAsync(
        int usuarioId,
        Dictionary<int, decimal> cantidadesFaltantes)
    {
        var ingredienteIds = cantidadesFaltantes.Keys.ToList();

        var ingredientes = await _ingredienteRepository.FindAsync(ingrediente =>
            ingredienteIds.Contains(ingrediente.Id));

        var nombrePorIngrediente = ingredientes.ToDictionary(
            ingrediente => ingrediente.Id,
            ingrediente => ingrediente.Nombre);

        return cantidadesFaltantes
            .Select(faltante => new ItemListaCompra
            {
                UsuarioId = usuarioId,
                NombreArticulo = nombrePorIngrediente.TryGetValue(faltante.Key, out var nombre)
                    ? nombre
                    : $"Ingrediente #{faltante.Key}",
                CantidadFaltante = (float)faltante.Value,
                EstaComprado = false
            })
            .ToList();
    }

    private static (DateTime inicioSemana, DateTime finSemana) ObtenerRangoSemanaActual()
    {
        var hoy = DateTime.Today;

        // DayOfWeek arranca en Domingo (0); con este cálculo el Lunes queda como inicio.
        var diasDesdeLunes = ((int)hoy.DayOfWeek + 6) % 7;

        var inicioSemana = hoy.AddDays(-diasDesdeLunes);
        var finSemana = inicioSemana.AddDays(7);

        return (inicioSemana, finSemana);
    }
}
