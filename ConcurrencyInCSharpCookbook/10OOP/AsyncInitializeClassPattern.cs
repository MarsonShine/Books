using System;
using System.Threading.Tasks;

namespace _10OOP {
    /// <summary>
    /// 异步初始化模式，常用语 反射、控制反转、Ioc、数据绑定、Activator etc...
    /// </summary>
    public class AsyncInitializeClassPattern {

    }

    interface IAsyncInitialize {
        /// <summary>
        /// 标识一个类构造函数需要异步初始化，并提供初始化的结果
        /// 因为 await Initialization 就说明构造函数已经调用结束
        /// </summary>
        /// <value></value>
        Task Initialization { get; }
    }

    interface IMyClass {

    }
    public class MyClass : IMyClass, IAsyncInitialize {
        public MyClass() {
            Initialization = InitializeAsync();
        }

        private async Task InitializeAsync() {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        public Task Initialization { get; }
    }

    interface ISubClass {

    }
    /// <summary>
    /// 组合类时，异步初始化，组合的类初始化一定要放在本类的前面
    /// </summary>
    class SubClass : ISubClass, IAsyncInitialize {
        private readonly IMyClass _myClass;
        public SubClass(IMyClass myClass) {
            _myClass = myClass;
            Initialization = InitializeAsync();
        }

        private async Task InitializeAsync() {
            var myclass = _myClass as MyClass;
            await myclass.Initialization;
            //上面当有多个组合类是，有很多代码要初始化，强转，导致代码结构臃肿难堪
            //可以用建立辅助方法简化结构
            await AsyncInitialization.WhenAllInitializedAsync(_myClass);
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        public Task Initialization { get; }
    }
}