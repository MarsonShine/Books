using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignPattern.Decorator
{
    class Person
    {
        public Person() { }

        private string m_name;
        public Person(string name)
        {
            m_name = name;
        }

        public virtual void Show()
        {
            Console.WriteLine($"妆扮的{m_name}");
        }
    }
}
