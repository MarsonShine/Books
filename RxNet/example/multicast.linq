<Query Kind="Program">
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Disposables</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

void Main()
{
	IObservable<int> src = Observable.Create<int>(obs =>
	{
		Console.WriteLine("Create callback called");
		obs.OnNext(1);
		obs.OnNext(2);
		obs.OnCompleted();
		return Disposable.Empty;
	});

	//	
	//	IConnectableObservable<int> m = src.Multicast(new Subject<int>());
	//	// 多次订阅
	//	m.Subscribe(x => Console.WriteLine($"Sub1: {x}"));
	//	m.Subscribe(x => Console.WriteLine($"Sub2: {x}"));
	//	m.Subscribe(x => Console.WriteLine($"Sub3: {x}"));
	//	m.Connect();
	//
	//	// 上述效果等价于下面代码片段
	//	var s = new Subject<int>();
	//	s.Subscribe(x => Console.WriteLine($"Sub1: {x}"));
	//	s.Subscribe(x => Console.WriteLine($"Sub2: {x}"));
	//	s.Subscribe(x => Console.WriteLine($"Sub3: {x}"));
	//	src.Subscribe(s);
	//
	//	IObservable<(int, int)> ps = src.Zip(src.Skip(1));
	//	ps.Subscribe(ps => Console.WriteLine(ps));
	//
	//	// publish
	//	//IConnectableObservable<int> m2 = src.Publish();
	//	IConnectableObservable<long> publishedTicks = Observable
	//	.Interval(TimeSpan.FromSeconds(1))
	//	.Take(4)
	//	.Publish();
	//
	//	publishedTicks.Subscribe(x => Console.WriteLine($"Sub1: {x} ({DateTime.Now})"));
	//	publishedTicks.Subscribe(x => Console.WriteLine($"Sub2: {x} ({DateTime.Now})"));
	//
	//	publishedTicks.Connect();
	//	Thread.Sleep(2500);
	//	Console.WriteLine();
	//	Console.WriteLine("Adding more subscribers");
	//	Console.WriteLine();
	//
	//	publishedTicks.Subscribe(x => Console.WriteLine($"Sub3: {x} ({DateTime.Now})"));
	//	publishedTicks.Subscribe(x => Console.WriteLine($"Sub4: {x} ({DateTime.Now})"));

	//IObservable<(int, int)> ps2 = src.Multicast(() => new Subject<int>(), s => s.Zip(s.Skip(1)));
	//ps2.Subscribe(ps => Console.WriteLine(ps));
	////上述两行代码等价于
	//IObservable<(int, int)> ps3 = src.Publish(s => s.Zip(s.Skip(1)));
	//ps3.Subscribe(ps => Console.WriteLine(ps));

	//	//publishlast
	//	IConnectableObservable<long> pticks = Observable
	//	.Interval(TimeSpan.FromSeconds(0.1))
	//	.Take(4)
	//	.PublishLast();
	//
	//	pticks.Subscribe(x => Console.WriteLine($"Sub1: {x} ({DateTime.Now})"));
	//	pticks.Subscribe(x => Console.WriteLine($"Sub2: {x} ({DateTime.Now})"));
	//
	//	pticks.Connect();
	//	Thread.Sleep(1000);
	//	Console.WriteLine();
	//	Console.WriteLine("Adding more subscribers");
	//	Console.WriteLine();
	//
	//	pticks.Subscribe(x => Console.WriteLine($"Sub3: {x} ({DateTime.Now})"));
	//	pticks.Subscribe(x => Console.WriteLine($"Sub4: {x} ({DateTime.Now})"));

	// Replay
//	IConnectableObservable<long> pticks = Observable
//	.Interval(TimeSpan.FromSeconds(1))
//	.Take(4)
//	.Replay();
//
//	pticks.Subscribe(x => Console.WriteLine($"Sub1: {x} ({DateTime.Now})"));
//	pticks.Subscribe(x => Console.WriteLine($"Sub2: {x} ({DateTime.Now})"));
//
//	pticks.Connect();
//	Thread.Sleep(2500);
//	Console.WriteLine();
//	Console.WriteLine("Adding more subscribers");
//	Console.WriteLine();
//
//	pticks.Subscribe(x => Console.WriteLine($"Sub3: {x} ({DateTime.Now})"));
//	pticks.Subscribe(x => Console.WriteLine($"Sub4: {x} ({DateTime.Now})"));

	// RefCount
	IObservable<int> src2 = Observable.Create<int>(async obs =>
	{
		Console.WriteLine("Create callback called");
		obs.OnNext(1);
		await Task.Delay(250).ConfigureAwait(false);
		obs.OnNext(2);
		await Task.Delay(250).ConfigureAwait(false);
		obs.OnNext(3);
		await Task.Delay(250).ConfigureAwait(false);
		obs.OnNext(4);
		await Task.Delay(100).ConfigureAwait(false);
		obs.OnCompleted();
	});
	IObservable<int> rc = src2
	.Publish()
	.RefCount();
	
	//rc.Subscribe(x => Console.WriteLine($"Sub1: {x} ({DateTime.Now})"));
	//rc.Subscribe(x => Console.WriteLine($"Sub2: {x} ({DateTime.Now})"));
	//Thread.Sleep(600);
	//Console.WriteLine();
	//Console.WriteLine("Adding more subscribers");
	//Console.WriteLine();
	//rc.Subscribe(x => Console.WriteLine($"Sub3: {x} ({DateTime.Now})"));
	//rc.Subscribe(x => Console.WriteLine($"Sub4: {x} ({DateTime.Now})"));

	IDisposable s1 = rc.Subscribe(x => Console.WriteLine($"Sub1: {x} ({DateTime.Now})"));
	IDisposable s2 = rc.Subscribe(x => Console.WriteLine($"Sub2: {x} ({DateTime.Now})"));
	Thread.Sleep(600);

	Console.WriteLine();
	Console.WriteLine("Removing subscribers");
	s1.Dispose();
	s2.Dispose();
	Thread.Sleep(600);
	Console.WriteLine();

	Console.WriteLine();
	Console.WriteLine("Adding more subscribers");
	Console.WriteLine();
	rc.Subscribe(x => Console.WriteLine($"Sub3: {x} ({DateTime.Now})"));
	rc.Subscribe(x => Console.WriteLine($"Sub4: {x} ({DateTime.Now})"));
}

// You can define other methods, fields, classes and namespaces here
