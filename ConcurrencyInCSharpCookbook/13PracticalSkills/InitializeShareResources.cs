using System;
using System.Threading.Tasks;

namespace _13PracticalSkills {
    /// <summary>
    /// 初始化共享资源
    /// 程序的多个部分共享资源，要对第一次访问资源时对其初始化
    /// </summary>
    public class InitializeShareResources {
        //用延迟执行类可以解决这个问题
        //Lazy<T>
        static int _simpleValue;
        static readonly Lazy<int> MySharedInteger = new Lazy<int>(() => _simpleValue++);

        void UseSharedInteger() {
            //不管同时有多少线程同时调用 UseSharedInteger，这个工厂委托只会执行一次，并且所有线程都是等待同一个实例。实例在创建后会被缓存起来，以后对这个属性的方法都会返回同一个实例
            //Lazy<T> 内部其实就是用的单例模式
            int sharedValue = MySharedInteger.Value;
        }

        //如果初始化过程需要执行异步任务，可以采用这种方法 Lazy<Task<T>>
        static readonly Lazy<Task<int>> MySharedAsyncInteger = new Lazy<Task<int>>(async() => {
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            return _simpleValue++;
        });

        async Task GetSharedIntegerAsync() {
            int sharedValue = await MySharedAsyncInteger.Value;
        }
        //不同上下文线程（线程池，UI线程）访问共享资源时，尽量都访问线程池线程中返回的实例
        static readonly Lazy<Task<int>> MySharedAsyncInteger_ThreadPool = new Lazy<Task<int>>(() => Task.Run(async() => {
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            return _simpleValue++;
        }));
    }
}