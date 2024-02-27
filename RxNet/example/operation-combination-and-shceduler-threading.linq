<Query Kind="Statements">
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Concurrency</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

//Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}");
//var subject = new Subject<string>();
//
//subject.Subscribe(
//	m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
//
//object sync = new();
//ParameterizedThreadStart notify = arg =>
//{
//	string message = arg?.ToString() ?? "null";
//	Console.WriteLine(
//		$"OnNext({message}) on thread: {Environment.CurrentManagedThreadId}");
//	lock (sync)
//	{
//		subject.OnNext(message);
//	}
//};
//
//notify("Main");
//new Thread(notify).Start("First worker thread");
//new Thread(notify).Start("Second worker thread");
//
//Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}");
//
//Console.WriteLine("================================");
//
//Observable
//	.Range(1, 5)
//	.Subscribe(m =>
//	  Console.WriteLine(
//		$"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
//
//Console.WriteLine("Subscribe returned");
//
//Console.WriteLine("================================");
//
//Observable
//	.Range(1, 5)
//	.SelectMany(i => Observable.Range(i * 10, 5))
//	.Subscribe(m =>
//	  Console.WriteLine(
//		$"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
//Console.WriteLine("Subscribe returned");
//
//Console.WriteLine("================================");
//Observable
//	.Range(1, 5)
//	.SelectMany(i => Observable.Range(i * 10, 5, ImmediateScheduler.Instance))
//	.Subscribe(
//	m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
//
//Console.WriteLine("================================");
//Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}");
//Observable
//.Range(1, 5, TaskPoolScheduler.Default)
//.Subscribe(
//m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
//Console.WriteLine("Subscribe returned");
//
//Console.WriteLine("================================");
//Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}");
//Observable
//	.Range(1, 5)
//	.SelectMany(i => Observable.Range(i * 10, 5,DefaultScheduler.Instance))
//	.Subscribe(
//		m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
//
//Console.WriteLine("Subscribe returned");

// EventLoopScheduler
//IEnumerable<int> xs = GetNumbers();
//Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}");
//Observable
//	.Using(
//		() => new EventLoopScheduler(),
//		scheduler => xs.ToObservable(scheduler))
//	.Subscribe(m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
//
//IEnumerable<int> GetNumbers()
//{
//	return Enumerable.Range(1,5);
//}

Console.WriteLine($"[T:{Environment.CurrentManagedThreadId}] Main thread");

Observable
	.Interval(TimeSpan.FromSeconds(1))
	.SubscribeOn(new EventLoopScheduler((start) =>
	{
		Thread t = new(start) { IsBackground = false };
		Console.WriteLine($"[T:{t.ManagedThreadId}] Created thread for EventLoopScheduler");
		return t;
	}))
	.Subscribe(tick =>
		  Console.WriteLine(
			$"[T:{Environment.CurrentManagedThreadId}] {DateTime.Now}: Tick {tick}"));

Console.WriteLine($"[T:{Environment.CurrentManagedThreadId}] {DateTime.Now}: Main thread exiting");
