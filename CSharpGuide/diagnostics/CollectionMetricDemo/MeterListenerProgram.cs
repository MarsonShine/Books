using OpenTelemetry;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;

namespace CollectionMetricDemo
{
    public class MeterListenerProgram
    {
        static Meter s_meter = new Meter("HatCo.HatStore", "1.0.0");
        static Counter<int> s_hatsSold = s_meter.CreateCounter<int>(name: "hats-sold",
                                                                    unit: "Hats",
                                                                    description: "The number of hats sold in our store");
        public static void Main(string[] args)
        {
            using MeterListener meterListner = new MeterListener();
            meterListner.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "HatCo.HatStore")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            meterListner.SetMeasurementEventCallback<int>(OnMeasurementRecorded!);
            meterListner.Start();

            Console.WriteLine("Press any key to exit");
            while (!Console.KeyAvailable)
            {
                // 模拟每秒出售4个帽子
                Thread.Sleep(1000);
                s_hatsSold.Add(RandomNumberGenerator.GetInt32(1, 10));
            }
        }

        static void OnMeasurementRecorded<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object>> tags, object state)
        {
            Console.WriteLine($"{instrument.Name} recorded measurement {measurement}");
        }
    }
}
