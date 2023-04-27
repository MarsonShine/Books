using System.Diagnostics.Tracing;

namespace EventSourceDemo
{
    // EventKeywords 不能单独写在一个类里面，否则 EventSource 在采集追踪的时候会报 ”EventSource Use of undefine Use of undefined keyword value 0x1 for event AppStarted.“
    [Obsolete("", true)]
    internal class EventSourceKeywordsConsts
    {
        public const EventKeywords Startup = (EventKeywords)0x1;
        public const EventKeywords Requests = (EventKeywords)0x2;
        public const EventKeywords Kwd1 = (EventKeywords)0x3;
    }
}
