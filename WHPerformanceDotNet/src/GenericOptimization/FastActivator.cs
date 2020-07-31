using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace GenericOptimization
{
    public class FastActivator
    {
        public static T CreateInstance<T>()
            where T : new()
        {
            return FastActivatorImpl<T>.NewFunction();
        }

        private class FastActivatorImpl<T> where T : new()
        {
            // 利用表达式树就会“提示”编译器不会调用 Activator.CreateInstance 
            private static readonly Expression<Func<T>> NewExpression = () => new T();
            internal static readonly Func<T> NewFunction = NewExpression.Compile();
        }
    }

    public static class FastActivator<T> where T : new()
    {
        public static readonly Func<T> Create = DynamicModuleLambdaCompiler.GenerateFactory<T>();
    }
}
