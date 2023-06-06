# DiagnosticSource 和 DiagnosticListener

> 这篇文章适用于： ✔️ .NET Core 3.1 及以后版本 ✔️ .NET Framework 4.5 及以后版本
>

[System.Diagnostics.DiagnosticSource](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.diagnosticsource) 是一个模块，它允许代码在生产时记录丰富的数据有效载荷，以便在被记录的过程中消费。在运行时，消费者可以动态地发现数据源并订阅感兴趣的数据源。`System.Diagnostics.DiagnosticSource` 被设计为允许进程中的工具访问丰富的数据。当使用 `System.Diagnostics.DiagnosticSource` 时，消费者被认为是在同一进程中，因此，可以传递不可序列化的类型（例如，`HttpResponseMessage` 或 `HttpContext`），给客户提供大量的数据。

## DiagnosticSource 入门指南

本攻略展示了如何用 [System.Diagnostics.DiagnosticSource](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.diagnosticsource) 创建一个 DiagnosticSource 事件和仪表代码。然后，它解释了如何通过寻找有趣的 DiagnosticListeners 来消费事件，订阅它们的事件，以及解码事件数据有效载荷。最后，它描述了过滤，只允许特定的事件通过系统。

## DiagnosticSource 实现

你将与以下代码一起工作。这段代码是一个 HttpClient 类，它有一个 `SendWebRequest` 方法，向 URL 发送 HTTP 请求并接收回复。

```c#
using System.Diagnostics;
MyListener TheListener = new MyListener();
TheListener.Listening();
HTTPClient Client = new HTTPClient();
Client.SendWebRequest("https://learn.microsoft.com/dotnet/core/diagnostics/");

class HTTPClient
{
    private static DiagnosticSource httpLogger = new DiagnosticListener("System.Net.Http");
    public byte[] SendWebRequest(string url)
    {
        if (httpLogger.IsEnabled("RequestStart"))
        {
            httpLogger.Write("RequestStart", new { Url = url });
        }
        //Pretend this sends an HTTP request to the url and gets back a reply.
        byte[] reply = new byte[] { };
        return reply;
    }
}
class Observer<T> : IObserver<T>
{
    public Observer(Action<T> onNext, Action onCompleted)
    {
        _onNext = onNext ?? new Action<T>(_ => { });
        _onCompleted = onCompleted ?? new Action(() => { });
    }
    public void OnCompleted() { _onCompleted(); }
    public void OnError(Exception error) { }
    public void OnNext(T value) { _onNext(value); }
    private Action<T> _onNext;
    private Action _onCompleted;
}
class MyListener
{
    IDisposable networkSubscription;
    IDisposable listenerSubscription;
    private readonly object allListeners = new();
    public void Listening()
    {
        Action<KeyValuePair<string, object>> whenHeard = delegate (KeyValuePair<string, object> data)
        {
            Console.WriteLine($"Data received: {data.Key}: {data.Value}");
        };
        Action<DiagnosticListener> onNewListener = delegate (DiagnosticListener listener)
        {
            Console.WriteLine($"New Listener discovered: {listener.Name}");
            //Subscribe to the specific DiagnosticListener of interest.
            if (listener.Name == "System.Net.Http")
            {
                //Use lock to ensure the callback code is thread safe.
                lock (allListeners)
                {
                    if (networkSubscription != null)
                    {
                        networkSubscription.Dispose();
                    }
                    IObserver<KeyValuePair<string, object>> iobserver = new Observer<KeyValuePair<string, object>>(whenHeard, null);
                    networkSubscription = listener.Subscribe(iobserver);
                }

            }
        };
        //Subscribe to discover all DiagnosticListeners
        IObserver<DiagnosticListener> observer = new Observer<DiagnosticListener>(onNewListener, null);
        //When a listener is created, invoke the onNext function which calls the delegate.
        listenerSubscription = DiagnosticListener.AllListeners.Subscribe(observer);
    }
    // Typically you leave the listenerSubscription subscription active forever.
    // However when you no longer want your callback to be called, you can
    // call listenerSubscription.Dispose() to cancel your subscription to the IObservable.
}
```

执行代码结果如下：

```
New Listener discovered: System.Net.Http
Data received: RequestStart: { Url = https://learn.microsoft.com/dotnet/core/diagnostics/ }
```

## 记录一个事件

`DiagnosticSource` 类型是一个抽象的基类，定义了记录事件所需的方法。持有实现的类是 `DiagnosticListener`。用 `DiagnosticSource` 工具化代码的第一步是创建一个 `DiagnosticListener`。比如说：

