using System;
using System.Collections.Generic;
using System.Text;

namespace CSharpGuide.LanguageVersions._8._0
{
    /// <summary>
    /// 这种通用的写法是不允许为空的
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    interface IDoStuff<TIn, TOut>
    {
        TOut DoStuff(TIn input);
    }
    /// <summary>
    /// 这样会自动生成检查是为空参数的警告
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    interface IDoStuff_NotNull<TIn, TOut>
        where TIn : notnull
        where TOut : notnull
    {
        TOut DoStuff(TIn input);
    }

    public class DoStuff<TIn, TOut> : IDoStuff_NotNull<TIn, TOut>
        //where TIn : notnull
        //where TOut : notnull
    {
        TOut IDoStuff_NotNull<TIn, TOut>.DoStuff(TIn input)
        {
            throw new NotImplementedException();
        }
    }

    class NullableRefferenceExample
    {
    }
}
