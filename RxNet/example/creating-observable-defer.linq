<Query Kind="Statements">
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>System.Reactive.Linq</Namespace>
</Query>

static IObservable<int> WithoutDeferal()
{
	Console.WriteLine("Doing some startup work...");
	return Observable.Range(1,3);
}

Console.WriteLine("Calling factory method");
IObservable<int> s = WithoutDeferal();

Console.WriteLine("First subscription");
s.Subscribe(Console.WriteLine);

Console.WriteLine("Second subscription");
s.Subscribe(Console.WriteLine);

static IObservable<int> WithDeferal()
{
	return Observable.Defer(() => {
		Console.WriteLine("Doing some startup work...");
		return Observable.Range(1,3);
	});
}

Console.WriteLine("Calling factory method");
IObservable<int> s2 = WithDeferal();

Console.WriteLine("First subscription");
s2.Subscribe(Console.WriteLine);

Console.WriteLine("Second subscription");
s2.Subscribe(Console.WriteLine);
