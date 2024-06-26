# .NET API 接口数据传输加密最佳实践

我们在做 Api 接口时，相信一定会有接触到要给传输的请求 body 的内容进行加密传输。其目的就是为了防止一些敏感的内容直接被 UI 层查看或篡改。

其实粗略一想就能想到很多种方案，但是哪些方案是目前最适合我们项目的呢？

## 硬编码方式

最先想到的应该就是硬编码方式，就是哪个接口需要进行传输加密，那么就针对该接口特殊处理：

```c#
public class SecurityApiController {
	...
	public async Task<Result> UpdateUser([FromBody] SecurityRequest request) {
		var requestBody = RsaHelper.Decrypt(privateKey, request.Content);
        var user = JsonHelper.Deserialize<UserDto>(requestBody);
		await UpdateUserAsync(user);
		return new Result(RsaHelper.Encrypt(publicKey, new{ Success=true}));
	}
}
```

这种方式好处是简单明了，按需编程即可，不会对其它接口造成污染。

一旦这种需求越来越多，我们就会写大量如上的重复性代码；而对于前端而言也是如此，所以当我们需要传输加密乃是最基础的需求时，上面硬编码的方式就显得很不合适了。

这个时候我们可以采用统一入口的方式来实现

## 统一入口

回顾上面的硬编码方式，其实每个接口处的加解密处理从 SRP 原则上理解，不应该是接口的职责。所以需要把这部分的代码移到一个单独的方法，再加解密之后我们再把该请求调度到具体的接口。

这种方式其实有很多种实现方式，在这里我先说一下我司其中一个 .NET4.5 的项目采取的方式。

其实就是额外提供了一个统一的入口，所有需要传输加密的需求都走这一个接口：如`http://api.example.com/security`

```c#
public class SecurityController {
	...
	public async Task<object> EntryPoint([FromBody] SecurityRequest request) {
		var requestBody = RsaHelper.Decrypt(privateKey, request.Content);
		var user = JsonHelper.Deserialize<UserDto>(requestBody);
		var obj = await DispathRouter(requestBody.Router, user);
        return new Result(RsaHelper.Encrypt(publicKey, obj));
	}
	
	public async Task<object> DispathRouter(Router router, object body) {
		...
		Type objectCon = typeof(BaseController);
        MethodInfo methInfo = objectCon.GetMethod(router.Name);
        var resp = (Task<object>)methInfo.Invoke(null, body);
        return await resp;
	}
}
```

很明显这是通过统一入口地址调用并配合反射来实现这种目的。

这种好处如前面所说，统一了调用入口，这样提高了代码复用率，让加解密不再是业务接口的一部分了。同样，这种利用一些不好的点；比如用了反射性能会大打折扣。并且我们过度的进行统一了。我们看到这种方式只能将所有的接口方法都写到 BaseController。所以我司项目的 Controller 部分，会看到大量如下的写法：

```c#
// 文件 UserController.cs
public partial class BaseController {
	...
}
// 文件 AccountController.cs 
public partial class BaseController {

}
// ...
```

这样势必就会导致一个明显的问题，就是“代码爆炸”。这相当于将所有的业务逻辑全部灌输到一个控制器中，刚开始写的时候方便了，但是后期维护以及交接换人的时候阅读代码是非常痛苦的一个过程。因为在不同的 Controller 文件中势必会重复初始化一些模块，而我们在引用方法的时候 IDE 每次都会显示上千个方法，有时候还不得不查看哪些方法名一样或相近的具体意义。

> 针对上述代码爆炸的方式还有一种优化，就是将控制器的选择开放给应用端，也就是将方法名和控制器名都作为请求参数暴露给前端，但是这样会加大前端的开发心智负担。

综上所述我是非常不建议采用这种方式的。虽说是很古老的.Net4/4.5 的框架，但是我们还是有其它相对更优雅的实现方式。

## 中间件

其实我们熟悉了 .NETCore 下的 [Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-6.0)机制，我们会很容易的在 .NETCore 下实现如标题的这种需求：

