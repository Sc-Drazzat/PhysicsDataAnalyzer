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
}