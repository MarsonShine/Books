void swap();

int buf[2] = {1, 2};

int main()
{
    swap();
    return 0;
}

// buf 是全局变量，所以所处的节点是 .data 节点，在符号标表，符号表类型为 extern（外部）
// bufp0 是全局变量，所处节点.data，在符号表中（全局）
// bufp1 是全局变量, 因为是未初始化的所以节点在COMMON中，类型为全局
    // 为什么不是.bss节点？尽管.bss和COMMON都是未初始化的全局变量，但是.bss指的静态变量，以及初始化为0的全局或静态变量
// swap 函数，所处节点为 .text，在符号表中，类型为全局