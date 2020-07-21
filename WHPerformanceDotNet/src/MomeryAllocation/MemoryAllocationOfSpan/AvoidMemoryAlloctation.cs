using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MomeryAllocation.MemoryAllocationOfSpan
{
    /// <summary>
    /// Span<T> 表示一个连续块的内存
    /// </summary>
    public class AvoidMemoryAlloctation
    {
        public static void Start()
        {
            // Span<T> 用在了托管堆
#pragma warning disable CS0164 // 这个标签尚未被引用
            CASE1:
#pragma warning restore CS0164 // 这个标签尚未被引用
            var array = new byte[] { 0, 1, 2, 3 };
            Span<byte> byteSpan = new Span<byte>(array, 1, 3);
            PrintSpan(byteSpan);
            // Span<T> 包装栈分配数组
#pragma warning disable CS0164 // 这个标签尚未被引用
            CASE2:
#pragma warning restore CS0164 // 这个标签尚未被引用
            unsafe
            {
                int* stackMem = stackalloc int[4];
                Span<int> intSpan = new Span<int>(stackMem, 4);
                for (int i = 0; i < intSpan.Length; i++)
                {
                    intSpan[i] = 13 + i;
                }
                PrintSpan(intSpan);
            }
            // 分配本地堆，当你用Span<T>来包装非托管内存时，必须指定要分配的字节数。
            // 分配的内存要指定类型，所以span的长度是对象的个数，而不是字节数组的长度。
#pragma warning disable CS0164 // 这个标签尚未被引用
            CASE3:
#pragma warning restore CS0164 // 这个标签尚未被引用
            unsafe
            {
                int memSize = sizeof(int) * 4;
                IntPtr hNative = Marshal.AllocHGlobal(memSize);
                Span<int> unmanagedSpan = new Span<int>(hNative.ToPointer(), 4);
                for (int i = 0; i < unmanagedSpan.Length; i++)
                {
                    unmanagedSpan[i] = 100 + i;
                }
                PrintSpan(unmanagedSpan);
                Marshal.FreeHGlobal(hNative);
            }
            ReadOnlySpan<char> subString = "NonAllocatingSubstring".AsSpan().Slice(13);
            PrintSpan(subString);
        }

        private static void PrintSpan<T>(Span<T> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                ref T val = ref span[i];    // 避免内存再次分配，复制
                Console.Write(val);
                if (i < span.Length - 1) {
                    Console.Write(", ");
                }
            }
            Console.ReadLine();
        }

        private static void PrintSpan<T>(ReadOnlySpan<T> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                T val = span[i];    // 避免内存再次分配，复制
                Console.Write(val);
                if (i < span.Length - 1)
                {
                    Console.Write(", ");
                }
            }
            Console.ReadLine();
        }
    }
}
