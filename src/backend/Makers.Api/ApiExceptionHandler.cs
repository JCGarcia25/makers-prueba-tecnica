using Makers.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Makers.Api;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            BusinessException business => business.StatusCode,
            BadHttpRequestException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };
        if (status >= 500) logger.LogError(exception, "Error inesperado al procesar {Path}", context.Request.Path);
        var detail = exception is BusinessException or UnauthorizedAccessException
            ? exception.Message
            : exception is BadHttpRequestException
                ? "El cuerpo de la solicitud no contiene JSON válido."
            : "Ocurrió un error interno. Intenta de nuevo más tarde.";
        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = status switch { 400 => "Solicitud inválida", 401 => "No autorizado", 404 => "No encontrado", 409 => "Conflicto", _ => "Error interno" },
                Detail = detail,
                Instance = context.Request.Path
            }
        });
    }
}
