using OpenTelemetry.Resources;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace DistributedTraceDemo
{
    class Program
    {
        private static ActivitySource source = new ActivitySource("Sample.DistributedTracing", "1.0.0");
        static async Task Main(string[] args)
        {
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MySample"))
                .AddSource("Sample.DistributedTracing")
                .AddConsoleExporter()
                .Build();

            await DoSomeWork("banana", 8);
            Console.WriteLine("Example work done");
        }

        static async Task DoSomeWork(string foo, int bar)
        {
            // 添加 Activity
            using var activity = source.StartActivity("DoSomeWork");
            // 添加标签
            activity?.SetTag("foo", foo);
            activity?.SetTag("bar", bar);
            await StepOne();
            // 添加事件
            activity?.AddEvent(new ActivityEvent("Part Way there"));
            await StepTwo();
            activity?.AddEvent(new ActivityEvent("Done now"));
            // 添加状态
            activity?.SetTag("otel.status_code", "OK");
            activity?.SetTag("otel.status_description", "Use this text give more information about the error");
            //await StepOne();
            //await StepTwo();
        }

        private static async Task StepTwo()
        {
            // 添加子 Activity
            using var activity = source.StartActivity("StepTwo");
            await Task.Delay(1000);
        }

        private static async Task StepOne()
        {
            // 添加子 Activity
            using var activity = source.StartActivity("StepOne");
            await Task.Delay(500);
        }
    }
}
