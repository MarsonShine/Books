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
    }
}
