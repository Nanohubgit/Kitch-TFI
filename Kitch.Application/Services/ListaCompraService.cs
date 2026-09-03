using Kitch.Application.DTOs.ListaCompra;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class ListaCompraService : IListaCompraService
{
    private const float ToleranciaCantidad = 0.001f;

    private readonly IRepository<ItemListaCompra> _itemRepository;
    private readonly IRepository<ComidaPlanificada> _comidaRepository;
    private readonly IRepository<IngredienteReceta> _ingredienteRecetaRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;
    private readonly IIngredienteNormalizerService _normalizer;

    public ListaCompraService(
        IRepository<ItemListaCompra> itemRepository,
        IRepository<ComidaPlanificada> comidaRepository,
        IRepository<IngredienteReceta> ingredienteRecetaRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<Ingrediente> ingredienteRepository,
        IIngredienteNormalizerService normalizer)
    {
        _itemRepository = itemRepository;
        _comidaRepository = comidaRepository;
        _ingredienteRecetaRepository = ingredienteRecetaRepository;
        _stockRepository = stockRepository;
        _ingredienteRepository = ingredienteRepository;
        _normalizer = normalizer;
    }

    public async Task<IEnumerable<ItemListaCompraResponseDto>> GetByUsuarioIdAsync(int usuarioId)
    {
        var items = await _itemRepository.FindAsync(item => item.UsuarioId == usuarioId);
        return items
            .OrderBy(item => item.EstaComprado)
            .ThenBy(item => item.NombreArticulo)
            .Select(item => item.ToResponseDto());
    }

    public async Task<ItemListaCompraResponseDto?> GetByIdAsync(int id, int usuarioId)
    {
        var item = await _itemRepository.GetByIdAsync(id);

        if (item is null || item.UsuarioId != usuarioId)
        {
            return null;
        }

        return item.ToResponseDto();
    }

    public async Task<ItemListaCompraResponseDto> CreateAsync(ItemListaCompraCreateDto item)
    {
        var nombre = item.NombreArticulo.Trim();
        var unidad = (item.UnidadMedida ?? string.Empty).Trim();

        var pendientes = await _itemRepository.FindAsync(existente =>
            existente.UsuarioId == item.UsuarioId && !existente.EstaComprado);

        var duplicado = pendientes.FirstOrDefault(existente =>
            CorrespondeAlMismoArticulo(existente, item.IngredienteId, nombre));

        if (duplicado is not null)
        {
            duplicado.CantidadFaltante += item.CantidadFaltante;
            duplicado.NombreArticulo = nombre;
            duplicado.IngredienteId ??= item.IngredienteId;
            if (string.IsNullOrWhiteSpace(duplicado.UnidadMedida) && unidad.Length > 0)
            {
                duplicado.UnidadMedida = unidad;
            }

            await _itemRepository.UpdateAsync(duplicado);
            return duplicado.ToResponseDto();
        }

        var entity = new ItemListaCompra
        {
            UsuarioId = item.UsuarioId,
            IngredienteId = item.IngredienteId,
            NombreArticulo = nombre,
            UnidadMedida = unidad,
            CantidadFaltante = item.CantidadFaltante,
            EstaComprado = false
        };

        var created = await _itemRepository.AddAsync(entity);
        return created.ToResponseDto();
    }

    public async Task<bool> UpdateAsync(int id, ItemListaCompraUpdateDto item, int usuarioId)
    {
        var existingItem = await _itemRepository.GetByIdAsync(id);

        if (existingItem is null || existingItem.UsuarioId != usuarioId)
        {
            return false;
        }

        existingItem.NombreArticulo = item.NombreArticulo.Trim();
        existingItem.CantidadFaltante = item.CantidadFaltante;
        existingItem.EstaComprado = item.EstaComprado;
        if (!string.IsNullOrWhiteSpace(item.UnidadMedida))
        {
            existingItem.UnidadMedida = item.UnidadMedida.Trim();
        }

        if (item.IngredienteId.HasValue)
        {
            existingItem.IngredienteId = item.IngredienteId;
        }

        await _itemRepository.UpdateAsync(existingItem);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, int usuarioId)
    {
        var item = await _itemRepository.GetByIdAsync(id);

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

        if (item is null || item.UsuarioId != usuarioId)
        {
            return false;
        }

        item.EstaComprado = true;
        await _itemRepository.UpdateAsync(item);
        await SincronizarFaltantesAsync(usuarioId);

        return true;
    }

    public async Task<IEnumerable<ItemListaCompraResponseDto>> SincronizarFaltantesAsync(int usuarioId)
    {
        var items = (await _itemRepository.FindAsync(item => item.UsuarioId == usuarioId)).ToList();
        var necesidades = await CalcularNecesidadesDelPlanAsync(usuarioId);
        var stockActual = await ObtenerStockActualAsync(usuarioId);
        var reclamados = new HashSet<int>();

        foreach (var necesidad in necesidades.Values)
        {
            stockActual.TryGetValue(necesidad.IngredienteId, out var enStock);
            var aComprar = Math.Max(0m, necesidad.Cantidad - enStock);

            var coincidencias = items
                .Where(item => ItemCorrespondeAIngrediente(item, necesidad))
                .ToList();

            var comprados = coincidencias.Where(item => item.EstaComprado).ToList();
            var pendientes = coincidencias.Where(item => !item.EstaComprado).ToList();

            foreach (var pendiente in pendientes)
            {
                reclamados.Add(pendiente.Id);
            }

            foreach (var comprado in comprados.Where(item => item.IngredienteId is null))
            {
                comprado.IngredienteId = necesidad.IngredienteId;
                if (string.IsNullOrWhiteSpace(comprado.UnidadMedida) && necesidad.UnidadMedida.Length > 0)
                {
                    comprado.UnidadMedida = necesidad.UnidadMedida;
                }

                await _itemRepository.UpdateAsync(comprado);
            }

            var yaComprado = comprados.Sum(item => (decimal)item.CantidadFaltante);
            var pendienteNeto = aComprar - yaComprado;

            if (pendienteNeto > (decimal)ToleranciaCantidad)
            {
                await UpsertPendienteAsync(usuarioId, items, pendientes, necesidad, pendienteNeto);
            }
            else
            {
                await EliminarPendientesAsync(items, pendientes);
            }
        }

        var obsoletos = items
            .Where(item =>
                !item.EstaComprado &&
                item.IngredienteId.HasValue &&
                !reclamados.Contains(item.Id) &&
                !necesidades.ContainsKey(item.IngredienteId.Value))
            .ToList();

        await EliminarPendientesAsync(items, obsoletos);

        return items
            .Where(item => !item.EstaComprado)
            .OrderBy(item => item.NombreArticulo)
            .Select(item => item.ToResponseDto())
            .ToList();
    }

    private async Task UpsertPendienteAsync(
        int usuarioId,
        List<ItemListaCompra> items,
        List<ItemListaCompra> pendientes,
        NecesidadIngrediente necesidad,
        decimal cantidad)
    {
        var cantidadRedondeada = (float)Math.Round((double)cantidad, 2);
        var keeper = pendientes
            .OrderByDescending(item => item.IngredienteId.HasValue)
            .ThenBy(item => item.Id)
            .FirstOrDefault();

        if (keeper is null)
        {
            var creado = await _itemRepository.AddAsync(new ItemListaCompra
            {
                UsuarioId = usuarioId,
                IngredienteId = necesidad.IngredienteId,
                NombreArticulo = necesidad.Nombre,
                UnidadMedida = necesidad.UnidadMedida,
                CantidadFaltante = cantidadRedondeada,
                EstaComprado = false
            });

            items.Add(creado);
            return;
        }

        var extras = pendientes.Where(item => item.Id != keeper.Id).ToList();
        await EliminarPendientesAsync(items, extras);

        var cambio =
            keeper.IngredienteId != necesidad.IngredienteId ||
            !string.Equals(keeper.NombreArticulo, necesidad.Nombre, StringComparison.Ordinal) ||
            !string.Equals(keeper.UnidadMedida, necesidad.UnidadMedida, StringComparison.Ordinal) ||
            Math.Abs(keeper.CantidadFaltante - cantidadRedondeada) > ToleranciaCantidad;

        if (!cambio)
        {
            return;
        }

        keeper.IngredienteId = necesidad.IngredienteId;
        keeper.NombreArticulo = necesidad.Nombre;
        keeper.UnidadMedida = necesidad.UnidadMedida;
        keeper.CantidadFaltante = cantidadRedondeada;
        await _itemRepository.UpdateAsync(keeper);
    }

    private async Task EliminarPendientesAsync(List<ItemListaCompra> items, List<ItemListaCompra> pendientes)
    {
        foreach (var pendiente in pendientes)
        {
            await _itemRepository.DeleteAsync(pendiente);
            items.Remove(pendiente);
        }
    }

    private async Task<Dictionary<int, NecesidadIngrediente>> CalcularNecesidadesDelPlanAsync(int usuarioId)
    {
        var hoy = DateTime.UtcNow.Date;
        var comidas = await _comidaRepository.FindAsync(comida =>
            comida.UsuarioId == usuarioId &&
            comida.FechaAsignada >= hoy);

        if (comidas.Count == 0)
        {
            return new Dictionary<int, NecesidadIngrediente>();
        }

        var recetaIds = comidas.Select(comida => comida.RecetaId).Distinct().ToList();
        var ingredientesDeRecetas = await _ingredienteRecetaRepository.FindAsync(ingrediente =>
            recetaIds.Contains(ingrediente.RecetaId));

        var ingredientesPorReceta = ingredientesDeRecetas
            .GroupBy(ingrediente => ingrediente.RecetaId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList());

        var necesidades = new Dictionary<int, NecesidadIngrediente>();

        foreach (var comida in comidas)
        {
            if (!ingredientesPorReceta.TryGetValue(comida.RecetaId, out var ingredientes))
            {
                continue;
            }

            foreach (var ingrediente in ingredientes)
            {
                if (!necesidades.TryGetValue(ingrediente.IngredienteId, out var necesidad))
                {
                    necesidad = new NecesidadIngrediente
                    {
                        IngredienteId = ingrediente.IngredienteId,
                        UnidadMedida = (ingrediente.UnidadMedida ?? string.Empty).Trim()
                    };
                    necesidades[ingrediente.IngredienteId] = necesidad;
                }

                necesidad.Cantidad += ingrediente.Cantidad;
                if (string.IsNullOrWhiteSpace(necesidad.UnidadMedida)
                    && !string.IsNullOrWhiteSpace(ingrediente.UnidadMedida))
                {
                    necesidad.UnidadMedida = ingrediente.UnidadMedida.Trim();
                }
            }
        }

        if (necesidades.Count == 0)
        {
            return necesidades;
        }

        var ingredienteIds = necesidades.Keys.ToList();
        var catalogo = await _ingredienteRepository.FindAsync(ingrediente =>
            ingredienteIds.Contains(ingrediente.Id));
        var nombrePorId = catalogo.ToDictionary(ingrediente => ingrediente.Id, ingrediente => ingrediente.Nombre);

        foreach (var necesidad in necesidades.Values)
        {
            necesidad.Nombre = nombrePorId.TryGetValue(necesidad.IngredienteId, out var nombre)
                ? nombre
                : $"Ingrediente #{necesidad.IngredienteId}";
            necesidad.NombreNormalizado = NormalizarNombre(necesidad.Nombre);
        }

        return necesidades;
    }

    private async Task<Dictionary<int, decimal>> ObtenerStockActualAsync(int usuarioId)
    {
        var stock = await _stockRepository.FindAsync(item => item.UsuarioId == usuarioId);

        return stock
            .GroupBy(item => item.IngredienteId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(item => item.Cantidad));
    }

    private bool ItemCorrespondeAIngrediente(ItemListaCompra item, NecesidadIngrediente necesidad)
    {
        if (item.IngredienteId == necesidad.IngredienteId)
        {
            return true;
        }

        if (item.IngredienteId is not null)
        {
            return false;
        }

        var nombreItem = NormalizarNombre(item.NombreArticulo);
        return nombreItem.Length > 0 && nombreItem == necesidad.NombreNormalizado;
    }

    private bool CorrespondeAlMismoArticulo(ItemListaCompra existente, int? ingredienteId, string nombre)
    {
        if (ingredienteId is int id && existente.IngredienteId == id)
        {
            return true;
        }

        var nombreExistente = NormalizarNombre(existente.NombreArticulo);
        var nombreNuevo = NormalizarNombre(nombre);
        return nombreExistente.Length > 0 && nombreExistente == nombreNuevo;
    }

    private string NormalizarNombre(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return string.Empty;
        }

        return _normalizer.Normalizar(nombre);
    }

    private sealed class NecesidadIngrediente
    {
        public int IngredienteId { get; init; }
        public string Nombre { get; set; } = string.Empty;
        public string NombreNormalizado { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
    }
}
