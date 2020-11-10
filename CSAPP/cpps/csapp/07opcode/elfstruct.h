#pragma once
/// <summary>
/// ELF 符号表条目结构
/// </summary>
typedef struct {
	int name;	// 字符串表中的字节偏移
	char type : 4,
		binding : 4;	// 表示符号是本地还是全局的
	char reserved;
	short section;	// 到头部表的索引
	long value;	// 符号地址，对于可重定位的模块来说就是举例目标的节起始位置的偏移
	long size;	// 目标的大小（以字节为单位）
} Elf64_Symbol;
