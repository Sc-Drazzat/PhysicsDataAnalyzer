using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Physics;

namespace PhysicsDataAnalyzer.Tests;

public class SpecificHeatEstimatorTests
{
    [Fact]
    public void EstimatesWholeExperimentHeatCapacity()
    {
        bool success = SpecificHeatEstimator.TryEstimateWholeExperiment(10000, 2, 5, out double estimate);

        Assert.True(success);
        Assert.Equal(1000, estimate);
    }

    [Fact]
    public void EstimatesMeanOfIntervalHeatCapacities()
    {
        var intervals = new[]
        {
            new CalculatedInterval(0, 10, 20, 22, 2000),
            new CalculatedInterval(10, 20, 22, 24, 4000)
        };

        bool success = SpecificHeatEstimator.TryEstimateMeanInterval(intervals, 1, out double estimate);

        Assert.True(success);
        Assert.Equal(1500, estimate);
    }

    [Fact]
    public void RejectsZeroTemperatureChange()
    {
        bool success = SpecificHeatEstimator.TryEstimateWholeExperiment(100, 1, 0, out double estimate);

        Assert.False(success);
        Assert.Equal(0, estimate);
    }

    [Fact]
    public void RejectsInvalidInterval()
    {
        var intervals = new[]
        {
            new CalculatedInterval(0, 10, 20, 20, 100)
        };

        Assert.False(SpecificHeatEstimator.TryEstimateMeanInterval(intervals, 1, out _));
    }
}
