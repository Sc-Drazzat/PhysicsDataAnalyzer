using PhysicsDataAnalyzer.Interop;
using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Tests;

public class NativeEnergyAdapterTests
{
    [Fact]
    public void Converts3600JoulesToOneWattHour()
    {
        var adapter = new NativeEnergyAdapter();

        var result = adapter.ConvertJoules(3600);

        Assert.Equal(1, Assert.IsType<Result<double, DomainError>.Success>(result).Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void RejectsInvalidInputWithoutCallingNativeCode(double input)
    {
        int calls = 0;
        var adapter = new NativeEnergyAdapter(value =>
        {
            calls++;
            return value / 3600;
        });

        var result = adapter.ConvertJoules(input);

        Assert.Equal(DomainError.Codes.NativeEnergyInputInvalid,
            Assert.IsType<Result<double, DomainError>.Failure>(result).Error.Code);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ConvertsLoadingFailureToStructuredError()
    {
        var adapter = new NativeEnergyAdapter(_ => throw new DllNotFoundException("missing test library"));

        var result = adapter.ConvertJoules(3600);

        Assert.Equal(DomainError.Codes.NativeLibraryUnavailable,
            Assert.IsType<Result<double, DomainError>.Failure>(result).Error.Code);
    }
}
