namespace Kitch.Application.Interfaces;

public interface ICuotaIaService
{
    Task ConsumirAsync(int usuarioId);

    Task DevolverAsync(int usuarioId);
}
