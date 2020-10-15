using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace GenericOptimization.Benchmarks
{
    [RPlotExporter]
    [AsciiDocExporter]
    [CsvExporter]
    [HtmlExporter]
    public class GenericConstraintBenchmark
    {
        [Params(1000,10000000)]
        public int IterationCount;
        [Benchmark]
        public void DirectConstructor()
        {
            for (int i = 0; i < IterationCount; i++)
            {
                new Node();
            }
        }
        [Benchmark]
        public void GenericConstraintConstructor()
        {
            for (int i = 0; i < IterationCount; i++)
            {
                Create<Node>();
            }
        }

        [Benchmark]
        public void DelegateConstructor()
        {
            for (int i = 0; i < IterationCount; i++)
            {
                NodeFactory();
            }
        }

        [Benchmark]
        public void ExpressionTreeConstructor()
        {
            for (int i = 0; i < IterationCount; i++)
            {
                FastActivator.CreateInstance<Node>();
            }
        }

        [Benchmark]
        public void DynamicGenerateCodeConstructor()
        {
            for (int i = 0; i < IterationCount; i++)
            {
                FastActivator<Node>.Create();
            }
        }



        public static T Create<T>() where T : new() => new T();
        public static Func<Node> NodeFactory => () => new Node();
    }
}
