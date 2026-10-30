using PhysicsDataAnalyzer.Models;

namespace PhysicsDataAnalyzer.Extensions;
public static class ExperimentReportExtensions
{
    public static IEnumerable<ExperimentReport> WhereReportable(this IEnumerable<ExperimentReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);

        return reports.Where(report =>
            double.IsFinite(report.Result.EstimatedSpecificHeatCapacityJPerKgK) &&
            double.IsFinite(report.Result.SpecificHeatCapacityJPerKgK) &&
            report.Result.SpecificHeatCapacityJPerKgK > 0);
    }

    public static IEnumerable<ExperimentAccuracyRow> ToAccuracyRows(this IEnumerable<ExperimentReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);

        return reports.Select(report =>
        {
            double estimate = report.Result.EstimatedSpecificHeatCapacityJPerKgK;
            double reference = report.Result.SpecificHeatCapacityJPerKgK;
            double absoluteError = Math.Abs(estimate - reference);
            return new ExperimentAccuracyRow(
                report.ExperimentId,
                report.Material,
                report.Result.Policy,
                estimate,
                reference,
                absoluteError,
                absoluteError / reference * 100);
        });
    }
}
