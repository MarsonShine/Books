# PostgreSQL 代码解析

```sql
INSERT INTO auditlogs("BizId","Table","Type","Status","Message","CreateTime","UpdateTime") 
VALUES (@BizId,@Table,@Type,@Status,@Message,@CreateTime,@UpdateTime) 
ON CONFLICT("Table","BizId","Type") 
DO UPDATE SET "Status"=EXCLUDED."Status","Type"=EXCLUDED."Type","Message"=EXCLUDED."Message","UpdateTime"=EXCLUDED."UpdateTime"
```

`INSERT INTO table_name (column1, column2, ...) VALUES (value1, value2, ...) ON CONFLICT (conflict_target) DO UPDATE SET column1 = value1, column2 = value2, ...` 是一个特定语法结构，用于实现 UPSERT（更新或插入）操作。

如果表中已有一条记录使得插入失败（如主键冲突），通常会抛出错误。

其中 `EXCLUDED` 表示在冲突发生时的数据。你可以使用 `EXCLUDED.column_name` 来引用这些新行中的某个列的值。

```sql
WITH batch AS (
  SELECT id FROM vac 
  WHERE NOT processed 
  LIMIT 1000
  FOR UPDATE SKIP LOCKED
)
UPDATE vac 
SET processed = true
WHERE id IN (SELECT id FROM batch);
```

`WITH batch` 是一个 **CTE（公用表表达式）**，临时选择出最多 1000 条未处理（`processed = false`）的记录。

`FOR UPDATE`: 将查询结果中的行加锁，防止其他事务同时修改这些行。

`SKIP LOCKED`: 如果某些行已经被其他事务加锁，则跳过这些行。

这可以避免事务因为等待锁而阻塞，提高并发性能。

```sql
EXPLAIN (analyze, buffers, costs off, timing off, summary off)
  SELECT * FROM knowledgepoints;
```

`EXPLAIN` 是 PostgreSQL 中用于分析查询执行计划的工具，可以帮助了解查询语句的性能表现和执行路径。

- analyze: 运行查询并报告实际执行时间和行数统计信息，而不仅仅是估计值。目的是通过实际执行数据来更准确地评估查询性能。
- buffers：报告查询中访问的内存缓冲区（内存块）和磁盘块的信息。查询是否频繁访问磁盘，以及内存使用是否高效。
- costs：off 关闭，隐藏成本相关指标（如启动成本、总成本）。目的是为了减少输出中无关的成本信息，使分析更聚焦。
- timing：off 关闭，同理 costs off。避免计时误差对查询分析的干扰，尤其是在并行查询或非常短的查询中。
- summary：off 关闭，同理关闭关于查询总执行时间的汇总信息。减少额外的汇总数据，简化分析结果。

> 也可以直接像 MYSQL 那样直接：EXPLAIN SELECT * FROM knowledgepoints。这样查询的只是估计值，要想精确的分析性能和查询计划，还是得配合 analyze 看实际执行的计划。

以我查询的结果为例分析：

```
Seq Scan on knowledgepoints (actual rows=98 loops=1)
  Buffers: shared hit=5
Planning:
  Buffers: shared hit=52
```

结果表明查询对 `knowledgepoints` 表执行了顺序扫描（即全表扫描）。循环一次查询返回98行数据。Buffers 显示查询访问了5个共享内存页，这说明没有发生磁盘访问（I/O）。Planning 表明在估计阶段，数据查询会访问52个共享内存页（元数据和系统缓存）。

