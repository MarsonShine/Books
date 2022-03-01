using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DesignPatternCore.Visitor.Version1
{
    public interface IVisitor
    {
        void Visitor(Element element);
    }

    public class Visitor1 : IVisitor
    {
        public void Visitor(Element element)
        {
            
        }
    }
}