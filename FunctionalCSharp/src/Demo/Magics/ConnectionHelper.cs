using System.Data;
using System.Data.SqlClient;
using static MarsonShine.Functional.F;

namespace Demo.Magics
{
    public static class ConnectionHelper
    {
        // 利用函数式风格代码来避免重复
        // 该拓展函数完成了创建与释放
        public static TResult Connec<TResult>(string connectionString, Func<IDbConnection, TResult> function)
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            return function(conn);
        }

        // 将实例化也参数化
        public static TResult Connect<TResult>(string connectionString, Func<IDbConnection, TResult> f) => Using(new SqlConnection(connectionString), conn => { conn.Open(); return f(conn); });
    }
}
