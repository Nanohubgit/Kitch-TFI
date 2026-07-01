using Kitch.Application.Interfaces;
using Kitch.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Kitch.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IRecetaService, RecetaService>();
        services.AddScoped<IPagoService, PagoService>();
        services.AddScoped<ISuscripcionService, SuscripcionService>();
        services.AddScoped<IContratoSubService, ContratoSubService>();
        services.AddScoped<IStockUsuarioService, StockUsuarioService>();
        services.AddScoped<IPlanificadorService, PlanificadorService>();
        services.AddScoped<IListaCompraService, ListaCompraService>();
        services.AddScoped<IFavoritoService, FavoritoService>();
        services.AddScoped<IPreparacionService, PreparacionService>();
        services.AddScoped<ISustitucionService, SustitucionService>();
        services.AddScoped<ISustitutoService, SustitutoService>();
        services.AddScoped<IIngredienteService, IngredienteService>();
        services.AddScoped<IChatIaService, ChatIaService>();
        services.AddScoped<IRecetaIaService, RecetaIaService>();
        services.AddScoped<IRecomendacionService, RecomendacionService>();

        return services;
    }
}
