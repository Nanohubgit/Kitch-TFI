namespace Kitch.Application.Exceptions;

/// <summary>
/// El usuario autenticado no tiene permiso de plan para la acción (mapear a HTTP 403).
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
