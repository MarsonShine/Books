namespace MarsonShine.Functional
{
    using Unit = ValueTuple;
    public static partial class F
    {
        public static Task<T> Async<T>(T t) => Task.FromResult(t);
    }
    public static class TaskExt
    {
        public static async Task<R> Map<T, R>(this Task<T> task, Func<T, R> f) => f(await task.ConfigureAwait(false));
        public static async Task<R> Map<R>(this Task task, Func<R> f)
        {
            await task;
            return f();
        }

        public static Task<T> Recover<T>(this Task<T> task, Func<Exception, T> fallback) => task.ContinueWith(t => t.Status == TaskStatus.Faulted ? fallback(t.Exception!) : t.Result);
        public static Task<T> RecoverWith<T>(this Task<T> task, Func<Exception, Task<T>> fallback) => task.ContinueWith(t => t.Status == TaskStatus.Faulted ? fallback(t.Exception!) : Task.FromResult(t.Result))
            .Unwrap();

        public static Task<Func<T2, R>> Map<T1, T2, R>(this Task<T1> task, Func<T1, T2, R> f) => task.Map(f.Curry());
        public static Task<Func<T2, T3, R>> Map<T1, T2, T3, R>(this Task<T1> task, Func<T1, T2, T3, R> f) => task.Map(f.CurryFirst());
        public static Task<Func<T2, T3, T4, R>> Map<T1, T2, T3, T4, R>(this Task<T1> task, Func<T1, T2, T3, T4, R> f) => task.Map(f.CurryFirst());

        public static Task<R> Map<T, R>(this Task<T> task, Func<Exception, R> Faulted, Func<T, R> Completed) => task.ContinueWith(t => t.Status == TaskStatus.Faulted ? Faulted(t.Exception!) : Completed(t.Result));
        public static Task<Unit> ForEach<T>(this Task<T> task, Action<T> continuation) => task.ContinueWith(t => continuation.ToFunc()(t.Result), TaskContinuationOptions.OnlyOnRanToCompletion);

        public static async Task<R> Bind<T, R>(this Task<T> task, Func<T, Task<R>> f) => await f(await task.ConfigureAwait(false)).ConfigureAwait(false);
        public static async Task<R> Apply<T, R>(this Task<Func<T, R>> f, Task<T> arg) => (await f.ConfigureAwait(false))(await arg.ConfigureAwait(false));
    }
}
