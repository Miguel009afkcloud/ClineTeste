using System.Net;
using System.Text.Json;

namespace BancaRevistas.API.Middleware;

/// <summary>
/// Middleware global que converte exceções de negócio em respostas HTTP
/// padronizadas, evitando que-stack traces vazem para o front-end.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Erro não tratado em {Path}", context.Request.Path);

            var status = ex switch
            {
                InvalidOperationException => HttpStatusCode.BadRequest,
                ArgumentException => HttpStatusCode.BadRequest,
                KeyNotFoundException => HttpStatusCode.NotFound,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                _ => HttpStatusCode.InternalServerError
            };

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                mensagem = ex is InvalidOperationException or ArgumentException
                    ? ex.Message
                    : "Ocorreu um erro inesperado. Tente novamente.",
                erro = ex.GetType().Name
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
