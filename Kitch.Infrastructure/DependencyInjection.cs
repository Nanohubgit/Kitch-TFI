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

        // AuthService se queda en Infrastructure porque maneja JWT (detalle de infraestructura)
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
