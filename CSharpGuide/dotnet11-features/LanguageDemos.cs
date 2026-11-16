public record Product(string Name, decimal Price);
public record MissingProduct(string Id);
public union ProductResult(Product, MissingProduct);

public closed record PaymentState;
public sealed record Pending : PaymentState;
public sealed record Paid(decimal Amount) : PaymentState;

public interface ITemperature
{
    public static double CelsiusToFahrenheit(double celsius) => celsius * 9 / 5 + 32;
}

public static class EnumerableIndexer
{
    extension(IEnumerable<int> numbers)
    {
        public int this[int index] => numbers.ElementAt(index);
    }
}

public static class LanguageDemos
{
    public static void Run()
    {
        Console.WriteLine("\n=== C# 15：集合表达式、联合类型和控制流 ===");
        HashSet<string> tags = [with(StringComparer.OrdinalIgnoreCase), "CSharp", "csharp", "NET"];
        Console.WriteLine($"集合比较器：{string.Join(", ", tags)}；个数 {tags.Count}");
        List<int> prices = [with(capacity: 8), 10, 20, 30];
        Console.WriteLine($"指定容量：Count={prices.Count}, Capacity={prices.Capacity}");

        ProductResult found = new Product("键盘", 120m);
        ProductResult missing = new MissingProduct("P-404");
        Console.WriteLine(Describe(found));
        Console.WriteLine(Describe(missing));
        PaymentState payment = new Paid(120m);
        Console.WriteLine(payment switch { Pending => "待支付", Paid p => $"已支付 {p.Amount}" });

        IEnumerable<int> sequence = Enumerable.Range(10, 5).Where(x => x % 2 == 0);
        Console.WriteLine($"扩展索引器：第二项 {sequence[1]}（ElementAt 可能遍历序列）");
        int[][] grid = [[0, 0], [0, 9], [0, 0]];
        int rowFound = -1, columnFound = -1;
        outer: for (int row = 0; row < grid.Length; row++)
        {
            for (int column = 0; column < grid[row].Length; column++)
            {
                if (grid[row][column] != 9) continue;
                rowFound = row;
                columnFound = column;
                break outer;
            }
        }
        Console.WriteLine($"带标签 break：找到 ({rowFound}, {columnFound})");
        int completedRows = 0;
        rows: for (int row = 0; row < grid.Length; row++)
        {
            for (int column = 0; column < grid[row].Length; column++)
            {
                if (grid[row][column] == 9) continue rows;
            }
            completedRows++;
        }
        Console.WriteLine($"带标签 continue：不含 9 的行数 {completedRows}");
        Console.WriteLine($"接口静态方法：25°C = {ITemperature.CelsiusToFahrenheit(25)}°F");
    }

    private static string Describe(ProductResult result) => result switch
    {
        Product p => $"找到商品 {p.Name}，价格 {p.Price}",
        MissingProduct m => $"商品不存在：{m.Id}"
    };
}