```c#
// .NET Core 版本
public class SecuriryTransportMiddleware {
	private readonly RequestDelegate _next;

    public RequestCultureMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
    	// request handle
        var encryptedBody = context.Request.Body;
        var encryptedContent = new StreamReader(encryptedBody).ReadToEnd();
        var decryptedBody = RsaHelper.Decrypt(privateKey, encryptedContent);
        var originBody = JsonHelper.Deserialize<object>(decryptedBody);
        
        var json = JsonHelper.Serialize(dataSource);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
        stream = await requestContent.ReadAsStreamAsync();
        context.Request.Body = stream;
        
        await _next(context);
        // response handle
        var originContent = new StreamReader(context.Response.Body).ReadToEnd();
        var encryptedBody = RsaHelper.Encrypt(privateKey, originContent);
        var responseContent = new StringContent(json, Encoding.UTF8, "application/json");
        context.Response.Body = await responseContent.ReadAsStreamAsync();
        // 或者直接
        // await context.Response.WriteAsync(encryptedBody);
    }
}
```

为了方便描述，以上代码我省略了必要的校验和异常错误处理

这样有什么好处呢？一个最明显的好处就是解耦了加解密与真正业务需求。对真正的业务代码几乎没有侵略性。其实我认为业务开发能做到这里其实就差不多了，还有其它需求都可以基于这个中间件进行拓展开发。

那么在 .NET Framwork 没有中间件怎么办呢？

