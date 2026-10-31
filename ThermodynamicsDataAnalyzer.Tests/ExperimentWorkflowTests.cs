using System.Text.Json;
using PhysicsDataAnalyzer.Interop;
using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;

namespace PhysicsDataAnalyzer.Tests;

public class ExperimentWorkflowTests
{
    [Fact]
    public void BuildsBothReportsAndRealNativeConversionsForAcceptedExperiments()
    {
        var records = new[]
        {
            new ExperimentFileRecord("one.json", TestData.ValidExperiment() with { ExperimentId = "one" }, null),
            new ExperimentFileRecord("two.json", TestData.ValidExperiment() with { ExperimentId = "two", Material = "oil" }, null)
        };

        var result = ExperimentPipeline.AnalyzeWithReports(
            records,
            PhysicsDataAnalyzer.Models.AnalysisPolicy.WholeExperiment,
            new NativeEnergyAdapter());

        var report = Assert.IsType<Result<ExperimentAnalysisReports, DomainError>.Success>(result).Value;
        Assert.Equal(2, report.BatchReport.Accepted.Count);
        Assert.Equal(2, report.AccuracyRanking.Count);
        Assert.Equal(2, report.MaterialSummaries.Count);
        Assert.Equal(2, report.EnergyConversions.Count);
        Assert.All(report.EnergyConversions, conversion =>
            Assert.Equal(conversion.EnergyJoules / 3600, conversion.WattHours));
        string json = JsonSerializer.Serialize(report);
        Assert.Contains("\"WattHours\"", json);
        Assert.DoesNotContain("\"EnergyConversions\":[{}", json);
        Assert.Equal(2, report.LenientScores.Count);
        Assert.Equal(2, report.StrictScores.Count);
    }

    [Fact]
    public void ReturnsNativeFailureWithoutProducingPartialReports()
    {
        var records = new[]
        {
            new ExperimentFileRecord("one.json", TestData.ValidExperiment() with { ExperimentId = "one" }, null)
        };
        var expectedError = new DomainError("test.native.failure", "Native conversion failed.");
        var converter = new FailingEnergyConverter(expectedError);

        var result = ExperimentPipeline.AnalyzeWithReports(
            records,
            PhysicsDataAnalyzer.Models.AnalysisPolicy.WholeExperiment,
            converter);

        var error = Assert.IsType<Result<ExperimentAnalysisReports, DomainError>.Failure>(result).Error;
        Assert.Equal(expectedError, error);
        Assert.Equal(1, converter.CallCount);
    }

    private sealed class FailingEnergyConverter(DomainError error) : INativeEnergyConverter
    {
        public int CallCount { get; private set; }

        public Result<double, DomainError> ConvertJoules(double energyJoules)
        {
            CallCount++;
            return new Result<double, DomainError>.Failure(error);
        }
    }
}
