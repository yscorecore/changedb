using ChangeDB.Migration;

namespace ChangeDB.Import
{
    public record ImportContext : AgentContext<ImportSetting>
    {
    }
    public static class EventReporterExtensions
    {
        public static void ReportSqlExecuted(this IEventReporter eventReporter, SqlSegmentInfo segmentInfo)
        {
            eventReporter?.RaiseEvent(segmentInfo);
        }
    }
}
