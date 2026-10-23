using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;
using AnalysisPolicyType = PhysicsDataAnalyzer.Models.AnalysisPolicy;

namespace PhysicsDataAnalyzer.Tests;

public class ExperimentPipelineTests
{
    [Fact]
    public void AnalyzesWholeExperimentPolicyAndBuildsReport()
    {
        var result = ExperimentPipeline.Analyze(Catalog(UnevenExperiment()), "test-001", AnalysisPolicyType.WholeExperiment);
        var report = Assert.IsType<Result<ExperimentReport, DomainError>.Success>(result).Value;

        Assert.Equal("test-001", report.ExperimentId);
        Assert.Equal(11, report.TemperatureChangeCelsius);
        Assert.Equal(100, report.ElapsedTimeSeconds);
        Assert.Equal(4545.454545, report.Result.EstimatedSpecificHeatCapacityJPerKgK, 4);
        Assert.Equal(AnalysisPolicyType.WholeExperiment, report.Result.Policy);
    }

    [Fact]
    public void AnalyzesMeanIntervalPolicyAndBuildsReport()
    {
        var result = ExperimentPipeline.Analyze(Catalog(UnevenExperiment()), "test-001", AnalysisPolicyType.MeanInterval);
        var report = Assert.IsType<Result<ExperimentReport, DomainError>.Success>(result).Value;

        Assert.Equal(5333.333333, report.Result.EstimatedSpecificHeatCapacityJPerKgK, 5);
        Assert.Equal(AnalysisPolicyType.MeanInterval, report.Result.Policy);
    }

    [Fact]
    public void ReturnsNotFoundFailureForMissingExperiment()
    {
        var result = ExperimentPipeline.Analyze(Catalog(UnevenExperiment()), "missing", AnalysisPolicyType.WholeExperiment);

        var error = Assert.IsType<Result<ExperimentReport, DomainError>.Failure>(result).Error;
        Assert.Equal(DomainError.Codes.ExperimentNotFound, error.Code);
        Assert.Contains("missing", error.Message);
    }

    [Fact]
    public void ReturnsValidationFailureForInvalidInput()
    {
        var result = ExperimentPipeline.Analyze(Catalog(TestData.ValidExperiment(massKg: 0)), "test-001", AnalysisPolicyType.WholeExperiment);

        var error = Assert.IsType<Result<ExperimentReport, DomainError>.Failure>(result).Error;
        Assert.Equal(DomainError.Codes.MassPositiveFinite, error.Code);
    }

    [Fact]
    public void ReturnsDomainFailureWhenCalculationCannotProducePositiveEstimate()
    {
        var result = ExperimentPipeline.Analyze(
            Catalog(UnevenExperiment()),
            "test-001",
            AnalysisPolicyType.WholeExperiment,
            (ValidatedExperiment _, out double estimate) => { estimate = 0; return false; });

        var error = Assert.IsType<Result<ExperimentReport, DomainError>.Failure>(result).Error;
        Assert.Equal(DomainError.Codes.CalculationRejected, error.Code);
        Assert.Contains("positive finite estimate", error.Message);
    }

    [Fact]
    public void DoesNotCallCalculationWhenLookupFails()
    {
        var called = false;

        var result = ExperimentPipeline.Analyze(
            Catalog(UnevenExperiment()),
            "missing",
            AnalysisPolicyType.WholeExperiment,
            (ValidatedExperiment _, out double estimate) => { called = true; estimate = 1; return true; });

        Assert.IsType<Result<ExperimentReport, DomainError>.Failure>(result);
        Assert.False(called);
    }

    [Fact]
    public void DoesNotCallCalculationWhenValidationFails()
    {
        var called = false;

        var result = ExperimentPipeline.Analyze(
            Catalog(TestData.ValidExperiment(massKg: 0)),
            "test-001",
            AnalysisPolicyType.WholeExperiment,
            (ValidatedExperiment _, out double estimate) => { called = true; estimate = 1; return true; });

        Assert.IsType<Result<ExperimentReport, DomainError>.Failure>(result);
        Assert.False(called);
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

        Assert.IsType<Result<ExperimentReport, DomainError>.Success>(
            ExperimentPipeline.Analyze(Catalog(input), "test-001", AnalysisPolicyType.WholeExperiment));

        Assert.Equal(measurements, input.Measurements);
    }

    [Fact]
    public void RepeatedAnalysisReturnsTheSameReportWithoutChangingTheInput()
    {
        var input = UnevenExperiment();
        var catalog = Catalog(input);

        var first = Assert.IsType<Result<ExperimentReport, DomainError>.Success>(
            ExperimentPipeline.Analyze(catalog, "test-001", AnalysisPolicyType.WholeExperiment)).Value;
        var second = Assert.IsType<Result<ExperimentReport, DomainError>.Success>(
            ExperimentPipeline.Analyze(catalog, "test-001", AnalysisPolicyType.WholeExperiment)).Value;

        Assert.Equal(first, second);
        Assert.Equal(6, input.Measurements!.Count);
    }

    private static ExperimentCatalog Catalog(ExperimentInput? input) =>
        new(new[] { new ExperimentFileRecord("test.json", input, null) });

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
