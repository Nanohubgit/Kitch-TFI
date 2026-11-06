namespace Kitch.Domain.Interfaces;

public interface IUnidadDeTrabajo
{
    Task EjecutarAsync(Func<Task> operacion);
    Task<T> EjecutarAsync<T>(Func<Task<T>> operacion);
    Task BloquearRecursoAsync(string recurso);
}
