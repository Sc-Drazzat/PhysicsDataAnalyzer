using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Validation;

namespace PhysicsDataAnalyzer.Pipeline;

public static class ExperimentCreationWorkflow
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    static ExperimentCreationWorkflow() => JsonOptions.Converters.Add(new JsonStringEnumConverter());

    public static Result<string, DomainError> Create(ExperimentInput? input, string? outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            return Failure(DomainError.Codes.FilePathRequired, "Output file path cannot be empty.");

        switch (ExperimentFactory.Create(input))
        {
            case Result<ValidatedExperiment, DomainError>.Failure failure:
                return new Result<string, DomainError>.Failure(failure.Error);
            case Result<ValidatedExperiment, DomainError>.Success:
                break;
        }

        try
        {
            string fullPath = Path.GetFullPath(outputPath);
            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(fullPath, JsonSerializer.Serialize(input, JsonOptions));
            return new Result<string, DomainError>.Success(fullPath);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure(DomainError.Codes.FileAccessDenied, $"Access denied while saving experiment to '{outputPath}': {exception.Message}");
        }
        catch (IOException exception)
        {
            return Failure(DomainError.Codes.FileWriteFailed, $"Could not save experiment to '{outputPath}': {exception.Message}");
        }
        catch (ArgumentException exception)
        {
            return Failure(DomainError.Codes.FilePathRequired, $"Invalid output file path: {exception.Message}");
        }
    }

    private static Result<string, DomainError>.Failure Failure(string code, string message) =>
        new(new DomainError(code, message));
}
