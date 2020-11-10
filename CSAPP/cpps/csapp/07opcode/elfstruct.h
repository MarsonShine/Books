#pragma once
/// <summary>
/// ELF ���ű���Ŀ�ṹ
/// </summary>
typedef struct {
	int name;	// �ַ������е��ֽ�ƫ��
	char type : 4,
		binding : 4;	// ��ʾ�����Ǳ��ػ���ȫ�ֵ�
	char reserved;
	short section;	// ��ͷ���������
	long value;	// ���ŵ�ַ�����ڿ��ض�λ��ģ����˵���Ǿ���Ŀ��Ľ���ʼλ�õ�ƫ��
	long size;	// Ŀ��Ĵ�С�����ֽ�Ϊ��λ��
} Elf64_Symbol;
