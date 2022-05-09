using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Net6WebDemo;

public class Program
{
	private static void Main(string[] args)
	{
		// WebApplication.CreateBuilder 生成 WebApplicationBuilder 示例
		// 主要目的为对 环境变量、Host/WebHost 进行一些默认的配置（通过 BootstrapHostBuilder）
		// 调用 WebApplicationBuilder.Build 确保对基础设施服务注册和配置项的操作结束
		// 里面有两个核心变量，一个是 HostBuilder，用来对 Host 构建配置项、环境变量、以及依赖注入服务。
		// 另一个 ApplicationBuilder 供 Build 返回给外部使用
		//var builder = WebApplication.CreateBuilder(args);
		//builder.Services.AddTransient<IStartupFilter, DemoIStartupFilter>();
		//var app = builder.Build();

		//app.MapGet("/", () => "Hello World!");
		//// 内部注册 IHost <- Microsoft.Extensions.Hosting.Internal.Host 的方法 StartAsync
		//// 内部方法通过注入的 IHostedService 服务将所有的 IStartupFilter 反向（Reverse）执行。
		//app.Run();
	}
}
