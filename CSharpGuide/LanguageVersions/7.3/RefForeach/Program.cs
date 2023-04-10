// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System.Runtime.InteropServices;
using static RefForeach.CustomWrapper;

namespace RefForeach
{
    //[SimpleJob(RuntimeMoniker.Net70), MemoryDiagnoser]
    [Config(typeof(MyEnvVars))]
    public class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run(typeof(Program).Assembly, args: args);
        }

        private readonly List<SomeStruct> _list = new();
        private SomeStruct[] _array;
        private CustomWrapper _custom;

        [GlobalSetup]
        public void Populate()
        {
            for (int i = 0; i < 1000; i++)
            {
                _list.Add(new SomeStruct(i));
            }
            _array = _list.ToArray();
            _custom = new CustomWrapper(_array);
        }
        [Benchmark]
        public int ListForEachLoop()
        {
            int total = 0;
            foreach (var tmp in _list)
            {
                total += tmp.Value;
            }
            return total;
        }
        [Benchmark]
        public int ArrayForEachLoop()
        {
            int total = 0;
            foreach (var tmp in _array)
            {
                total += tmp.Value;
            }
            return total;
        }
        [Benchmark]
        public int ListForLoop()
        {
            int total = 0;
            for (int i = 0; i < _list.Count; i++)
            {
                total += _list[i].Value;
            }
            return total;
        }
        [Benchmark]
        public int ArrayForLoop()
        {
            int total = 0;
            for (int i = 0; i < _array.Length; i++)
            {
                total += _array[i].Value;
            }
            return total;
        }
        [Benchmark]
        public int CustomForLoop()
        {
            int total = 0;
            var snapshot = _custom;
            int length = _custom.Length;
            for (int i = 0; i < length; i++)
            {
                total += snapshot[i].Value;
            }
            return total;
        }
        [Benchmark]
        public int ListLinqSum()
            => _list.Sum(x => x.Value);
        [Benchmark]
        public int ArrayLinqSum()
            => _array.Sum(x => x.Value);
        [Benchmark]
        public int ListForEachMethod()
        {
            int total = 0;
            _list.ForEach(x => total += x.Value);
            return total;
        }
        [Benchmark]
        public int ListRefForeachLoop()
        {
            int total = 0;
            foreach (ref var tmp in CollectionsMarshal.AsSpan(_list))
            {
                total += tmp.Value;
            }
            return total;
        }
        [Benchmark]
        public int ListSpanForLoop()
        {
            int total = 0;
            var span = CollectionsMarshal.AsSpan(_list);
            for (int i = 0; i < span.Length; i++)
            {
                total += span[i].Value;
            }
            return total;
        }
        [Benchmark]
        public int ArrayRefForeachLoop()
        {
            int total = 0;
            foreach (ref var tmp in _array.AsSpan())
            {   // also works identically with "ref readonly var", since this is
                // a readonly struct
                total += tmp.Value;
            }
            return total;
        }

        [Benchmark]
        public int CustomRefForeachLoop()
        {
            int total = 0;
            foreach (ref readonly var tmp in _custom)
            {   // need "ref readonly" here, as we've protected the value
                total += tmp.Value;
            }
            return total;
        }


        [Benchmark]
        public int CustomSpanForeachLoop()
        {
            int total = 0;
            foreach (var tmp in _custom.AsSpan())
            {
                total += tmp.Value;
            }
            return total;
        }

        [Benchmark]
        public int CustomSpanRefForeachLoop()
        {
            int total = 0;
            foreach (ref readonly var tmp in _custom.AsSpan())
            {   // need "ref readonly" here, as we've protected the value
                total += tmp.Value;
            }
            return total;
        }
    }

    public readonly struct SomeStruct
    {
        public SomeStruct(int value)
        {
            Value = value;
            When = DateTime.UtcNow;
            Id = Guid.NewGuid();
            HowMuch = 123.45m;
        }

        public int Value { get; }

        public DateTime When { get; }
        public decimal HowMuch { get; }
        public Guid Id { get; }
    }

    public readonly struct CustomWrapper
    {
        private readonly SomeStruct[] _array; // or some other underlying store
        public CustomWrapper(SomeStruct[] array)
            => _array = array;

        public ReadOnlySpan<SomeStruct> AsSpan() => _array; // for convenience

        public int Length => _array.Length;

        public ref readonly SomeStruct this[int index]
            => ref _array[index];

        public Enumerator GetEnumerator()
            => new Enumerator(_array);

        public struct Enumerator
        {
            private readonly SomeStruct[] _array;
            private int _index;

            internal Enumerator(SomeStruct[] array)
            {
                _array = array;
                _index = -1;
            }

            public bool MoveNext()
                => ++_index < _array.Length;

            public ref readonly SomeStruct Current
                => ref _array[_index];
        }

        // Custom config to define "Default vs PGO"
        public class MyEnvVars : ManualConfig
        {
            public MyEnvVars()
            {
                //// Use .NET 6.0 default mode:
                //AddJob(Job.Default.WithId("Default mode"));

                // Use Dynamic PGO mode:
                //[SimpleJob(RuntimeMoniker.Net70), MemoryDiagnoser]
                AddJob(Job.Default.WithId("Dynamic PGO")
                    .WithEnvironmentVariables(
                        new EnvironmentVariable("DOTNET_TieredPGO", "1"),
                        new EnvironmentVariable("DOTNET_TC_QuickJitForLoops", "1"),
                        new EnvironmentVariable("DOTNET_ReadyToRun", "0")));
            }
        }
    }
}
