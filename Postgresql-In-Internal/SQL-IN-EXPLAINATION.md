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