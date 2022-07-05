namespace Demo.Examples._7.DbQueries
{
    public class SqlTemplate
    {
        public string Value { get; }
        public SqlTemplate(string value) => Value = value;

        public static implicit operator string(SqlTemplate sqlTemplate) => sqlTemplate.Value;
        public static implicit operator SqlTemplate(string value) => new(value);

        public override string ToString() => Value;
    }
}
