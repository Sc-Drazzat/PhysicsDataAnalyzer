using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Reports;

public static class ExperimentReportQueries
{
    public static IReadOnlyList<ExperimentAccuracyRow> BuildAccuracyRanking(IEnumerable<ExperimentReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);

        return reports
            .Where(IsReportable)
            .Select(report => new ExperimentAccuracyRow(
                report.ExperimentId,
                report.Material,
                report.Result.Policy,
                report.Result.EstimatedSpecificHeatCapacityJPerKgK,
                report.Result.SpecificHeatCapacityJPerKgK,
                report.Result.AbsoluteErrorJPerKgK,
                report.Result.PercentageError))
            .OrderByDescending(row => row.PercentageError)
            .ThenBy(row => row.ExperimentId, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<MaterialSummaryRow> BuildMaterialSummaries(IEnumerable<ExperimentReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);

        return reports
            .Where(IsReportable)
            .GroupBy(report => report.Material, StringComparer.Ordinal)
            .Select(group => new MaterialSummaryRow(
                group.Key,
                group.Count(),
                group.Average(report => report.Result.EstimatedSpecificHeatCapacityJPerKgK),
                group.Sum(report => report.TotalEnergySuppliedJoules),
                group.Average(report => report.Result.PercentageError)))
            .OrderByDescending(row => row.TotalEnergyJoules)
            .ThenBy(row => row.Material, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsReportable(ExperimentReport report) =>
        double.IsFinite(report.Result.EstimatedSpecificHeatCapacityJPerKgK) &&
        double.IsFinite(report.Result.SpecificHeatCapacityJPerKgK) &&
        report.Result.SpecificHeatCapacityJPerKgK > 0 &&
        double.IsFinite(report.Result.AbsoluteErrorJPerKgK) &&
        double.IsFinite(report.Result.PercentageError) &&
        double.IsFinite(report.TotalEnergySuppliedJoules);
}
