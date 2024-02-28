<Query Kind="Program">
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Reactive.Concurrency</Namespace>
  <Namespace>System.Reactive.Threading.Tasks</Namespace>
  <Namespace>System.Reactive</Namespace>
</Query>

async void Main()
{
	//	long v = await Observable.Timer(TimeSpan.FromSeconds(2)).FirstAsync();
	//	Console.WriteLine(v);
	//
	//	IObservable<long> source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(5);
	//	await source.ForEachAsync(i => Console.WriteLine($"received {i} @ {DateTime.Now}"));
	//	Console.WriteLine($"finished @ {DateTime.Now}");
	//
	//	IObservable<long> source2 = Observable.Interval(TimeSpan.FromSeconds(1)).Take(5);
	//	source2.Subscribe(i => Console.WriteLine($"received {i} @ {DateTime.Now}"));
	//	Console.WriteLine($"finished @ {DateTime.Now}");
	//
	//	// 不要这么做
	//	IObservable<long> source3 = Observable.Interval(TimeSpan.FromSeconds(1),ImmediateScheduler.Instance).Take(5);
	//	source3.Subscribe(i => Console.WriteLine($"received {i} @ {DateTime.Now}"));
	//	Console.WriteLine($"finished @ {DateTime.Now}");

	//	var period = TimeSpan.FromMilliseconds(200);
	//	IObservable<long> source = Observable.Timer(TimeSpan.Zero, period).Take(5);
	//	IEnumerable<long> result = source.ToEnumerable();
	//
	//	foreach (long value in result)
	//	{
	//		Console.WriteLine(value);
	//	}
	//
	//	Console.WriteLine("done");

	//	TimeSpan period = TimeSpan.FromMilliseconds(200);
	//	IObservable<long> source = Observable.Timer(TimeSpan.Zero, period).Take(5);
	//	IObservable<long[]> resultSource = source.ToArray();
	//
	//	long[] result = await resultSource;
	//	foreach (long value in result)
	//	{
	//		Console.WriteLine(value);
	//	}

	//IObservable<long> source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(5);
	//Task<long> resultTask = source.ToTask();
	//long result = await resultTask; // Will take 5 seconds. 
	//Console.WriteLine(result);

	//var source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(5);
	//var result = source.ToEvent();
	//result.OnNext += val => Console.WriteLine(val);
	//Console.WriteLine("finished");

	IObservable<EventPattern<MyEventArgs>> source = Observable.Interval(TimeSpan.FromSeconds(1))
		.Select(o => new EventPattern<MyEventArgs>(this, new MyEventArgs(o)));

	IEventPatternSource<MyEventArgs> result = source.ToEventPattern();
	result.OnNext += (sender, args) => Console.WriteLine(args.Value);
}


// You can define other methods, fields, classes and namespaces here
public class MyEventArgs : EventArgs
{
	private readonly long _value;
	public MyEventArgs(long value)
	{
		_value = value;
	}

	public long Value => _value;
}
