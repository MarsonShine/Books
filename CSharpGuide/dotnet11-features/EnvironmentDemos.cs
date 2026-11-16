using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>需要操作系统或网络环境的示例：默认不访问外部网络。</summary>
public static class EnvironmentDemos
{
    public static async Task RunAsync(string[] args)
    {
        if (args.Length == 0 || args[0] == "process")
        {
            await ProcessAsync();
            Console.WriteLine("可选：environment http（本地回环 HTTP 指标）；environment dns <域名> [DNS服务器IP]。");
            return;
        }

        switch (args[0])
        {
            case "http" when args.Length == 1:
                await HttpMetricsAsync();
                break;
            case "dns" when args.Length is 2 or 3:
                await DnsAsync(args[1], args.Length == 3 ? args[2] : null);
                break;
            default:
                throw new ArgumentException("用法：environment [process|http|dns <域名> [DNS服务器IP]]");
        }
    }

    private static async Task ProcessAsync()
    {
        Console.WriteLine("\n=== .NET 11 进程查找和退出状态 ===");
        if (Process.TryGetProcessById(Environment.ProcessId, out Process? current))
        {
            using (current)
                Console.WriteLine($"找到当前进程：PID={current!.Id}，名称={current.ProcessName}");
        }

        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("--version");
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("未能启动 dotnet 子进程。");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await foreach (ProcessOutputLine line in process.ReadAllLinesAsync(timeout.Token))
                Console.WriteLine($"{(line.StandardError ? "stderr" : "stdout")}: {line.Content}");
            ProcessExitStatus status = await process.WaitForExitStatusAsync(timeout.Token);
            Console.WriteLine($"退出码={status.ExitCode}，是否因信号结束={status.Signal is not null}，取消={status.Canceled}");
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw new TimeoutException("dotnet --version 未在 10 秒内完成，已请求结束这个示例启动的子进程。");
        }
    }

    private static async Task DnsAsync(string domain, string? server)
    {
        if (!(OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
        {
            Console.WriteLine("当前演示仅在 Windows、Linux 和 macOS 上运行 DNS 查询。");
            return;
        }
        Console.WriteLine($"\n=== MX 记录：{domain} ===");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            DnsResult<MxRecord> result;
            if (server is null)
            {
                result = await Dns.ResolveMxAsync(domain, timeout.Token);
            }
            else
            {
                if (!IPAddress.TryParse(server, out IPAddress? address))
                    throw new ArgumentException("DNS 服务器必须是 IPv4 或 IPv6 地址。", nameof(server));
                var options = new DnsResolverOptions { Servers = [new IPEndPoint(address, 53)] };
                using var resolver = new DnsResolver(options);
                result = await resolver.ResolveMxAsync(domain, timeout.Token);
            }
            Console.WriteLine($"响应码={result.ResponseCode}，记录数={result.Records.Count}，负缓存 TTL={result.NegativeCacheTtl}");
            foreach (MxRecord record in result.Records)
                Console.WriteLine($"优先级={record.Preference}，邮件交换主机={record.Exchange}，TTL={record.Ttl}");
            if (result.Records.Count == 0) Console.WriteLine("没有返回 MX 记录；请结合响应码判断原因。");
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            Console.WriteLine("DNS 查询超过 5 秒，等待已取消；请检查域名和系统 DNS 配置。");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"DNS 查询失败：{ex.SocketErrorCode} ({ex.Message})");
        }
    }

    private static async Task HttpMetricsAsync()
    {
        Console.WriteLine("\n=== 本地 HTTP 客户端指标 ===");
        var measurements = new ConcurrentQueue<string>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "System.Net.Http" &&
                (instrument.Name == "http.client.open_connections" || instrument.Name == "http.client.active_requests"))
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            string dimensions = string.Join(", ", tags.ToArray().Select(tag => $"{tag.Key}={tag.Value}"));
            measurements.Enqueue($"{instrument.Name}={value} [{dimensions}]");
        });
        meterListener.Start();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var server = new TcpListener(IPAddress.Loopback, 0);
        server.Start();
        int port = ((IPEndPoint)server.LocalEndpoint).Port;
        var requestReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseResponse = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            Task serverTask = RespondOnceAsync(server, requestReceived, releaseResponse.Task, timeout.Token);
            using var handler = new SocketsHttpHandler { UseProxy = false };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
            Task<string> request = client.GetStringAsync($"http://127.0.0.1:{port}/", timeout.Token);
            await requestReceived.Task.WaitAsync(timeout.Token);
            Console.WriteLine("请求已到达本机服务端，响应暂未发送。主动采集：");
            meterListener.RecordObservableInstruments();
            PrintMeasurements(measurements);
            releaseResponse.TrySetResult();
            Console.WriteLine($"收到响应：{await request}");
            await serverTask;
            Console.WriteLine("请求结束后再次采集：");
            meterListener.RecordObservableInstruments();
            PrintMeasurements(measurements);
        }
        finally
        {
            releaseResponse.TrySetResult();
            server.Stop();
        }
    }

    private static void PrintMeasurements(ConcurrentQueue<string> measurements)
    {
        if (measurements.IsEmpty) Console.WriteLine("此次采集没有收到上述指标；可能没有对应的活动测量，也可能与运行时和配置有关。");
        while (measurements.TryDequeue(out string? measurement)) Console.WriteLine(measurement);
    }

    private static async Task RespondOnceAsync(TcpListener server, TaskCompletionSource requestReceived, Task releaseResponse, CancellationToken token)
    {
        using TcpClient connection = await server.AcceptTcpClientAsync(token);
        using NetworkStream stream = connection.GetStream();
        byte[] buffer = new byte[1024];
        string headers = "";
        while (!headers.Contains("\r\n\r\n", StringComparison.Ordinal))
        {
            int count = await stream.ReadAsync(buffer, token);
            if (count == 0) throw new IOException("请求头尚未收齐，客户端已断开。");
            headers += Encoding.ASCII.GetString(buffer, 0, count);
            if (headers.Length > 16_384) throw new IOException("请求头过长。");
        }
        requestReceived.TrySetResult();
        await releaseResponse.WaitAsync(token);
        byte[] response = "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nOK"u8.ToArray();
        await stream.WriteAsync(response, token);
    }
}
