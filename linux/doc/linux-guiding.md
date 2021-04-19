# Linux 分析数据/批处理

```bash
cat /var/log/nginx/access/log |	# 读取日志文件
	awk '{print $7}' |	# 文本分析命令，将每行按空格分割成不同的字段，每行只输出第七个字段
	sort |	# 按字母顺序排序 URL 地址列表
	uniq -c |	# 通过检查两条相邻的行是否相同来过滤掉其输入中的重复行，-c 选项为一个计数器，统计重复的次数
	sort -r -n |	# 排序，-n 按每行起始处的数字排序，然后反向顺序 -r 输出结果。
	head -n 5	# head 只输出输入中的前 5 行，并丢弃其他数据
```

awk 命令是 linux 文本分析非常强大的一个工具。awk 除了分析每行的内容之外，还能增加多种限制。如可以增加正则式：

`$7 !~ /\.css$/ {print $7}`

更多用法资料详见：https://www.runoob.com/linux/linux-comm-awk.html