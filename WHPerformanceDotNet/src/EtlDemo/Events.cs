using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;

namespace EtlDemo
{
    [EventSource(Name = "EtlDemo")]
    internal sealed class Events : EventSource
    {
        // 这个是全局的，因为这是通用的，在代码的其它部分都会引用
        public static readonly Events Log = new Events();

        public class Keywords
        {
            // 事件关键字这个可以任意申明
            // 可以作为对事件进行分类的一种方式，这种分类方式对应用程序有意义。
            // 侦听器可以根据 Keywords 过滤拿到想要的内容
            // 注意，EventKeywords 被标记了 [Flag]，所以你赋值一定得是 2 的幂，这样监听器就可以侦听多个关键字
            public const EventKeywords General = (EventKeywords)1;
            public const EventKeywords PrimeOutput = (EventKeywords)2;
        }

        // 事件常量标识符，这样让代码更加可读和引用方便
        internal const int ProcessingStartId = 1;
        internal const int ProcessingFinishId = 2;
        internal const int FoundPrimeId = 3;

        [Event(ProcessingStartId, Level = EventLevel.Informational, Keywords = Keywords.General)]
        public void ProcessingStart()
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(ProcessingStartId);
            }
        }

        [Event(ProcessingFinishId, Level = EventLevel.Informational, Keywords = Keywords.General)]
        public void ProcessingFinish()
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(ProcessingFinishId);
            }
        }

        public void FoundPrime(long primeNumber)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(FoundPrimeId, primeNumber);
            }
        }

        [Event(5, Level = EventLevel.Informational, Keywords = Keywords.General)]
        public void Error(string message)
        {
            if (IsEnabled())
            {
                this.WriteEvent(5, message ?? string.Empty);    // 防止 message 为 null 报错
            }
        }
    }
}
