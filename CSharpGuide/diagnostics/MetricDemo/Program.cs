// See https://aka.ms/new-console-template for more information
using MetricDemo;
using System.Diagnostics.Metrics;

namespace CSharpGuide.diagnostics.MetricDemo;
class Program
{
    static Meter s_meter = new Meter("HatCo.HatStore", "1.0.0");
    static Counter<int> s_hatsSold = s_meter.CreateCounter<int>("hatsSold");
    static void Main(string[] args)
    {
        #region Demo1
        //Console.WriteLine("Press any key to exit");
        //while (!Console.KeyAvailable)
        //{
        //    // 模拟销售，每1秒出售4个帽子
        //    Thread.Sleep(1000);
        //    s_hatsSold.Add(4);
        //} 
        #endregion

        #region Demo2
        //Program2.Main2(args);
        #endregion
        
        #region Demo3
        //Program3.Main3(args);
        #endregion

        #region Demo4
        //Program4.Main4(args);
        #endregion

        #region Demo5
        Program5.Main5(args);
        #endregion
    }
}
