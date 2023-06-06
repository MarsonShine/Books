using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourceDemo
{
    public abstract class UtilBaseEventSource : EventSource
    {
        protected UtilBaseEventSource() : base()
        {
        }
        protected UtilBaseEventSource(bool throwOnEventWriteErrors) : base(throwOnEventWriteErrors)
        {
        }
        protected UtilBaseEventSource(string eventSourceName) : base(eventSourceName)
        {
        }

        protected unsafe void WriteEvent(int eventId, int arg1, short arg2, long arg3)
        {
            if (IsEnabled())
            {
                EventData* descrs = stackalloc EventData[2];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 4;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 2;
                descrs[2].DataPointer = (IntPtr)(&arg3);
                descrs[2].Size = 8;
                WriteEventCore(eventId, 3, descrs);
            }
        }
    }

    [EventSource(Name = "MyCompany-OptimizedEventSource")]
    public sealed class OptimizedEventSource : UtilBaseEventSource
    {
        public static OptimizedEventSource Log { get; } = new OptimizedEventSource();

        [Event(1, Keywords = Keywords.Kwd1, Level = EventLevel.Informational,
           Message = "LogElements called {0}/{1}/{2}.")]
        public void LogElements(int n, short sh, long l)
        {
            WriteEvent(1, n, sh, l); // Calls UtilBaseEventSource.WriteEvent
        }

        [NonEvent]
        public unsafe void WriteEvent(int eventId, int arg1, int arg2,
                              int arg3, int arg4)
        {
            EventData* descrs = stackalloc EventData[4];

            descrs[0].DataPointer = (IntPtr)(&arg1);
            descrs[0].Size = 4;
            descrs[1].DataPointer = (IntPtr)(&arg2);
            descrs[1].Size = 4;
            descrs[2].DataPointer = (IntPtr)(&arg3);
            descrs[2].Size = 4;
            descrs[3].DataPointer = (IntPtr)(&arg4);
            descrs[3].Size = 4;

            WriteEventCore(eventId, 4, descrs);
        }
        public class Keywords
        {
            public const EventKeywords Kwd1 = (EventKeywords)0x0001;
        }
    }
}
