# EF Core 11：把继承类型和 JSON 资料存进数据库

动物收容站要保存狗和猫的资料。它们都有名字和年龄，狗有品种，猫有是否室内饲养的标记；每只动物还记录城市和负责的兽医。这个例子用 EF Core 11 把动物按类型分表，并把城市、兽医放进 JSON 列，最后通过 LINQ 查询。

代码在 [ef-sqlite/Program.cs](../ef-sqlite/Program.cs)。它使用 SQLite 内存数据库，不需要安装数据库服务。运行环境是 .NET 11 SDK，以及 SQLite 原生组件支持的操作系统和架构：

```powershell
dotnet run --project CSharpGuide/dotnet11-features/ef-sqlite -c Release
```

第一次运行需要通过 HTTPS 从 nuget.org 下载 `Microsoft.EntityFrameworkCore.Sqlite` 11.0.0 及其依赖；包已缓存后可以离线运行。示例目录下的 `NuGet.Config` 只配置了 nuget.org，在使用私有镜像的环境中请按本地安全策略调整。

程序会创建数据库、写入几条记录并执行查询。数据库只存在于打开的 SQLite 内存连接中，关闭连接后数据消失。`EnsureCreatedAsync` 适合这样的临时示例，生产数据库的结构变更仍应根据项目情况管理迁移。

## 一只狗为什么要存两行

示例把 `Animal` 作为基类，`Dog` 和 `Cat` 分别继承它。`UseTptMappingStrategy()` 选择 TPT（Table per Type）映射：基类和每个派生类型各有一张表。

```text
Animals：Id、Name、Age、Details
Dogs：   Id、Breed
Cats：   Id、IsIndoor
```

名叫豆豆的狗会在 `Animals` 中有一行，保存名字、年龄和资料；在 `Dogs` 中还有一行，保存品种。两行通过 `Id` 对应。猫也是一样，只是额外数据保存在 `Cats`。

读取 `context.Animals` 时，EF 需要把这些表连接起来，才能知道每个动物是狗还是猫。程序打印的查询 SQL 中有对 `Dogs` 和 `Cats` 的 `LEFT JOIN`，随后输出的对象类型是 `Dog` 或 `Cat`。

## 把城市和兽医放在一列 JSON 里

`AnimalDetails` 有 `City` 和 `Veterinarian` 两个属性。它描述动物本身，没有独立主键。下面的配置告诉 EF 把它当作复杂类型，并写入 JSON 列：

```csharp
modelBuilder.Entity<Animal>().UseTptMappingStrategy()
    .ComplexProperty(a => a.Details, details => details.ToJson());
```

于是豆豆的 `Animals.Details` 列中会有类似这样的内容：

```json
{"City":"上海","Veterinarian":"李医生"}
```

这里同时做了两件事：`UseTptMappingStrategy()` 决定狗和猫如何分表，`ToJson()` 决定资料如何存入 `Animals` 表。EF 早已支持复杂类型和 JSON 列；EF Core 11 扩展的是它们与 TPT、TPC 继承映射的组合支持。

查询时仍然使用 C# 属性：

```csharp
var shanghai = context.Animals.AsNoTracking()
    .Where(animal => animal.Details.City == "上海");
```

`ToQueryString()` 会打印 SQLite 提供程序生成的 SQL，其中的 `Details ->> 'City'` 在数据库端从 JSON 提取城市，再交给 `WHERE` 筛选。程序输出的结果只有豆豆。换用其他数据库时，JSON 查询语法和提供程序支持情况可能不同。[EF Core 11 说明](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew#complex-types-and-json-columns-on-entity-types-with-tpttpc-inheritance)

## 最大年龄和年龄最大的动物

下面两个查询要的结果不同：

```csharp
await context.Animals.MaxAsync(animal => animal.Age);   // 年龄数字
await context.Animals.MaxByAsync(animal => animal.Age); // 动物对象
```

`MaxByAsync` 返回整只动物，拿到结果后还能读取名字和类型。示例中豆豆四岁、团子两岁，因此输出豆豆。EF Core 11 支持将 `MaxBy`、`MinBy` 翻译给数据库执行；具体翻译仍取决于数据库提供程序。[官方说明](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew#maxby-and-minby)

若多个动物年龄相同且都是最大值，示例没有指定并列时的顺序，不能依赖返回其中某一只。本例先插入数据，空表行为需要根据所用 API 单独处理。

## FullJoin 会把两边没匹配上的记录留下

示例还准备了两张表：小林没有订单，小周有订单 101，订单 102 的顾客编号找不到对应顾客。程序刻意没有给订单配置外键，以便展示最后一种情况。

```text
顾客小林 + 无订单
顾客小周 + 订单 101
无顾客   + 订单 102
```

`FullJoin` 返回三行：匹配的记录放在一起，左右两边未匹配的记录也保留。选择器里缺失的一侧是 `null`，示例用 `?.` 和 `??` 处理。

程序通过 `ToQueryString()` 打印 SQL；当前 SQLite 提供程序把它翻译成 `FULL JOIN`。`Enumerable.FullJoin` 在内存中运行，而这里使用的是数据库查询，能否翻译还取决于所用的数据库和 EF 提供程序版本。[官方说明](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew#support-for-the-new-net-11-fulljoin-operator)

示例使用 nuget.org 上的 EF Core SQLite 11.0.0 正式包。编写时 [EF Core 11 新功能页面](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew)仍标有“开发中”；采用其他提供程序时，请核对该提供程序的版本与支持范围。
