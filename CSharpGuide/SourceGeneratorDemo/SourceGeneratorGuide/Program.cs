// See https://aka.ms/new-console-template for more information
namespace MySourceGenerator;

partial class Program
{
    static void Main(string[] args)
    {
        HelloFrom("Generated Code");
        Console.ReadLine();
    }

    static partial void HelloFrom(string name);
}
