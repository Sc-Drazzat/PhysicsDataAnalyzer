using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;
using AnalysisPolicyType = PhysicsDataAnalyzer.Models.AnalysisPolicy;

namespace PhysicsDataAnalyzer.Tests;

public class ExperimentPipelineTests
{
    [Fact]
    public void AnalyzesWholeExperimentPolicyAndBuildsReport()
    {
        var success = Assert.IsType<AnalysisOutcome.Success>(ExperimentPipeline.Analyze(
            UnevenExperiment(),
            AnalysisPolicyType.WholeExperiment));

        var report = success.Report;
        Assert.Equal("test-001", report.ExperimentId);
        Assert.Equal(11, report.TemperatureChangeCelsius);
        Assert.Equal(100, report.ElapsedTimeSeconds);
        Assert.Equal(4545.454545, report.Result.EstimatedSpecificHeatCapacityJPerKgK, 4);
        Assert.Equal(AnalysisPolicyType.WholeExperiment, report.Result.Policy);
    }

    [Fact]
    public void AnalyzesMeanIntervalPolicyAndBuildsReport()
    {
        var success = Assert.IsType<AnalysisOutcome.Success>(ExperimentPipeline.Analyze(
            UnevenExperiment(),
            AnalysisPolicyType.MeanInterval));

        var report = success.Report;
        Assert.Equal(5333.333333, report.Result.EstimatedSpecificHeatCapacityJPerKgK, 5);
        Assert.Equal(AnalysisPolicyType.MeanInterval, report.Result.Policy);
    }

    [Fact]
    public void ReturnsValidationErrorForInvalidInput()
    {
        var invalidInput = TestData.ValidExperiment(massKg: 0);

        var failure = Assert.IsType<AnalysisOutcome.Failure>(ExperimentPipeline.Analyze(
            invalidInput,
            AnalysisPolicyType.WholeExperiment));

        Assert.Contains("mass", string.Join(" ", failure.Reasons), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReturnsAnalysisErrorWhenTemperatureDoesNotIncrease()
    {
        var input = TestData.ValidExperiment(new[]
        {
            new TemperatureMeasurement(0, 20),
            new TemperatureMeasurement(20, 20)
        });

        var failure = Assert.IsType<AnalysisOutcome.Failure>(ExperimentPipeline.Analyze(
            input,
            AnalysisPolicyType.MeanInterval));

        Assert.NotEmpty(failure.Reasons);
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
        var before = measurements.ToArray();

        Assert.IsType<AnalysisOutcome.Success>(
            ExperimentPipeline.Analyze(input, AnalysisPolicyType.WholeExperiment));

        Assert.Equal(before, measurements);
    }

    [Fact]
    public void RepeatedAnalysisReturnsTheSameReportWithoutChangingTheInput()
    {
        var input = UnevenExperiment();

        var first = Assert.IsType<AnalysisOutcome.Success>(
            ExperimentPipeline.Analyze(input, AnalysisPolicyType.WholeExperiment));
        var second = Assert.IsType<AnalysisOutcome.Success>(
            ExperimentPipeline.Analyze(input, AnalysisPolicyType.WholeExperiment));

        Assert.Equal(first.Report, second.Report);
        Assert.Equal(6, input.Measurements!.Count);
    }

    private static ExperimentInput UnevenExperiment() => TestData.ValidExperiment(new[]
    {
        new TemperatureMeasurement(0, 20),
        new TemperatureMeasurement(20, 21),
        new TemperatureMeasurement(40, 24),
        new TemperatureMeasurement(60, 27),
        new TemperatureMeasurement(80, 29),
        new TemperatureMeasurement(100, 31)
    });
}
