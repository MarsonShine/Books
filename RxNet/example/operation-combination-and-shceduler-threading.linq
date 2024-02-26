<Query Kind="Statements">
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Concurrency</Namespace>
</Query>

Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}");
var subject = new Subject<string>();

subject.Subscribe(
	m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));

object sync = new();
ParameterizedThreadStart notify = arg =>
{
	string message = arg?.ToString() ?? "null";
	Console.WriteLine(
		$"OnNext({message}) on thread: {Environment.CurrentManagedThreadId}");
	lock (sync)
	{
		subject.OnNext(message);
	}
};

notify("Main");
new Thread(notify).Start("First worker thread");
new Thread(notify).Start("Second worker thread");

Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}");

Console.WriteLine("================================");

Observable
	.Range(1, 5)
	.Subscribe(m =>
	  Console.WriteLine(
		$"Received {m} on thread: {Environment.CurrentManagedThreadId}"));

Console.WriteLine("Subscribe returned");

Console.WriteLine("================================");

Observable
	.Range(1, 5)
	.SelectMany(i => Observable.Range(i * 10, 5))
	.Subscribe(m =>
	  Console.WriteLine(
		$"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
Console.WriteLine("Subscribe returned");

Console.WriteLine("================================");
Observable
	.Range(1, 5)
	.SelectMany(i => Observable.Range(i * 10, 5, ImmediateScheduler.Instance))
	.Subscribe(
	m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));

Console.WriteLine("================================");
Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}");
Observable
.Range(1, 5, TaskPoolScheduler.Default)
.Subscribe(
m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
Console.WriteLine("Subscribe returned");