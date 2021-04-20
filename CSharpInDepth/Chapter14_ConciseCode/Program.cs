using System;

namespace Chapter14_ConciseCode
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            byte b1 = 135;
            byte b2 = 0x83;
            byte b3 = 0b10000111;

            // 下划线
            byte b4 = 135;
            byte b5 = 0x83;
            byte b6 = 0b10000111;
            byte b7 = 0b1000_0111;

            int maxInt32 = 2_147_483_647;
            decimal largeSalary = 123_456_789.12m;
            ulong alternatingBytes = 0xff_00_ff_00_ff_00_ff_00;
            ulong alternatingWords = 0xffff_0000_ffff_0000;
            ulong alternatingDwords = 0xffffffff_00000000;


            int wideFifteen = 1____________________5;
            ulong notQuiteAlternatingWords = 0xffff_000_ffff_0000;

            Console.WriteLine($"{wideFifteen} {notQuiteAlternatingWords}");
            Console.ReadLine();
        }
    }
}
