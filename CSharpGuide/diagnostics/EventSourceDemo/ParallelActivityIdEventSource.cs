using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceDemo
{
    [EventSource(Name = "MyCompany-ParallelActivityId-Demo")]
    public class ParallelActivityIdEventSource : EventSource
    {
        public static ParallelActivityIdEventSource Log = new ParallelActivityIdEventSource();

        [Event(1)]
        public void WorkStart(string requestName) => WriteEvent(1, requestName);
        [Event(2)]
        public void WorkStop() => WriteEvent(2);
        [Event(3)]
        public void DebugMessage(string message) => WriteEvent(3, message);
        [Event(4)]
        public void QueryStart(string query) => WriteEvent(4, query);
        [Event(5)]
        public void QueryStop() => WriteEvent(5);
    }
}
