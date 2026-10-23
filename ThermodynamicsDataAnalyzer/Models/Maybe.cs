namespace PhysicsDataAnalyzer.Models;

public abstract record Maybe<T>
{
    private Maybe() { }

    public sealed record Some(T Value) : Maybe<T>;
    public sealed record None : Maybe<T>;
}
