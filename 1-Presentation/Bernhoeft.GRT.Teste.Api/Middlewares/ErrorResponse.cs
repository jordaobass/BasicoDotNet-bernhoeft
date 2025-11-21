namespace Bernhoeft.GRT.Teste.Api.Middlewares
{
    /// <summary>
    /// Modelo padronizado de resposta de erro da API
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Indica se a operação foi bem sucedida
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Código de status HTTP
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Mensagem de erro amigável
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Detalhes adicionais do erro (apenas em desenvolvimento)
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// Identificador único do erro para rastreamento
        /// </summary>
        public string TraceId { get; set; }

        /// <summary>
        /// Timestamp do erro
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
