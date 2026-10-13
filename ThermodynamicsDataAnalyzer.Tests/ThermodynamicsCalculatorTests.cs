using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Physics;

namespace PhysicsDataAnalyzer.Tests;

public class ThermodynamicsCalculatorTests
{
    private static readonly IReadOnlyList<TemperatureMeasurement> Measurements = new[]
    {
        new TemperatureMeasurement(5, 20),
        new TemperatureMeasurement(15, 23),
        new TemperatureMeasurement(35, 28)
    };

    [Fact]
    public void CalculatesElapsedTimeFromFirstAndLastMeasurements()
    {
        Assert.Equal(30, ThermodynamicsCalculator.CalculateElapsedTimeSeconds(Measurements));
    }

    [Fact]
    public void CalculatesTemperatureChangeFromFirstAndLastMeasurements()
    {
        Assert.Equal(8, ThermodynamicsCalculator.CalculateTemperatureChangeCelsius(Measurements));
    }

    [Fact]
    public void CalculatesSuppliedEnergy()
    {
        Assert.Equal(2000, ThermodynamicsCalculator.CalculateEnergySupplied(100, 20));
        Assert.Equal(500, ThermodynamicsCalculator.CalculateIntervalEnergySupplied(25, 20));
    }

    [Fact]
    public void CalculatesIntervalsAndTheirEnergy()
    {
        var intervals = IntervalCalculator.CalculateIntervals(TestData.Validated());

        Assert.Equal(2, intervals.Count);
        Assert.Equal(20, intervals[0].DurationSeconds);
        Assert.Equal(2, intervals[0].TemperatureChangeCelsius);
        Assert.Equal(2000, intervals[0].SuppliedEnergyJoules);
        Assert.Equal(2000, intervals[1].SuppliedEnergyJoules);
    }
}
