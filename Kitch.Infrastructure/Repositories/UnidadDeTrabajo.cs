using Kitch.Application.Exceptions;
using Kitch.Domain.Interfaces;
using Kitch.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Infrastructure.Repositories;

public class UnidadDeTrabajo : IUnidadDeTrabajo
{
    private const int TimeoutBloqueoMilisegundos = 15000;
    private const int ErrorClaveDuplicada = 2627;
    private const int ErrorIndiceUnicoDuplicado = 2601;
    private const int ErrorBloqueoNoObtenido = 50001;

    private readonly KitchDbContext _context;

    public UnidadDeTrabajo(KitchDbContext context)
    {
        _context = context;
    }

    public Task EjecutarAsync(Func<Task> operacion) =>
        EjecutarAsync(async () =>
        {
            await operacion();
            return true;
        });

    public async Task<T> EjecutarAsync<T>(Func<Task<T>> operacion)
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            return await operacion();
        }

        var estrategia = _context.Database.CreateExecutionStrategy();

        try
        {
            return await estrategia.ExecuteAsync(async () =>
            {
                await using var transaccion = await _context.Database.BeginTransactionAsync();
                try
                {
                    var resultado = await operacion();
                    await transaccion.CommitAsync();
                    return resultado;
                }
                catch
                {
                    _context.ChangeTracker.Clear();
                    throw;
                }
            });
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException
        {
            Number: ErrorClaveDuplicada or ErrorIndiceUnicoDuplicado
        })
        {
            throw new ConflictException(
                "Otra operación guardó el mismo dato al mismo tiempo. Actualizá y volvé a intentar.", ex);
        }
        catch (SqlException ex) when (ex.Number == ErrorBloqueoNoObtenido)
        {
            throw new ConflictException(
                "Hay otra operación en curso sobre estos datos. Esperá unos segundos y volvé a intentar.", ex);
        }
    }

    public async Task BloquearRecursoAsync(string recurso)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("El bloqueo solo puede tomarse dentro de una transacción.");
        }

        await _context.Database.ExecuteSqlRawAsync(
            "DECLARE @resultado int; " +
            "EXEC @resultado = sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', " +
            "@LockOwner = 'Transaction', @LockTimeout = {1}; " +
            "IF @resultado < 0 THROW 50001, 'No se pudo obtener el bloqueo.', 1;",
            recurso,
            TimeoutBloqueoMilisegundos);
    }
}
