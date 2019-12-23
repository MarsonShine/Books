using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignPattern.Visitor
{
    abstract class Person
    {
        public abstract void Accept(Action visitor);
    }
}
