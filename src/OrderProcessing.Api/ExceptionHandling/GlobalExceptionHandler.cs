using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Application.Exceptions;

namespace OrderProcessing.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ValidationAppException validation)
        {
            var problem = new HttpValidationProblemDetails(validation.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Falha na validação",
                Detail = validation.Message,
                Instance = httpContext.Request.Path
            };

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

        if (exception is UnauthorizedAppException unauthorized)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Não autorizado",
                Detail = unauthorized.Message,
                Instance = httpContext.Request.Path
            };

            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

        if (exception is NotFoundAppException notFound)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = $"{notFound.Resource} não encontrado",
                Detail = notFound.Message,
                Instance = httpContext.Request.Path
            };

            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

        _logger.LogError(exception, "Unhandled exception.");

        var fallback = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erro inesperado",
            Detail = "Não deu para concluir o pedido.",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(fallback, cancellationToken);
        return true;
    }
}
