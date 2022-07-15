using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DesignPatternCore.ServiceLocator
{
    public class Client
    {
        public IServiceA ServiceA;
        public IServiceB ServiceB;
        public void DoWork() {
            ServiceA?.UsefulMethod();
            ServiceB?.UsefulMethod();
        }
    }
}