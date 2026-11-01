namespace Uta.Server.Application.Common;

public sealed class UseCaseException(UseCaseError error, string message) : Exception(message)
{
    public UseCaseError Error { get; } = error;
}
