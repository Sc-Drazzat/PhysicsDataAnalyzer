using System.Text.Json;
using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.IO;

public static class ExperimentFileReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static Result<ExperimentInput, DomainError> TryReadExperiment(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Failure(DomainError.Codes.FilePathRequired, "Read experiment file", "File path cannot be empty.");

        if (!File.Exists(path))
            return Failure(DomainError.Codes.FileNotFound, "Read experiment file", $"File was not found: '{path}'.");

        try
        {
            string json = File.ReadAllText(path);
            var experiment = JsonSerializer.Deserialize<ExperimentInput>(json, JsonOptions);

            return experiment is null
                ? Failure(DomainError.Codes.FileJsonInvalid, "Parse experiment file", $"JSON contained no experiment: '{path}'.")
                : new Result<ExperimentInput, DomainError>.Success(experiment);
        }
        catch (JsonException exception)
        {
            return Failure(DomainError.Codes.FileJsonInvalid, "Parse experiment file", $"Malformed JSON in '{path}': {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(DomainError.Codes.FileAccessDenied, "Read experiment file", $"Access denied for '{path}': {exception.Message}");
        }
        catch (IOException exception)
        {
            return Failure(DomainError.Codes.FileReadFailed, "Read experiment file", $"Could not read '{path}': {exception.Message}");
        }
    }

    public static Result<IReadOnlyList<ExperimentFileRecord>, DomainError> TryReadDataset(string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            return Failure<IReadOnlyList<ExperimentFileRecord>>(
                DomainError.Codes.DatasetPathRequired, "Read dataset", "Directory path cannot be empty.");

        if (!Directory.Exists(directoryPath))
            return Failure<IReadOnlyList<ExperimentFileRecord>>(
                DomainError.Codes.DatasetNotFound, "Read dataset", $"Directory was not found: '{directoryPath}'.");

        try
        {
            string[] paths = Directory.GetFiles(directoryPath, "*.json")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var records = new List<ExperimentFileRecord>(paths.Length);
            foreach (string path in paths)
            {
                switch (TryReadExperiment(path))
                {
                    case Result<ExperimentInput, DomainError>.Success success:
                        records.Add(new ExperimentFileRecord(Path.GetFileName(path), success.Value, null));
                        break;
                    case Result<ExperimentInput, DomainError>.Failure failure:
                        return new Result<IReadOnlyList<ExperimentFileRecord>, DomainError>.Failure(failure.Error);
                    default:
                        throw new InvalidOperationException("Unknown file read result");
                }
            }

            return new Result<IReadOnlyList<ExperimentFileRecord>, DomainError>.Success(records.AsReadOnly());
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure<IReadOnlyList<ExperimentFileRecord>>(
                DomainError.Codes.DatasetAccessDenied, "Read dataset", $"Access denied for directory '{directoryPath}': {exception.Message}");
        }
        catch (IOException exception)
        {
            return Failure<IReadOnlyList<ExperimentFileRecord>>(
                DomainError.Codes.DatasetReadFailed, "Read dataset", $"Could not read directory '{directoryPath}': {exception.Message}");
        }
    }

    private static Result<ExperimentInput, DomainError>.Failure Failure(
        string code,
        string operation,
        string detail) =>
        new(new DomainError(code, $"{operation} failed. {detail}"));

    private static Result<T, DomainError>.Failure Failure<T>(string code, string operation, string detail) =>
        new(new DomainError(code, $"{operation} failed. {detail}"));
}
