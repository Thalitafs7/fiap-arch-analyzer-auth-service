using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Infrastructure.Logging
{
    public class LogService<T>(
        ICurrentApiKeyService currentApiKeyService,
        ICorrelationIdService correlationIdService,
        ILogger<T> logger) : ILogService<T>
    {
        public void LogInicio(string metodo, object? props = null)
            => LogGeneric(LogLevel.Information, metodo, props);

        public void LogFim(string metodo, object? retorno = null)
            => LogGeneric(LogLevel.Information, metodo, retorno);

        public void LogErro(string metodo, Exception ex)
        {
            var dados = new
            {
                Mensagem = ex.Message,
                Tipo = ex.GetType().Name,
                ex.StackTrace
            };

            LogGeneric(LogLevel.Error, metodo, dados, ex);
        }

        private void LogGeneric(
            LogLevel nivel,
            string metodo,
            object? dados,
            Exception? exception = null)
        {
            var activity = Activity.Current;
            var entry = new LogEntryDto
            {
                Nivel = nivel.ToString(),
                Classe = typeof(T).Name,
                Metodo = metodo,
                CorrelationId = correlationIdService.GetCorrelationId(),
                TraceId = activity?.TraceId.ToString(),
                SpanId = activity?.SpanId.ToString(),
                Dados = dados,
                Timestamp = DateTime.UtcNow,
                ApiKey = currentApiKeyService.ApiKey
            };

            if (nivel == LogLevel.Error)
                logger.LogError(exception, "{@LogEntry}", entry);
            else
                logger.LogInformation("{@LogEntry}", entry);
        }
    }
}
