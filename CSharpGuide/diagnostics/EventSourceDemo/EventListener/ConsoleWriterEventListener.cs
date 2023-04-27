using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceDemo.EventListeners
{
    public class ConsoleWriterEventListener : EventListener
    {
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if(eventSource.Name == "MyCompany-MyEventSource-Demo")
            {
                EnableEvents(eventSource, EventLevel.Informational);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            Console.WriteLine(eventData.TimeStamp + " " + eventData.EventName);
        }
    }
}
