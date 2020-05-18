
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// from https://blog.marcgravell.com/2020/05/the-anatomy-of-async-iterators-aka.html
/// </summary>
namespace CSharpGuide.LanguageVersions._8._0
{
    public class AsyncStream_PartII
    {
        public async ValueTask StartAsync()
        {
            await foreach (var item in SomeSourceAsync(42))
            {
                Console.WriteLine(item);
            }

            var cancellationToken = CancellationToken.None;
            var tokenA = new CancellationToken();
            var tokenB = new CancellationToken();
            // option A - no cancellation
            await foreach (var item in SomeSourceAsync(42)) { }

            // option B - cancellation via WithCancellation
            await foreach (var item in SomeSourceAsync(42).WithCancellation(cancellationToken)) { }

            // option C - cancellation via SomeSourceAsync
            await foreach (var item in SomeSourceAsync(42, cancellationToken)) { }

            // option D - cancellation via both
            await foreach (var item in SomeSourceAsync(42, cancellationToken).WithCancellation(cancellationToken)) { }

            // option E - cancellation via both with different tokens
            await foreach (var item in SomeSourceAsync(42, tokenA).WithCancellation(tokenB)) { }
        }

        private async IAsyncEnumerable<string> SomeSourceAsync(int x, [EnumeratorCancellation]CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(100, cancellationToken);
                yield return $"result from SomeSource, x={x}, result {i}";
            }
        }
    }
}
