namespace PhysicsDataAnalyzer.Models;

public sealed record AcceptedExperiment(
    string SourceFile,
    ExperimentReport Report);

public sealed record RejectedExperiment(
    string SourceFile,
    string Reason);

public sealed record ExperimentBatchReport(
    AnalysisPolicy Policy,
    int TotalRecords,
    IReadOnlyList<AcceptedExperiment> Accepted,
    IReadOnlyList<RejectedExperiment> Rejected);