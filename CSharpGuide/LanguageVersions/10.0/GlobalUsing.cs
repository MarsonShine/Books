global using System;
global using static System.Console;
global using static System.Math;
global using Env = System.Environment;

namespace CSharpGuide.LanguageVersions._10._0
{
    internal class GlobalUsing
    {
        static void Start()
        {
            WriteLine(Sqrt(3 * 3 + 4 * 4));
            WriteLine(string.Join(',', Env.GetEnvironmentVariables().Keys));
        }
    }
}
