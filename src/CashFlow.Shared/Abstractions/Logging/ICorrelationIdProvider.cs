namespace CashFlow.Shared.Abstractions.Logging;

public interface ICorrelationIdProvider
{
    string GetCorrelationId();
    void SetCorrelationId(string correlationId);
}

public class CorrelationIdProvider : ICorrelationIdProvider
{
    private string _correlationId = Guid.NewGuid().ToString();

    public string GetCorrelationId() => _correlationId;
    public void SetCorrelationId(string correlationId) => _correlationId = correlationId;
}
