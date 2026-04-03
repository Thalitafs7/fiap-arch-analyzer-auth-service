using Application.Common.Interfaces;

namespace Infrastructure.Logging
{
    public class CorrelationIdService : ICorrelationIdService
    {
        private string _correlationId = Guid.NewGuid().ToString();

        public string GetCorrelationId()
        {
            return _correlationId;
        }

        public void SetCorrelationId(Guid? correlationId = null)
        {
            if (correlationId.HasValue && correlationId != Guid.Empty)
            {
                _correlationId = correlationId.Value.ToString();
            }
        }
    }
}
