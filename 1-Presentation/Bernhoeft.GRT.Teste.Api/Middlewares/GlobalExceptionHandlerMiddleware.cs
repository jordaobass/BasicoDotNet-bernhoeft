using System.Net;
using System.Text.Json;

namespace Bernhoeft.GRT.Teste.Api.Middlewares
{
    /// <summary>
    /// Middleware para tratamento global de exceções não capturadas
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
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
            var traceId = context.TraceIdentifier;

            _logger.LogError(exception,
                "Erro não tratado. TraceId: {TraceId}, Path: {Path}, Method: {Method}",
                traceId,
                context.Request.Path,
                context.Request.Method);

            var (statusCode, message) = GetStatusCodeAndMessage(exception);

            var errorResponse = new ErrorResponse
            {
                StatusCode = statusCode,
                Message = message,
                TraceId = traceId,
                Details = _environment.IsDevelopment()
                    ? $"{exception.Message}\n{exception.StackTrace}"
                    : null
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null, // Manter PascalCase como o resto da API
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(errorResponse, jsonOptions);
            await context.Response.WriteAsync(json);
        }

        private static (int StatusCode, string Message) GetStatusCodeAndMessage(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException =>
                    ((int)HttpStatusCode.BadRequest, "Parâmetro obrigatório não fornecido."),

                ArgumentException =>
                    ((int)HttpStatusCode.BadRequest, "Parâmetro inválido."),

                KeyNotFoundException =>
                    ((int)HttpStatusCode.NotFound, "Recurso não encontrado."),

                UnauthorizedAccessException =>
                    ((int)HttpStatusCode.Unauthorized, "Acesso não autorizado."),

                InvalidOperationException =>
                    ((int)HttpStatusCode.Conflict, "Operação inválida para o estado atual do recurso."),

                NotImplementedException =>
                    ((int)HttpStatusCode.NotImplemented, "Funcionalidade não implementada."),

                TimeoutException =>
                    ((int)HttpStatusCode.RequestTimeout, "A operação excedeu o tempo limite."),

                OperationCanceledException =>
                    ((int)HttpStatusCode.BadRequest, "A operação foi cancelada."),

                _ =>
                    ((int)HttpStatusCode.InternalServerError, "Ocorreu um erro interno no servidor.")
            };
        }
    }

    /// <summary>
    /// Extension methods para registro do middleware
    /// </summary>
    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        /// <summary>
        /// Adiciona o middleware de tratamento global de exceções ao pipeline
        /// </summary>
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}
