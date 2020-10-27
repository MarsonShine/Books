using System;
using System.Collections.Generic;
using System.Text;

// https://github.com/dotnet/runtime/issues/43391
namespace CSharpGuide.performance
{
    public class ZeroingMemoryAllocatedByStackalloc
    {
        public static unsafe byte Test(int i)
        {
            //int j = i;
            byte* p = stackalloc byte[8];
            p[i] = 42;
            return p[1];
        }
    }
}
