namespace strings
{
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.Buffers;
    using System.Buffers.Text;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    // 见 https://twitter.com/davidfowl/status/1678738294933159937?s=19
    internal class Strings
    {
        /// <summary>
        /// 从查询字符串中移除instanceId
        /// 这里的字符串操作存在很多内存分配：
        /// 1. 字符串数组通过Split分割
        /// 2. 每个键值对字符串数组用=分割每个部分
        /// 3. List<string> 结果
        /// 4. 最后的字符串 string.Join
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static string? RemoveInstanceIdFromQueryString(string query)
        {
            if (string.IsNullOrEmpty(query))
                return null;

            var parameters = query.Split('&');
            var newParameters = new List<string>(parameters.Length);
            foreach (var parameter in parameters)
            {
                var kvp = parameter.Split('=');
                if (kvp.Length == 2 && !kvp[0].Equals("instanceId", StringComparison.OrdinalIgnoreCase))
                {
                    newParameters.Add(kvp[0]);
                }
                newParameters.Add(parameter);
            }
            return string.Join("&", newParameters);
        }

        // 下面是优化后的代码
        public static string? RemoveInstanceIdFromQueryString2(string query)
        {
            // 如果我们已知query的长度，那么我们可以避免分配一个新的字符串数组
            Span<char> chars = query.Length < 256 ? stackalloc char[query.Length] : new char[query.Length]; // 优先分配栈上内存
            int length = 0;
            // 通过引入 QueryStringEnumberable https://learn.microsoft.com/zh-cn/dotnet/api/microsoft.aspnetcore.webutilities.querystringenumerable
            foreach (var pair in new QueryStringEnumerable(query))
            {
                if (pair.DecodeName().Span.Equals("instanceId", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (length > 0)
                {
                    chars[length++] = '&';
                }

                chars[length..].TryWrite($"{pair.EncodedName.Span}={pair.EncodedValue.Span}", out var written);
                length += written;
            }
            return new string(chars);
        }


        public static void EnumerateQuery(string query)
        {
            Span<Range> range = stackalloc Range[2];
            foreach (StringSegment segment in new StringTokenizer(query, new char[] { '&' }))
            {
                var p = segment.AsSpan();
#if NET8
                var count = p.Split(range, '='); // NET8 新增API
#endif
                ReadOnlySpan<char> key = p[range[0]];
                ReadOnlySpan<char> value = p[range[1]];
            }
        }

        public static void Utf8StringHandle()
        {
            var data = new[]
            {
                "Hello"u8.ToArray(),
                "Twitter"u8.ToArray(),
                "Opimize"u8.ToArray()
            }.Select(m => new Message(m)).ToArray();

            var response = new Response(MessageBytesToString(data));
            Console.WriteLine(JsonSerializer.Serialize(response));
        }

        // 优化后的代码
        public static void Utf8StringHandle2()
        {
            var data = new[]
            {
                "Hello"u8.ToArray(),
                "Twitter"u8.ToArray(),
                "Opimize"u8.ToArray()
            }.Select(m => new Message(m)).ToArray();

            var response = new Response(MessageBytesToString2(data));
            Console.WriteLine(JsonSerializer.Serialize(response));
        }

        class Message(byte[] bytes)
        {
            public byte[] Payload = bytes;
        }
        class Response(string messages)
        {
            [JsonPropertyName("messages")]
            public string Messages => messages;
        }

        static string MessageBytesToString(Message[] messages)
        {
            return string.Join(",", messages.Select(m => Convert.ToBase64String(m.Payload)));
        }

        static string MessageBytesToString2(Message[] messages)
        {
            var writer = new ArrayBufferWriter<char>();
            foreach (var m in messages)
            {
                var needComma = writer.WrittenCount > 0;
                var length = Base64.GetMaxEncodedToUtf8Length(m.Payload.Length);
                if (needComma)
                {
                    length++;
                }
                // 获取一个可写的Span
                var span = writer.GetSpan(length);
                if (needComma)
                {
                    span[0] = ',';
                    span = span[1..];
                }

                Convert.TryToBase64Chars(m.Payload, span, out var written);
                writer.Advance(written + (needComma ? 1 : 0));
            }
            return new string(writer.WrittenSpan);
        }

        static string MessageBytesToString3(Message[] messages)
        {
            if (messages.Length == 0) return "";
            var length = messages.Length - 1; // 逗号
            foreach (var m in messages)
            {
                length += Base64.GetMaxEncodedToUtf8Length(m.Payload.Length);
            }

            var writer = new ArrayBufferWriter<char>();
            foreach (var m in messages)
            {
                var needComma = writer.WrittenCount > 0;
                length = Base64.GetMaxEncodedToUtf8Length(m.Payload.Length);
                if (needComma)
                {
                    length++;
                }
                // 获取一个可写的Span
                var span = writer.GetSpan(length);
                if (needComma)
                {
                    span[0] = ',';
                    span = span[1..];
                }

                Convert.TryToBase64Chars(m.Payload, span, out var written);
                writer.Advance(written + (needComma ? 1 : 0));
            }
            return new string(writer.WrittenSpan);
        }
    }
}
