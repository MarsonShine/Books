using System;

namespace DesignPatternCore.ServiceLocator
{
    public class ServiceA:IServiceA
    {
        public void UsefulMethod()
        {
            Console.WriteLine("ServiceA-UsefulMethod");
        }
    }

    public class ServiceB : IServiceB
    {
        public void UsefulMethod()
        {
            Console.WriteLine("ServiceB-UsefulMethod");
        }
    }
}