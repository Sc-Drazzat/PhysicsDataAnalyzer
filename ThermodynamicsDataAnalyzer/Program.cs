using System.Text.Json;
using System.Text.Json.Serialization;
using PhysicsDataAnalyzer.IO;
using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;
using AnalysisPolicy = PhysicsDataAnalyzer.Pipeline.AnalysisPolicy;
using AnalysisPolicyType = PhysicsDataAnalyzer.Models.AnalysisPolicy;

string datasetDirectory = args.Length > 0
    ? Path.GetFullPath(args[0])
    : FindDefaultDatasetPath();

string policyName = args.Length > 2
    ? args[2].ToLowerInvariant()
    : "whole";

string outputPath = args.Length > 1
    ? Path.GetFullPath(args[1])
    : Path.GetFullPath(Path.Combine(
        datasetDirectory,
        "..",
        "reports",
        $"batch-report-{policyName}.json"));

AnalysisPolicy.SpecificHeatPolicy selectedPolicy;
AnalysisPolicyType selectedPolicyType;

switch (policyName)
{
    case "whole":
        selectedPolicyType = AnalysisPolicyType.WholeExperiment;
        selectedPolicy = AnalysisPolicy.TryEstimateWholeExperimentPolicy;
        break;
    case "mean":
        selectedPolicyType = AnalysisPolicyType.MeanInterval;
        selectedPolicy = AnalysisPolicy.TryEstimateMeanIntervalPolicy;
        break;
    default:
        Console.WriteLine("Policy must be 'whole' or 'mean'");
        return 1;
}


if (!ExperimentFileReader.TryReadDataset(
        datasetDirectory,
        out var records,
        out string? errorMessage))
{
    Console.Error.WriteLine($"Input error: {errorMessage}");
    return 1;
}

if (!ExperimentPipeline.TryAnalyzeBatch(
        records,
        selectedPolicyType,
        selectedPolicy,
        out errorMessage,
        out ExperimentBatchReport? report))
{
    Console.Error.WriteLine($"Analysis error: {errorMessage}");
    return 1;
}

if (report is null)
{
    Console.Error.WriteLine("Analysis completed without producing a report");
    return 1;
}

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
jsonOptions.Converters.Add(new JsonStringEnumConverter());

Console.WriteLine(JsonSerializer.Serialize(report, jsonOptions));

if (!ReportWriter.TryWriteBatchReport(report, outputPath, out errorMessage))
{
    Console.Error.WriteLine($"Output error: {errorMessage}");
    return 1;
}
Console.WriteLine($"Accepted {report.Accepted.Count} of {report.TotalRecords} records, rejected {report.Rejected.Count}.");

Console.WriteLine($"Report saved to {outputPath}");
return 0;

static string FindDefaultDatasetPath()
{
    var directory = new DirectoryInfo(Environment.CurrentDirectory);
    while (directory is not null)
    {
        string[] candidates =
        {
            Path.Combine(directory.FullName, "Data", "experiments"),
            Path.Combine(directory.FullName, "ThermodynamicsDataAnalyzer", "Data", "experiments")
        };

        foreach (string candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        directory = directory.Parent;
    }

    return Path.GetFullPath(Path.Combine("ThermodynamicsDataAnalyzer", "Data", "experiments"));
}
