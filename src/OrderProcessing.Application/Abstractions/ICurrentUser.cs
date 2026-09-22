namespace OrderProcessing.Application.Abstractions;

public interface ICurrentUser
{
    string UserId { get; }
}
