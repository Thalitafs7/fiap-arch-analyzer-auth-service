using Application.Common.Interfaces;

namespace API.Middlewares
{
    public class CorrelationIdMiddleware : IMiddleware
    {
        private const string CorrelationIdHeader = "X-Correlation-ID";

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var correlationIdService = context.RequestServices.GetRequiredService<ICorrelationIdService>();

            if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) &&
                Guid.TryParse(correlationId, out var correlationIdGuid))
            {
                correlationIdService.SetCorrelationId(correlationIdGuid);
            }

            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
                {
                    context.Response.Headers.Append(CorrelationIdHeader, correlationIdService.GetCorrelationId());
                }
                return Task.CompletedTask;
            });

            await next(context);
        }
    }
}
