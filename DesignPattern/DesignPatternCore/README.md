# 工厂模式

工厂模式一般有三种，我们最常用的就是简单工厂、抽象工厂。而第三个就是基于反射的工厂模式。

工厂模式可以理解为，在一间工厂下， 有工人在负责作业，这些作业都是一些“机械化”的，属于重复劳动的。但是虽说在这间工厂里，工人们做的都是些固有逻辑的行为，但是这里面还是有分不同的流水线的。不同的流水线做的东西又有差异。怎么派发员工去不同的流水线那就是工厂自己要做的事了（也就是工厂包工头）。

以具体的案例作为实例，现在有这么一个任务：将用户上传的格式为“doc/docx、xls/xlsx、ppt/pptx“的文件全部转换为 pdf 文件。刚开始是这么写的

```c#
public class WordToPdfHelper
{
  	// ... 引入 word 相关组件
  	public static void ConvertToPdf("example.docx"){
      	// 验证文件是否存在，判断文件的合法性等
      	SomeValidations();
      	Console.WriteLine("word convert pdf success!");
    }
}

public class ExcelToPdfHelper
{
  	// ... 引入 excel 相关组件
  	public static void ConvertToPdf("example.xlsx")
    {
      	// 验证文件相关信息
      	SomeValidations();
      	Console.WriteLine("excel convert pdf success!");
    }
}
... PowerPointToPdfHelper
```

我们可以看到，这三个帮助类完成的是对复合要求格式的文件进行 pdf 转换。如果这时候又开了一个需求，我们要把 wps 相关格式的文件也要转成 pdf 文件。显然，我们之前写的那三个针对微软 office 的，是不适用于金山的 wps 的。所以按照我们之前的习惯，我还是得新建三个帮助类来处理这种情况。这么做其实是没有什么问题的，并且也到了职责分明，符合 SRP 原则。但是我们可以看到，这些文件其实还是有很多内容是相同的，比如文件验证逻辑等。再比如如果要在每次文件转换的前后要记录日志，我们是不是就得在这 6 个文件中输入日志类功能（当然你也可以用 AOP，但不能避免要在这些类中标记特性）。所以当我们碰到**含有相同逻辑运算时**，我们就应该想到“**是时候重构代码了**”。

我们把共同点，会因业务需求变化而变化的部分抽象出来，即文件转换方法。我们抽象成一个接口 `IPdfConvertor`，为了达到 SRP，文件转换与 PDF 转换其实不能简单的写在一起。而是再给文件转换方法抽象成另一个接口 `IFileConvertor`。然后用抽象类作为骨架来实现这些接口，然后具体的文件转换实现类就集成这个抽象基类：

```c#
public abstract class FileConvertorBase : IFileConvertor {
    protected abstract void FileConvert(string filePath);
    public void Convert(string filePath) {
        Console.WriteLine("do something before file convert...");
        FileConvert(filePath);
        Console.WriteLine("do something after file convert...");
    }
}

public class WordToPdfConvertor : IFileConvertor {
    public void Convert(string filePath) {
        Console.WriteLine("word convert pdf success!");
    }
}
public class WpsToPdfConvertor : FileConvertorBase, IPdfConvertor {
    public void ConverToPdf(string filePath) {
        Console.WriteLine("wps convert pdf success!");
    }

    protected override void FileConvert(string filePath) {
        Console.WriteLine($"{nameof(WpsToPdfConvertor)} execute file convert actually.");
        ConverToPdf(filePath);
    }
}
```

那么我们就可以在工厂类 `PdfConvertorFactory` 根据文件格式类型来创建出具体的实现类：

```c#
public class PdfConvertorFactory {
    public static IFileConvertor Create(FileType fileType) {
        switch (fileType) {
            case FileType.Word:
                return new WordToPdfConvertor();
            case FileType.Excel:
                return new ExcelToPdfConvertor();
            case FileType.PowerPoint:
                return new PowerPointToPdfConvertor();
            case FileType.Wps:
                return new WpsToPdfConvertor();
            default:
                throw new NotImplementedException();
        }
    }
}
```

这就是我们平时最常用到的工厂模式。

# 策略模式

抽象工厂我们还暂且不提，这里其实我们还可以用另外一种模式，来避免增加一个类我们就要在 `switch case` 块修改代码，并且还要修改枚举类的值（不用枚举，用其他方法也是如此，比如用字符串代替枚举）。

只需要做些微的改动即可，接口和其具体实现类还是保持不变

```c#
public interface IFileConvertor {
  	void Convert(string filePath);
}

// implement
public class WordToPdfConvertor : IFileConvertor {
    public void Convert(string filePath) {
        Console.WriteLine("strategy:word to pdf success!");
    }
}

public class ExcelToPdfConvertor : IFileConvertor {
    public void Convert(string filePath) {
        Console.WriteLine("strategy:excel to pdf success!");
    }
}
```

接下来我们就要新增一个上下文**来应付不同需求具体执行不同的业务逻辑**

```c#
public class FileConvertorContext {
  private readonly IFileConvertor _convertor;
  
  public FileConvertorContext(IFileConvertor converotr)
  {
    	_convertor = convertor;
  }
  
  public void Execute(string filePath)
  {
    	_convertor.Convert(filePath);
  }
}
```

这样我们调用就只需要像下面这样：

```c#
WordToPdfConvertor word = new WordToPdfConvertor();
var fileConvertor = new FileConvertorContext(word);
fileConvertor.Execute("example.docx");

ExcelToPdfConvertor excel = new ExcelToPdfConvertor();
fileConvertor = new FileConvertorContext(excel);
fileConvertor.Execute("example.xlsx");
```

这样我们共同的转换文件格式方法抽象成接口，然后根据实际业务的不同，具体执行到不同的实现类中。这样也做到了可插拔的，符合 OCP 原则。这样即便是多了另外一种文件转换需求，这种写法也不会影响到客户端，这样就能做到“热更新”了。

# 装饰者模式（Facade）

装饰者主要是降低不同模块之间耦合。

装饰者现在很像客服电话功能。当我们打电话过去咨询人工服务，但是我们总是先经过一个电话转播，系统会提示你拨打数字1是转售前，数字2转咨询，数字3转售后，等等。说实话这种模式很烦，因为我们就想第一时间找到人工，而不是根据提示去转对应的线路。

但是装饰者实际上就是这种情况，装饰者提供一个统一的入口（界面），然后内部负责转不同的功能。这样才能降低客户端与业务服务的耦合。降低客户端的使用复杂度（无需关注众多烦炸的业务逻辑）

这种特别适合于新老系统交互，在我们的老系统中，由于公司高速发展，以前的业务已经不适合现在公司的需要，需要迎合新的业务做新的需求业务，但是以前的业务不能受影响。且针对于用户操作来说跟以前没变化。这个时候我们就可以很好的运用装饰者模式。

我们新建一个业务B类去处理逻辑B。然后新建一个装饰者类Facade，用户点击按钮，触发业务流程。

```c#
// 老业务逻辑
public class BusinessA {
  	public void HandleA(){}
}
// 新业务逻辑
public class BusinessB {
  	public void HandleB(){}
}
// 外观装饰
public class Facade {
  	private BusinessA ba;
  	private BusinessB bb;
  	public Facade() {
      	ba = new BusinessA();
      	bb = new BusinessB();
    }
  
  	public void Execute() {
      	ba.HandleA();
      	bb.HandleB();
    }
}
```

这样业务A于业务B就解藕了，弱化了两者关系。具体怎么执行，由外观者决定。