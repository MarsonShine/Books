# IO操作

C++ IO标准库中有三个独立的文件

- iostream
  - istream,wistream 从流中读取数据
  - ostream,wostream 向流写入数据
  - iostream,wiostream 读写流
- fstream
  - ifstream,wifstream 从文件中读取数据
  - ofstream,wofstream 向文件中写入数据
  - fstream,wfstream 读写文件
- sstream
  - istringstream,wistringstream 从string读取数据
  - ostringstream,wostringstream 向string写入数据
  - stringstream,wstringstream 读写string

流类型无法拷贝和赋值。

## 缓冲区

每个输出流都会管理自己的缓冲区，缓冲区是保存在操作系统中的。**有了缓冲机制就可以将程序的多个输出操作合并成一个系统级写操作**。这样可以带来很大的性能提升。

### 刷新缓冲

通过操作`endl`、`ends`、`flush`可以立即输出流并刷新缓冲区。

设置`unitbuf`则表明之后的操作每次写操作之后都进行一次flush操作。而要恢复，则需要调用`nounitbuf`恢复到之前的缓冲方式。

```c++
cout << unitbuf; // 所有输出操作后都会立即刷新缓冲区
cout << nounitbuf; // 恢复之前的缓冲机制
```

## 文件操作

`fstream`、`ifstream`、`ofstream`等都是对文件的操作，这些都是集成自`iostream`类型的，除了拥有父类的所有行为之外，还特地为文件操作提供了几个操作

| fstream fstrm;          | 创建一个未绑定的文件流                                    |
| ----------------------- | --------------------------------------------------------- |
| fstream fstrm(s);       | 创建一个fstream文件流，参数可以是string类型，也可以是指针 |
| fstream fstrm(s, mode); | 按指定的模式创建一个fstream文件流                         |
| fstrm.open(s);          | 打开名为s的文件                                           |
| fstrm.close();          | 关闭文件流                                                |
| fstrm.is_open();        | 判断文件是否打开（正在操作中）                            |

### 最佳实践

在初始化文件流或打开文件时，一定要记得检查是否成功（异常）

```c++
ofstream out;
out.open(ifile+".copy");
if (out) {
	// open 成功，可以正常后续操作
}
```

注意我们使用文件流这些对象时，一定要对**销毁操作**留个心眼。<font color='red'>如果这个对象是在循环中创建的，那么这个文件流对象的作用域就仅仅在循环块中，每次循环结束都会自动进行销毁。即自动调用`close`方法</font>

### 文件序列化对象

有这样一个文件内容

```
marsonshine 18975152235 010-8562-5562
summerzhu 15711255547
happyxi 13365236652 010-8856-2236 020-5523-856
```

这样文件内容都是以一个人名开始，后面跟着手机电话号码。那么可以先申明一个对象来接收文件信息

```c++
struct PersonInfo {
	string name;
	vector<string> phones;
};
```

接收信息并实例化对象

```c++
string line, word;
vector<PersonInfo> people;
// 逐行读取数据
while (getline(cin, line)) {
	PersonInfo info;
	istringstream record(line);	// 将读取的一行记录绑定到字符串流中
	record >> info.name;// 读取名字
	while (record >> word) { // 读取电话号码
		info.phones.push_back(word);
	}
	people.push_back(info);
}
```

