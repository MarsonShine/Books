namespace Program
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var list1 = new List<int> { 1, 2, 3 };
            var list2 = new List<int> { 4, 5, 6 };

            list1.AddIfNotNull(10);
            var list3 = list1 + list2;
            Console.WriteLine(string.Join(", ", list3));

            User? user = GetUserOrNull();
            user?.FirstName = "Alice";
            Console.WriteLine(user?.FirstName);

            var s = "Hello, World".IsNullOrEmpty2; // 不能与内置的同名方法冲突
        }

        private static User? GetUserOrNull()
        {
            return default;
        }
    }
}
