using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicLoadExecutor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Assembly assembly = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, "DynamicLoadExtension.dll"));
            Type type = assembly.GetType("DynamicLoadExtension.Extension");
            MethodInfo methodInfo = type.GetMethod("DoWork");

            // il generator
            Func<object> creationDel = GenerateNewObjDelegate<Func<object>>(type);
            object extensionObj = creationDel();

            Func<object, string, bool> doWorkDel = GenerateMethodCallDelegate<Func<object, string, bool>>(methodInfo, type, typeof(bool), new Type[] { typeof(object), typeof(string) });
            bool result = doWorkDel(extensionObj, "parameterValue");
        }

        private static T GenerateNewObjDelegate<T>(Type type)
            where T : class
        {
            // 创建一个无参的动态方法
            var dynamicMethod = new DynamicMethod("Ctor_" + type.FullName, type, Type.EmptyTypes);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // 创建想要的构造函数信息
            var ctroInfo = type.GetConstructor(Type.EmptyTypes);
            if(ctroInfo != null)
            {
                ilGenerator.Emit(OpCodes.Newobj, ctroInfo);
                ilGenerator.Emit(OpCodes.Ret);

                object del = dynamicMethod.CreateDelegate(typeof(T));
                return (T)del;
            }
            return null;
        }

        static bool CallMethodTemplate(object extensionObj,string argument)
        {
            var extension = (DynamicLoadExtension.Extension)extensionObj;
            return extension.DoWork(argument);
        }

        private static T GenerateMethodCallDelegate<T>(MethodInfo methodInfo,Type extensionType,Type returnType,Type[] parameterTypes) where T : class
        {
            var dynamicMethod = new DynamicMethod("Invoke_" + methodInfo.Name, returnType, parameterTypes, restrictedSkipVisibility: true);
            var ilGenerator = dynamicMethod.GetILGenerator();
            ilGenerator.DeclareLocal(extensionType);
            // 对象 this 参数
            ilGenerator.Emit(OpCodes.Ldarg_0);
            // 转化对应的类型
            ilGenerator.Emit(OpCodes.Castclass, extensionType);
            // 实际方法的参数
            // 注释部分可以优化掉
            //ilGenerator.Emit(OpCodes.Stloc_0);
            //ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            //ilGenerator.EmitCall(OpCodes.Callvirt, methodInfo, null);
            ilGenerator.EmitCall(OpCodes.Call, methodInfo, null);
            ilGenerator.Emit(OpCodes.Ret);

            object del = dynamicMethod.CreateDelegate(typeof(T));
            return (T)del;
        }
    }
}
