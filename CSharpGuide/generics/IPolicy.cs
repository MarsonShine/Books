using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.generics
{
    public interface IPolicy
    {
        string PolicyName { get; set; }
    }

    public class LifePolicy : IPolicy
    {
        public string PolicyName { get; set; } = "Life";
    }
    public class HomePolicy : IPolicy {
        public string PolicyName { get; set; } = "Home";
    }
    public class AutoPolicy : IPolicy {
        public string PolicyName { get; set; } = "Auto";
    }
}
