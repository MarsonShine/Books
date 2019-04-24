using System;
using System.Collections.Immutable;
namespace _08Collections {
    //不可变栈和队列
    //使用情景：不是经常改变，可以被多个线程共同访问的栈或队列
    //不可变栈和队列在栈（Stack）和队列（Queue）性能上，时间复杂度是相同的
    //但是在频繁的更新操作情况下，Stack、Queue 性能要比 不可变Stack、Queue 要高
    public class ImmutableStackAndQueue {
        //stack push值返回的是新的不可变集合
        // stack 与 newStack 是两个对象，并且值互不受影响
        // 并且返回的新的集合过程中，会共享之前 stack 的空间
        public static void StartStack() {
            var stack = ImmutableStack<int>.Empty;
            stack = stack.Push(100);
            //返回的stack 共享了之前 100 的内存
            stack = stack.Push(99);
            //newStack 共享了 100,99 的内存空间
            var newStack = stack.Push(50);
            foreach (var item in stack) {
                Console.WriteLine(item);
            }
            foreach (var item in newStack) {
                Console.WriteLine(item);
            }
        }

        public static void StartQueue() {
            var queue = ImmutableQueue<int>.Empty;
            queue.Enqueue(100);
            queue.Enqueue(90);
            var newQueue = queue.Enqueue(80);
            foreach (var item in queue) {
                Console.WriteLine(item);
            }
            foreach (var item in newQueue) {
                Console.WriteLine(item);
            }
        }
    }
}