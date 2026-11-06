using Kitch.Application.DTOs.ListaCompra;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class ListaCompraService : IListaCompraService
{
    private const decimal ToleranciaCantidad = 0.01m;

    private readonly IRepository<ItemListaCompra> _itemRepository;
    private readonly IRepository<ComidaPlanificada> _comidaRepository;
    private readonly IRepository<IngredienteReceta> _ingredienteRecetaRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;
    private readonly IIngredienteNormalizerService _normalizer;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    public ListaCompraService(
        IRepository<ItemListaCompra> itemRepository,
        IRepository<ComidaPlanificada> comidaRepository,
        IRepository<IngredienteReceta> ingredienteRecetaRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<Ingrediente> ingredienteRepository,
        IIngredienteNormalizerService normalizer,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
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
        return await MapearConCategoriaAsync(items
            .OrderBy(item => item.EstaComprado)
            .ThenBy(item => item.NombreArticulo));
    }

    public async Task<ItemListaCompraResponseDto?> GetByIdAsync(int id, int usuarioId)
    {
        var item = await _itemRepository.GetByIdAsync(id);

        if (item is null || item.UsuarioId != usuarioId)
        {
            return null;
        }

        return await MapearUnoConCategoriaAsync(item);
    }

    public Task<ItemListaCompraResponseDto> CreateAsync(ItemListaCompraCreateDto item) =>
        EnTransaccionAsync(item.UsuarioId, () => CreateEnTransaccionAsync(item));

    private async Task<ItemListaCompraResponseDto> CreateEnTransaccionAsync(ItemListaCompraCreateDto item)
    {
        var nombre = item.NombreArticulo.Trim();
        var unidad = NormalizarUnidad(item.UnidadMedida);

        if (nombre.Length == 0)
        {
            throw new ArgumentException("El nombre del artículo es obligatorio.");
        }

        if (item.CantidadFaltante <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor a cero.");
        }

        await ValidarIngredienteExistenteAsync(item.IngredienteId);
        var ingredienteId = item.IngredienteId ?? await ResolverIngredientePorNombreAsync(nombre);

        var manualesPendientes = await _itemRepository.FindAsync(existente =>
            existente.UsuarioId == item.UsuarioId && !existente.EstaComprado && !existente.GeneradoPorPlan);

        var duplicado = manualesPendientes.FirstOrDefault(existente =>
            CorrespondeAlMismoArticulo(existente, ingredienteId, nombre) &&
            UnidadesMedida.SonCompatibles(existente.UnidadMedida, unidad));

        if (duplicado is not null &&
            UnidadesMedida.TryConvertir((decimal)item.CantidadFaltante, unidad, duplicado.UnidadMedida, out var sumada))
        {
            duplicado.CantidadFaltante = (float)Math.Round((decimal)duplicado.CantidadFaltante + sumada, 2);
            duplicado.IngredienteId ??= ingredienteId;
            await _itemRepository.UpdateAsync(duplicado);
            return await MapearUnoConCategoriaAsync(duplicado);
        }

        var entity = new ItemListaCompra
        {
            UsuarioId = item.UsuarioId,
            IngredienteId = ingredienteId,
            NombreArticulo = nombre,
            UnidadMedida = unidad,
            CantidadFaltante = item.CantidadFaltante,
            EstaComprado = false,
            GeneradoPorPlan = false
        };

        var created = await _itemRepository.AddAsync(entity);
        return await MapearUnoConCategoriaAsync(created);
    }

    public Task<bool> UpdateAsync(int id, ItemListaCompraUpdateDto item, int usuarioId) =>
        EnTransaccionAsync(usuarioId, () => UpdateEnTransaccionAsync(id, item, usuarioId));

    private async Task<bool> UpdateEnTransaccionAsync(int id, ItemListaCompraUpdateDto item, int usuarioId)
    {
        var existingItem = await _itemRepository.GetByIdAsync(id);

        if (existingItem is null || existingItem.UsuarioId != usuarioId)
        {
            return false;
        }

        var nombre = item.NombreArticulo?.Trim() ?? string.Empty;
        if (nombre.Length == 0)
        {
            throw new ArgumentException("El nombre del artículo es obligatorio.");
        }

        if (item.CantidadFaltante <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor a cero.");
        }

        await ValidarIngredienteExistenteAsync(item.IngredienteId);

        var pasaAComprado = !existingItem.EstaComprado && item.EstaComprado;

        existingItem.NombreArticulo = nombre;
        existingItem.CantidadFaltante = item.CantidadFaltante;
        existingItem.EstaComprado = item.EstaComprado;
        existingItem.GeneradoPorPlan = false;
        if (!string.IsNullOrWhiteSpace(item.UnidadMedida))
        {
            existingItem.UnidadMedida = NormalizarUnidad(item.UnidadMedida);
        }

        if (item.IngredienteId.HasValue)
        {
            existingItem.IngredienteId = item.IngredienteId;
        }

        if (pasaAComprado)
        {
            existingItem.IngredienteId ??= await ResolverIngredientePorNombreAsync(existingItem.NombreArticulo);
        }

        await _itemRepository.UpdateAsync(existingItem);

        if (pasaAComprado)
        {
            await SumarCompradoAlStockAsync(usuarioId, existingItem);
            await SincronizarFaltantesAsync(usuarioId);
        }

        return true;
    }

    private async Task ValidarIngredienteExistenteAsync(int? ingredienteId)
    {
        if (ingredienteId is int id && !await _ingredienteRepository.AnyAsync(ingrediente => ingrediente.Id == id))
        {
            throw new ArgumentException($"El ingrediente con id {id} no existe. Indicá el artículo por nombre.");
        }
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

    public Task<bool> MarcarComoCompradoAsync(int id, int usuarioId) =>
        EnTransaccionAsync(usuarioId, () => MarcarComoCompradoEnTransaccionAsync(id, usuarioId));

    private async Task<bool> MarcarComoCompradoEnTransaccionAsync(int id, int usuarioId)
    {
        var item = await _itemRepository.GetByIdAsync(id);

        if (item is null || item.UsuarioId != usuarioId)
        {
            return false;
        }

        if (item.EstaComprado)
        {
            return true;
        }

        item.EstaComprado = true;
        item.IngredienteId ??= await ResolverIngredientePorNombreAsync(item.NombreArticulo);
        await _itemRepository.UpdateAsync(item);
        await SumarCompradoAlStockAsync(usuarioId, item);
        await SincronizarFaltantesAsync(usuarioId);

        return true;
    }

    private async Task SumarCompradoAlStockAsync(int usuarioId, ItemListaCompra item)
    {
        if (item.IngredienteId is not int ingredienteId || item.CantidadFaltante <= 0)
        {
            return;
        }

        var cantidad = Math.Round((decimal)item.CantidadFaltante, 2);
        var unidad = NormalizarUnidad(item.UnidadMedida);
        var existente = await _stockRepository.FirstOrDefaultAsync(stock =>
            stock.UsuarioId == usuarioId && stock.IngredienteId == ingredienteId);

        if (existente is not null)
        {
            if (existente.Cantidad <= 0)
            {
                existente.Cantidad = cantidad;
                existente.UnidadMedida = unidad;
            }
            else if (UnidadesMedida.TryConvertir(cantidad, unidad, existente.UnidadMedida, out var convertida))
            {
                existente.Cantidad = Math.Round(existente.Cantidad + convertida, 2);
            }
            else
            {
                return;
            }

            await _stockRepository.UpdateAsync(existente);
            return;
        }

        await _stockRepository.AddAsync(new StockUsuario
        {
            UsuarioId = usuarioId,
            IngredienteId = ingredienteId,
            Cantidad = cantidad,
            UnidadMedida = unidad
        });
    }

    public Task<IEnumerable<ItemListaCompraResponseDto>> SincronizarFaltantesAsync(int usuarioId) =>
        EnTransaccionAsync(usuarioId, () => SincronizarEnTransaccionAsync(usuarioId));

    private Task<T> EnTransaccionAsync<T>(int usuarioId, Func<Task<T>> operacion) =>
        _unidadDeTrabajo.EjecutarAsync(async () =>
        {
            await _unidadDeTrabajo.BloquearRecursoAsync($"kitch:stock:usuario:{usuarioId}");
            return await operacion();
        });

    private async Task<IEnumerable<ItemListaCompraResponseDto>> SincronizarEnTransaccionAsync(int usuarioId)
    {
        var items = (await _itemRepository.FindAsync(item => item.UsuarioId == usuarioId)).ToList();
        var necesidades = await CalcularNecesidadesDelPlanAsync(usuarioId);
        var stockPorIngrediente = await ObtenerStockPorIngredienteAsync(usuarioId);
        var autoReclamados = new HashSet<int>();

        foreach (var necesidad in necesidades)
        {
            var aComprar = necesidad.Cantidad;

            if (stockPorIngrediente.TryGetValue(necesidad.IngredienteId, out var stock) &&
                stock.Cantidad > 0 &&
                UnidadesMedida.TryConvertir(stock.Cantidad, stock.UnidadMedida, necesidad.UnidadMedida, out var enStock))
            {
                aComprar -= enStock;
            }

            var coincidencias = items
                .Where(item => !item.EstaComprado && ItemCorrespondeAIngrediente(item, necesidad))
                .ToList();

            foreach (var manual in coincidencias.Where(item => !item.GeneradoPorPlan))
            {
                if (UnidadesMedida.TryConvertir((decimal)manual.CantidadFaltante, manual.UnidadMedida, necesidad.UnidadMedida, out var yaAnotado))
                {
                    aComprar -= yaAnotado;
                }
            }

            var automaticos = coincidencias.Where(item => item.GeneradoPorPlan).ToList();

            if (aComprar > ToleranciaCantidad)
            {
                var keeper = await UpsertAutomaticoAsync(usuarioId, items, automaticos, necesidad, aComprar);
                autoReclamados.Add(keeper.Id);
            }
            else
            {
                await EliminarAsync(items, automaticos);
            }
        }

        var obsoletos = items
            .Where(item => item.GeneradoPorPlan && !item.EstaComprado && !autoReclamados.Contains(item.Id))
            .ToList();

        await EliminarAsync(items, obsoletos);

        return await MapearConCategoriaAsync(items
            .Where(item => !item.EstaComprado)
            .OrderBy(item => item.NombreArticulo));
    }

    public async Task<ListaCompraResumenDto> GetResumenAsync(int usuarioId)
    {
        var items = (await SincronizarFaltantesAsync(usuarioId)).ToList();
        var todoCubierto = items.Count == 0;
        return new ListaCompraResumenDto
        {
            TodoCubierto = todoCubierto,
            Mensaje = todoCubierto
                ? "¡Tenés todo lo necesario en tu alacena!"
                : "Esta es la lista de lo que te falta para el menú planificado.",
            Items = items
        };
    }

    private async Task<ItemListaCompra> UpsertAutomaticoAsync(
        int usuarioId,
        List<ItemListaCompra> items,
        List<ItemListaCompra> automaticos,
        NecesidadIngrediente necesidad,
        decimal cantidad)
    {
        var cantidadRedondeada = (float)Math.Round(cantidad, 2);
        var keeper = automaticos
            .OrderBy(item => item.Id)
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
                EstaComprado = false,
                GeneradoPorPlan = true
            });

            items.Add(creado);
            return creado;
        }

        await EliminarAsync(items, automaticos.Where(item => item.Id != keeper.Id).ToList());

        var cambio =
            keeper.IngredienteId != necesidad.IngredienteId ||
            !string.Equals(keeper.NombreArticulo, necesidad.Nombre, StringComparison.Ordinal) ||
            !string.Equals(keeper.UnidadMedida, necesidad.UnidadMedida, StringComparison.Ordinal) ||
            Math.Abs((decimal)keeper.CantidadFaltante - (decimal)cantidadRedondeada) > ToleranciaCantidad;

        if (cambio)
        {
            keeper.IngredienteId = necesidad.IngredienteId;
            keeper.NombreArticulo = necesidad.Nombre;
            keeper.UnidadMedida = necesidad.UnidadMedida;
            keeper.CantidadFaltante = cantidadRedondeada;
            await _itemRepository.UpdateAsync(keeper);
        }

        return keeper;
    }

    private async Task EliminarAsync(List<ItemListaCompra> items, List<ItemListaCompra> aEliminar)
    {
        foreach (var item in aEliminar)
        {
            await _itemRepository.DeleteAsync(item);
            items.Remove(item);
        }
    }

    private async Task<List<NecesidadIngrediente>> CalcularNecesidadesDelPlanAsync(int usuarioId)
    {
        var hoy = FechaLocal.Hoy();
        var comidas = await _comidaRepository.FindAsync(comida =>
            comida.UsuarioId == usuarioId &&
            comida.FechaAsignada >= hoy);

        if (comidas.Count == 0)
        {
            return [];
        }

        var recetaIds = comidas.Select(comida => comida.RecetaId).Distinct().ToList();
        var ingredientesDeRecetas = await _ingredienteRecetaRepository.FindAsync(ingrediente =>
            recetaIds.Contains(ingrediente.RecetaId));

        var ingredientesPorReceta = ingredientesDeRecetas
            .GroupBy(ingrediente => ingrediente.RecetaId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList());

        var necesidades = new Dictionary<(int IngredienteId, string Dimension), NecesidadIngrediente>();

        foreach (var comida in comidas)
        {
            if (!ingredientesPorReceta.TryGetValue(comida.RecetaId, out var ingredientes))
            {
                continue;
            }

            foreach (var ingrediente in ingredientes)
            {
                var unidad = NormalizarUnidad(ingrediente.UnidadMedida);
                var clave = (ingrediente.IngredienteId, UnidadesMedida.Dimension(unidad));

                if (!necesidades.TryGetValue(clave, out var necesidad))
                {
                    necesidad = new NecesidadIngrediente
                    {
                        IngredienteId = ingrediente.IngredienteId,
                        UnidadMedida = unidad
                    };
                    necesidades[clave] = necesidad;
                }

                if (UnidadesMedida.TryConvertir(ingrediente.Cantidad, unidad, necesidad.UnidadMedida, out var convertida))
                {
                    necesidad.Cantidad += convertida;
                }
            }
        }

        if (necesidades.Count == 0)
        {
            return [];
        }

        var ingredienteIds = necesidades.Values.Select(necesidad => necesidad.IngredienteId).Distinct().ToList();
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

        return necesidades.Values.ToList();
    }

    private async Task<Dictionary<int, StockUsuario>> ObtenerStockPorIngredienteAsync(int usuarioId)
    {
        var stock = await _stockRepository.FindAsync(item => item.UsuarioId == usuarioId);

        return stock
            .GroupBy(item => item.IngredienteId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First());
    }

    private async Task<int?> ResolverIngredientePorNombreAsync(string? nombre)
    {
        var normalizado = NormalizarNombre(nombre);
        if (normalizado.Length == 0)
        {
            return null;
        }

        var ingrediente = await _ingredienteRepository.FirstOrDefaultAsync(existente => existente.Nombre == normalizado);
        return ingrediente?.Id;
    }

    private bool ItemCorrespondeAIngrediente(ItemListaCompra item, NecesidadIngrediente necesidad)
    {
        if (!UnidadesMedida.SonCompatibles(item.UnidadMedida, necesidad.UnidadMedida))
        {
            return false;
        }

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

    private static string NormalizarUnidad(string? unidad)
    {
        var limpia = unidad?.Trim() ?? string.Empty;
        if (limpia.Length == 0)
        {
            return UnidadesMedida.Unidad;
        }

        return limpia.Length > 50 ? limpia[..50] : limpia;
    }

    private string NormalizarNombre(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return string.Empty;
        }

        return _normalizer.Normalizar(nombre);
    }

    private async Task<ItemListaCompraResponseDto> MapearUnoConCategoriaAsync(ItemListaCompra item)
    {
        var mapped = await MapearConCategoriaAsync([item]);
        return mapped[0];
    }

    private async Task<List<ItemListaCompraResponseDto>> MapearConCategoriaAsync(IEnumerable<ItemListaCompra> items)
    {
        var lista = items.ToList();
        var ids = lista
            .Where(item => item.IngredienteId.HasValue)
            .Select(item => item.IngredienteId!.Value)
            .Distinct()
            .ToList();

        var categorias = new Dictionary<int, string>();
        if (ids.Count > 0)
        {
            var ingredientes = await _ingredienteRepository.FindAsync(ingrediente => ids.Contains(ingrediente.Id));
            categorias = ingredientes.ToDictionary(
                ingrediente => ingrediente.Id,
                ingrediente => string.IsNullOrWhiteSpace(ingrediente.Categoria) ? "Varios" : ingrediente.Categoria);
        }

        return lista.Select(item =>
        {
            var dto = item.ToResponseDto();
            if (item.IngredienteId is int id && categorias.TryGetValue(id, out var categoria))
            {
                dto.Categoria = categoria;
            }

            return dto;
        }).ToList();
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
