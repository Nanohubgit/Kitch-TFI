namespace Kitch.Application.Exceptions;

public enum MotivoFallaIa
{
    Saturado,
    NoDisponible,
    Timeout
}

public class ServicioIaException : ServicioExternoException
{
    public ServicioIaException(MotivoFallaIa motivo, string message) : base(message)
    {
        Motivo = motivo;
    }

    public ServicioIaException(MotivoFallaIa motivo, string message, Exception innerException)
        : base(message, innerException)
    {
        Motivo = motivo;
    }

    public MotivoFallaIa Motivo { get; }
}
