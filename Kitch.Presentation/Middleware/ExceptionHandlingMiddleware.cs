using System.Net;
using Kitch.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Middleware;

public class ExceptionHandlingMiddleware
{
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
        catch (Exception ex)
        {
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
                "Plan insuficiente",
                exception.Message),
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "No encontrado",
                exception.Message),
            InvalidOperationException => (
                HttpStatusCode.BadRequest,
                "Pedido inválido",
                exception.Message),
            _ => (
                HttpStatusCode.InternalServerError,
                "Error del servidor",
                "Ocurrió un error inesperado. Intentá nuevamente más tarde.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Error no controlado al procesar la solicitud {Path}", context.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Solicitud inválida en {Path}: {Message}", context.Request.Path, exception.Message);
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

        await context.Response.WriteAsJsonAsync(problem);
    }
}
