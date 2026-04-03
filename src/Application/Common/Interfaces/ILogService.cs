namespace Application.Common.Interfaces
{
    public interface ILogService<T>
    {
        void LogInicio(
            string metodo,
            object? props = null);

        void LogFim(
            string metodo,
            object? retorno = null);

        void LogErro(
            string metodo,
            Exception ex);
    }
}
