namespace OrderProcessing.Application.Abstractions;

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; set; }
}
