using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Examples._7.DbQueries
{
    public class ConnectionString
    {
        string Value { get; }

        public ConnectionString(string value) => Value = value;

        public static implicit operator string(ConnectionString value) => value.Value;
        public static implicit operator ConnectionString(string value) => new(value);

        public override string ToString() => Value;
    }
}
