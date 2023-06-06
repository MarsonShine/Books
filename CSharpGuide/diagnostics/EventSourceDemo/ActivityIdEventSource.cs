using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceDemo
{
    [EventSource(Name = "MyCompany-ActivityId-Demo")]
    public class ActivityIdEventSource : EventSource
    {
        public static ActivityIdEventSource Log = new ActivityIdEventSource();

        [Event(1)]
        public void WorkStart(string requestName) => WriteEvent(1, requestName);
        [Event(2)]
        public void WorkStop() => WriteEvent(2);

        [Event(3)]
        public void DebugMessage(string message) => WriteEvent(3, message);
    }
}
