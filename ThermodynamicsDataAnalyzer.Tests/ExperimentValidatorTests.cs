using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Validation;

namespace PhysicsDataAnalyzer.Tests;

public class ExperimentValidatorTests
{
    [Fact]
    public void AcceptsValidExperiment()
    {
        bool valid = ExperimentValidator.TryValidate(
            TestData.ValidExperiment(), out var experiment, out var error);

        Assert.True(valid);
        Assert.NotNull(experiment);
        Assert.Null(error);
    }

    [Fact]
    public void RejectsNullInputAsMissingFields()
    {
        Assert.False(ExperimentValidator.TryValidate(null, out _, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void RejectsMissingRequiredTextFields()
    {
        var missingId = TestData.ValidExperiment() with { ExperimentId = " " };
        var missingMaterial = TestData.ValidExperiment() with { Material = "" };

        Assert.False(ExperimentValidator.TryValidate(missingId, out _, out _));
        Assert.False(ExperimentValidator.TryValidate(missingMaterial, out _, out _));
    }

    [Fact]
    public void RejectsNonPositiveMass()
    {
        Assert.False(ExperimentValidator.TryValidate(
            TestData.ValidExperiment(massKg: 0), out _, out var error));
        Assert.Contains("mass", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsInsufficientMeasurements()
    {
        var input = TestData.ValidExperiment(new[] { new TemperatureMeasurement(0, 20) });

        Assert.False(ExperimentValidator.TryValidate(input, out _, out _));
    }

    [Fact]
    public void RejectsNullMeasurementEntry()
    {
        var input = TestData.ValidExperiment() with
        {
            Measurements = new TemperatureMeasurement?[]
            {
                new(0, 20),
                null
            }
        };

        Assert.False(ExperimentValidator.TryValidate(input, out _, out var error));
        Assert.Contains("null", error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(10, 5)]
    public void RejectsDuplicateOrDecreasingTimestamps(double secondTime, double thirdTime)
    {
        var input = TestData.ValidExperiment(new[]
        {
            new TemperatureMeasurement(0, 20),
            new TemperatureMeasurement(secondTime, 21),
            new TemperatureMeasurement(thirdTime, 22)
        });

        Assert.False(ExperimentValidator.TryValidate(input, out _, out _));
    }
}
