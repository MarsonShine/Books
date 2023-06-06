// See https://aka.ms/new-console-template for more information
using CollectionMetricDemo;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

class Program
{
    static Meter s_meter = new Meter("HatCo.HatStore", "1.0.0");
    static Counter<int> s_hatsSold = s_meter.CreateCounter<int>("hats-sold");

    static void Main(string[] args)
    {
        //Console.WriteLine("Press any key to exit");
        //while (!Console.KeyAvailable)
        //{
        //    // 模拟每秒出售4个帽子
        //    Thread.Sleep(1000);
        //    s_hatsSold.Add(RandomNumberGenerator.GetInt32(1,10));
        //}

        #region Demo2
        PromethusProgram.Main2(args);
        #endregion

        //var workingSetCounter = new PollingCounter(
        //    "working-set",  CallConvThiscall, () => (double)(Environment.WorkingSet / 1_000_000))
        //{

        //}
    }
}
