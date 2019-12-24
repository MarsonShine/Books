using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.LanguageVersions._8._0
{
    public class Introducer
    {
        public void Start()
        {
            //可空引用类型
            string s = null;
            //引用null.toString 程序报错
            s.ToString();
#pragma warning disable CS8632 // 应仅在 “#nullable” 上下文中的代码中使用可为 null 的引用类型的注释。
            string? s1 = null;
#pragma warning restore CS8632 // 应仅在 “#nullable” 上下文中的代码中使用可为 null 的引用类型的注释。
            //运行到这里不会报错
            s1?.ToString();
            M(s1);//M 方法也正常运行

            #region warning 可空类型引用值可能为null
            string? name = null;
            var _ = name.Length;
            #endregion

            string? name1 = null;
            _ = name1!.Length;

            //异步流
            //Ranges 与 索引

        }

        void M(string? s)
        {
            Console.WriteLine(s?.Length);
            if (s != null)
            {
                Console.WriteLine(s.Length);
            }
        }

        async Task<int> GetBigResultAsync()
        {
            var result = await GetResultAsync();
            if (result > 20) return result;
            else return -1;
        }

        private async Task<int> GetResultAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            return 20;
        }

        //async IAsyncEnumerable<int> GetBigResultAsync()
        //{
        //    await foreach (var result in GetResultsAsync())
        //    {

        //    }
        //}

        private IEnumerable<int> GetResultsAsync()
        {
            throw new NotImplementedException();
        }
    }
}
