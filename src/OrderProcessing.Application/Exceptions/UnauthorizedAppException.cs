namespace OrderProcessing.Application.Exceptions;

public sealed class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message)
        : base(message)
    {
    }
}
