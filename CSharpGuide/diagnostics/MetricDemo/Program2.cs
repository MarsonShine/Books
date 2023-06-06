using System.Diagnostics.Metrics;

namespace MetricDemo
{
    class Program2
    {
        static Meter s_meter = new Meter("HatCo.HatStore", "1.0.0");
        // 计数器
        static Counter<int> s_hatsSold = s_meter.CreateCounter<int>("hats-sold");
        // 直方图
        static Histogram<int> s_orderProcessingTimeMs = s_meter.CreateHistogram<int>("order-processing-time");
        static int s_coatsSold;
        static int s_ordersPending;

        static Random s_rand = new Random();

        public static void Main2(string[] args)
        {
            s_meter.CreateObservableCounter<int>("coats-sold", () => s_coatsSold);
            s_meter.CreateObservableGauge<int>("orders-pending", () => s_ordersPending);

            Console.WriteLine("Press any key to exit");
            while (!Console.KeyAvailable)
            {
                // 假设每 100 毫秒有一个交易，每个交易出售 4 顶帽子
                Thread.Sleep(100);
                s_hatsSold.Add(4);

                // 假设我们也卖了3件外套。对于 ObservableCounter，我们跟踪变量中的值，并在回调中根据需要报告它
                s_coatsSold += 3;

                // 假设我们有一些随时间变化的订单队列。“订单挂起”度量的回调将按需报告此值
                s_ordersPending = s_rand.Next(0, 20);

                // 最后，我们假设我们测量了完成事务所需的时间(例如，我们可以使用 Stopwatch 计时)。
                s_orderProcessingTimeMs.Record(s_rand.Next(5, 15));
            }
        }
    }
}
