using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Net6WebDemo;

public class Program
{
	private static void Main(string[] args)
	{
		// WebApplication.CreateBuilder ���� WebApplicationBuilder ʾ��
		// ��ҪĿ��Ϊ�� ����������Host/WebHost ����һЩĬ�ϵ����ã�ͨ�� BootstrapHostBuilder��
		// ���� WebApplicationBuilder.Build ȷ���Ի�����ʩ����ע���������Ĳ�������
		// �������������ı�����һ���� HostBuilder�������� Host ��������������������Լ�����ע�����
		// ��һ�� ApplicationBuilder �� Build ���ظ��ⲿʹ��
		//var builder = WebApplication.CreateBuilder(args);
		//builder.Services.AddTransient<IStartupFilter, DemoIStartupFilter>();
		//var app = builder.Build();

		//app.MapGet("/", () => "Hello World!");
		//// �ڲ�ע�� IHost <- Microsoft.Extensions.Hosting.Internal.Host �ķ��� StartAsync
		//// �ڲ�����ͨ��ע��� IHostedService �������е� IStartupFilter ����Reverse��ִ�С�
		//app.Run();
	}
}
