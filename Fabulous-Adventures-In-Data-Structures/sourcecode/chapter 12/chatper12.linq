<Query Kind="Statements">
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

static Lazy<R> Map<T,R>(Lazy<T> x,Func<T,R> func) => new Lazy<R>(() => func(x.Value));

static Nullable<R> Bind<T,R>(Nullable<T> x, Func<T, Nullable<R>> func)
	where T:struct
	where R: struct => x.HasValue ? func(x.Value) :null;
	
static Lazy<R> Bind<T,R>(Lazy<T> x, Func<T, Lazy<R>> func) => new Lazy<R>(() => func(x.Value).Value);

static async Task<R> Bind<T,R>(Task<T> x, Func<T, Task<R>> func) => await func(await x);

static IEnumerable<R> Bind<T,R>(IEnumerable<T> x, Func<T, IEnumerable<R>> func)
{
	foreach (T t in x)
	{
		foreach (R r in func(t))
		{
			yield return r;
		}
	}
}
