namespace PhysicsDataAnalyzer.Models;

public abstract record Result<T, E>
{
    private Result() { }

    public sealed record Success(T Value) : Result<T, E>;
    public sealed record Failure(E Error) : Result<T, E>;

    public Result<U, E> Map<U>(Func<T, U> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return this switch
        {
            Success success => new Result<U, E>.Success(map(success.Value)),
            Failure failure => new Result<U, E>.Failure(failure.Error),
            _ => throw new InvalidOperationException("Unknown result case.")
        };
    }

    public Result<U, E> Bind<U>(Func<T, Result<U, E>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);

        return this switch
        {
            Success success => bind(success.Value),
            Failure failure => new Result<U, E>.Failure(failure.Error),
            _ => throw new InvalidOperationException("Unknown result case.")
        };
    }
}
