using System.Text.Json;
using System.Text.Json.Serialization;
using PhysicsDataAnalyzer.IO;
using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;
using AnalysisPolicyType = PhysicsDataAnalyzer.Models.AnalysisPolicy;

namespace PhysicsDataAnalyzer.Commands;

public static class AnalyzeSingleExperimentCommand
{
    public static int Run(string datasetDirectory, string experimentId, string? outputPath, AnalysisPolicyType policy)
    {
        var result = SingleExperimentAnalysisWorkflow.AnalyzeDataset(datasetDirectory, experimentId, policy);
        switch (result)
        {
            case Result<ExperimentReport, DomainError>.Failure failure:
                Console.Error.WriteLine($"Analysis error: {failure.Error.Message}");
                return 1;
            case Result<ExperimentReport, DomainError>.Success success:
                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    if (!ReportWriter.TryWriteReport(success.Value, outputPath, out string? error))
                    {
                        Console.Error.WriteLine($"Output error: {error}");
                        return 1;
                    }
                    Console.WriteLine($"Report saved to {Path.GetFullPath(outputPath)}");
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new JsonStringEnumConverter());
                Console.WriteLine(JsonSerializer.Serialize(success.Value, options));
                return 0;
            default:
                Console.Error.WriteLine("Analysis error: Unknown result");
                return 1;
        }
    }
}
