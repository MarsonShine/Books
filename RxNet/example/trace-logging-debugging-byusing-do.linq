<Query Kind="Program">
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>System.Reactive.Linq</Namespace>
</Query>

void Main()
{
	IObservable<long> source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(3);
	IObservable<long> loggedSource = source.Do(
		i => Log(i),
		ex => Log(ex),
		() => Log());

	loggedSource.Subscribe(
		Console.WriteLine,
		() => Console.WriteLine("completed"));
}

// You can define other methods, fields, classes and namespaces here
private static void Log(object onNextValue)
{
	Console.WriteLine($"Logging OnNext({onNextValue}) @ {DateTime.Now}");
}

private static void Log(Exception error)
{
	Console.WriteLine($"Logging OnError({error}) @ {DateTime.Now}");
}

private static void Log()
{
	Console.WriteLine($"Logging OnCompleted()@ {DateTime.Now}");
}
