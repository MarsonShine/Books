## EnumSet\<T>

EnumSet\<T> 的基础是 long，一个 long 是 64 位，而一个 enum 实例一个 bit 就能表示，就说明 EnumSet\<T> 理论上只能存储 64 个 enum 实例。那么事实上是不是这样呢？我们用超过 64 个 enum 存储发现，事实并不是这样的，说明内部不是我想的这样。

我们来看看 EnumSet\<T> 源码是如何做处理的



```java
public static <E extends Enum<E>> EnumSet<E> noneOf(Class<E> elementType) {
    Enum<?>[] universe = getUniverse(elementType);
    if (universe == null)
        throw new ClassCastException(elementType + " not an enum");

    if (universe.length <= 64)
        return new RegularEnumSet<>(elementType, universe);
    else
        return new JumboEnumSet<>(elementType, universe);
}
```

首先是把元素集合转化成数组，之后重点就在判断 `universe.Length <= 64` ，如果小于 64 那么就在我们之前理解的那样。那么如果大于 64 位呢，我们重点就来看下 `JumboEnumSet<>` 这个范型类做了什么。

```java
JumboEnumSet(Class<E>elementType, Enum<?>[] universe) {
    super(elementType, universe);
    elements = new long[(universe.length + 63) >>> 6];
}
```

证明在枚举实例个数超过 64 个的时候，构建的超大(jumbo)的集合所做的就是用一个 long 的数组来装载。有意思的是，是用 `(universe.Length + 63) >>> 6` 右移 6 位来保证数组不会过大尔浪费内存，来保证性能。

## EnumMap

EnumMap 是一种特殊的 Map，它要求 key 必须来自 enum，enum 本身的限制，所以 EnumMap 内部是用数组组成的，所以查询速度很快。

## Java 枚举 VS C# 枚举

java 枚举与 c# 枚举有很大的区别。因为 java 枚举除了定义数据项之外，还能在枚举类定义方法，不仅如此，还会给枚举类中的每项定义自定义。以至于可以抽象出来达到每个项成为不同的状态（多态）。

同样，在 Java 中枚举中的项是不能像 C# 中枚举一样可以在方法中当作参数传递的。

考虑下面代码：

```java
enum LikesClass {
    ITEM1 { void action(){ print("action1");}},
    ITEM2 { void action(){ print("action2");}},
    ITEM3 { void action(){ print("action3");}};
    abstract void action();
}
//以下是尝试把枚举项传参
//是不允许这样的
class NotPromise {
    void method(LikesClass.ITEM1 instance){}
}
//编译器会尝试翻译以下代码
//资料来源《thinking in java》
abstract class LikesClass extends java.lang.Enum{
    public static final LikesClass ITEM1;
    public static final LikesClass ITEM2;
    public static final LikesClass ITEM3;
}
//就好比非静态方法是不允许调用一个静态成员的。
```

