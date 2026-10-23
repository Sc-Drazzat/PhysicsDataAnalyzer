namespace PhysicsDataAnalyzer.Models;
public sealed record DomainError
{
    public string Code { get; }
    public string Message { get; }

    public DomainError(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("An error code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("An error message is required.", nameof(message));

        Code = code;
        Message = message;
    }
}
