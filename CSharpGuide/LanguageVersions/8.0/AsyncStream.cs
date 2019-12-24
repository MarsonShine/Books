using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.LanguageVersions._8._0
{
    public class AsyncStream
    {
        //申明异步流
        internal async IAsyncEnumerable<int> GenerateSequence()
        {
            for (int i = 0; i < 20; i++)
            {
                //每三个元素等待2秒
                if (i % 3 == 0)
                    await Task.Delay(TimeSpan.FromSeconds(2));
                yield return i;
            }
        }

        internal async Task<int> ConsumeStream()
        {
            await foreach (var number in GenerateSequence())
            {
                Console.WriteLine($"The time is {DateTime.Now:hh:mm:sssss}. Retrieved {number}");
            }
            return 0;
        }
    }
}
