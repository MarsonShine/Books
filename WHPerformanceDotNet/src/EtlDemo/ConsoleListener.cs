using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;

namespace EtlDemo
{
    class ConsoleListener : BaseListener
    {
        public ConsoleListener(IEnumerable<SourceConfig> sources) : base(sources)
        {
        }

        protected override void WriteEvent(EventWrittenEventArgs eventData)
        {
            string outputString = eventData.EventId switch
            {
                Events.ProcessingStartId => string.Format("ProcessingS tart ({0})", eventData.EventId),
                Events.ProcessingFinishId => string.Format("ProcessingF inish ({0})", eventData.EventId),
                Events.FoundPrimeId => string.Format("FoundPrime ({0}): {1}", eventData.EventId, (long)eventData.Payload[0
]),
                _ => throw new InvalidOperationException("Unkn own event"),
            };

            Console.WriteLine(outputString);
        }
    }
}
