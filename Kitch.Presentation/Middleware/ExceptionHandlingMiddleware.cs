using System.Net;
using Kitch.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Presentation.Middleware;

public class ExceptionHandlingMiddleware
{
    private const string MensajeServicioExterno =
        "Un servicio externo no respondió a tiempo. Intentá nuevamente en unos minutos.";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Solicitud cancelada por el cliente en {Path}", context.Request.Path);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex, "Error luego de iniciar la respuesta en {Path}", context.Request.Path);
                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            ArgumentException => (
                HttpStatusCode.BadRequest,
                "Pedido inválido",
                exception.Message),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Tenés que iniciar sesión",
                exception.Message),
            ForbiddenException => (
                HttpStatusCode.Forbidden,
                "Sin permiso",
                exception.Message),
            ConflictException => (
                HttpStatusCode.Conflict,
                "Conflicto",
                exception.Message),
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "No encontrado",
                exception.Message),
            ServicioExternoException => (
                HttpStatusCode.ServiceUnavailable,
                "Servicio no disponible",
                exception.Message),
            HttpRequestException or TaskCanceledException or TimeoutException => (
                HttpStatusCode.ServiceUnavailable,
                "Servicio no disponible",
                MensajeServicioExterno),
            DbUpdateConcurrencyException => (
                HttpStatusCode.Conflict,
                "Conflicto",
                "Los datos cambiaron mientras los editabas. Recargá e intentá nuevamente."),
            DbUpdateException => (
                HttpStatusCode.Conflict,
                "Conflicto",
                "No se pudo guardar porque el dato está duplicado o lo usan otros registros."),
            InvalidOperationException => (
                HttpStatusCode.BadRequest,
                "Pedido inválido",
                exception.Message),
            _ => (
                HttpStatusCode.InternalServerError,
                "Error del servidor",
                "Ocurrió un error inesperado. Intentá nuevamente más tarde.")
        };

        if (statusCode is HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable ||
            exception is DbUpdateException)
        {
            _logger.LogError(exception, "Error al procesar la solicitud {Path}", context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Solicitud rechazada en {Path}: {Message}", context.Request.Path, exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}
