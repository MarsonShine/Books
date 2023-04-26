using System.Diagnostics.Tracing;

namespace EventSourceDemo
{
    internal class EventSourceKeywordsConsts
    {
        public const EventKeywords Startup = (EventKeywords)0x0001;
        public const EventKeywords Requests = (EventKeywords)0x0002;
        public const EventKeywords Kwd1 = (EventKeywords)0x0003;
    }
}
