namespace Demo.Examples._11
{
    public delegate dynamic Middleware<T>(Func<T, dynamic> continueF);

    internal static class Middleware
    {
        public static T Run<T>(this Middleware<T> middleware) => middleware(t => t);

        public static Middleware<R> Map<T, R>(this Middleware<T> middleware, Func<T, R> f) => Select(middleware, f);

        public static Middleware<R> Select<T, R>(this Middleware<T> middleware, Func<T, R> f) => cont => middleware(t => cont(f(t)));

        public static Middleware<R> SelectMany<T, R>(this Middleware<T> middleware, Func<T, Middleware<R>> f) => cont => middleware(t => f(t)(cont));

        public static Middleware<RR> SelectMany<T, R, RR>(this Middleware<T> middleware, Func<T, Middleware<R>> f, Func<T, R, RR> project) => cont => middleware(t => f(t)(r => cont(project(t, r))));
    }
}