```c#
private static DiagnosticSource httpLogger = new DiagnosticListener("System.Net.Http");
```

请注意，httpLogger 被打成了 DiagnosticSource。这是因为这段代码只写事件，因此只关注 DiagnosticListener 实现的 DiagnosticSource 方法。 DiagnosticListeners 在创建时被赋予名称，这个名称应该是相关事件的逻辑分组的名称（通常是组件）。后来，这个名字被用来找到监听器并订阅它的任何事件。因此，事件名称只需要在一个组件内是唯一的。
DiagnosticSource 的记录接口由两个方法组成：

```c#
bool IsEnabled(string name)
void Write(string name, object value);
```

这是由仪表点(instrument site)决定的。你需要检查仪表点，看哪些类型被传入 IsEnabled。这为你提供了信息，让你知道该把有效载荷投给什么。

一个典型的调用点将看起来像：

```c#
if (httpLogger.IsEnabled("RequestStart"))
{
    httpLogger.Write("RequestStart", new { Url = url });
}
```

每个事件都有一个字符串名称（例如，RequestStart），以及正好一个作为有效载荷的对象。如果你需要发送一个以上的项目，你可以通过创建一个具有表示其所有信息的属性的对象来实现。C# 的匿名类型功能通常用于创建一个类型来"即时"传递，并使这种方案非常方便。在仪表点，你必须用 `IsEnabled()` 检查来保护对 `Write()` 的调用，并对同一事件名称进行检查。如果没有这个检查，即使是在仪表不活动的情况下，C# 语言的规则也需要完成创建有效载荷对象和调用 `Write()` 的所有工作，即使实际上没有任何东西在监听数据。通过对 `Write()` 的调用进行防护，你可以在源码未启用时使其有效。

结合上面说的一切，代码如下：

```c#
class HTTPClient
{
    private static DiagnosticSource httpLogger = new DiagnosticListener("System.Net.Http");
    public byte[] SendWebRequest(string url)
    {
        if (httpLogger.IsEnabled("RequestStart"))
        {
            httpLogger.Write("RequestStart", new { Url = url });
        }
        //Pretend this sends an HTTP request to the url and gets back a reply.
        byte[] reply = new byte[] { };
        return reply;
    }
}
```

### 发现 DiagnosticListeners

