using Dapper;
using System.Data.SqlClient;
using static Demo.Magics.ConnectionHelper;

namespace Demo.Persistent
{
    public class DbLogger
    {
        readonly string connectionString;
        public DbLogger(string connectionString)
        {
            this.connectionString = connectionString;
        }
        // 这样就避免了下面的重复using语句
        public void Log(object message) => Connec(connectionString, db => db.Execute("sql command", message));

        public void Log2(object message)
        {
            using var conn = new SqlConnection(connectionString);
            int rows = conn.Execute("sql command", message);
        }

        public void Log3(object mesesage) => Connect(connectionString, db => db.Execute("sql command", mesesage));
    }
}
