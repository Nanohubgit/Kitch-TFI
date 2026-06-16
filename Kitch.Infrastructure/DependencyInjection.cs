using Kitch.Application.Interfaces;
using Kitch.Domain.Interfaces;
using Kitch.Infrastructure.Persistence;
using Kitch.Infrastructure.Repositories;
using Kitch.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kitch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<KitchDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IRecetaService, RecetaService>();
        services.AddScoped<IStockUsuarioService, StockUsuarioService>();
        services.AddScoped<ISuscripcionService, SuscripcionService>();
        services.AddScoped<IPagoService, PagoService>();
        services.AddScoped<IFavoritoService, FavoritoService>();
        services.AddScoped<IPlanificadorService, PlanificadorService>();
        services.AddScoped<IListaCompraService, ListaCompraService>();
        services.AddScoped<IContratoSubService, ContratoSubService>();

        return services;
    }
}