using System.Diagnostics.Tracing;

namespace EventSourceDemo
{
    [EventSource(Name = "MyCompany-MyEventSource-AdvancedCustomize")]
    public class AdvancedCustomizedEventSource : EventSource
    {
        static public AdvancedCustomizedEventSource Log { get; } = new AdvancedCustomizedEventSource();

        [Event(1, Task = EventTaskConsts.Request, Opcode = EventOpcode.Start)]
        public void RequestStart(int RequestID, string Url)
        {
            WriteEvent(1, RequestID, Url);
        }

        [Event(2, Task = EventTaskConsts.Request, Opcode = EventOpcode.Info)]
        public void RequestPhase(int RequestID, string PhaseName)
        {
            WriteEvent(2, RequestID, PhaseName);
        }

        [Event(3, Keywords = Keywords.Requests,
               Task = EventTaskConsts.Request, Opcode = EventOpcode.Stop)]
        public void RequestStop(int RequestID)
        {
            WriteEvent(3, RequestID);
        }

        public class Keywords
        {
            public const EventKeywords Startup = (EventKeywords)0x0001;
            public const EventKeywords Requests = (EventKeywords)0x0002;
        }
    }
}
