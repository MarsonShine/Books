# 非空引用类型——C#8.0﻿

该新增的特性最关键的作用是处理泛型和更高级 API 的使用场景。这些都是我们从 .NETCore 上注解衍生过来的经验。

## 通用不为 NULL 约束

通常的做法是不允许泛型类型为 NULL。我们给出下面代码：

```c#
interface IDoStuff<Tin, Tout>
{
	Tout DoStuff(Tin input);
}
```

这种做法对为空引用和值类型也许令人满意的。也就是说对 `string` 或者 `or` 来说很好，但是对 `string?` 或 `or` 却不是。

这样可以通过 `notnull` 约束来实现。

```c#
interface IDoStuff<Tin, Tout>
	where Tin: notnull
	where Tout: notnull
{
	Tout DoStuff(Tin input);
}
```

