using System.Diagnostics.Tracing;

namespace EventSourceDemo
{
    [EventSource(Name = "MyCompany-MyEventSource-Demo")]
    public class CustomEventSource : EventSource
    {
        public static CustomEventSource Log { get; } = new CustomEventSource();
        [Event(1, Level = EventLevel.Informational)]
        public void AppStarted(string message, int favoriteNumber) => WriteEvent(1, message, favoriteNumber);
        [Event(2, Level = EventLevel.Verbose)]
        public void DebugMessage(string message) => WriteEvent(2, message);
        [Event(3, Keywords = EventSourceKeywordsConsts.Startup)]
        public void RequestStart(int requestId) => WriteEvent(3, requestId);
        [Event(4, Keywords = EventSourceKeywordsConsts.Requests)]
        public void RequestStop(int requestId) => WriteEvent(4, requestId);
    }
}
