using System.Text.Json;
using System.Text.Json.Serialization;
using PhysicsDataAnalyzer.IO;
using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;
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

AnalysisPolicyType selectedPolicy;

switch (policyName)
{
    case "whole":
        selectedPolicy = AnalysisPolicyType.WholeExperiment;
        break;
    case "mean":
        selectedPolicy = AnalysisPolicyType.MeanInterval;
        break;
    default:
        Console.WriteLine("Policy must be 'whole' or 'mean'");
        return 1;
}


var datasetResult = ExperimentFileReader.TryReadDataset(datasetDirectory);
IReadOnlyList<ExperimentFileRecord> records;
switch (datasetResult)
{
    case Result<IReadOnlyList<ExperimentFileRecord>, DomainError>.Success success:
        records = success.Value;
        break;
    case Result<IReadOnlyList<ExperimentFileRecord>, DomainError>.Failure failure:
        Console.Error.WriteLine($"Input error: {failure.Error.Message}");
        return 1;
    default:
        Console.Error.WriteLine("Input error: Unknown dataset result");
        return 1;
}

var batchResult = ExperimentPipeline.AnalyzeBatch(records, selectedPolicy);
ExperimentBatchReport report;
switch (batchResult)
{
    case Result<ExperimentBatchReport, DomainError>.Success success:
        report = success.Value;
        break;
    case Result<ExperimentBatchReport, DomainError>.Failure failure:
        Console.Error.WriteLine($"Analysis error: {failure.Error.Message}");
        return 1;
    default:
        Console.Error.WriteLine("Analysis error: Unknown batch result");
        return 1;
}

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
jsonOptions.Converters.Add(new JsonStringEnumConverter());

Console.WriteLine(JsonSerializer.Serialize(report, jsonOptions));

if (!ReportWriter.TryWriteBatchReport(report, outputPath, out string? errorMessage))
{
    Console.Error.WriteLine($"Output error: {errorMessage}");
    return 1;
}
Console.WriteLine($"Accepted {report.Accepted.Count} of {report.TotalRecords} records, rejected {report.Rejected.Count}.");
foreach (RejectedExperiment rejected in report.Rejected)
    Console.Error.WriteLine($"Rejected {rejected.SourceFile}: {rejected.Reason}");

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
