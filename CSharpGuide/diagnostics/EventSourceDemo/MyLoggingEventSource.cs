using System.Diagnostics.Tracing;

namespace EventSourceDemo
{
    public interface IMyLogging
    {
        void Error(int errorCode, string message);
        void Warning(string message);
    }
    [EventSource(Name = "MyCompany-MyEventSource-MyComponentLogging")]
    internal class MyLoggingEventSource : EventSource, IMyLogging
    {
        public static MyLoggingEventSource Log { get; } = new MyLoggingEventSource();
        [Event(1)]
        public void Error(int errorCode, string message)
        {
            WriteEvent(1, errorCode, message);
        }
        [Event(2)]
        public void Warning(string message)
        {
            WriteEvent(2, message);
        }
    }
}