其实在 .NET Framwork 框架下 [IHttpModule](https://learn.microsoft.com/en-us/previous-versions/aspnet/ms227673(v=vs.100)) 能和中间件一样能做到这点：

```c#
public class SecurityTransportHttpModule : IHttpModule {
	...
	public void Init(HttpApplication context) {
		...
		context.BeginRequest += ContextBeginRequest;
		context.PostRequestHandlerExecute += ContextPostRequestHandlerExecute;
	}
	
	private void ContextBeginRequest(object sender, EventArgs e) {
		HttpContext context = ((HttpApplication)sender).Context;
		var encryptedBody = context.Request.Body;
		...
		context.Request.Body = stream;
	}
	
	private void ContextPostRequestHandlerExecute(object sender, EventArgs e)
    {
        HttpContext context = ((HttpApplication)sender).Context;
        ...
        context.Response.Write(encryptedBody)
    }
}
```

为什么之前提到这种方案就“差不多”了呢，实际上上面这种方式在某些场景下会显得比较“累赘”。因为无论通过中间件和还是 IHttpModule 都会导致所有请求都会经过它，相当于增加了一个过滤器，如果这时候我要新增一个上传文件接口，那必然也会经过这个处理程序。说的更直接一点，如果碰到那些少数不需要加解密的接口请求那要怎么办呢？

其实上面可以进行拓展处理，比如对特定的请求进行过滤：

```c#
if(context.Request.Path.Contains("upload")) {
	return;
}
```

> 注意上述代码只是做个 demo 展示，真正还是需要通过如 `context.GetRouterData()` 获取路由数据进行精准比对。

当类似于这种需求开始变多以后（吐槽：谁知道业务是怎么发展的呢？）原来的中间件的“任务量”开始变得厚重了起来。到时候也会变得难以维护和阅读。

这个时候就是我目前较为满意的解决方案登场了，它就是[模型绑定 ModelBinding](https://learn.microsoft.com/en-us/aspnet/core/mvc/advanced/custom-model-binding?view=aspnetcore-7.0)。

## 模型绑定

回到需求的开端，不难发现，我们其实要是**如何将前端加密后的请求体绑定到我们编写的接口方法中**。这里面的过程很复杂，需要解析前端发起的请求，解密之后还要反序列化成目标接口需要的方法参数。而这个过程还要伴随着参数校验，如这个请求是否符合加密格式。而这个过程的一切都是模型绑定要解决的事。我们以 NETCore 版本为例子，讲一下大概的流程；

模型绑定的过程其实就是将请求体的各个字段于具体的 CLR 类型的字段属性进行一一匹配的过程。.NetCore 再程序启动时会默认提供了一些内置的模型绑定器，并开放 [IModelBinderProvider](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.modelbinding.imodelbinderprovider?view=aspnetcore-7.0) 接口允许用户自定义模型绑定器。我们通过查看 [MvcCoreMvcOptionsSetup](https://source.dot.net/#Microsoft.AspNetCore.Mvc.Core/Infrastructure/MvcCoreMvcOptionsSetup.cs,56) 就清楚看到框架为我们添加 18 个自带的模型绑定器。以及如何调用的方式。

所以接下来我们很容易的可以一葫芦画瓢的照抄下来：

```c#
public class SecurityTransportModelBinder : IModelBinder {
	...
	public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        try
        {
            var request = bindingContext.HttpContext.Request;
            var model = await JsonSerializer.DeserializeAsync<SafeDataWrapper>(request.Body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            var decryptContent = RsaHelper.Decrypt(model.Info, privateKey);
            var activateModel = JsonSerializer.Deserialize(decryptContent, bindingContext.ModelMetadata.ModelType, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            //重新包装
            if (activateModel == null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Success(activateModel);
            }
        }
        catch (Exception exception)
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                exception,
                bindingContext.ModelMetadata);
        }

        _logger.DoneAttemptingToBindModel(bindingContext);
        //return Task.CompletedTask;
    }
}
```

抄了 ModelBinder 还不行，还要抄 ModelBinderProvider：

```c#
public class SecurityTransportModelBinderProvider : IModelBinderProvider
{
    public IModelBinder GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Metadata.IsComplexType && typeof(IApiEncrypt).IsAssignableFrom(context.Metadata.ModelType))
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            var configuration = context.Services.GetRequiredService<IConfiguration>();
            return new SecurityTransportModelBinder(loggerFactory, configuration);
        }

        return null;
    }
}
```

这里我做了一个方便我自己的拓展功能，就是显示打了 `IApiEncrypt` 接口标签的才会正常进行解析绑定。

剩下的就是在 ConfigureService 中添加进去即可：

```c#
services.AddControllers(options => {
	...
	options.ModelBinderProviders.Insert(0, new SecurityTransportModelBinderProvider());
})
```

这样实现过后，我们就能像使用 `FromBody` 那样就能按需调用即可：

```c#
[HttpPost("security")]
public async Task<ResultDto> DemoDecrypt([ModelBinder(typeof(SecurityTransportModelBinder))] OriginBusinessRequest request)
{
    //激活结果
	...
    return await Task.FromResult(WriteSafeData(data, publicKey));
}
```

如果是默认处理加解密也是可以的，直接在对应的请求实体类打上 `IApiEncrypt` 标签就会自动执行模型绑定

```c#
public class UserUpdateRequest: IApiEncrypt {
	public int UserId { get; set; }
	public string Phone { get; set; }
	public string Address { get; set; }
	...
}
```

这种方案其实也还是有缺点的，从刚刚的使用来看就知道，模型绑定无法解决返回自动加密处理。所以我们不得不在每个接口处写下如 `WriteSafeData(data, publicKey)` 这种显式加密的代码。

优化的方式也很简单，其实我们可以通过过滤器可以解决，这也是为什么我要加 `IApiEncrypt` 的原因，因为有了这个就能确定知道这是一个安全传输的请求，进而进行特殊处理。

> 注意，这不是 .NET Core 独有的特性，.Net Framework 也有[模型绑定器](https://learn.microsoft.com/en-us/dotnet/api/system.web.mvc.imodelbinder?view=aspnet-mvc-5.2)

## 总结

针对接口级传输加密这个需求，我们总共讨论了四种实现方式。其实各有各的好处和缺点。

硬编码方式适合只有特定需求的场景下是适合这种方案的。但是一旦这种需求成为一种规范或普遍场景时，这个方法就不合适了

统一入口适合制定了一种接口规范，所有加密的请求都走这一个接口。然后通过路由解析调度到不同的控制器。其实我讲了我司在 .NET Framework 下采取的一种方案，好处时实现简单，做到了代码复用，对业务代码进行了解耦。但缺点是用了反射成为了性能消耗点，并且当业务越来越多就会产生代码爆炸，成为了维护灾难。

而中间件或 IHttpModule 方式就很好的解决这了这点。但同样也不是使用所有场景，如对新增的不需要加密的接口要进行过滤处理等。

最后介绍了模型绑定这种方式，技能很方便的满足大多数场景，也满足个别列外的需求。但同样也有缺点，就是无法针对接口响应体进行自动加密处理，所谓好事做到底，送佛送到西，这事只能算是做到一半吧。

其实我还想到了一种类似 Aop 的方案，那就是实现一个路由过滤器的功能，当请求进来，通过路由处理程序对请求体进行解密，然后重写请求流。然后调度到对应的原始路由，最后在响应的时候再加密重写一次。不过查阅了一番资料，并没有收获。

## 参考资料

- https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.modelbinding.imodelbinderprovider?view=aspnetcore-7.0
- https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write?view=aspnetcore-6.0
- https://www.stevejgordon.co.uk/html-encode-string-aspnet-core-model-binding