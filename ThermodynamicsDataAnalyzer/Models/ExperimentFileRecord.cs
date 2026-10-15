namespace PhysicsDataAnalyzer.Models;

public sealed record ExperimentFileRecord(
    string SourceFile,
    ExperimentInput? Input,
    string? ReadError);