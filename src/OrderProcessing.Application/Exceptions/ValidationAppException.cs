namespace OrderProcessing.Application.Exceptions;

public sealed class ValidationAppException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationAppException(string field, string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>
        {
            [field] = [message]
        };
    }

    public ValidationAppException(IDictionary<string, string[]> errors)
        : base("Um ou mais campos estão inválidos.")
    {
        Errors = errors;
    }
}
