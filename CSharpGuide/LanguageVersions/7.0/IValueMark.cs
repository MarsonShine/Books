using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CSharpGuide.LanguageVersions._7._0
{
    public interface ITuple
    {
        object? this[int? index] { get; }

        int? Length { get; }
    }

    public class TupleMark : ITuple
    {
        public object? this[int? index] => throw new NotImplementedException();

        public int? Length => throw new NotImplementedException();
    }
}
