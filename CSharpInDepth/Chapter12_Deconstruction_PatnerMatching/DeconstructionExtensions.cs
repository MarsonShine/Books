using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chapter12_Deconstruction_PatnerMatching
{
    /// <summary>
    /// 拓展函数解构
    /// </summary>
    public static class DeconstructionExtensions
    {
        public static void Deconstruct(this DateTime dateTime, out int year, out int month, out int day) => (year, month, day) = (dateTime.Year, dateTime.Month, dateTime.Day);
        public static void Deconstruct(this DateTime datetTime, out int year, out int month, out int day, out int hour, out int minute, out int second) =>
            (year, month, day, hour, minute, second) = (datetTime.Year, datetTime.Month, datetTime.Day, datetTime.Hour, datetTime.Minute, datetTime.Second);
        /// <summary>
        /// 相同的 Deconstruct 的参数数量，无法完成调用，因为编译器无法知道该解构哪一个
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        public static void Deconstruct(this DateTime dateTime, out string year, out int month, out int day) => (year, month, day) = (dateTime.Year.ToString(), dateTime.Month, dateTime.Day);
    }
}
