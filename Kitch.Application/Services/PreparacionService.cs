using Kitch.Application.DTOs.Preparacion;
using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class PreparacionService : IPreparacionService
{
    private readonly IRepository<Receta> _recetaRepository;
    private readonly IRepository<IngredienteReceta> _ingredienteRecetaRepository;
    private readonly IRepository<StockUsuario> _stockRepository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;

    public PreparacionService(
        IRepository<Receta> recetaRepository,
        IRepository<IngredienteReceta> ingredienteRecetaRepository,
        IRepository<StockUsuario> stockRepository,
        IRepository<Ingrediente> ingredienteRepository)
    {
        _recetaRepository = recetaRepository;
        _ingredienteRecetaRepository = ingredienteRecetaRepository;
        _stockRepository = stockRepository;
        _ingredienteRepository = ingredienteRepository;
    }

    public async Task<PrevisualizarPorcionesResponseDto> PrevisualizarRecalculoPorcionesAsync(PrevisualizarPorcionesRequestDto request)
    {
        var receta = await GetRecetaConIngredientesAsync(request.RecetaId);

        if (receta.Porciones <= 0)
        {
            throw new InvalidOperationException("La receta no tiene porciones validas para recalcular.");
        }

        var factor = (decimal)request.NuevasPorciones / receta.Porciones;

        return new PrevisualizarPorcionesResponseDto
        {
            Receta = receta.Titulo,
            PorcionesOriginales = receta.Porciones,
            NuevasPorciones = request.NuevasPorciones,
            Ingredientes = receta.IngredientesReceta
                .Select(ingrediente => new IngredienteAjustadoDto
                {
                    CantidadOriginal = ingrediente.Cantidad,
                    CantidadAjustada = Math.Round(ingrediente.Cantidad * factor, 2),
                    UnidadMedida = ingrediente.UnidadMedida
                })
                .ToList()
        };
    }

    public async Task<PrevisualizarDescuentoStockResponseDto> PrevisualizarDescuentoStockAsync(PrevisualizarDescuentoStockRequestDto request)
    {
        var receta = await GetRecetaConIngredientesAsync(request.RecetaId);

        if (receta.Porciones <= 0)
        {
            throw new InvalidOperationException("La receta no tiene porciones validas para calcular descuento.");
        }

        var factor = (decimal)request.PorcionesCocinadas / receta.Porciones;
        var stock = await _stockRepository.FindAsync(item => item.UsuarioId == request.UsuarioId);

        return new PrevisualizarDescuentoStockResponseDto
        {
            Receta = receta.Titulo,
            PorcionesCocinadas = request.PorcionesCocinadas,
            Ingredientes = receta.IngredientesReceta
                .Select(ingrediente =>
                {
                    var stockItem = stock.FirstOrDefault(item => item.IngredienteId == ingrediente.IngredienteId);
                    var cantidadDisponible = stockItem?.Cantidad ?? 0;
                    var cantidadNecesaria = Math.Round(ingrediente.Cantidad * factor, 2);
                    var cantidadPosterior = Math.Max(0, cantidadDisponible - cantidadNecesaria);

                    return new IngredienteDescuentoDto
                    {
                        CantidadDisponible = cantidadDisponible,
                        CantidadNecesaria = cantidadNecesaria,
                        CantidadPosterior = cantidadPosterior,
                        CantidadFaltante = Math.Max(0, cantidadNecesaria - cantidadDisponible),
                        UnidadMedida = ingrediente.UnidadMedida
                    };
                })
                .ToList()
        };
    }

    private async Task<Receta> GetRecetaConIngredientesAsync(int recetaId)
    {
        var recetas = await _recetaRepository.FindWithIncludesAsync(
            receta => receta.Id == recetaId,
            receta => receta.IngredientesReceta);

        var receta = recetas.FirstOrDefault();

        if (receta is null)
        {
            throw new InvalidOperationException("La receta no existe.");
        }

        if (receta.IngredientesReceta.Count == 0)
        {
            throw new InvalidOperationException("La receta debe tener al menos un ingrediente.");
        }

        return receta;
    }

    public async Task DescontarIngredientesAsync(int usuarioId, int recetaId, int porciones)
    {
        if (porciones <= 0)
        {
            throw new ArgumentException("Las porciones deben ser mayores a cero.", nameof(porciones));
        }

        var receta = await _recetaRepository.GetByIdAsync(recetaId)
            ?? throw new InvalidOperationException("La receta no existe.");

        var ingredientesReceta = await _ingredienteRecetaRepository.FindAsync(
            ingrediente => ingrediente.RecetaId == recetaId);

        if (ingredientesReceta.Count == 0)
        {
            throw new InvalidOperationException("La receta no tiene ingredientes cargados.");
        }

        // La receta declara una cantidad de ingredientes para "receta.Porciones" porciones base.
        // Escalamos a las porciones que el usuario realmente va a cocinar.
        var porcionesBase = receta.Porciones > 0 ? receta.Porciones : 1;
        var factor = (decimal)porciones / porcionesBase;

        var stockUsuario = await _stockRepository.FindAsync(stock => stock.UsuarioId == usuarioId);
        var stockPorIngrediente = stockUsuario.ToDictionary(stock => stock.IngredienteId);

        // Primero validamos que alcance para TODOS los ingredientes, así no descontamos a medias.
        var descuentos = new List<(StockUsuario Stock, decimal Cantidad)>();

        foreach (var ingrediente in ingredientesReceta)
        {
            var cantidadADescontar = ingrediente.Cantidad * factor;

            if (!stockPorIngrediente.TryGetValue(ingrediente.IngredienteId, out var stock))
            {
                throw new InvalidOperationException(
                    $"No hay stock cargado del ingrediente '{await ObtenerNombreIngredienteAsync(ingrediente.IngredienteId)}'.");
            }

            if (stock.Cantidad < cantidadADescontar)
            {
                throw new InvalidOperationException(
                    $"Stock insuficiente del ingrediente '{await ObtenerNombreIngredienteAsync(ingrediente.IngredienteId)}'. " +
                    $"Necesario: {cantidadADescontar}, disponible: {stock.Cantidad}.");
            }

            descuentos.Add((stock, cantidadADescontar));
        }

        foreach (var (stock, cantidad) in descuentos)
        {
            stock.Cantidad -= cantidad;
            await _stockRepository.UpdateAsync(stock);
        }
    }

    private async Task<string> ObtenerNombreIngredienteAsync(int ingredienteId)
    {
        var ingrediente = await _ingredienteRepository.GetByIdAsync(ingredienteId);
        return ingrediente?.Nombre ?? $"#{ingredienteId}";
    }
}
