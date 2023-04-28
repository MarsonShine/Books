using System.Diagnostics.Tracing;

namespace EventSourceDemo.EventListeners
{
    public class ConsoleWriterEventListener : EventListener
    {
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "MyCompany-MyEventSource-Demo")
            {
                EnableEvents(eventSource, EventLevel.Informational);
            }
            else if (eventSource.Name == "MyCompany-ActivityId-Demo" || eventSource.Name == "MyCompany-ParallelActivityId-Demo")
            {
                Console.WriteLine("{0,-5} {1,-40} {2,-15} {3}", "TID", "Activity ID", "Event", "Arguments");
                EnableEvents(eventSource, EventLevel.Verbose);
            }
            else if (eventSource.Name == "System.Threading.Tasks.TplEventSource")
            {
                // ActivityIDs 默认是不开启的
                // 通过关键字 EventKeywords 0x80 开启
                // 详情可见 https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Threading/Tasks/TplEventSource.cs
                EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)0x80);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            lock (this)
            {
                Console.Write("{0,-5} {1,-40} {2,-40} {3, -15} ", eventData.OSThreadId, eventData.ActivityId, eventData.RelatedActivityId, eventData.EventName);
                if (eventData.Payload?.Count == 1)
                {
                    Console.WriteLine(eventData.Payload[0]);
                }
                else
                {
                    Console.WriteLine();
                }
            }
            //Console.WriteLine(eventData.TimeStamp + " " + eventData.EventName);
        }
    }
}
