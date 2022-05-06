namespace ChangeDB.Default
{
    public class DefaultEventReporter : IEventReporter
    {
        public void RaiseEvent<T>(T eventInfo)
            where T : IEventInfo
        {
        }
    }
}
