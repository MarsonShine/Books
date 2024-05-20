using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloSyntaxTree
{
	internal class StringSyntaxExample
	{
		public static void SetDataFormat([StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string dateFormat)
		{
			Console.WriteLine(dateFormat);
		}

		public static void SetJsonDataFormat([StringSyntax(StringSyntaxAttribute.Json)] string dateFormat)
		{
			Console.WriteLine(dateFormat);
		}

		public static void SetRegexDataFormat([StringSyntax(StringSyntaxAttribute.Regex)] string dateFormat)
		{
			Console.WriteLine(dateFormat);
		}

		public static void SetData([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string data, params object[] args)
		{
			Console.WriteLine(data);
		}

		public static void SetXml([StringSyntax(StringSyntaxAttribute.Xml)] string dateFormat)
		{
			Console.WriteLine(dateFormat);
		}
	}
}
