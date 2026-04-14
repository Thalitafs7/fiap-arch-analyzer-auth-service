namespace Application.Common.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message = "Não autorizado")
            : base(message)
        {
        }
    }
}
