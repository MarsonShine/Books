using Demo.Examples._7.DbQueries;
using Demo.Magics;
using Microsoft.Extensions.Logging;
using System.Data;
using MarsonShine.Functional;

namespace Demo.Examples._11
{
    using Unit = ValueTuple;
    public class DbLogger
    {
        Middleware<IDbConnection> Connect;
        Func<string, Middleware<Unit>> Time;
        Func<string, Middleware<Unit>> Trace;

        public DbLogger(ConnectionString connString, ILogger logger)
        {
            Connect = f => ConnectionHelper.Connect(connString, f);
            Time = op => f => Instrumentation.Time(logger, op, f.ToNullary());
            Trace = op => f => Instrumentation.Trace(logger, op, f.ToNullary());
        }

        Middleware<IDbConnection> BasicPipline =>
            from _ in Time("InsertLog") // 需要实现SelectMany
            from conn in Connect // 需要实现SelectMany
            select conn;

        Middleware<IDbConnection> BasicPipline2 => Middleware.SelectMany(Time("InsertLog"), unit => Connect, (u, c) => c);

        Middleware<IDbConnection> BasicPipline3 => Time("InsertLog")
            .SelectMany(unit => Connect)
            .Select(connect => connect);

        public void Equivalent()
        {
            // 等价实现
            //Middleware<Unit> f = Time("InsertLog");
            //f.SelectMany(unit => Connect).SelectMany(conn => Connect);
            
        }
    }
}
