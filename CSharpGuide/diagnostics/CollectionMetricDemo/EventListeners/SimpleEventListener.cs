using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectionMetricDemo.EventListeners
{
    public class SimpleEventListener : EventListener
    {
        public SimpleEventListener()
        {
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (!eventSource.Name.Equals("System.Runtime"))
            {
                return;
            }
            EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All, new Dictionary<string, string?>()
            {
                ["EventCounterIntervalSec"] = "1"
            });
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (!eventData.EventName!.Equals("EventCounters"))
            {
                return;
            }
            for (int i = 0; i < eventData.Payload?.Count; i++)
            {
                if (eventData.Payload[i] is IDictionary<string, object> eventPayload)
                {
                    var (counterName, counterValue) = GetRelevantMetric(eventPayload);
                    Console.WriteLine($"{counterName} : {counterValue}");
                }
            }
        }

        private static (string counterName, string counterValue) GetRelevantMetric(IDictionary<string, object> eventPayload)
        {
            string counterName = "";
            string counterValue = "";
            if (eventPayload == null)
            {
                throw new ArgumentNullException(nameof(eventPayload));
            }
            if (eventPayload.TryGetValue("DisplayName", out object? displayValue))
            {
                counterName = displayValue.ToString()!;
            }
            if (eventPayload.TryGetValue("Mean", out object? value) ||
                eventPayload.TryGetValue("Increment", out value))
            {
                counterValue = value.ToString()!;
            }

            return (counterName, counterValue);
        }
    }
}
