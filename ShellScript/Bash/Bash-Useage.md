## 重命名命令

```bash
alias lm='ls -al' 
```

输入 `lm` 就相当于输入 `ls -al`，提高效率。

## 输出相关文件的权限

```bash
ls -ld `locate crontab` # 或者
ls -ld $(locate crontab)
```

## 添加环境变量

```
export
```

## 参数的删除和替换

```
${variable#/*local/bin:}	# 其中 # 为关键字！删除指定的内容
echo ${path##/*:}	# 两个 # 表示符合最长的变量；一个 # 为最短的
echo ${path%:*bin}	# 删除最后面的目录，一直到 bin 为止的字符串
```

| 命令                       | 说明                                                     |
| -------------------------- | -------------------------------------------------------- |
| ${变量#关键字}             | 将变量内容从头开始，符合关键字的，将符合的最短内容删除   |
| ${变量##关键字}            | 将变量内容从头开始，符合关键字的，将符合的最长内容项删除 |
| ${变量%关键字}             | 将变量内容从前查找，符合关键字的，将符合的最短内容项删除 |
| ${变量%%关键字}            | 将变量内容从前查找，符合关键字的，将符合的最长内容项删除 |
| ${变量/旧字符串/新字符串}  | 变量内容中的旧字符串，匹配出第一个内容将其用新字符串替换 |
| ${变量//旧字符串/新字符串} | 变量内容中的旧字符串，匹配出全部内容将其用新字符串替换   |

null 替换：

```
username=""
username=${username-root} # 如果username不存在或为空，则将赋值为root
```

