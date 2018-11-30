using System;
using System.Threading.Tasks;

namespace _10OOP {
    /// <summary>
    /// 异步构建构造函数，通过私有化构造器，并用工厂方法构建一个异步构造类
    /// </summary>
    public class AsyncConstructorClass {
        private AsyncConstructorClass() { }

        private async Task<AsyncConstructorClass> InitializeAsync() {
            await Task.Delay(TimeSpan.FromSeconds(1)); //初始化前的一些逻辑
            return this;
        }
        public static async Task<AsyncConstructorClass> CreateInstanceAsync() {
            var constructorClass = new AsyncConstructorClass();
            return await constructorClass.InitializeAsync();
        }
    }
}