using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.LanguageVersions._7._0
{
    public class ValueTasks
    {
        public static async ValueTask Starup()
        {
            int ret = await GetIdAsync();
        }

        private static async ValueTask<int> GetIdAsync()
        {
            return await new ValueTask<int>(5);
        }
    }
}
