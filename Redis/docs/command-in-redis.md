# Redis中的指令

## 范围操作

范围操作是指集合类型中的遍历操作，可以返回集中中所有的数据。此类命令有

- Hash类型的HGETALL
- Set类型的SMEMBERS

返回部分数据

- List类型的LRANGE
- ZSet类型的ZRANGE

范围操作一般都是链表结构，其操作复杂度一般是O(N)。

这种范围查询的操作尽量避免，因为比较耗时。特别是像这种 `HGETALL、SMEMBERS` 这种一次性返回所有元素，会很容易导致 Redis 阻塞。

比上面范围查询性能较好的，就是Redis2.8以上版本推出的 `SCAN` 系列命令操作，这种命令只会返回有限数量的元素，这样就避免了一次性返回所有元素（如前描述）引起的 Redis 阻塞。

通过**渐进式遍历**返回有限数量部分数据

- Hash类型的HSCAN
- Set类型的SSCAN
- Ziplist类型的ZSCAN

集合的统计操作速度非常快，因为当集合采用压缩列表、双向链表、整数数组这些数据结构时，这些结构其实已经专门记录了元素的个数。