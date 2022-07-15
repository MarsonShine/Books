using System;

namespace DesignPatternCore.ServiceLocator
{
    public class Program
    {
        public static void Main() {
            Client client = new Client();
            client.ServiceA = ServiceLocator.Instance.GetServiceA();
            client.ServiceB = ServiceLocator.Instance.GetServiceB();
            client.DoWork();
            Console.WriteLine();
        }
    }
}