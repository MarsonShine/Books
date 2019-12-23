using DesignPattern.Bridge;
using DesignPattern.ChainOfResponsibility;
using DesignPattern.Decorator;
using DesignPattern.Proxy;
using DesignPattern.Visitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignPattern
{
    class Program
    {
        static void Main(string[] args)
        {
#if DECORATOR
            Person ms = new Person("MarsonShine");
            Console.WriteLine("\n 第一种妆扮：");
            TShirts dtx = new TShirts();
            BigTrouser bt = new BigTrouser();
            dtx.Decorate(ms);
            bt.Decorate(dtx);
            bt.Show(); 
#endif
#if Proxy
            SchoolGirl zhuqin = new SchoolGirl();
            zhuqin.Name = "祝琴";
            Proxy.Proxy ms = new Proxy.Proxy(zhuqin);
            ms.GiveChocolate();
            ms.GiveDolls();
            ms.GiveFlowers();
            Console.ReadLine(); 
#endif

#if ChanOfResposibility
            HandsetBrand hb;
            hb = new HandsetBrandN();
            hb.SetHandsetSoft(new HandsetGame());
            hb.Run();

            hb.SetHandsetSoft(new HandsetAddressList());
            hb.Run();

            HandsetBrand hb2;
            hb2 = new HandsetBrandM();
            hb2.SetHandsetSoft(new HandsetGame());
            hb2.Run();

            hb2.SetHandsetSoft(new HandsetAddressList());
            hb2.Run(); 
#endif
#if ChainOfResiposibility
            CommonManager jinli = new CommonManager("jinli");
            Majordomo zongjian = new Majordomo("zongjian");
            GeneralManager zhongjingli = new GeneralManager("zhongjinli");
            jinli.SetSuperior(jinli);
            zongjian.SetSuperior(zhongjingli);

            Request request = new Request();
            request.RequestType = "请假";
            request.RequestContent = "我要请假";
            request.Number = 1;
            jinli.RequestApplications(request);

            Request request2 = new Request();
            request2.RequestType = "请假";
            request2.RequestContent = "我要请假";
            request.Number = 4;
            jinli.RequestApplications(request2);

            Request request3 = new Request();
            request3.RequestType = "请假";
            request3.RequestContent = "我还是要请假";
            request.Number = 500;
            jinli.RequestApplications(request3); 
#endif
            ObjectStructure o = new ObjectStructure();
            o.Attach(new Man());
            o.Attach(new Woman());

            Success v1 = new Success();
            o.Display(v1);

            Failing v2 = new Failing();
            o.Display(v2);


        }
    }
}
