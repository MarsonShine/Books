using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.LanguageVersions._10._0
{
    internal class CirCle
    {
        public double Radius { get; set; }
        public double Circumference => 2 * Radius * PI;
    }
}
