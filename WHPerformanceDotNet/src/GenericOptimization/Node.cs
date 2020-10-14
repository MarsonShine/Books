using System;

namespace GenericOptimization
{
public class Node
{
    public Node()
    {
        // 从构造函数抛出的错
        // 会将错误包装到 TargetInvocationException 错误中
        //throw new InvalidOperationException();
        //Create();
    }

    public Node Create()
    {
        throw new InvalidOperationException();
    }
}
}