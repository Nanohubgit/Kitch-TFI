using Kitch.Application.DTOs.Preparacion;
using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Infrastructure.Services;

public class PreparacionService : IPreparacionService
{
    private readonly IRepository<Receta> _recetaRepository;
    private readonly IRepository<StockUsuario> _stockRepository;

    public PreparacionService(
        IRepository<Receta> recetaRepository,
        IRepository<StockUsuario> stockRepository)
    {
        _recetaRepository = recetaRepository;
        _stockRepository = stockRepository;
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
}
