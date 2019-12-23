using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignPattern.ChainOfResponsibility
{
    class CommonManager : Manager
    {
        public CommonManager(string name) : base(name)
        {
        }

        public override void RequestApplications(Request request)
        {
            if(request.RequestType =="请假"&&request.Number <= 2)
            {
                Console.WriteLine($"{name}:{request.RequestContent} 数量{request.Number} 被批准");
            }
            else
            {
                if(superior != null)
                {
                    superior.RequestApplications(request);
                }
            }
        }
    }
}
