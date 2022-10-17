# Shell Script 调试技巧

## 检查语法问题

```shell
sh -nvx scripts.sh
```

选项参数：

-n：不要执行 script，仅检查语法问题

-v: 在执行 script 前，先将 scripts 的内容输出到屏幕

-x: 将使用到的 script 内容展示到屏幕中