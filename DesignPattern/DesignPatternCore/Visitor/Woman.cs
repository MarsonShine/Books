using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignPatternCore.Visitor
{
    class Woman:Person
    {
        public override void Accept(Action visitor)
        {
            visitor.GetWomanConclusion(this);
        }
    }
}
