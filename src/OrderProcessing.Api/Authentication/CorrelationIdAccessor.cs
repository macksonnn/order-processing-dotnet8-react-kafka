using OrderProcessing.Application.Abstractions;

namespace OrderProcessing.Api.Authentication;

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}
