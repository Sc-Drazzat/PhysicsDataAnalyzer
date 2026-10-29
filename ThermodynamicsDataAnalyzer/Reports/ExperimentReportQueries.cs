using PhysicsDataAnalyzer.Models;
using PhysicsDataAnalyzer.Extensions;

namespace PhysicsDataAnalyzer.Reports;

public static class ExperimentReportQueries
{
    public static IReadOnlyList<ExperimentAccuracyRow> BuildAccuracyRanking(IEnumerable<ExperimentReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);

        return reports
            .WhereReportable()
            .ToAccuracyRows()
            .OrderByDescending(row => row.PercentageError)
            .ThenBy(row => row.ExperimentId, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<MaterialSummaryRow> BuildMaterialSummaries(IEnumerable<ExperimentReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);

        return reports
            .WhereReportable()
            .Where(report => double.IsFinite(report.Result.PercentageError) &&
                             double.IsFinite(report.TotalEnergySuppliedJoules))
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

    public static IReadOnlyList<(ExperimentReport Report, double Score)> BuildScoredReports(
        IEnumerable<ExperimentReport> reports,
        Func<ExperimentReport, double> score)
    {
        ArgumentNullException.ThrowIfNull(reports);
        ArgumentNullException.ThrowIfNull(score);

        return reports
            .WhereReportable()
            .Select(report => (Report: report, Score: score(report)))
            .ToArray();
    }

}
