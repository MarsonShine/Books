# EF Core 11：继承、JSON 复杂类型和查询翻译

这个示例使用 EF Core 11.0.0 和 SQLite 内存数据库，不需要安装数据库服务。它是独立项目：第一次运行需要从 nuget.org 下载 EF Core SQLite 包及依赖，之后在包已缓存时可离线运行。

在仓库根目录执行：

```powershell
dotnet run --project CSharpGuide/dotnet11-features/ef-sqlite -c Release
```

需要 .NET 11 SDK、可用的 nuget.org HTTPS 连接，以及 EF Core SQLite 原生组件支持的操作系统和架构。项目自己的 `NuGet.Config` 只配置 nuget.org，不使用其他示例目录中清空的包源。不要在使用私有镜像的环境中照搬这个配置；按实际安全策略替换即可。

程序只在内存中创建临时数据库。连接一关闭，数据就消失；`EnsureCreatedAsync` 是为了让例子立即运行，不代表生产环境应该跳过迁移。代码在 [Program.cs](../ef-sqlite/Program.cs)。

截至这份示例编写时，[官方 EF Core 11 新功能页面](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew)仍写着“开发中”，但 nuget.org 已提供 `Microsoft.EntityFrameworkCore.Sqlite` **11.0.0** 正式包。本示例使用该正式版本；数据库提供程序对具体查询的支持仍需分别确认。

## 一只动物分成两张表，资料放进 JSON

示例有公共的 `Animal`，派生出 `Dog` 和 `Cat`。三者都有名字、年龄和 `Details`；狗另有品种，猫有是否室内饲养。

TPT（table per type，按类型分表）把这些信息拆开：

```text
Animals：Id、Name、Age、Details
Dogs：   Id、Breed
Cats：   Id、IsIndoor
```

一只狗在 `Animals` 有一行，在 `Dogs` 也有一行，通过相同的 Id 对上。查询 `Animal` 时，EF 要用连接把它们拼起来，判断每一行实际是狗还是猫。示例打印的 SQL 中可以看到 `LEFT JOIN`，不需要凭猜测判断数据库做了什么。

`Details` 包含城市和兽医。它不是另一个有独立主键的实体，而是动物资料的一部分。示例用 `ComplexProperty(...).ToJson()` 将它存成 `Animals.Details` 一列中的 JSON：

```json
{"City":"上海","Veterinarian":"李医生"}
```

EF 11 支持将复杂类型和 JSON 列与 TPT/TPC 继承映射组合使用。示例中的两个设置各管一件事：`UseTptMappingStrategy()` 选择继承怎样分表，`ToJson()` 选择 `Details` 怎样保存。EF 早已有复杂类型和 JSON 列，新增的是这些能力与 TPT/TPC 的组合支持。

查询 `animal.Details.City == "上海"` 时，示例通过 `ToQueryString()` 打印 SQL。SQLite 会对 JSON 列提取 `City` 再筛选，因此不会先把所有动物拿到应用内存后才过滤。具体 SQL 写法由 SQLite 提供程序决定；SQL Server 和其他数据库不一定长一样。[官方说明](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew#complex-types-and-json-columns-on-entity-types-with-tpttpc-inheritance)

## MaxByAsync：拿到实体，而不只是最大年龄

`MaxAsync(animal => animal.Age)` 返回的是年龄数字。`MaxByAsync(animal => animal.Age)` 返回年龄最大的那只动物，调用方还能读取名字和类型。

EF 11 支持将 `MaxBy`/`MinBy` 翻译成数据库查询。示例返回四岁的豆豆。若有多只动物并列最大年龄，示例没有指定稳定的并列顺序，不要假定一定返回其中哪一只。数据库表为空时也要按对应 API 的空序列行为处理；本示例先写入了数据。[官方说明](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew#maxby-and-minby)

## FullJoin：没有顾客的订单也能看到

示例刻意准备三种情况：小林没有订单，小周有订单 101，订单 102 的顾客编号找不到对应顾客。这里未定义数据库外键，是为了演示不匹配的数据；真实业务应根据数据要求决定约束。

```text
顾客小林 + 无订单
顾客小周 + 订单 101
无顾客   + 订单 102
```

`FullJoin` 同时保留两侧未匹配的记录；选择器里的缺失一侧是 `null`，所以输出时用 `?.` 和 `??`。示例也打印查询 SQL，可以看到 SQLite 提供程序生成的 `FULL JOIN`。这比内存中的 `Enumerable.FullJoin` 多了一步：EF 要把 LINQ 表达式翻译成数据库能执行的 SQL。

本例在 SQLite 提供程序上运行。其他数据库及其提供程序版本是否支持相同翻译，需要使用各自的环境验证；不能仅因为 C# 能编译 `Queryable.FullJoin` 就认为任何数据库都能运行。[官方说明](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew#support-for-the-new-net-11-fulljoin-operator)
