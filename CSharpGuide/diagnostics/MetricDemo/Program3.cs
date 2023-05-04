using System.Diagnostics.Metrics;

namespace MetricDemo
{
    class Program3
    {
        static Meter s_meter = new Meter("HatCo.HatStore", "1.0.0");
        static Counter<int> s_hatsSold = s_meter.CreateCounter<int>(name: "hats-sold",
                                                                    unit: "Hats",
                                                                    description: "The number of hats sold in our store");

        public static void Main3(string[] args)
        {
            Console.WriteLine("Press any key to exit");
            while (!Console.KeyAvailable)
            {
                // Pretend our store has a transaction each 100ms that sells 4 hats
                Thread.Sleep(100);
                s_hatsSold.Add(4);
            }
        }
    }
}
