# Redis中的数据结构

## 压缩链表（ziplist）

ziplist **是一个经过特殊编码的双链表**，旨在提高内存效率。**它存储字符串和整数值**，其中整数被编码为实际整数而不是一系列字符。**它允许在 O(1) 时间内对列表的头部和尾部进行推送和弹出操作**。但是，**由于每个操作都需要重新分配 ziplist 使用的内存，因此实际复杂度与 ziplist 使用的内存量有关。**

压缩链表总体布局如下图所示

![](https://static001.geekbang.org/resource/image/5d/22/5df168dcafa0db242b0221ab10114e22.jpg)

<uint32_t zlbytes> 是一个无符号整数，用来保存 ziplist 占用的字节数，包括 zlbytes 字段本身的四个字节。需要存储这个值才能调整整个结构的大小，而无需先遍历它。

<uint32_t zltail> 是列表中最后一个条目的偏移量。 这允许在列表的远端进行弹出操作，而无需完全遍历。 

<uint16_t zllen> 是条目数。 当条目超过 2^16-2 时，该值设置为 2^16-1，我们需要遍历整个列表才能知道它包含多少项。 

<uint8_t zlend> 是表示 ziplist 结尾的特殊条目。被编码为等于 255 的单个字节。没有其他正常条目以设置为 255 值的字节开头。

ziplist 中的每个条目都以包含两条信息的元数据为前缀。首先，存储前一个条目的长度，以便能够从后到前遍历列表。其次，提供条目编码。它表示条目类型，整数或字符串，如果是字符串，它还表示字符串有效负载的长度。因此，完整的条目存储如下：

```
<prevlen> <encoding> <entry-data>
```

有时编码代表条目本身，就像我们稍后会看到的小整数(small integers)一样。在这种情况下，缺少 `<entry-data>` 部分，我们可以：

```
<prevlen> <encoding>
```

前一个条目的长度 `<prevlen>` 以下列方式编码：

如果这个长度小于 254 字节，它将只消耗一个字节，将长度表示为一个无符号的 8 位整数。当长度大于等于254时，会消耗5个字节。第一个字节设置为 254 (FE)，表示后面有一个更大的值。剩下的 4 个字节取前一个条目的长度作为值。

所以实际上一个条目是按以下方式编码的:

```
<prevlen from 0 to 253> <encoding> <entry>
```

或者，如果前面的条目长度大于253字节则使用以下编码:

```
0xFE <4字节unsigned 小端 prevlen> <encoding> <entry>
```

条目的编码字段取决于条目的内容。当条目是字符串时，编码第一个字节的前 2 位将保存用于存储字符串长度的编码类型，然后是字符串的实际长度。当条目是整数时，前 2 位都设置为 1。接下来的 2 位用于指定在此标头之后将存储哪种整数。不同类型和编码的概述如下。第一个字节总是足以确定条目的类型。

// TODO，后续的数据存储形式与编码方式等深入了解之后再回过头来看



压缩列表和双向列表都会记录表头和表尾的偏移量，这样对于 `List` 类型的 `LPOP、RPOP、LPUSH、RPUSH` 这四个操作命令，它们在链表的头部和尾部增加删除元素，这种方式是非常快的，可以通过偏移量直接定位。复杂度是 O(1) 的。