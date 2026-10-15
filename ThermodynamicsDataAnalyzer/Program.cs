using System.Text.Json;
using System.Text.Json.Serialization;
using PhysicsDataAnalyzer.IO;
using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Pipeline;
using AnalysisPolicy = PhysicsDataAnalyzer.Pipeline.AnalysisPolicy;
using AnalysisPolicyType = PhysicsDataAnalyzer.Models.AnalysisPolicy;

string inputPath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : FindSampleInputPath();

string outputPath = args.Length > 1
    ? args[1]
    : Path.Combine(Path.GetDirectoryName(inputPath)!, "report.json");

if (!ExperimentFileReader.TryReadExperiment(
        inputPath,
        out ExperimentInput? input,
        out string? errorMessage))
{
    Console.Error.WriteLine($"Input error: {errorMessage}");
    return 1;
}

AnalysisPolicy.SpecificHeatPolicy selectedPolicy;
AnalysisPolicyType selectedPolicyType;

string policyName = args.Length > 2
    ? args[2].ToLowerInvariant()
    : "whole";

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

if (!ExperimentPipeline.TryAnalyze(
        input,
        selectedPolicyType,
        selectedPolicy,
        out errorMessage,
        out ExperimentReport? report))
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

if (!ReportWriter.TryWriteReport(report, outputPath, out errorMessage))
{
    Console.Error.WriteLine($"Output error: {errorMessage}");
    return 1;
}

Console.WriteLine($"Report saved to {outputPath}");
return 0;

static string FindSampleInputPath()
{
    var directory = new DirectoryInfo(Environment.CurrentDirectory);
    while (directory is not null)
    {
        string[] candidates =
        {
            Path.Combine(directory.FullName, "Data", "sample-experiment.json"),
            Path.Combine(directory.FullName, "ThermodynamicsDataAnalyzer", "Data", "sample-experiment.json")
        };

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        directory = directory.Parent;
    }

    return Path.GetFullPath(Path.Combine("ThermodynamicsDataAnalyzer", "Data", "sample-experiment.json"));
}
