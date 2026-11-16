using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// 单独打开并保持连接，确保多个 DbContext 使用同一内存数据库。
using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<ShelterContext>()
    .UseSqlite(connection)
    .Options;

await using (var context = new ShelterContext(options))
{
    await context.Database.EnsureCreatedAsync();
    context.AddRange(
        new Dog { Name = "豆豆", Age = 4, Breed = "柴犬", Details = new AnimalDetails { Veterinarian = "李医生", City = "上海" } },
        new Cat { Name = "团子", Age = 2, IsIndoor = true, Details = new AnimalDetails { Veterinarian = "王医生", City = "杭州" } });
    context.Customers.AddRange(new Customer { Id = 1, Name = "小林" }, new Customer { Id = 2, Name = "小周" });
    context.Orders.AddRange(new Order { Id = 101, CustomerId = 2 }, new Order { Id = 102, CustomerId = 3 });
    await context.SaveChangesAsync();
}

await using (var context = new ShelterContext(options))
{
    Console.WriteLine("=== TPT 继承 + JSON 复杂类型 ===");
    var shanghai = context.Animals.AsNoTracking().Where(animal => animal.Details.City == "上海");
    Console.WriteLine(shanghai.ToQueryString());
    foreach (var animal in await shanghai.ToListAsync())
        Console.WriteLine($"{animal.GetType().Name}: {animal.Name}, {animal.Details.City}, {animal.Details.Veterinarian}");

    Console.WriteLine("\n=== MaxByAsync：直接返回年龄最大的实体 ===");
    var oldest = await context.Animals.AsNoTracking().MaxByAsync(animal => animal.Age);
    Console.WriteLine($"{oldest?.Name}, {oldest?.Age} 岁");

    Console.WriteLine("\n=== FullJoin：顾客和订单两边没匹配的都保留 ===");
    var matches = context.Customers.FullJoin(
        context.Orders,
        customer => customer.Id,
        order => order.CustomerId,
        (customer, order) => new { Customer = customer, Order = order });
    Console.WriteLine(matches.ToQueryString());
    foreach (var match in await matches.ToListAsync())
        Console.WriteLine($"顾客={match.Customer?.Name ?? "（无）"}, 订单={match.Order?.Id.ToString() ?? "（无）"}");
}

public abstract class Animal
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Age { get; set; }
    public required AnimalDetails Details { get; set; }
}
public sealed class Dog : Animal { public required string Breed { get; set; } }
public sealed class Cat : Animal { public bool IsIndoor { get; set; } }
public sealed class AnimalDetails { public required string Veterinarian { get; set; } public required string City { get; set; } }
public sealed class Customer { public int Id { get; set; } public required string Name { get; set; } }
public sealed class Order { public int Id { get; set; } public int CustomerId { get; set; } }

public sealed class ShelterContext(DbContextOptions<ShelterContext> options) : DbContext(options)
{
    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Animal>().UseTptMappingStrategy()
            .ComplexProperty(a => a.Details, details => details.ToJson());
        modelBuilder.Entity<Dog>().ToTable("Dogs");
        modelBuilder.Entity<Cat>().ToTable("Cats");
    }
}
