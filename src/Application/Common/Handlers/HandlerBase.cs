using Application.Common.Interfaces;

namespace Application.Common.Handlers
{
    public abstract class HandlerBase<THandler>(ILogService<THandler> logService) where THandler : class
    {
        protected void LogInicio(string metodo, object? props = null)
            => logService.LogInicio(metodo, props);

        protected void LogFim(string metodo, object? retorno = null)
            => logService.LogFim(metodo, retorno);

        protected void LogErro(string metodo, Exception ex)
            => logService.LogErro(metodo, ex);
    }
}
