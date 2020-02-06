#define DECORATOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DesignPatternCore.Bridge;
using DesignPatternCore.ChainOfResponsibility;
using DesignPatternCore.Decorator;
using DesignPatternCore.Factory;
using DesignPatternCore.Factory.AbstractFactory;
using DesignPatternCore.Observer;
using DesignPatternCore.Property;
using DesignPatternCore.Proxy;
using DesignPatternCore.Strategy;
using DesignPatternCore.Visitor;

namespace DesignPatternCore {
    class Program {
        static void Main(string[] args) {
#if DECORATOR
            Decorator.Person ms = new Decorator.Person("MarsonShine");
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

            // 根据业务需求得知文件格式
            var fileType = Enum.Parse<FileType>("Word");
            var wordConvertor = PdfConvertorFactory.Create(fileType);
            wordConvertor.Convert("example.docx");
            fileType = Enum.Parse<FileType>("Wps");
            var wpsConvertor = PdfConvertorFactory.Create(fileType);
            wpsConvertor.Convert("example.wps");

            // 策略模式
            var vertor = new Strategy.WordToPdfConvertor();
            var strategy = new StrategyContext(vertor);
            strategy.DoWork("example.docx");
            var excel = new Strategy.ExcelToPdfConvertor();
            strategy = new StrategyContext(excel);
            strategy.DoWork("example.xlsx");
            // 策略模式+工厂模式 封装部分相同逻辑，又有部分业务不同的逻辑变化

            // 抽象工厂模式
            IConvertorFactory factory = new WordToPdfConvertorFactory();
            Console.WriteLine("==========抽象工厂============");
            factory.Create().Convert("example.docx");
            // 原型模式
            Console.WriteLine("==========原型模式============");
            Resume r = new Resume("marson shine");
            r.SetPersonalInfo("男", "27");
            r.SetWorkExperience("6", "kingdee.cpl");
            r.Display();
            // 如果我要复制三个 Resume，则不需要实例化三次，而是调用Clone()即可
            var r2 = (Resume) r.Clone();
            var r3 = (Resume) r.Clone();
            r2.SetWorkExperience("5", "yhglobal.cpl");
            r2.Display();
            r3.SetWorkExperience("3", "che100.cpl");
            r3.Display();

            // 观察者模式
            Console.WriteLine("==========观察者模式============");
            StudentOnDuty studentOnDuty = new StudentOnDuty();
            var student = new StudentObserver("marson shine", studentOnDuty);
            studentOnDuty.Attach(student);
            studentOnDuty.Attach(new StudentObserver("summer zhu", studentOnDuty));
            studentOnDuty.Notify();
            studentOnDuty.UpdateEvent += student.Update;
            Console.WriteLine("==========观察者模式============");
        }
    }
}