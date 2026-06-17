using Kitch.Application.Interfaces;
using Kitch.Domain.Interfaces;
using Kitch.Infrastructure.Repositories;
using Kitch.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Kitch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IRecetaService, RecetaService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPagoService, PagoService>();
        services.AddScoped<ISuscripcionService, SuscripcionService>();
        services.AddScoped<IContratoSubService, ContratoSubService>();
        services.AddScoped<IStockUsuarioService, StockUsuarioService>();
        services.AddScoped<IPlanificadorService, PlanificadorService>();
        services.AddScoped<IListaCompraService, ListaCompraService>();
        services.AddScoped<IFavoritoService, FavoritoService>();

        return services;
    }
}
