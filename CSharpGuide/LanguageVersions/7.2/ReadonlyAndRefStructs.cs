using System;

namespace CSharpGuide.LanguageVersions.Seven.Two {
    /// <summary>
    /// 用 readonly 修饰符声明一个结构体，编译器会知道你的目的就是建立一个不变的结构体类型。
    /// 编译器就会根据两个规则来执行这个设计决定
    /// 1. 所有的字段必须是只读的 readonly
    /// 2. 所有的属性必须是只读的 readonly，包括自动实现属性
    /// 以上两条足已确保没有readonly struct 修饰符的成员来修改结构的状态—— struct 是不变的
    /// </summary>
    public struct Point3D {
        private static Point3D origin = new Point3D (0, 0, 0);
        public Point3D (double x, double y, double z) {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Point3D Origin => new Point3D (0, 0, 0);
        //注意：这里返回是内部存储的易变的引用
        public ref Point3D Origin2 => ref origin;
        //返回一个不变（只读）的按引用传递的值
        public ref readonly Point3D Origin3 => ref origin;

        public static double CalculateDistance ( in Point3D point1, in Point3D point2) {
            double xDifference = point1.X - point2.X;
            double yDifference = point1.Y - point2.Y;
            double zDifference = point1.Z - point2.Z;

            return Math.Sqrt (xDifference * xDifference + yDifference * yDifference + zDifference * zDifference);
        }

        public static void DemoCalculateDistance ( in Point3D point1, in Point3D point2) {
            var distance = CalculateDistance (point1, point2);
            var fromOrigin = CalculateDistance (point1, new Point3D ());
        }

        private static double CalculateDistance2 ( in Point3D point1, in Point3D point2 = default) {
            double xDifference = point1.X - point2.X;
            double yDifference = point1.Y - point2.Y;
            double zDifference = point1.Z - point2.Z;
            return Math.Sqrt (xDifference * xDifference + yDifference * yDifference + zDifference * zDifference);
        }

        private static void DemoCalculateDistanceForExplicit (Point3D point1, Point3D point2) {
            var distance = CalculateDistance ( in point1, in point2);
            distance = CalculateDistance ( in point1, new Point3D ());
            distance = CalculateDistance (point1, in Point3D.origin);
        }
    }

    readonly public struct ReadonlyPoint3D {
        private static ReadonlyPoint3D origin = new ReadonlyPoint3D (0, 0, 0);
        public ReadonlyPoint3D (double x, double y, double z) {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public ReadonlyPoint3D Origin => new ReadonlyPoint3D (0, 0, 0);
        //注意：这里返回是内部存储的易变的引用
        public ref ReadonlyPoint3D Origin2 => ref origin;
        //返回一个不变（只读）的按引用传递的值
        public ref readonly ReadonlyPoint3D Origin3 => ref origin;

        private static double CalculateDistance3 ( in ReadonlyPoint3D point1, in ReadonlyPoint3D point2 = default) {
            double xDifference = point1.X - point2.X;
            double yDifference = point1.Y - point2.Y;
            double zDifference = point1.Z - point2.Z;

            return Math.Sqrt (xDifference * xDifference + yDifference * yDifference + zDifference * zDifference);
        }
    }

    public class CallSite {
        public void Caller () {

        }
    }
}