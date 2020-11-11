#define NET5
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace CSharpGuide.performance
{
    public class ArrayChunkOfStruct<T> where T : struct
    {
        private readonly T[] _array;
        public ArrayChunkOfStruct(int size)
        {
            _array = new T[size];
        }
#if FIRST
        /// <summary>
        /// 由于<see cref="T"/>是结构体，这个索引返回的其实是_array[index]的副本
        /// 这里存在一定的性能损失（非常小，但是有可优化的空间）
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index] => _array[index]; 
#endif
#if Second
        /// <summary>
        /// 关键字ref，可以让 _array[index] 返回地址的引用，而不需要发生数据的拷贝
        /// 性能比上一个好
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref T this[int index] => ref _array[index]; 
#endif
        /// <summary>
        /// 实际上，上面的代码观察汇编代码就会发现，有一个数组去索引值会有一个边界检查的判断指令 cmp
        /// 如果能保证访问的索引值是正确的（不会索引越界），那么就会省去一个检查边界的指令时间，性能更优
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref T ItemRef(int index) => ref Unsafe.Add(ref _array[0],index); // 这里还是存在边界检查，因为 _array[0] 还是存在索引访问

        public ref T ItemRef2(int index)
        {
            var span = new Span<T>(_array);
            return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index); // 性能还要差，因为多了 Span 的转换和处理，数组边界检查也少不了
        }

#if NET5
        /// <summary>
        /// .NET5 提供了这个函数<see cref="MemoryMarshal.GetArrayDataReference{T}({T[]})"/>
        /// 直接对象元素地址引用
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref T ItemRef3(int index)
        {
            ref var data = ref MemoryMarshal.GetArrayDataReference(_array);
            return ref Unsafe.Add(ref data, index);
        } 
#endif

#if NET50
        internal static ref T GetArrayDataReference<T>(T[] array)
        {
            return ref Unsafe.As<byte, T>(ref Unsafe.As<RowArrayData>(array).Data);
        } 
#endif
    }
}
