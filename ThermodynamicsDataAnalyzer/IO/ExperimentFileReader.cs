using System.Collections.Immutable;
using System.Text.Json;
using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.IO;

public static class ExperimentFileReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool TryReadExperiment(string? path, out ExperimentInput? experiment, out string? errorMessage)
    {
        experiment = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            errorMessage = "Input file path cannot be empty";
            return false;
        }

        if (!File.Exists(path))
        {
            errorMessage = $"Input file was not found: {path}";
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);

            experiment = JsonSerializer.Deserialize<ExperimentInput>(
                json,
                JsonOptions);

            if (experiment == null)
            {
                errorMessage = "The JSON file did not contain experiment";
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            errorMessage = "The input file contains invalid JSON";
            return false;
        }
        catch (IOException)
        {
            errorMessage = $"Could not read the input file: {path}";
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            errorMessage = $"Access denied while reading input file: {path}";
            return false;
        }
    }

    public static bool TryReadDataset(string? directoryPath, out IReadOnlyList<ExperimentFileRecord> records,
        out string? errorMessage)
    {
        records = Array.Empty<ExperimentFileRecord>();
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            errorMessage = "Dataset directory path cannot be empty";
            return false;
        }

        if (!Directory.Exists(directoryPath))
        {
            errorMessage = $"Dataset directory was not found: {directoryPath}";
            return false;
        }

        try
        {
            string[] paths = Directory
                .GetFiles(directoryPath, "*.json")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var results = new List<ExperimentFileRecord>();

            foreach (string path in paths)
            {
                bool success = TryReadExperiment(path, out ExperimentInput? input, out string? readError);

                results.Add(new ExperimentFileRecord(
                    Path.GetFileName(path) ?? path,
                    success ? input : null,
                    success ? null : readError ?? "Could not read experiment file"));
            }

            records = results.AsReadOnly();
            return true;
        }
        catch (IOException)
        {
            errorMessage = $"Could not read dataset directory: {directoryPath}";
            return false;
        }

        catch (UnauthorizedAccessException)
        {
            errorMessage = $"Access denied while reading directory: {directoryPath}";
            return false;
        }
    }
}
