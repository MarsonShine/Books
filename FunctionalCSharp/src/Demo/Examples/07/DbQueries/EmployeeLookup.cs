using MarsonShine.Functional;

namespace Demo.Examples._7.DbQueries
{
    public class EmployeeLookup
    {
        public static void Main()
        {
            ConnectionString conn = "my-database";

            SqlTemplate select = "SELECT * FROM EMPLOYEES"
               , sqlById = $"{select} WHERE ID = @Id"
               , sqlByName = $"{select} WHERE LASTNAME = @LastName";

            var queryEmployees = conn.Query<Employee>();

            var queryById = queryEmployees.Apply(sqlById);
            var queryByName = queryEmployees.Apply(sqlByName);

            Option<Employee> LookupEmployee(Guid id) => queryById(new { Id = id }).FirstOrDefault();

            IEnumerable<Employee> FindEmployeesByLastName(string lastName) => queryByName(new { LastName = lastName });
        }
    }

    public class Employee { }
}
