using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Tests;

public class ResultTests
{
    private static DomainError Error() =>
        new("experiment.mass.invalid", "Experiment 'exp-1' mass must be finite and greater than zero.");

    [Fact]
    public void SuccessCarriesItsValue()
    {
        var result = new Result<int, DomainError>.Success(42);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void FailureCarriesItsError()
    {
        var error = Error();
        var result = new Result<int, DomainError>.Failure(error);

        Assert.Same(error, result.Error);
        Assert.Equal("experiment.mass.invalid", result.Error.Code);
        Assert.Contains("exp-1", result.Error.Message);
    }

    [Fact]
    public void MapTransformsSuccess()
    {
        Result<int, DomainError> result = new Result<int, DomainError>.Success(21);

        var mapped = result.Map(value => value * 2);

        Assert.Equal(42, Assert.IsType<Result<int, DomainError>.Success>(mapped).Value);
    }

    [Fact]
    public void BindContinuesFromSuccess()
    {
        Result<int, DomainError> result = new Result<int, DomainError>.Success(21);

        var bound = result.Bind(value =>
            new Result<string, DomainError>.Success(value.ToString()));

        Assert.Equal("21", Assert.IsType<Result<string, DomainError>.Success>(bound).Value);
    }

    [Fact]
    public void MapPreservesFailureWithoutCallingFunction()
    {
        var error = Error();
        Result<int, DomainError> result = new Result<int, DomainError>.Failure(error);
        bool called = false;

        var mapped = result.Map(value =>
        {
            called = true;
            return value * 2;
        });

        Assert.False(called);
        Assert.Same(error, Assert.IsType<Result<int, DomainError>.Failure>(mapped).Error);
    }

    [Fact]
    public void BindPreservesFailureWithoutCallingFunction()
    {
        var error = Error();
        Result<int, DomainError> result = new Result<int, DomainError>.Failure(error);
        bool called = false;

        var bound = result.Bind(value =>
        {
            called = true;
            return new Result<string, DomainError>.Success(value.ToString());
        });

        Assert.False(called);
        Assert.Same(error, Assert.IsType<Result<string, DomainError>.Failure>(bound).Error);
    }
}
