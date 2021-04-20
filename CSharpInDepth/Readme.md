《深入理解C#》书里涉及到的内容与知识点

# Span<T> 和 stackalloc

.NET 提供了一些方式来访问内存块。最常用的就是数组，ArraySegment<T> 以及指针都是可以的。直接使用数组的一个很大的缺点是该数组实际上它所占据的内存;数组绝不仅是大段内存的一部分。直到你看到了这些方法的签名，看起来也不坏 

```c#
int ReadData(byte[] buffer, int offset, int length)
```

其中的 “buffer，offset，length” 这些参数在 .NET 中到处可见，这实际上是一种征兆，表明我们没有正确的抽象，可以继续优化的。而 `Span<T>` 就是为了修复这个问题的。

Span<T> 是一个 ref-like 结构，它提供了读/写的能力，索引访问内存的一部分，就像数组一样，但是自己却没有内存的概念。一个 span 总是由其它东西创建的（可能是指针，数组甚至是在堆栈上直接创建的数据）。当使用 Span<T> 时，你无需关心内存被分配到哪里。Span 是可分割（sliced）的：您可以创建一个 span 作为另一个 span 的子节，而无需复制任何数据。在新版本的框架中，JIT 编译器会识别 Span<T>，并以高度优化的方式处理它。

Span<T> 的性质类似 ref-like，可能听起来不相干，但是这里有两个有意义的点：

- 它允许 span 引用具有严格控制生命周期的内存，因为 span 只能存在于堆栈中。分配内存的代码能传递一个 span 到其它代码，并释放内存，随后确保这里没有剩下任何 span 引用到了释放的内存。
- 它允许在 span 中自定义一次性初始化数据，不会发生拷贝，也不存在代码事后更改数据的风险。

通过下面生成随机字符串的例子来看一下。首先是传统代码：

```c#
public static string Generate(string alphabet, Random random, int length)
{
    char[] chars = new char[length];
    for (int i = 0; i < length; i++)
    {
        chars[i] = alphabet[random.Next(alphabet.Length)];
    }
    return new string(chars);
}
```

传统方法这里有两初进行了内存分配：一个是 chars 数组，一个是 return new string()，数据还发生了拷贝：数据需要从 chars 复制到新的对象以便构造 string。

我们可以使用不安全代码来优化：

```c#
unsafe static string Generate2(string alphabet, Random random, int length)
{
    char* chars = stackalloc char[length];
    for (int i = 0; i < length; i++)
    {
        chars[i] = alphabet[random.Next(alphabet.Length)];
    }
    return new string(chars);
}
```

上面的代码只发生了一次堆分配：就是 string()。临时缓冲区是分配给堆栈的，但是您需要使用 unsafe 修饰符，因为您使用的是指针。尽量不要使用不安全代码，很容易出现问题，尽管能保证上面的代码是没问题的。

一个好消息就是我们可以通过 Span<T> 安全的实现上面不安全实现的功能：

```c#
public static string Generate3(string alphabet, Random random, int length)
{
    Span<char> chars = stackalloc char[length];
    for (int i = 0; i < length; i++)
    {
        chars[i] = alphabet[random.Next(alphabet.Length)];
    }
    return new string(chars);
}
```

通过 Span + stackalloc 安全代码来实现不安全代码同样的效果。但是这样仅仅只是少了一次内存分配，但数据复制还是没有避免。我们可以继续优化：

```c#
public static string Generate4(string alphabet, Random random, int length) =>
string.Create(length, (alphabet, random), (span, state) =>
{
    var alphabet2 = state.alphabet;
    var random2 = state.random;
    for (int i = 0; i < span.Length; i++)
    {
        span[i] = alphabet2[random2.Next(alphabet2.Length)];
    }
});
```

通过 span 以及内置的 `public static String Create<TState>(int length, TState state, SpanAction<char, TState> action);` 来减少数据的拷贝。