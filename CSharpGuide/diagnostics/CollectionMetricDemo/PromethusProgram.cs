using OpenTelemetry;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;

namespace CollectionMetricDemo
{
    public class PromethusProgram
    {
        static Meter s_meter = new Meter("HatCo.HatStore", "1.0.0");
        static Counter<int> s_hatsSold = s_meter.CreateCounter<int>(name: "hats-sold",
                                                                    unit: "Hats",
                                                                    description: "The number of hats sold in our store");
        public static void Main2(string[] args)
        {
            using MeterProvider meterProvider = Sdk.CreateMeterProviderBuilder()
                                .AddMeter("HatCo.HatStore", "1.0.0")
                                .AddPrometheusExporter(opt => {
                                    opt.StartHttpListener = true;
                                    opt.HttpListenerPrefixes = new string[] { "http://localhost:9184" };
                                })
                                .Build();

            Console.WriteLine("Press any key to exit");
            while (!Console.KeyAvailable)
            {
                // 模拟每秒出售4个帽子
                Thread.Sleep(1000);
                s_hatsSold.Add(RandomNumberGenerator.GetInt32(1, 10));
            }
        }
    }
}
