using System.Diagnostics.Tracing;

namespace CollectionMetricDemo.EventCounters
{
    [EventSource(Name = "Sample.EventCounter.Minimal")]
    public class MinimalEventCounterSource : EventSource
    {
        public static readonly MinimalEventCounterSource Log = new MinimalEventCounterSource();
        private readonly EventCounter _requestCounter;

        private MinimalEventCounterSource()
        {
            _requestCounter = new EventCounter("request-time", this)
            {
                DisplayName = "Request Processing Time",
                DisplayUnits = "ms"
            };
            // 报告当前映射到应用程序的进程（工作集）的物理内存量
            var workingSetCounter = new PollingCounter(
            "working-set", this, () => (double)(Environment.WorkingSet / 1_000_000))
            {
                DisplayName = "Working Set",
                DisplayUnits = "MB"
            };
            // 报告锁争用的总计数的增量
            var monitorContentCounter = new IncrementingPollingCounter("monitor-lock-content-count", this, () => Monitor.LockContentionCount)
            {
                DisplayName = "Monitor Lock Contention Count",
                DisplayRateTimeScale = TimeSpan.FromSeconds(1)
            };
        }

        public void Request(string url, long elapsedMilliseconds)
        {
            WriteEvent(1, url, elapsedMilliseconds);
            _requestCounter.WriteMetric(elapsedMilliseconds);
        }

        protected override void Dispose(bool disposing)
        {
            _requestCounter?.Dispose();
            base.Dispose(disposing);
        }
    }
}
