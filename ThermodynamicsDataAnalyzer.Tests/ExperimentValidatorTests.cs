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
        Assert.Equal("test-001", experiment.ExperimentId);
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
    public void TryValidateAdapterReturnsTheStructuredErrorMessage()
    {
        var input = TestData.ValidExperiment(massKg: 0);
        var result = ExperimentFactory.Create(input);

        Assert.False(ExperimentValidator.TryValidate(input, out var experiment, out var error));
        var failure = Assert.IsType<Result<ValidatedExperiment, DomainError>.Failure>(result);
        Assert.Null(experiment);
        Assert.Equal(failure.Error.Message, error);
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
    
    [Fact]
    public void RejectsMissingMeasurementList()
    {
        var input = TestData.ValidExperiment() with { Measurements = null };

        Assert.False(ExperimentValidator.TryValidate(input, out var experiment, out var error));
        Assert.Null(experiment);
        Assert.NotNull(error);
    }

    [Fact]
    public void AcceptsExactlyTwoMeasurements()
    {
        var input = TestData.ValidExperiment(new[]
        {
            new TemperatureMeasurement(0, 20),
            new TemperatureMeasurement(30, 25)
        });

        bool valid = ExperimentValidator.TryValidate(input, out var experiment, out var error);

        Assert.True(valid);
        Assert.Null(error);
        Assert.NotNull(experiment);
        Assert.Equal(2, experiment.Measurements.Count);
    }

    [Fact]
    public void ValidatedExperimentKeepsAnIsolatedSnapshotOfMeasurements()
    {
        var sourceMeasurements = new List<TemperatureMeasurement?>
        {
            new(0, 20),
            new(30, 25)
        };
        var input = TestData.ValidExperiment() with { Measurements = sourceMeasurements };

        bool valid = ExperimentValidator.TryValidate(input, out var experiment, out var error);
        Assert.True(valid);
        Assert.Null(error);
        Assert.NotNull(experiment);

        sourceMeasurements[0] = new TemperatureMeasurement(0, 99);
        sourceMeasurements.Add(new TemperatureMeasurement(60, 30));

        Assert.Equal(2, experiment.Measurements.Count);
        Assert.Equal(20, experiment.Measurements[0].TemperatureCelsius);
    }

    [Fact]
    public void RejectsNonFiniteExperimentNumbers()
    {
        double[] invalidValues =
        {
            double.NaN,
            double.PositiveInfinity,
            double.NegativeInfinity
        };

        foreach (double value in invalidValues)
        {
            Assert.False(ExperimentValidator.TryValidate(
                TestData.ValidExperiment() with { MassKg = value }, out _, out _));

            Assert.False(ExperimentValidator.TryValidate(
                TestData.ValidExperiment() with { HeaterPowerW = value }, out _, out _));

            Assert.False(ExperimentValidator.TryValidate(
                TestData.ValidExperiment() with
                {
                    SpecificHeatCapacityJPerKgK = value
                }, out _, out _));
        }
    }

    [Fact]
    public void RejectsNonFiniteMeasurementValues()
    {
        double[] invalidValues =
        {
            double.NaN,
            double.PositiveInfinity,
            double.NegativeInfinity
        };

        foreach (double value in invalidValues)
        {
            var badTimeInput = TestData.ValidExperiment(new[]
            {
                new TemperatureMeasurement(value, 20),
                new TemperatureMeasurement(30, 25)
            });

            var badTemperatureInput = TestData.ValidExperiment(new[]
            {
                new TemperatureMeasurement(0, value),
                new TemperatureMeasurement(30, 25)
            });

            Assert.False(ExperimentValidator.TryValidate(badTimeInput, out _, out _));
            Assert.False(ExperimentValidator.TryValidate(badTemperatureInput, out _, out _));
        }
    }
}
