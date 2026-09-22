namespace OrderProcessing.Application.Exceptions;

public sealed class NotFoundAppException : Exception
{
    public string Resource { get; }

    public NotFoundAppException(string resource, string message)
        : base(message)
    {
        Resource = resource;
    }

    public static NotFoundAppException Order(Guid orderId)
    {
        return new NotFoundAppException("Pedido", $"Pedido '{orderId}' não encontrado.");
    }
}
