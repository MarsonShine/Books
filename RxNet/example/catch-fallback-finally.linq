<Query Kind="Program">
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
</Query>

void Main()
{
	
	//IObservable<int> result = source.Catch<int, TimeoutException>(_ => Observable.Empty<int>());
	//IObservable<string> result = source.Catch(
	//(FileNotFoundException x) => x.FileName == "settings.txt"
	//	? Observable.Return(DefaultSettings) : Observable.Throw<string>(x));

	//var source = new Subject<int>();
	//IObservable<int> result = source.Finally(() => Console.WriteLine("Finally action ran"));
	//result.Dump("Finally");
	//source.OnNext(1);
	//source.OnNext(2);
	//source.OnNext(3);
	//source.OnCompleted();

	//var source = new Subject<int>();
	//var result = source.Finally(() => Console.WriteLine("Finally"));
	//var subscription = result.Subscribe(
	//	Console.WriteLine,
	//	Console.WriteLine,
	//	() => Console.WriteLine("Completed"));
	//source.OnNext(1);
	//source.OnNext(2);
	//source.OnNext(3);
	//subscription.Dispose();

//	var source = new Subject<int>();
//	var result = source.Finally(() => Console.WriteLine("Finally"));
//	result.Subscribe(
//		Console.WriteLine,
//		// Console.WriteLine,
//		() => Console.WriteLine("Completed"));
//	source.OnNext(1);
//	source.OnNext(2);
//	source.OnNext(3);
//
//	// Brings the app down. Finally action might not be called.
//	source.OnError(new Exception("Fail"));
}

// You can define other methods, fields, classes and namespaces here
