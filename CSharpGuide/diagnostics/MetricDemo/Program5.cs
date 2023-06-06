using System.Diagnostics.Metrics;

namespace MetricDemo
{
    class Program5
    {
        static Meter s_meter = new("HatCo.HatStore", "1.0.0");

        public static void Main5(string[] args)
        {
            s_meter.CreateObservableGauge<int>("orders-pending", observeValues: GetOrdersPending);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static IEnumerable<Measurement<int>> GetOrdersPending()
        {
            return new Measurement<int>[]
            {
                new Measurement<int>(6, new KeyValuePair<string, object?>("Country", "Italy")),
                new Measurement<int>(3, new KeyValuePair<string, object?>("Country", "France")),
                new Measurement<int>(1, new KeyValuePair<string, object?>("Country", "Germany")),
            };
        }
    }
}
