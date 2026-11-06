namespace Kitch.Application.Exceptions;

public class ServicioExternoException : Exception
{
    public ServicioExternoException(string message) : base(message)
    {
    }

    public ServicioExternoException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
