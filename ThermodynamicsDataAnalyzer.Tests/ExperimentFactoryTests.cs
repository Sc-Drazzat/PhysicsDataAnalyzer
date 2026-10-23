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

        var success = Assert.IsType<Result<ValidatedExperiment, DomainError>.Success>(result);
        Assert.Equal(input.ExperimentId, success.Value.ExperimentId);
        Assert.Equal(input.Material, success.Value.Material);
        Assert.Equal(input.MassKg, success.Value.MassKg);
        Assert.Equal(input.HeaterPowerW, success.Value.HeaterPowerW);
        Assert.Equal(input.SpecificHeatCapacityJPerKgK, success.Value.SpecificHeatCapacityJPerKgK);
        Assert.Equal(2, success.Value.Measurements.Count);
        Assert.Equal(20, success.Value.Measurements[0].TemperatureCelsius);

        sourceMeasurements[0] = new TemperatureMeasurement(0, 99);
        Assert.Equal(20, success.Value.Measurements[0].TemperatureCelsius);
    }

    [Fact]
    public void FactoryCasesAreBothReachableAndPatternMatched()
    {
        var successResult = ExperimentFactory.Create(TestData.ValidExperiment());
        var failureResult = ExperimentFactory.Create(TestData.ValidExperiment(massKg: 0));

        string successMatch = successResult switch
        {
            Result<ValidatedExperiment, DomainError>.Success success => $"success:{success.Value.ExperimentId}",
            Result<ValidatedExperiment, DomainError>.Failure => "failure",
            _ => throw new InvalidOperationException("Unknown result case")
        };
        string failureMatch = failureResult switch
        {
            Result<ValidatedExperiment, DomainError>.Success => "success",
            Result<ValidatedExperiment, DomainError>.Failure failure => $"failure:{failure.Error.Code}",
            _ => throw new InvalidOperationException("Unknown result case")
        };

        Assert.StartsWith("success:", successMatch);
        Assert.Equal($"failure:{DomainError.Codes.MassPositiveFinite}", failureMatch);
    }

    [Fact]
    public void RejectsEveryValidationRuleWithAUsefulStructuredError()
    {
        var cases = new (ExperimentInput? Input, string Code, string MessagePart)[]
        {
            (null, DomainError.Codes.InputRequired, "input"),
            (TestData.ValidExperiment() with { ExperimentId = " " }, DomainError.Codes.ExperimentIdRequired, "ID"),
            (TestData.ValidExperiment() with { Material = " " }, DomainError.Codes.MaterialRequired, "material"),
            (TestData.ValidExperiment(massKg: 0), DomainError.Codes.MassPositiveFinite, "mass"),
            (TestData.ValidExperiment(massKg: -1), DomainError.Codes.MassPositiveFinite, "mass"),
            (TestData.ValidExperiment() with { MassKg = double.NaN }, DomainError.Codes.MassPositiveFinite, "mass"),
            (TestData.ValidExperiment() with { MassKg = double.PositiveInfinity }, DomainError.Codes.MassPositiveFinite, "mass"),
            (TestData.ValidExperiment() with { HeaterPowerW = 0 }, DomainError.Codes.HeaterPowerPositiveFinite, "heater power"),
            (TestData.ValidExperiment() with { HeaterPowerW = double.NegativeInfinity }, DomainError.Codes.HeaterPowerPositiveFinite, "heater power"),
            (TestData.ValidExperiment() with { SpecificHeatCapacityJPerKgK = 0 }, DomainError.Codes.ReferenceHeatCapacityPositiveFinite, "reference heat capacity"),
            (TestData.ValidExperiment() with { SpecificHeatCapacityJPerKgK = double.NaN }, DomainError.Codes.ReferenceHeatCapacityPositiveFinite, "reference heat capacity"),
            (TestData.ValidExperiment() with { Measurements = null }, DomainError.Codes.MeasurementsMinimum, "measurements"),
            (TestData.ValidExperiment(new[] { new TemperatureMeasurement(0, 20) }), DomainError.Codes.MeasurementsMinimum, "at least two"),
            (TestData.ValidExperiment() with { Measurements = new TemperatureMeasurement?[] { new(0, 20), null } }, DomainError.Codes.MeasurementRequired, "measurement 1"),
            (TestData.ValidExperiment(new[] { new TemperatureMeasurement(double.NaN, 20), new TemperatureMeasurement(30, 25) }), DomainError.Codes.MeasurementFinite, "measurement 0"),
            (TestData.ValidExperiment(new[] { new TemperatureMeasurement(0, double.PositiveInfinity), new TemperatureMeasurement(30, 25) }), DomainError.Codes.MeasurementFinite, "measurement 0"),
            (TestData.ValidExperiment(new[] { new TemperatureMeasurement(-1, 20), new TemperatureMeasurement(30, 25) }), DomainError.Codes.MeasurementTimeNonNegative, "measurement 0"),
            (TestData.ValidExperiment(new[] { new TemperatureMeasurement(0, 20), new TemperatureMeasurement(0, 25) }), DomainError.Codes.MeasurementTimeIncreasing, "measurement 1"),
            (TestData.ValidExperiment(new[] { new TemperatureMeasurement(10, 20), new TemperatureMeasurement(5, 25) }), DomainError.Codes.MeasurementTimeIncreasing, "measurement 1")
        };

        foreach (var (input, expectedCode, messagePart) in cases)
        {
            var result = ExperimentFactory.Create(input);
            var failure = Assert.IsType<Result<ValidatedExperiment, DomainError>.Failure>(result);

            Assert.Equal(expectedCode, failure.Error.Code);
            Assert.Contains(messagePart, failure.Error.Message, StringComparison.OrdinalIgnoreCase);
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

        Assert.IsType<Result<ValidatedExperiment, DomainError>.Failure>(ExperimentFactory.Create(input));

        Assert.Equal(0, input.MassKg);
        Assert.Equal(before, measurements);
    }
}
