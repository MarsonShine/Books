using System.Diagnostics.Tracing;

namespace CollectionMetricDemo.EventCounters
{
    [EventSource(Name = "Sample.EventCounter.ConcurrentRequests")]
    public class CocurrentRequestEventCounterSource : EventSource
    {
        public static readonly CocurrentRequestEventCounterSource Log = new CocurrentRequestEventCounterSource();

        private IncrementingPollingCounter _requestRateCounter;
        private long _requestCount = 0;

        private CocurrentRequestEventCounterSource() =>
            _requestRateCounter = new IncrementingPollingCounter("request-rate", this, () => Interlocked.Read(ref _requestCount))
            {
                DisplayName = "Request Rate",
                DisplayRateTimeScale = TimeSpan.FromSeconds(1)
            };

        public void AddRequest() => Interlocked.Increment(ref _requestCount);
    }
}
