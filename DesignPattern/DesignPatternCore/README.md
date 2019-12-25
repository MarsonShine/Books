# 设计模式

## 工厂模式

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