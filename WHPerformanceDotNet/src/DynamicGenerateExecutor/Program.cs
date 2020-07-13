using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicGenerateExecutor {
    class Program {
        class TimingDummy {
            public bool DoWork(string arg) { return true; }
        }
        static void Main(string[] args) {
            var argument = Int64.MaxValue.ToString();
            var assembly = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, "DynamicGenerateCode.dll"));
            // 找到指定的类型和方法执行
            Type type = assembly.GetType("DynamicGenerateCode.Extension");
            MethodInfo methodInfo = type.GetMethod("DoWork");
            // 代码生成的委托，它负责创建 Extension 示例
            Func<object> creationDel = GenerteNewObjDelegate<Func<object>>(type);
            Func<object, string, bool> doWorkDel = GenerteMethodCallDelegate<Func<object, string, bool>>(methodInfo, type, typeof(bool), new Type[] { typeof(object), typeof(string) });

            // 比较运行时间
            Console.WriteLine("== 开始示例 ==");
            const int IterationCount = 10000;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < IterationCount; i++) {
                object extensionObject = new TimingDummy();
            }
            watch.Stop();
            var elapsedBaseline = watch.ElapsedTicks;
            Console.WriteLine("Direct ctor: 1.0x");

            watch.Start();
            for (int i = 0; i < IterationCount; i++) {
                object extensionObject = Activator.CreateInstance(type);
            }
            watch.Stop();

            watch.Restart();
            for (int i = 0; i < IterationCount; i++) {
                object extensionObject = creationDel();
            }
            watch.Stop();
            Console.WriteLine("Codegen: {0:F1}x", (double) watch.ElapsedTicks / elapsedBaseline);

            Console.WriteLine();
            Console.WriteLine("==METHOD INVOKE==");

            var extension = new TimingDummy();
            watch.Start();
            for (int i = 0; i < IterationCount; i++) {
                bool result = extension.DoWork(argument);
            }
            watch.Stop();
            elapsedBaseline = watch.ElapsedTicks;
            Console.WriteLine("Direct method: 1.0x");

            object instance = Activator.CreateInstance(type);
            watch.Start();
            for (int i = 0; i < IterationCount; i++) {
                bool result = (bool) methodInfo.Invoke(instance, new object[] { argument });
            }
            watch.Stop();
            Console.WriteLine("MethodInfo.Invoke: {0:F1}x", (double) watch.ElapsedTicks / elapsedBaseline);

            object extensionObj = creationDel();
            watch.Restart();
            for (int i = 0; i < IterationCount; i++) {
                doWorkDel(extensionObj, argument);
            }
            watch.Stop();
            Console.WriteLine("Codegen: {0:F1}x", (double) elapsedBaseline / watch.ElapsedTicks);
        }

        private static T GenerteMethodCallDelegate<T>(MethodInfo methodInfo, Type extensionType, Type returnType, Type[] parameterTypes) where T : class {
            var dynamicMethod = new DynamicMethod("Invoke_" + methodInfo.Name, returnType, parameterTypes, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // 加载对象本身这个参数 this
            ilGenerator.Emit(OpCodes.Ldarg_0);
            // 类型转换正确的类型
            ilGenerator.Emit(OpCodes.Castclass, extensionType);
            // 实际的方法参数
            // 下面两步的生命是多余的，我们把堆栈中经过类型转换的对象弹出，然后又马上压回堆栈。那么直接入栈类型转换之后的对象即可，注意，编译器有可能会对此自动进行优化
            // ilGenerator.Emit(OpCodes.Stloc_0);   // 弹出栈顶数据并保存在局部变量中
            // ilGenerator.Emit(OpCodes.Ldloc_0);   // 将局部变量压入堆栈
            ilGenerator.Emit(OpCodes.Ldarg_1);
            // ilGenerator.EmitCall(OpCodes.Callvirt, methodInfo, null);
            ilGenerator.EmitCall(OpCodes.Call, methodInfo, null);
            ilGenerator.Emit(OpCodes.Ret);

            object del = dynamicMethod.CreateDelegate(typeof(T));

            return (T) del;
        }

        private static T GenerteNewObjDelegate<T>(Type type) where T : class {
            // 创建新的无参（通过指定 Type.EmptyTypes）动态方法
            var dynamicMethod = new DynamicMethod("Ctor_" + type.FullName, type, Type.EmptyTypes, true);
            var ilGenerator = dynamicMethod.GetILGenerator();
            // 查看指定类型的我要创建的构造函数
            var ctorInfo = type.GetConstructor(Type.EmptyTypes);
            if (ctorInfo != null) {
                ilGenerator.Emit(OpCodes.Newobj, ctorInfo);
                ilGenerator.Emit(OpCodes.Ret);

                object del = dynamicMethod.CreateDelegate(typeof(T));
                return (T) del;
            }
            return null;
        }
    }
}