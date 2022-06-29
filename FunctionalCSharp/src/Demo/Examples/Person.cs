using MarsonShine.Functional;

namespace Demo.Examples {
    public class Person {
        public string FirstName { get; }
        public string LastName { get; }

        public decimal Earnings { get; set; }
        public Option<int> Age { get; set; }
        public Person(string firstName, string lastName) {
            FirstName = firstName;
            LastName = lastName;
        }
    }
}