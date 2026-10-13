using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;
using AnalysisPolicyType = PhysicsDataAnalyzer.Models.AnalysisPolicy;

namespace PhysicsDataAnalyzer.Tests;

public class ExperimentPipelineTests
{
    [Fact]
    public void AnalyzesBothPoliciesAndBuildsReport()
    {
        bool success = ExperimentPipeline.TryAnalyze(
            TestData.ValidExperiment(), out var error, out var report);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(report);
        Assert.Equal("test-001", report.ExperimentId);
        Assert.Equal(4, report.TemperatureChangeCelsius);
        Assert.Equal(40, report.ElapsedTimeSeconds);
        Assert.Equal(5000, report.WholeExperimentResult.EstimatedSpecificHeatCapacityJPerKgK, 6);
        Assert.Equal(5000, report.MeanIntervalResult.EstimatedSpecificHeatCapacityJPerKgK, 6);
        Assert.Equal(AnalysisPolicyType.WholeExperiment, report.WholeExperimentResult.Policy);
        Assert.Equal(AnalysisPolicyType.MeanInterval, report.MeanIntervalResult.Policy);
    }

    [Fact]
    public void ReturnsValidationErrorForInvalidInput()
    {
        var invalidInput = TestData.ValidExperiment(massKg: 0);

        bool success = ExperimentPipeline.TryAnalyze(invalidInput, out var error, out var report);

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Null(report);
    }

    [Fact]
    public void ReturnsAnalysisErrorWhenTemperatureDoesNotIncrease()
    {
        var input = TestData.ValidExperiment(new[]
        {
            new TemperatureMeasurement(0, 20),
            new TemperatureMeasurement(20, 20)
        });

        bool success = ExperimentPipeline.TryAnalyze(input, out var error, out var report);

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Null(report);
    }

    [Fact]
    public void DoesNotMutateInputMeasurements()
    {
        var measurements = new[]
        {
            new TemperatureMeasurement(0, 20),
            new TemperatureMeasurement(20, 22),
            new TemperatureMeasurement(40, 24)
        };
        var input = TestData.ValidExperiment(measurements);
        var before = input.Measurements.ToArray();

        Assert.True(ExperimentPipeline.TryAnalyze(input, out _, out _));

        Assert.Equal(before, input.Measurements);
    }
}
