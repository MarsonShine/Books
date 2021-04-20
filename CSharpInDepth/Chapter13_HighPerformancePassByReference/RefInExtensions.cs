using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chapter13_HighPerformancePassByReference
{
    /*
     对于结构体来说，拓展函数往往会发生值拷贝，而 ref，int 关键字修饰符的拓展函数就会避免发生拷贝
     */
    public static class RefInExtensions
    {
        public static double Magnitude2(this ref Vector3D vector) => vector.X + vector.Y + vector.Z;
    }

    public static class GenericExtension
    {
        public static double Magnitude(this Vector3D vector) => vector.X + vector.Y + vector.Z;
    }

    public readonly struct Vector3D
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
