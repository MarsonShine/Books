using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
    public interface IDoStuff_NotNull<TIn, TOut>
        where TIn : notnull
        where TOut : notnull
    {
        TOut DoStuff(TIn input);
    }

    public class DoStuff<TIn, TOut> : IDoStuff_NotNull<TIn, TOut>
        where TIn : notnull
        where TOut : notnull
    {
        TOut IDoStuff_NotNull<TIn, TOut>.DoStuff(TIn input)
        {
            throw new NotImplementedException();
        }

        public TOut Do(TIn input)
        {
            throw new NotImplementedException();
        }
    }

    public class DoStuff_Nulable<TIn, TOut> : IDoStuff_NotNull<TIn, TOut>
    {
        public TOut DoStuff(TIn input)
        {
            throw new NotImplementedException();
        }
    }

    class NullableRefferenceExample
    {
        public static void Start()
        {
            DoStuff<int?, string> stuff = new DoStuff<int?, string>();
            _ = stuff.Do(null);
            DoStuff_Nulable<int?, int?> doStuff = new DoStuff_Nulable<int?, int?>();

            var d2 = new Dictionary<string, string>(10);
            var nothing = d2[null];
        }
    }

    public class MyClass
    {
        private string _innerValue = string.Empty;

        [AllowNull]
        public string MyValue
        {
            get
            {
                return _innerValue;
            }
            set
            {
                _innerValue = value ?? string.Empty;
            }
        }
    }

    public static class HandleMethods
    {
        public static void DisposeAndClear(ref MyClass handle)
        {

        }

        public static void DisposeAndClear_DisallowNull([DisallowNull]ref MyClass handle)
        {


        }
    }

    class MyArray
    {
        public static T Find<T>(T[] array, Func<T, bool> match)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            return Array.Find<T>(array, s => match.Invoke(s));
        }

        public static void Resize<T>(ref T[] array, int newSize)
        {
            Array.Resize<T>(ref array, newSize);
        }
    }

    public class MyString
    {
        public static bool IsNullOrEmpty([NotNullWhen(false)]string? value)
        {
            return string.IsNullOrEmpty(value);
        }
    }
    public class MyVersion
    {
        public static bool TryParse(string? input, [NotNullWhen(true)]out Version? version)
        {
            return Version.TryParse(input, out version);
        }
    }

    public class MyQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();
        public bool TryDequeue([NotNullWhen(false)]out T result)
        {
            return _queue.TryDequeue(out result);
        }

        public void Enqueue(T result)
        {
            _queue.Enqueue(result);
        }
    }

    class MyPath
    {
        [return: NotNullIfNotNull("path")]
        public static string? GetFileName(string? path)
        {
            return Path.GetFileName(path);
        }
    }

    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowArgumentNullException(string? args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
        }
    }

    public static class MyAssertionLibrary
    {
        public static void MyAssert([DoesNotReturnIf(false)] bool condition)
        {
            //if (condition == false) throw new InvalidOperationException(nameof(condition));
        }
    }
}
