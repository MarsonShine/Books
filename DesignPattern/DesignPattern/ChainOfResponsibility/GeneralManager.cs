using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignPattern.ChainOfResponsibility
{
    class GeneralManager : Manager
    {
        public GeneralManager(string name) : base(name)
        {
        }

        public override void RequestApplications(Request request)
        {
            if (request.RequestType == "请假")
            {
                Console.WriteLine($"{name}:{request.RequestContent} 数量{request.Number} 被批准");
            }
            else if(request.RequestType == "加薪" && request.Number <=500)
            {
                Console.WriteLine($"{name}:{request.RequestContent} 数量{request.Number} 被批准");
            }else if(request.RequestType == "加薪" && request.Number > 500)
            {
                Console.WriteLine($"{name}:{request.RequestContent} 数量{request.Number} 再说吧");
            }
        }
    }
}
