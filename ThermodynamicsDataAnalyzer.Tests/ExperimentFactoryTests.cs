using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Validation;

namespace PhysicsDataAnalyzer.Tests;

public class ExperimentFactoryTests
{
    [Fact]
    public void CreatesValidExperimentWithExpectedDataAndSnapshot()
    {
        var sourceMeasurements = new List<TemperatureMeasurement?>
        {
            new(0, 20),
            new(30, 25)
        };
        var input = TestData.ValidExperiment() with { Measurements = sourceMeasurements };

        var result = ExperimentFactory.Create(input);

        var valid = Assert.IsType<ValidationOutcome.Valid>(result);
        Assert.Equal(input.ExperimentId, valid.Value.ExperimentId);
        Assert.Equal(input.Material, valid.Value.Material);
        Assert.Equal(input.MassKg, valid.Value.MassKg);
        Assert.Equal(input.HeaterPowerW, valid.Value.HeaterPowerW);
        Assert.Equal(input.SpecificHeatCapacityJPerKgK, valid.Value.SpecificHeatCapacityJPerKgK);
        Assert.Equal(2, valid.Value.Measurements.Count);
        Assert.Equal(20, valid.Value.Measurements[0].TemperatureCelsius);

        sourceMeasurements[0] = new TemperatureMeasurement(0, 99);
        Assert.Equal(20, valid.Value.Measurements[0].TemperatureCelsius);
    }

    [Fact]
    public void FactoryCasesAreBothReachableAndPatternMatched()
    {
        var validResult = ExperimentFactory.Create(TestData.ValidExperiment());
        var invalidResult = ExperimentFactory.Create(TestData.ValidExperiment(massKg: 0));

        string validMatch = validResult switch
        {
            ValidationOutcome.Valid valid => $"valid:{valid.Value.ExperimentId}",
            ValidationOutcome.Invalid => "invalid",
            _ => throw new InvalidOperationException("Unknown validation outcome")
        };
        string invalidMatch = invalidResult switch
        {
            ValidationOutcome.Valid => "valid",
            ValidationOutcome.Invalid invalid => $"invalid:{string.Join(" ", invalid.Reasons)}",
            _ => throw new InvalidOperationException("Unknown validation outcome")
        };

        Assert.StartsWith("valid:", validMatch);
        Assert.Contains("mass", invalidMatch, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsEveryInvalidRuleWithAUsefulReason()
    {
        var cases = new (ExperimentInput? Input, string Reason)[]
        {
            (null, "experiment"),
            (TestData.ValidExperiment() with { ExperimentId = " " }, "id"),
            (TestData.ValidExperiment() with { Material = " " }, "material"),
            (TestData.ValidExperiment(massKg: 0), "mass"),
            (TestData.ValidExperiment(massKg: -1), "mass"),
            (TestData.ValidExperiment() with { MassKg = double.NaN }, "mass"),
            (TestData.ValidExperiment() with { MassKg = double.PositiveInfinity }, "mass"),
            (TestData.ValidExperiment() with { HeaterPowerW = 0 }, "heater"),
            (TestData.ValidExperiment() with { HeaterPowerW = double.NegativeInfinity }, "heater"),
            (TestData.ValidExperiment() with { SpecificHeatCapacityJPerKgK = 0 }, "heat capacity"),
            (TestData.ValidExperiment() with { SpecificHeatCapacityJPerKgK = double.NaN }, "heat capacity"),
            (TestData.ValidExperiment() with { Measurements = null }, "measurement"),
            (TestData.ValidExperiment(new[] { new TemperatureMeasurement(0, 20) }), "at least 2"),
            (TestData.ValidExperiment() with { Measurements = new TemperatureMeasurement?[] { new(0, 20), null } }, "null"),
            (TestData.ValidExperiment(new[] { new TemperatureMeasurement(double.NaN, 20), new TemperatureMeasurement(30, 25) }), "finite"),
            (TestData.ValidExperiment(new[] { new TemperatureMeasurement(0, double.PositiveInfinity), new TemperatureMeasurement(30, 25) }), "finite"),
            (TestData.ValidExperiment(new[] { new TemperatureMeasurement(-1, 20), new TemperatureMeasurement(30, 25) }), "negative"),
            (TestData.ValidExperiment(new[] { new TemperatureMeasurement(0, 20), new TemperatureMeasurement(0, 25) }), "increase"),
            (TestData.ValidExperiment(new[] { new TemperatureMeasurement(0, 20), new TemperatureMeasurement(-1, 25) }), "negative"),
            (TestData.ValidExperiment(new[] { new TemperatureMeasurement(10, 20), new TemperatureMeasurement(5, 25) }), "increase")
        };

        foreach (var (input, expectedReason) in cases)
        {
            var outcome = ExperimentFactory.Create(input);
            var invalid = Assert.IsType<ValidationOutcome.Invalid>(outcome);
            Assert.Contains(expectedReason, string.Join(" ", invalid.Reasons), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void FailedCreationLeavesTheOriginalInputAndMeasurementsUnchanged()
    {
        var measurements = new List<TemperatureMeasurement?>
        {
            new(0, 20),
            new(30, 25)
        };
        var input = TestData.ValidExperiment() with { Measurements = measurements, MassKg = 0 };
        var before = measurements.ToArray();

        Assert.IsType<ValidationOutcome.Invalid>(ExperimentFactory.Create(input));

        Assert.Equal(0, input.MassKg);
        Assert.Equal(before, measurements);
    }
}
