#include "optvec.h"
#include <crtdbg.h>
vec_ptr new_vec(long len) {
	/* 分配头结构 */
	vec_ptr result = (vec_ptr)malloc(sizeof(vec_rec));
	data_t* data = NULL;
	if (!result) {
		return NULL;/* 无法分配内存 */
	}
	result->len = len;
	/* 分配数组 */
	if (len > 0) {
		data = (data_t*)calloc(len, sizeof(data_t));
		if (!data) {
			free((void*)result);
			return NULL;/* 无法分配内存 */
		}
	}

	/* Data 为null或者分配一个数组 */
	result->data = data;
	return result;
}

/// <summary>
/// 接收向量元素并存储到目标 dest 上
/// </summary>
/// <param name="v"></param>
/// <param name="index"></param>
/// <param name="dest"></param>
/// <returns>返回0（越界）或是1（成功）</returns>
int get_vec_element(vec_ptr v, long index, data_t* dest) {
	if (index < 0 || index >= v->len)	/* 做边界检查，带来了程序的安全性，但是作为代价，性能会有点损失 */
		return 0;
	*dest = v->data[index];
	return 1;
}

/// <summary>
/// 返回向量的长度
/// </summary>
/// <param name="v"></param>
/// <returns></returns>
long vec_length(vec_ptr v) {
	return v->len;
}

#define IDENT 0
#define OP +
/* 以下是上面优化的程序实例，合并程序 */
void combine1(vec_ptr v, data_t* dest) {
	long i;
	*dest = IDENT;
	for (i = 0; i < vec_length(v); i++)
	{
		data_t val;
		get_vec_element(v, i, &val);
		*dest = *dest OP val;
	}
}

void combine2(vec_ptr v, data_t* dest) {
	long i;
	long length = vec_length(v);
	*dest = IDENT;
	for (i = 0; i < length; i++)
	{
		data_t val;
		get_vec_element(v, i, &val);
		*dest = *dest OP val;
	}
}

data_t* get_vec_start(vec_ptr v) {
	return v->data;
}

/* 直接访问数组结构 */
void combine3(vec_ptr v, data_t* dest) {
	long i;
	long length = vec_length(v);
	data_t* data = get_vec_start(v);
	*dest = IDENT;
	for (size_t i = 0; i < length; i++)
	{
		*dest = *dest OP data[i];
	}
}

void combine4(vec_ptr v, data_t* dest) {
	long i;
	long length = vec_length(v);
	data_t* data = get_vec_start(v);
	data_t acc = IDENT;

	for (size_t i = 0; i < length; i++)
	{
		acc = acc OP data[i];
	}
	*dest = acc;
}

void combine5(vec_ptr v, data_t* dest) {
	long i = 0;
	long length = vec_length(v);
	long limit = length - 1;
	data_t* data = get_vec_start(v);
	data_t acc = IDENT;

	/* 一次计算两个元素 */
	for (i = 0; i < limit; i += 2)
	{
		acc = (acc OP data[i]) OP data[i + 1];
	}
	/* 完成剩下的元素 */
	for (; i < length; i++)
	{
		acc = acc OP data[i];
	}
}

void combine6(vec_ptr v, data_t* dest) {
	long i = 0;
	long length = vec_length(v);
	long limit = length - 1;
	data_t* data = get_vec_start(v);
	data_t acc0 = IDENT;
	data_t acc1 = IDENT;

	for (i = 0; i < limit; i+2)
	{
		acc0 = acc0 OP data[i];
		acc1 = acc1 OP data[i + 1];
	}
	// 剩下的
	for (; i < length; i++)
	{
		acc0 = acc0 OP data[i];
	}
	*dest = acc0 OP acc1;
}

void lower1(char* s) {
	long i;
	for (i = 0; i < strlen(s); i++)
	{
		if (s[i] >= 'A' && s[i] <= 'Z') {
			s[i] -= ('A' - 'a');
		}
	}
}

size_t strlen(const char* s) {
	long length = 0;
	while (*s != '\0')
	{
		s++;
		length++;
	}
	return length;
}