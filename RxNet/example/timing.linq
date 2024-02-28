<Query Kind="Program">
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive</Namespace>
</Query>

void Main()
{
	//Observable.Interval(TimeSpan.FromSeconds(1))
	//		  .Take(3)
	//		  .Timestamp() // 到达时间，即收到元素的时间
	//		  .Dump("Timestamp");
	
	//Observable.Interval(TimeSpan.FromSeconds(1))
	//.Take(3)
	//.TimeInterval() // 时间间隔
	//.Dump("TimeInterval");
	
	//IObservable<Timestamped<long>> source = Observable
	//	.Interval(TimeSpan.FromSeconds(1))
	//	.Take(5)
	//	.Timestamp();
	////source.Dump("source");
	//IObservable<Timestamped<long>> delay = source.Delay(TimeSpan.FromSeconds(2));
	//
	////delay.Dump("delay");
	//delay.Subscribe(value =>
	//   Console.WriteLine(
	//	 $"Item {value.Value} with timestamp {value.Timestamp} received at {DateTimeOffset.Now}"),
	//   () => Console.WriteLine("delay Completed"));

	var source = Observable.Interval(TimeSpan.FromMilliseconds(100))
					.Take(5)
					.Concat(Observable.Interval(TimeSpan.FromSeconds(2)));

	var timeout = source.Timeout(TimeSpan.FromSeconds(1));
	timeout.Subscribe(
		Console.WriteLine,
		Console.WriteLine,
		() => Console.WriteLine("Completed"));
}

public static class SampleExtensions
{
	public static void Dump<T>(this IObservable<T> source, string name)
	{
		source.Subscribe(
			value => Console.WriteLine($"{name}-->{value}"),
			ex => Console.WriteLine($"{name} failed-->{ex.Message}"),
			() => Console.WriteLine($"{name} completed"));
	}
}
