using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

//https://mattwarren.org/2020/02/19/Under-the-hood-of-Default-Interface-Methods/#background
namespace CSharpGuide.LanguageVersions._8._0 {
    using static Console;
    /// <summary>
    /// 默认接口方法，也叫默认接口实现
    /// </summary>
    public class DefaultInterfaceMethod {
        public static void Start() {
            INormal inormal = new CNormal();
            inormal.Normal();

            //GCHandle gch = GCHandle.Alloc(inormal, GCHandleType.Pinned);
            //IntPtr pObj = gch.AddrOfPinnedObject();
            //WriteLine(pObj.ToString());

            IDefaultMethod idefault = new CDefault();
            idefault.Default();

            IDefaultMethod idefaultOwnimp = new CDefaultOwnImp();
            idefaultOwnimp.Default();

            // 用 ShowInterfaceMapping 来实现接口默认方法
            //ShowInterfaceMapping(typeof(CNormal), typeof(INormal));
            //ShowInterfaceMapping(typeof(CDefault), typeof(IDefaultMethod));
            //ShowInterfaceMapping(typeof(CDefaultOwnImp), typeof(IDefaultMethod));
        }

        // 接口默认实现的实现方式
        private static void ShowInterfaceMapping(Type implemetation, Type @interface) {
            InterfaceMapping map = implemetation.GetInterfaceMap(@interface);
            WriteLine($"{map.TargetType}: GetInterfaceMap({map.InterfaceType})");
            for (int counter = 0; counter < map.InterfaceMethods.Length; counter++) {
                MethodInfo im = map.InterfaceMethods[counter];
                MethodInfo tm = map.TargetMethods[counter];
                WriteLine($"   {im.DeclaringType}::{im.Name} --> {tm.DeclaringType}::{tm.Name} ({(im == tm ? "same" : "different")})");
                WriteLine("       MethodHandle 0x{0:X} --> MethodHandle 0x{1:X}",
                    im.MethodHandle.Value.ToInt64(), tm.MethodHandle.Value.ToInt64());
                WriteLine("       FunctionPtr  0x{0:X} --> FunctionPtr  0x{1:X}",
                    im.MethodHandle.GetFunctionPointer().ToInt64(), tm.MethodHandle.GetFunctionPointer().ToInt64());
            }
            WriteLine();
        }
    }

    interface INormal {
        void Normal();
    }

    interface IDefaultMethod {
        void Default() => WriteLine("IDefaultMethod.Default");
    }

    class CNormal : INormal {
        public void Normal() => WriteLine("CNormal.Normal");
    }
    class CDefault : IDefaultMethod {
        // 不需要实现接口方法，因为接口已经有默认实现
    }
    class CDefaultOwnImp : IDefaultMethod {
        public void Default() => WriteLine("CDefaultOwnImpl.IDefaultMethod.Default");
    }

    interface IA {
        void M();
    }
    interface IB : IA {
        // override void M() { WriteLine("IB"); }
    }
    class Base : IA {
        void IA.M() { WriteLine("Base"); }
    }
    class Derived : Base, IB // allowed?
    {
        static void Main() {
            IA a = new Derived();
            a.M(); // what does it do?
        }
    }
}