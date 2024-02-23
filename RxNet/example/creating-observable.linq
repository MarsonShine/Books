<Query Kind="Statements">
  <NuGetReference>System.Reactive</NuGetReference>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Disposables</Namespace>
</Query>

static IObservable<T> Empty<T>()
{
	return Observable.Create<T>(o => {
		o.OnCompleted();
		return Disposable.Empty;
	});
}

static IObservable<T> Return<T>(T value)
{
	return Observable.Create<T>(o => {
		o.OnNext(value);
		o.OnCompleted();
		return Disposable.Empty;
	});
}

static IObservable<T> Never<T>()
{
	return Observable.Create<T>(o =>
	{
		return Disposable.Empty;
	});
}

static IObservable<T> Throws<T>(Exception exception)
{
	return Observable.Create<T>(o =>
	{
		o.OnError(exception);
		return Disposable.Empty;
	});
}
