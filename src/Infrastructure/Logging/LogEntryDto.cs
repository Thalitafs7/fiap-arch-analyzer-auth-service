namespace Infrastructure.Logging
{
    public class LogEntryDto
    {
        public string? Nivel { get; set; }
        public string? Classe { get; set; }
        public string? Metodo { get; set; }
        public string? CorrelationId { get; set; }
        public string? TraceId { get; set; }
        public string? SpanId { get; set; }
        public object? Dados { get; set; }
        public DateTime Timestamp { get; set; }
        public string? ApiKey { get; set; }
    }
}
