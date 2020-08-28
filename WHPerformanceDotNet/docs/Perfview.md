# ETW(Event Tracing for Windows) 

它是 Windows 提供的原生的事件跟踪日志系统。由于采用内核（Kernel）层面的缓冲和日志记录机制，所以ETW提供了一种非常高效的事件跟踪日志解决方案，具体详见 [https://www.cnblogs.com/artech/p/logging-via-etw.html](https://www.cnblogs.com/artech/p/logging-via-etw.html)

安装之后具体的用法，拿 dotnet core 距离：

```
1. 菜单. -> collect. -> run [弹出一个界面]
2. command: 输入 dotnet run
3. Data File 内容不变
4. Current Dir: 这个为你要收集项目的跟目录地址
5. Advanced Options. -> Additional Prividers: *EtlDemo
上面 Additional Prividers 的值为项目程序事件源的名字([EventSource(Name = "EtlDemo")])
```

## 自定义 ETW 事件监听器

定义自己的事件，使用 PerfView 工具下做收集与分析大多数是足够的。但是如果需要自定义记录器活执行接近实时的事件分析，那么就要自己创建侦听器了。只需要集成 `EventListener` 即可。