接收事件的第一步是发现你对哪些 `DiagnosticListeners` 感兴趣。`DiagnosticListener` 支持一种发现运行时在系统中活跃的 `DiagnosticListeners` 的方法。完成这个任务的 API 是 [AllListeners](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.diagnosticlistener.alllisteners#system-diagnostics-diagnosticlistener-alllisteners) 属性。

实现一个继承自 `IObservable` 接口的 `Observer<T>` 类，它是 `IEnumerable` 接口的"回调"版本。你可以在 [Reactive Extensions](https://github.com/dotnet/reactive) 网站上了解更多关于它的信息。一个 `IObserver` 有三个回调，`OnNext`, `OnComplete` 和 `OnError`。一个 `IObservable` 有一个叫做 `Subscribe` 的方法，它被传递给这些观察者中的一个。一旦连接，当事情发生时，观察者会得到回调（主要是 OnNext 回调）。

`AllListeners` 静态属性的一个典型用法是这样的：

```c#
class Observer<T> : IObserver<T>
{
    public Observer(Action<T> onNext, Action onCompleted)
    {
        _onNext = onNext ?? new Action<T>(_ => { });
        _onCompleted = onCompleted ?? new Action(() => { });
    }
    public void OnCompleted() { _onCompleted(); }
    public void OnError(Exception error) { }
    public void OnNext(T value) { _onNext(value); }
    private Action<T> _onNext;
    private Action _onCompleted;
}
class MyListener
{
    IDisposable networkSubscription;
    IDisposable listenerSubscription;
    private readonly object allListeners = new();
    public void Listening()
    {
        Action<KeyValuePair<string, object>> whenHeard = delegate (KeyValuePair<string, object> data)
        {
            Console.WriteLine($"Data received: {data.Key}: {data.Value}");
        };
        Action<DiagnosticListener> onNewListener = delegate (DiagnosticListener listener)
        {
            Console.WriteLine($"New Listener discovered: {listener.Name}");
            //Subscribe to the specific DiagnosticListener of interest.
            if (listener.Name == "System.Net.Http")
            {
                //Use lock to ensure the callback code is thread safe.
                lock (allListeners)
                {
                    if (networkSubscription != null)
                    {
                        networkSubscription.Dispose();
                    }
                    IObserver<KeyValuePair<string, object>> iobserver = new Observer<KeyValuePair<string, object>>(whenHeard, null);
                    networkSubscription = listener.Subscribe(iobserver);
                }

            }
        };
        //Subscribe to discover all DiagnosticListeners
        IObserver<DiagnosticListener> observer = new Observer<DiagnosticListener>(onNewListener, null);
        //When a listener is created, invoke the onNext function which calls the delegate.
        listenerSubscription = DiagnosticListener.AllListeners.Subscribe(observer);
    }
    // Typically you leave the listenerSubscription subscription active forever.
    // However when you no longer want your callback to be called, you can
    // call listenerSubscription.Dispose() to cancel your subscription to the IObservable.
}
```

这段代码创建了一个回调委托，并使用 `AllListeners.Subscribe` 方法，请求为系统中每个活动的 `DiagnosticListener` 调用该委托。是否订阅监听器的决定是通过检查它的名字来做出的。上面的代码正在寻找你之前创建的 "System.Net.Http" 监听器。

像所有对 `Subscribe()` 的调用一样，这个调用返回一个 `IDisposable`，代表订阅本身。只要没有人对这个订阅对象调用 `Dispose()`，回调就会继续发生。本例代码从未调用`Dispose()`，所以它将永远收到回调。

当你订阅 `AllListeners` 时，你会得到所有激活的 `DiagnosticListeners` 的回调。因此，在订阅时，你会得到所有现有 `DiagnosticListeners` 的大量回调，当新的 `DiagnosticListeners` 被创建时，你也会收到那些回调。你会收到一份可能被订阅的所有东西的完整列表。

### 订阅 DiagnosticListeners

`DiagnosticListener` 实现了 `IObservable<KeyValuePair<string, object>>` 接口，所以你也可以对它调用 `Subscribe()`。下面的代码显示了如何填写前面的例子：

```c#
IDisposable networkSubscription;
IDisposable listenerSubscription;
private readonly object allListeners = new();
public void Listening()
{
    Action<KeyValuePair<string, object>> whenHeard = delegate (KeyValuePair<string, object> data)
    {
        Console.WriteLine($"Data received: {data.Key}: {data.Value}");
    };
    Action<DiagnosticListener> onNewListener = delegate (DiagnosticListener listener)
    {
        Console.WriteLine($"New Listener discovered: {listener.Name}");
        //Subscribe to the specific DiagnosticListener of interest.
        if (listener.Name == "System.Net.Http")
        {
            //Use lock to ensure the callback code is thread safe.
            lock (allListeners)
            {
                if (networkSubscription != null)
                {
                    networkSubscription.Dispose();
                }
                IObserver<KeyValuePair<string, object>> iobserver = new Observer<KeyValuePair<string, object>>(whenHeard, null);
                networkSubscription = listener.Subscribe(iobserver);
            }

        }
    };
    //Subscribe to discover all DiagnosticListeners
    IObserver<DiagnosticListener> observer = new Observer<DiagnosticListener>(onNewListener, null);
    //When a listener is created, invoke the onNext function which calls the delegate.
    listenerSubscription = DiagnosticListener.AllListeners.Subscribe(observer);
}
```

在这个例子中，在发现了 "System.Net.Http" 的 DiagnosticListener 之后，创建了一个动作，打印出监听器的名称、事件和 `payload.ToString()`。

> 注意：
>
> DiagnosticListener 实现了 `IObservable<KeyValuePair<string, object>>`。这意味着所有的回调函数都会返回一个 `KeyValuePair` 对象。key 是事件名称，value 是有效载荷。这个例子只是把这些信息打印输出值控制台。

追踪对 `DiagnosticListener` 的订阅很重要。在前面的代码中，`networkSubscription` 变量记住了这一点。如果你由另一个创建构成，你必须取消订阅之前的监听器，并订阅新的监听器。

`DiagnosticSource/DiagnosticListener` 代码是线程安全的，但回调代码也需要是线程安全的。为了确保回调代码是线程安全的，我们使用了锁。有可能在同一时间创建两个具有相同名称的 `DiagnosticListeners`。为了避免竞赛条件，共享变量的更新是在锁的保护下进行的。

一旦前面的代码被运行，下次在 'System.Net.Http' DiagnosticListener 上执行 Write() 时，信息将被记录到控制台。

订阅是相互独立的。因此，其他代码可以做与代码示例完全相同的事情，并产生两个"管道"的日志信息。

### 译码有效载荷

传递给回调的 KeyvaluePair 有事件名称和有效载荷，但有效载荷被简单地打成一个对象。有两种方法可以获得更具体的数据：

如果有效载荷是一个众所周知的类型（例如，一个字符串，或一个 `HttpMessageRequest`），那么你可以简单地将对象转换为预期的类型（使用 `as` 操作符，以便在你错误时不会引起异常），然后访问字段。这是很有效的。

使用反射 API。例如，假设有以下方法：

```c#
/// Define a shortcut method that fetches a field of a particular name.
static class PropertyExtensions
{
    static object GetProperty(this object _this, string propertyName)
    {
        return _this.GetType().GetTypeInfo().GetDeclaredProperty(propertyName)?.GetValue(_this);
    }
}
```

为了更完整的译码整个载荷，你可以用下面代码替换 `listener.Subscribe()` 调用的方法内容：

```c#
networkSubscription = listener.Subscribe(delegate(KeyValuePair<string, object> evnt) {
        var eventName = evnt.Key;
        var payload = evnt.Value;
        if (eventName == "RequestStart")
        {
            var url = payload.GetProperty("Url") as string;
            var request = payload.GetProperty("Request");
            Console.WriteLine("Got RequestStart with URL {0} and Request {1}", url, request);
        }
    });
```

请注意，使用反射是相对昂贵的。然而，如果使用匿名类型生成的有效载荷，使用反射是唯一的选择。这种开销可以通过使用 [PropertyInfo.GetMethod.CreateDelegate()](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.methodinfo.createdelegate) 或 [System.Reflection.Emit](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.emit) 命名空间制作快速、专门的属性获取器来减少损耗，但这不在本文的讨论范围。(对于一个快速的、基于委托的属性获取器的例子，请看`DiagnosticSourceEventSource` 中使用的 [PropertySpec 类](https://github.com/dotnet/runtime/blob/6de7147b9266d7730b0d73ba67632b0c198cb11e/src/libraries/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/DiagnosticSourceEventSource.cs#L1235)）。

### 过滤器

在前面的例子中，代码使用 IObservable.Subscribe() 方法来衔接回调。这导致所有的事件都被赋予回调。然而，DiagnosticListener 有 Subscribe() 的重载，允许控制器控制哪些事件被给予。

前面的例子中 listenener.Subscribe() 的调用可以用下面的代码代替来演示。

```c#
// Create the callback delegate.
Action<KeyValuePair<string, object>> callback = (KeyValuePair<string, object> evnt) =>
    Console.WriteLine("From Listener {0} Received Event {1} with payload {2}", networkListener.Name, evnt.Key, evnt.Value.ToString());

// Turn it into an observer (using the Observer<T> Class above).
Observer<KeyValuePair<string, object>> observer = new Observer<KeyValuePair<string, object>>(callback);

// Create a predicate (asks only for one kind of event).
Predicate<string> predicate = (string eventName) => eventName == "RequestStart";

// Subscribe with a filter predicate.
IDisposable subscription = listener.Subscribe(observer, predicate);

// subscription.Dispose() to stop the callbacks.
```

这只有效地订阅了 "RequestStart" 事件。所有其他事件将导致 DiagnosticSource.IsEnabled() 方法返回 false，从而被有效地过滤掉。

### 基于 Context 过滤

有些场景需要基于扩展的上下文进行高级过滤。生产者可以调用 [DiagnosticSource.IsEnabled](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.diagnosticsource.isenabled) 重载并提供额外的事件属性，如以下代码所示。

```c#
//aRequest and anActivity are the current request and activity about to be logged.
if (httpLogger.IsEnabled("RequestStart", aRequest, anActivity))
    httpLogger.Write("RequestStart", new { Url="http://clr", Request=aRequest });
```

下面代码例子展示了消费者可以使用如属性来精确过滤事件：

```c#
// Create a predicate (asks only for Requests for certain URIs)
Func<string, object, object, bool> predicate = (string eventName, object context, object activity) =>
{
    if (eventName == "RequestStart")
    {
        if (context is HttpRequestMessage request)
        {
            return IsUriEnabled(request.RequestUri);
        }
    }
    return false;
}

// Subscribe with a filter predicate
IDisposable subscription = listener.Subscribe(observer, predicate);
```

生产者不知道消费者所提供的过滤器。DiagnosticListener 将调用所提供的过滤器，必要时省略额外的参数，因此过滤器应该期望收到一个空的上下文。如果生产者用事件名和上下文调用 IsEnabled()，这些调用被包含在一个只接受事件名的重载中。消费者必须确保他们的过滤器允许没有上下文的事件通过。