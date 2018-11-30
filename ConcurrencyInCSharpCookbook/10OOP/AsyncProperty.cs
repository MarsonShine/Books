using System;
using System.Threading.Tasks;

namespace _10OOP {
    /// <summary>
    /// 异步属性，把属性的获取改成异步方式
    /// </summary>
    public class AsyncProperty {

    }

    //pseudo-code
    // public int Data {
    //     async get {
    //         await Task.Delay(TimeSpan.FromSeconds(2));
    //         return 13;
    //     }
    // }
}
//分两种情况：
//1：每调用一次属性，就会产生后台工作线程
//这种应该吧属性改成异步方法
//2：调用多次执行一次异步计算
//这种是就把第一次异步计算的结果缓存起来，然后多次调用取缓存的值