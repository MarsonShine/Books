using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chapter13_HighPerformancePassByReference
{
    /*
     RefLikeStruct 仅作为实例字段的类型，绝不是静态字段。
     不能对 RefLikeStruct 装箱
     不能使用 RefLikeStruct 作为类型参数
     不能使用 RefLikeStruct[] 或者任何类似的数组类型作为 typeof 操作符的操作数。
     RefLikeStruct 不能使用在编译器要捕获的地方，如异步方法，迭代器块，任何本地方法的本地变量，linq、匿名方法、lambda
     */
    class RefLike
    {
    }
}
