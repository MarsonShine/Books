// 03opcode.cpp : 此文件包含 "main" 函数。程序执行将在此处开始并结束。
//

#include <iostream>
#include "03opcode.h"
using namespace std;


// 函数加载，就会把两个参数放进栈中
// xp 入栈寄存器 %rdi
// y 入栈寄存器 %rsi
long exchanges(long* xp, long y) {
	// 交换指令
	long x = *xp;   // movq (%rdi), %rax    // 将 %rdi 寄存器的值从源位置移动到 %rax,并设置返回值
	*xp = y;        // movq %rsi, (%rdi)
	return x;
}

void decode1(long* xp, long* yp, long* zp) {
	long x = *xp;
	long y = *yp;
	long z = *zp;

	*yp = x;
	*zp = y;
	*xp = z;
}

void remdiv(long x, long y, long* qp, long* rp) {
	long q = x / y;
	long r = x % y;
	*qp = q;
	*rp = r;
}

int main()
{
	cout << "Hello World!\n";
	long x = 32;

	cout << exchanges(&x, 23);
}


// 运行程序: Ctrl + F5 或调试 >“开始执行(不调试)”菜单
// 调试程序: F5 或调试 >“开始调试”菜单

// 入门使用技巧: 
//   1. 使用解决方案资源管理器窗口添加/管理文件
//   2. 使用团队资源管理器窗口连接到源代码管理
//   3. 使用输出窗口查看生成输出和其他消息
//   4. 使用错误列表窗口查看错误
//   5. 转到“项目”>“添加新项”以创建新的代码文件，或转到“项目”>“添加现有项”以将现有代码文件添加到项目
//   6. 将来，若要再次打开此项目，请转到“文件”>“打开”>“项目”并选择 .sln 文件
