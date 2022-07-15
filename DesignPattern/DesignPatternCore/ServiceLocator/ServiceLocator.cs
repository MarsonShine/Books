using System;
using System.Collections.Generic;

namespace DesignPatternCore.ServiceLocator
{
    public class ServiceLocator
    {
        private static ServiceLocator locator = null;

        public static ServiceLocator Instance {
            get {
                if (locator == null) {
                    locator = new ServiceLocator();
                }
                return locator;
            }
        }
        private ServiceLocator(){}
        // 以下都是硬编码 不推荐
        // 可以通过字典的方式实现运行时动态服务注册与解析
        private IServiceA serviceA;
        private IServiceB serviceB;

        public IServiceA GetServiceA() {
            if (serviceA == null) {
                serviceA = new ServiceA();
            }
            return serviceA;
        }

        public IServiceB GetServiceB() {
            if (serviceB == null) {
                serviceB = new ServiceB();
            }
            return serviceB;
        }

        // Dictionary 实现动态注入
        private Dictionary<Type, object> registry = new Dictionary<Type, object>();
        public void Register<T>(T ServiceInstance) {
            registry[typeof(T)] = ServiceInstance;
        }
        public T GetService<T>() {
            T serviceInstance = (T)registry[typeof(T)];
            return serviceInstance;
        }

    }
}