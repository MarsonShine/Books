using System.Diagnostics.Metrics;

namespace MetricDemo
{
    class Program4
    {
        static Meter s_meter = new Meter("HatCo.HatStore", "1.0.0");
        static Counter<int> s_hatsSold = s_meter.CreateCounter<int>("hats-sold");

        public static void Main4(string[] args)
        {
            Console.WriteLine("Press any key to exit");
            while (!Console.KeyAvailable)
            {
                // 模拟一个事务, 每 100ms, 出售 2 (size 12) red hats, and 1 (size 19) blue hat.
                Thread.Sleep(100);
                s_hatsSold.Add(2,
                               new KeyValuePair<string, object?>("Color", "Red"),
                               new KeyValuePair<string, object?>("Size", 12));
                s_hatsSold.Add(1,
                               new KeyValuePair<string, object?>("Color", "Blue"),
                               new KeyValuePair<string, object?>("Size", 19));
            }
        }
    }
}
