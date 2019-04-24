## Rx(Reactive Extensions)

Linq是对序列数据进行查询的一系列语言功能。内置的 LINQ to Objects（ 基于 IEnumerable<T> ）和LINQ to entites（基于 IQueryable<T>）是两个最常用的LINQ 提供者。查询是延后执行（lazily evaluated）的，只有在需要时才会从序列中获取数据。从概念上是一种拉取模式。在查询过程中数据项是被逐个拉取出来的。

Reactive Extensions（Rx）把事件看作是依次到达的数据序。因此，将 Rx 认作是LINQ to events（基于 IObervable<T>）也是可以的，它与其它 LINQ 提供者的主要区别在于，Rx 采用 “推送” 模式。也就是说 Rx 在事件到达时才决定程序该如何响应。

所有 LINQ 操作都可以 Rx 中使用。从概念上看，过滤（Where）、投影（Select）等简单操作都能在 Rx 作用，除此之外，还增加其它特别的操作。

要使用 Rx，需要 Nuget 引入 [System.Reactive.Core](https://www.nuget.org/packages/System.Reactive.Core/) 。

```c#
Install-Package System.Reactive -Version 4.1.2
```

### 转换.NET事件

把一个事件作为 Rx 输入流，每次事件发生时通过 OnNext 生成数据。

Observable 类定义了一些事件转换器。大部分 .NET 框架事件与 FromEventPattern 兼容，对于不遵守通用模式的事件，需要改用 FromEvent。

FromEventPattern 最合适使用委托类型为 EventHandler<T> 的事件。很多较新的框架类的事件都采用这种委托类型。比如Progress<T> 类定义了事件 ProcessChanged，这个事件就是 EventHandler 委托类型，因此很容易用FromEventPattern 封装进去：

```c#
public static void EncapsulateFromStandardEventHandler(){
    var progress = new Progress<int>();
    var progressReports = Observable.FromEventPattern<int>(
    	handler => progress.ProcessChanged += hander,
    	handler => progress.ProcessChanged -= handler);
    progressReports.Subscribe(data => Trace.WriteLine("OnNext:"+data.EventArgs));
}
```

Rx 用 FromEventPattern 中的两个 Lambda 参数来订阅和退订事件。

有些陈旧的事件则不是这种标准的 EventHandler 类型，比如 System.Timers.Timer 类有个Elapsed，它的类型是 ElapsedEventHandler ，对于这种不标准的事件，可以这样封装进FromEventPattern

```c#
public static void EncapsulateFromNotStandardEventHandler(){
    var timer = new System.Timers.Timer(interval: 1000) { Enabled = true };
    var ticks = ObServable.FromEventPattern<ElapsedEventHandler,ElapsedEventArgs>(
    	handler => (s, a) => handler(s, a),
    	handler => timer.Elapsed += handler,
        handler => timer.Elapsed -= handler);
    ticks.Subscribe(data => Trace.WriteLine("OnNext:"+data.EventArgs.SignalTime));
}
```

`FromEventPattern` 第一个参数是一个转换器，它将 `EventHandler<ElapsedEventArgs>` 转换成 `ElapsedEventHandler` 。除了传递事件，它不应该再做其他事。

我们还可以通过反射的方式完成上述同样的效果

```c#
public static void EncapsulateFromNotStandardEventHandlerByReflection(){
	var timer = new System.Timers.Timer(interval : 1000) { Enabled = true };
	var ticks = Observable.FromEventPattern(timer, "Elapsed");
	ticks.Subscribe(data => Trace.WriteLine("OnNext:" + ((ElapsedEventArgs) data.EventArgs).SignalTime));
}
```

（标准事件模式：第一个参数是事件发送者，第二个参数是事件的类型参数）。对于不标准的事件类型，可以用重载 Observable.FromEvent 的办法，把事件封装进 Observable 对象。

封进 Observable 后，每次引发该事件都会触发 OnNext。在处理 AsyncCompletedEventArgs 时会发生令人奇怪的现象，所有的异常信息都是通过数据形式传递的（ OnNext ），而不是通过错误传递（ OnError ）。来看一个例子：

```c#
public static void ObservableException() {
    var client = new WebClient();
    var donwloadedStrings = Observable.FromEventPattern(client, "DownloadStringCompleted");
    donwloadedStrings.Subscribe(
        data => {
            var eventArgs = (DownloadStringCompletedEventArgs) data.EventArgs;
            if (eventArgs.Error != null)
                Trace.WriteLine("OnNext:(Error) " + eventArgs);
            else
                Trace.WriteLine("OnNext:" + eventArgs.Result);
        },
        ex => Trace.WriteLine("OnError:" + ex),
        () => Trace.WriteLine("OnCompleted")
    );
    client.DownloadStringAsync(new Uri("http://invalid.example.com/"));
}
```

`WebClient.DownloadStringAsync` 出错并结束时，引发带有异常 `AsyncCompletedEventArgs.Error` 事件。Rx 会把这作为一个数据事件，因此这个程序的结果是显示 "OnNext:(Error)" 而不是 "OnError"。

有些事件的订阅和退订必须在特定的上下文中进行。例如，很多 UI 控件的事件必须在 UI 线程中。

Rx 提供了一个操作符 `SubscribeOn`，可以控制订阅和退订的上下文。