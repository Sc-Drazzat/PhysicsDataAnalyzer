using System.Text.Json;
using System.Text.Json.Serialization;
using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.IO;

public static class ReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    static ReportWriter()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public static bool TryWriteReport(
        ExperimentReport? report,
        string? path,
        out string? errorMessage)
    {
        return TryWriteJson(report, path, out errorMessage);
    }
    
    public static bool TryWriteBatchReport(
        ExperimentBatchReport? report,
        string? path,
        out string? errorMessage)
    {
        return TryWriteJson(report, path, out errorMessage);
    }

    public static bool TryWriteJson<T>(
        T? report, 
        string? path, 
        out string? errorMessage) 
        where T : class
    {
        errorMessage = null;

        if (report == null)
        {
            errorMessage = "Report cannot be null";
            return false;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            errorMessage = "Output file path cannot be empty";
            return false;
        }

        try
        {
            string fullPath = Path.GetFullPath(path);
            string? directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(report, JsonOptions);
            File.WriteAllText(fullPath, json);

            return true;
        }
        catch (JsonException)
        {
            errorMessage = "Could not serialize the report to JSON";
            return false;
        }
        catch (IOException)
        {
            errorMessage = $"Could not write the report to: {path}";
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            errorMessage = $"Access denied while writing the report to: {path}";
            return false;
        }
        catch (ArgumentException)
        {
            errorMessage = "The output file path is invalid";
            return false;
        }
    }
}