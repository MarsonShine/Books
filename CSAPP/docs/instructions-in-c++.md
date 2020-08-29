# 数据传输类指令

`mov s d`：数据传输类指令，是将数据从一个位置复制到另一个位置的指令。都是把**数据从源位置复制到目的位置**

s：源操作数；d：目的操作数

mov 类指令由下面四个指令构成

- `movb	s, d`	传输字节
- `movw    s, d`	传输字
- `movl    s, d`    传输双字
- `movq    s, d`    传输四字
- `movabsq    s, d`    传输绝对四字

> 需要注意的是，源操作数与目标操作数不能同时都是立即数或内存

movz 和 movs 将较小的源值复制到较大的目的时。所有的这些指令都是把**数据从源（寄存器或内存）复制到目的寄存器**

movz 把目的中剩余的字节填充为 0。

movs 通过符号拓展来填充，把源操作从最高位进行复制。这些指令的最后两个字符分别表明源的大小和目标的大小。

```c++
movzbw		将做了零拓展的字节传输到字
movzbl		将做了零拓展的字节传输到双子
movzwl
movzbq
movzwq

movsbw		将做了符号拓展的字节传输到字
movsbl		将做了符号拓展的字节传输到双字
movswl
movsbq
movswq
cltq		把 %eax 符号拓展到 %rax	// cltq 指令只用作与寄存器 %eax 和 %rax
```

## 练习题解析

- `movb		$0xf, (%ebx)`     // ebx 不能用作地址寄存器
  - ebx 对于（x86-64）来说是不能用作地址寄存器的，但是 rbx 是可以的。因为 ebx 是 rbx 的低 32 位，尝试用 32 位寄存器来引用 64 位内存来说毫无意义
- `movl %rax, (%rsp) movb %si, 8(%rbp)` // 指令后缀和寄存器id不匹配
  - 因为使用的指令是 'l'，说明是双字，也就是意味着这是 32 位的。但是 rax 是 64 位寄存器。如果你要写 64 位数据到 rax 上，那么应该使用 `movq`。所以当在 32 位下应该使用指令 `eax`
- `movl %eax, %rdx`     // 目标操作符大小不正确
  - 这个指令意思是尝试将一个 32 位的值传输到 64 位寄存器下。有一些指令可以进行这种转换，但是 movl 这个指令不能做这种转换。

逆向工程，根据指令内容写出具体的源代码

```c++
void decode1(long *xp, long *yp, long *zp)
// 其中 xp in %rdi, yp in %rsi, zp in %rdx
decode1:
  movq  (%rdi), %r8
  movq  (%rsi), %rcx
  movq  (%rdx), %rax
  movq  %r8, (%rsi)
  movq  %rcx, (%rdx)
  movq  %rax, (%rdi)
  ret
```

解析出来如下

```c++
void decode1(long* xp, long* yp, long* zp) {
	long x = *xp;
	long y = *yp;
	long z = *zp;
	
	*yp = x;
	*zp = y;
	*xp = z;
}
```

## 算术和逻辑操作指令

```
leaq  s, d		// 加载有效地址
inc	 d		// +1
dec  d		// -1
neg  d		//去负数
not  d		//取补
add	 s, d	// d = d + s
sub  s, d	// d = d - s
imul  s, d	// d = d * s
xor  s, d	// d = d ^ s
or  s, d	// d = d | s
and  s, d	// d = d & s
sal  k, d	// d = d << k	左移
shl  k, d	// d = d << k	左移（等同于sal）
sar	 k, d	// d = d >>a k	算数右移
shr  k, d	// d = d >>l k  逻辑右移
// 算术移动，填对应的符号位
// 逻辑移动，填充 0
```

leaq 是加载有效地址（load effective address）指令，是 movq 指令的变形。它的指令形式是从内存读数据到寄存器，但实际上它没有引用内存。而是将有效地址写入到目的操作数，它的目的操作一定是寄存器。比如 `leaq 7(%rdx,%rdx,4)` ，其中 rdx 中的值假设为 x。则结果就是将寄存器 rdx 的值设置为 (4 * x + x) + 7。

触发指令比较特殊，举例来说

```c++
void remdiv(long x, long y, long* qp, long* rp) {
	long q = x / y;
	long r = x % y;
	*qp = q;
	*rp = r;
}

// 上面对应得指令内容
// x in %rdi, y in %rsi, qp in %rdx, rp in %rcx
remdiv:
  movq  %rdx, %r8	// 复制 qp 的值出来，必须首先把参数 qp 保存到另一个寄存器，因为触发操作要使用参数寄存器 %rdx
  // 开始准备被除数
  movq  %rdi, %rax	// 移动 x 到低位 8 字节
  cqto			   // 复制符号拓展 x 到高位 8 字节
  idivq  %rsi	    // 除以被除数 y
  movq  %rax, (%r8)	// 存储商到 qp 中
  movq  %rdx, (%rcx)// 存储余数到 rp 中
  ret
```

# 条件码指令

除了整数寄存器，CPU 还维护着一组单个位的条件码（condition code）寄存器。可以检测这些寄存器来执行具体的条件指令。最常用的条件码有：

- CF：进位标志。最近的操作使 最高位产生进位。可以用来检测无符号操作的溢出。
- ZF：零标志。最近的操作得出的结果为 0。
- SF：符号标志。最近的操作得出的结果为负数。
- OF：溢出标志。最近的操作导致一个补码溢出 —— 正溢出或负溢出