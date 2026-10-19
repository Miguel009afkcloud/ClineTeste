using System.Net;
using System.Text.Json;
using BancaRevistas.Domain.Entities;

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

            // Apenas as violações de regra de negócio têm a mensagem
            // exposta ao usuário; erros técnicos recebem uma mensagem
            // genérica para não vazar detalhes de implementação.
            var status = ex switch
            {
                RegraDeNegocioException => HttpStatusCode.BadRequest,
                KeyNotFoundException => HttpStatusCode.NotFound,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                _ => HttpStatusCode.InternalServerError
            };

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                mensagem = ex is RegraDeNegocioException
                    ? ex.Message
                    : "Ocorreu um erro inesperado. Tente novamente.",
                erro = ex.GetType().Name
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
