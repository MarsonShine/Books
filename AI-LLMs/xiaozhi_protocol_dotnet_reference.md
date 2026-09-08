# 小智 WebSocket 协议与 .NET 调试服务器

更新日期：2026-09-08。本文用于理解和调试 `78/xiaozhi-esp32` 的 WebSocket 接入。部署 xinnan 全模块与设备配网见[局域网部署指南](xiaozhi_alientek_lan_full_guide_2026-09-08.md)。

**范围：** 下面的可编译示例只支持协议 v1、上行 Opus 16 kHz/单声道/60 ms，验证 OTA POST、鉴权、hello 和二进制接收。它不实现 Opus 解码、ASR、LLM、TTS、MCP、设备账号绑定、真正的打断或固件升级，不能作为完整语音服务。文中的音频流程属于后续实现规范。

## 1. OTA 配置、凭据与版本

设备启动时向 OTA URL 请求配置，随后按返回值建立 WebSocket。设备保存的设置可能包含此前的协议版本，因此自研 v1 服务要显式下发 `websocket.version = 1`，不能只改 URL。

OTA 返回对象可以包含：

```json
{
  "server_time": {
    "timestamp": 1788825600000,
    "timezone_offset": 480
  },
  "firmware": {
    "version": "ECHO_INSTALLED_VERSION",
    "url": ""
  },
  "websocket": {
    "url": "ws://192.168.50.10:5000/xiaozhi/v1/",
    "token": "REPLACE_WITH_DEVICE_TOKEN",
    "version": 1
  }
}
```

此 JSON 是形状示意：时间应在响应时生成，`timestamp` 是 Unix 毫秒，`timezone_offset` 是分钟；UTC 时间不能先手动加八小时再与 480 偏移叠加。未提供升级时回显设备安装版本并保留空 URL，不能虚构更高版本或设置强制升级。

**设备引导配置与后续连接鉴权不是同一件事。** 以下探针通过 Device-Id 限定目标设备，并给它下发一个配置的共享令牌。Device-Id/MAC 可被伪造，HTTP/ws 是明文，所以这不是安全的设备注册协议；只能放在受控测试 LAN。正式部署需要可信引导、设备身份、独立/可撤销令牌、TLS 与限流，不能认为“WS 检查了 token”就保护了公开的 OTA 发 token 入口。[OTA 源码](https://github.com/78/xiaozhi-esp32/blob/main/main/ota.cc)

## 2. 创建和启动 .NET 10 探针

先按[Ubuntu 的 .NET 安装文档](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install)安装对应 SDK，再确认：

```bash
dotnet --list-sdks
mkdir -p ~/xiaozhi-custom
cd ~/xiaozhi-custom
dotnet new web -n XiaoZhiGateway --framework net10.0
cd XiaoZhiGateway
dotnet user-secrets init
```

将 `Program.cs` 替换为下一节完整代码。此阶段没有编解码，不需要安装 Concentus。

用编辑器打开新建项目的 `appsettings.json`，保留默认日志配置并增加：

```json
{
  "XiaoZhi": {
    "WebSocketUrl": "ws://192.168.50.10:5000/xiaozhi/v1/",
    "DeviceId": "REPLACE_WITH_YOUR_DEVICE_MAC"
  }
}
```

示例代码确实从这些配置读取值，没有第二份硬编码地址。令牌通过 User Secrets 配置；下列 Bash 命令从标准输入写入密钥，避免真实令牌作为命令参数：

```bash
read -rsp '输入至少 32 字符的随机设备令牌: ' XIAOZHI_TOKEN
echo
export XIAOZHI_TOKEN
python3 -c 'import json,os; print(json.dumps({"XiaoZhi:Token": os.environ["XIAOZHI_TOKEN"]}))'   | dotnet user-secrets set
unset XIAOZHI_TOKEN
ASPNETCORE_ENVIRONMENT=Development   dotnet run --no-launch-profile --urls http://0.0.0.0:5000
```

User Secrets 只用于本地开发，并不加密。这里显式指定 Development 才会按默认配置加载它；正式环境应由部署系统注入 `XiaoZhi__Token` 等设置，不要为读取密钥而开启生产开发模式。双下划线是 .NET 环境变量表示配置层级的方式。

Windows PowerShell 可使用相同 C#；把环境赋值改为 `$env:ASPNETCORE_ENVIRONMENT = 'Development'`，其他命令中的 Bash 输入和管道示例应在 WSL 执行或改用本机编辑器设置。

## 3. 完整 Program.cs

消息累计上限为 64 KiB；首次 hello 超时为 10 秒，此后每条完整消息最多等待 30 秒。它们是调试探针的限制，安静时连接可能关闭，生产服务需按设备行为重新设计空闲策略。KeepAlive 不等于业务层 heartbeat，也不能替代 hello。

```csharp
using System.Net.WebSockets;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = 64 * 1024);

var advertisedUrl = builder.Configuration["XiaoZhi:WebSocketUrl"];
var token = builder.Configuration["XiaoZhi:Token"];
var allowedDevice = builder.Configuration["XiaoZhi:DeviceId"];
if (!Uri.TryCreate(advertisedUrl, UriKind.Absolute, out var uri) ||
    (uri.Scheme != "ws" && uri.Scheme != "wss") ||
    string.IsNullOrWhiteSpace(token) || token.Length < 32 ||
    string.IsNullOrWhiteSpace(allowedDevice))
{
    throw new InvalidOperationException(
        "Configure XiaoZhi:WebSocketUrl, Token (32+ chars) and DeviceId.");
}

var app = builder.Build();
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(20)
});

bool IsAllowedDevice(HttpContext context) =>
    string.Equals(context.Request.Headers["Device-Id"].ToString(),
        allowedDevice, StringComparison.OrdinalIgnoreCase);

app.MapGet("/health", () => Results.Ok(new { ok = true }));
// GET 只验证路由可达；设备配置由 POST 提供。
app.MapGet("/xiaozhi/ota/", () => Results.Text("OTA route reachable"));

app.MapPost("/xiaozhi/ota/", async (HttpContext context) =>
{
    context.Response.Headers.CacheControl = "no-store";
    if (!IsAllowedDevice(context))
        return Results.Unauthorized();

    try
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(
            context.RequestAborted);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        using var doc = await JsonDocument.ParseAsync(context.Request.Body,
            cancellationToken: deadline.Token);
        if (!doc.RootElement.TryObject("application", out var application) ||
            !application.TryText("version", out var version) ||
            string.IsNullOrWhiteSpace(version))
            return Results.BadRequest(new { error = "application.version required" });

        return Results.Json(new
        {
            server_time = new
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                timezone_offset = 480
            },
            // 回显已安装版本；此端点不提供固件升级。
            firmware = new { version, url = "" },
            websocket = new { url = advertisedUrl, token, version = 1 }
        });
    }
    catch (JsonException)
    {
        return Results.BadRequest(new { error = "invalid JSON" });
    }
    catch (OperationCanceledException)
    {
        return Results.StatusCode(StatusCodes.Status408RequestTimeout);
    }
});

app.Map("/xiaozhi/v1/", async context =>
{
    if (!IsAllowedDevice(context) ||
        !string.Equals(context.Request.Headers.Authorization.ToString(),
            $"Bearer {token}", StringComparison.Ordinal))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }
    if (context.Request.Headers["Protocol-Version"].ToString() != "1")
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    var sessionId = Guid.NewGuid().ToString("N");
    var helloDone = false;
    var audioPackets = 0;
    try
    {
        while (socket.State == WebSocketState.Open)
        {
            var message = await ReadMessageAsync(socket,
                helloDone ? TimeSpan.FromSeconds(30) : TimeSpan.FromSeconds(10),
                context.RequestAborted);
            if (message is null)
            {
                await CloseAsync(socket, WebSocketCloseStatus.NormalClosure, "bye");
                break;
            }
            if (message.Value.Type == WebSocketMessageType.Binary)
            {
                if (!helloDone)
                    throw new ProtocolError(WebSocketCloseStatus.PolicyViolation,
                        "hello required");
                if (message.Value.Payload.Length == 0)
                    throw new ProtocolError(WebSocketCloseStatus.InvalidPayloadData,
                        "empty audio packet");
                // 只统计负载，尚未解码或验证 Opus。
                audioPackets++;
                if (audioPackets == 1 || audioPackets % 100 == 0)
                    app.Logger.LogInformation("Session {Session}: audio packets {Count}",
                        sessionId, audioPackets);
                continue;
            }

            using var doc = JsonDocument.Parse(message.Value.Payload);
            var root = doc.RootElement;
            if (!root.TryText("type", out var type))
                throw new JsonException("type required");
            if (type == "hello")
            {
                if (helloDone || !root.HasInt("version", 1) ||
                    !root.HasText("transport", "websocket") ||
                    !root.TryObject("audio_params", out var audio) ||
                    !audio.HasText("format", "opus") ||
                    !audio.HasInt("sample_rate", 16000) ||
                    !audio.HasInt("channels", 1) ||
                    !audio.HasInt("frame_duration", 60))
                    throw new ProtocolError(WebSocketCloseStatus.PolicyViolation,
                        "requires v1 opus 16k mono 60ms");

                await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new
                {
                    type = "hello",
                    transport = "websocket",
                    session_id = sessionId,
                    audio_params = new
                    {
                        format = "opus",
                        sample_rate = 24000,
                        channels = 1,
                        frame_duration = 60
                    }
                }).AsMemory(), WebSocketMessageType.Text, true,
                    context.RequestAborted);
                helloDone = true;
                app.Logger.LogInformation("Session {Session}: hello accepted", sessionId);
            }
            else
            {
                if (!helloDone)
                    throw new ProtocolError(WebSocketCloseStatus.PolicyViolation,
                        "hello required");
                if (type is "listen" or "abort")
                    app.Logger.LogInformation("Session {Session}: event {Type}",
                        sessionId, type);
                // 探针不调用 MCP，也不记录原始 JSON 或用户语音文本。
            }
        }
    }
    catch (ProtocolError error)
    {
        await CloseAsync(socket, error.Status, error.Message);
    }
    catch (JsonException)
    {
        await CloseAsync(socket, WebSocketCloseStatus.InvalidPayloadData, "invalid JSON");
    }
    catch (OperationCanceledException)
    {
        await CloseAsync(socket, WebSocketCloseStatus.PolicyViolation, "timeout or cancelled");
    }
    catch (WebSocketException)
    {
        app.Logger.LogInformation("Session {Session}: transport disconnected", sessionId);
    }
});

await app.RunAsync();

static async Task<(WebSocketMessageType Type, byte[] Payload)?> ReadMessageAsync(
    WebSocket socket, TimeSpan timeout, CancellationToken cancellationToken)
{
    using var deadline =
        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    deadline.CancelAfter(timeout);
    var buffer = new byte[4096];
    using var stream = new MemoryStream();
    ValueWebSocketReceiveResult part;
    do
    {
        part = await socket.ReceiveAsync(buffer.AsMemory(), deadline.Token);
        if (part.MessageType == WebSocketMessageType.Close)
            return null;
        if (stream.Length + part.Count > 64 * 1024)
            throw new ProtocolError(WebSocketCloseStatus.MessageTooBig,
                "message exceeds 64 KiB");
        stream.Write(buffer, 0, part.Count);
    } while (!part.EndOfMessage);
    return (part.MessageType, stream.ToArray());
}

static async Task CloseAsync(WebSocket socket, WebSocketCloseStatus status, string reason)
{
    if (socket.State is not (WebSocketState.Open or WebSocketState.CloseReceived))
        return;
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    try
    {
        await socket.CloseOutputAsync(status, reason, deadline.Token);
    }
    catch (Exception error) when (error is WebSocketException or OperationCanceledException)
    {
        socket.Abort();
    }
}

sealed class ProtocolError(WebSocketCloseStatus status, string message) : Exception(message)
{
    public WebSocketCloseStatus Status { get; } = status;
}

static class JsonFields
{
    public static bool TryObject(this JsonElement root, string name, out JsonElement value)
    {
        value = default;
        return root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.Object;
    }

    public static bool TryText(this JsonElement root, string name, out string? value)
    {
        value = null;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty(name, out var field) ||
            field.ValueKind != JsonValueKind.String)
            return false;
        value = field.GetString();
        return true;
    }

    public static bool HasText(this JsonElement root, string name, string expected) =>
        root.TryText(name, out var actual) && actual == expected;

    public static bool HasInt(this JsonElement root, string name, int expected) =>
        root.ValueKind == JsonValueKind.Object &&
        root.TryGetProperty(name, out var field) &&
        field.ValueKind == JsonValueKind.Number &&
        field.TryGetInt32(out var actual) && actual == expected;
}

```

关键设计：没有 Authorization 也返回 401；HTTP 头和 hello 均验证 v1；先重组完整 WebSocket **消息**再解析，不能把一次 ReceiveAsync 当成一条音频消息；JSON 异常与大消息通过关闭码反馈。日志只记录事件类别与包数，不默认打印令牌、原始录音、转写或完整设备身份。

此探针串行收发，尚没有后台音频管线。扩展后一个连接应只有一个接收循环和一个串行发送器；不能让 TTS、STT 和错误处理并发调用同一 WebSocket 的 SendAsync。[ASP.NET Core WebSockets](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets?view=aspnetcore-10.0)

## 4. 验证到哪一层，才能声称哪一层完成

```bash
curl --fail http://127.0.0.1:5000/health
curl --fail http://127.0.0.1:5000/xiaozhi/ota/
```

若按 LAN URL 绑定并发布，可从 LAN 客户端将主机名换成真实服务器 IP。GET 只验证路由。OTA POST 必须带与配置一致的 Device-Id，以及形如 `{"application":{"version":"实际固件版本"}}` 的对象；它会返回包含令牌的 JSON，不要公开分享响应。

测试分层：

| 测试 | 预期 |
| --- | --- |
| OTA POST 缺 Device-Id 或不在允许列表 | 401 |
| OTA POST 非法 JSON 或缺版本 | 400 |
| WS 缺/错令牌 | 拒绝升级，401 |
| WS Protocol-Version 不是 1 | 拒绝升级，400 |
| hello 不是要求的协议/音频参数 | 关闭连接，策略违规 |
| 分片发送一条合法 hello | 重组后返回 hello |
| hello 前发送音频 | 拒绝，不能假装音频已可用 |
| 非法 JSON / 消息超过 64 KiB | 关闭连接并限制资源占用 |
| 合法 hello 后发送二进制 | 接收并统计；尚不证明它是有效 Opus |

本次验证在 .NET 10 上编译并使用本机 WebSocket 客户端检查上述协议边界；它不替代 ESP32 实机、Opus Codec 或云 API 测试。将设备 OTA 改为 `http://192.168.50.10:5000/xiaozhi/ota/` 后，应在日志中看到 hello 与音频计数。探针收到 `abort` 只记录事件，不会生成声音，也没有可取消的 LLM/TTS 请求。

## 5. 接入音频前先明确消息边界

协议 v1 的一条完整二进制 WebSocket 消息承载一个原始 Opus packet；v2/v3 带额外头。WebSocket 自身又可以分片，分片是传输层现象，不能直接送入解码器。不要把 v2/v3 的头当作 Opus，也不要把 Ogg/MP3/WAV 文件直接发成小智原始音频包。[协议说明](https://github.com/78/xiaozhi-esp32/blob/main/docs/websocket_zh.md)、[客户端封包实现](https://github.com/78/xiaozhi-esp32/blob/main/main/protocols/websocket_protocol.cc)

hello 中的上行描述与服务端回复的下行描述分别处理。16 kHz 输入、24 kHz 输出、60 ms 包时长是本例选择，不是所有板型/版本唯一允许的组合。

以单声道 16-bit PCM 为例：

| 方向 | 采样率 | 60 ms 的采样数 | PCM 字节数 |
| --- | ---: | ---: | ---: |
| 输入 | 16000 Hz | 960 | 1920 |
| 输出 | 24000 Hz | 1440 | 2880 |

Opus packet 的字节数是变化的，不能用 1920/2880 当作压缩包长度。一个 packet 也不一定等于编码器内部只有一个 frame。

后端应为每个设备会话维护独立编解码器。使用 [Concentus](https://www.nuget.org/packages/Concentus/) 等实现时，固定版本并按该版本的工厂/API 写代码，不把旧示例的 Create 方法当作永远稳定的接口。

解码处理的关键步骤：

```text
重组消息 → 按协议剥头 → Opus Decode → 检查返回的每声道采样数
→ 只使用本次有效 PCM → VAD / ASR
```

解码缓冲区按已支持的最大包时长分配，不应在允许更长包时仍固定为 960 个采样。遇到损坏包应有限丢弃或结束回合，不让无效数据继续污染 ASR。

TTS 返回压缩文件时，先解码并重采样到已声明的输出格式，再编码为独立 Opus packet。裸 PCM 需要单独约定采样率、声道、16-bit 有符号采样及字节序；WAV 还带文件头，不能直接视为裸 PCM。最后不足一包的 PCM 应按所选实现补零或使用支持的帧长度；发送只取编码器返回的有效字节。

设备缓冲有限，发送器应有有界队列、节奏或背压。背压表示下游来不及处理时，上游必须等待或停止生产，不能无限堆积。把一分钟音频瞬间发完可能导致缓存溢出与打断延迟。

## 6. 一轮语音、打断与故障恢复

控制消息通常包括 hello、listen、abort、stt 和 tts。listen 的 start/stop/detect 与 auto/manual/realtime 含义需结合固件实现；唤醒词音频可能早于 detect 事件到达，不应假设所有二进制都严格位于 start/stop 之间。

建议先实现有界的半双工回合：

```text
Listening → 语音分段完成 → ASR → LLM → TTS → Speaking → 下一轮
                         任意阶段失败 → 报告失败并清理回合
                         abort / 断开 → 取消当前回合及待发送结果
```

VAD 需要预留句首音频、最短语音长度、静音门槛及最长录音时间。能量门控只能识别音量变化，不能可靠区分人声与噪声；例如 700 ms 静音只是可调试起点，孩子较长停顿可能仍属于同一句。auto 模式可能由服务端判断句尾，不能无限等待客户端 stop。

上行解码后，ASR 输出转写；可用 `stt` JSON 展示。下行顺序是 `tts:start` → 可选 `sentence_start` → 二进制音频包 → `tts:stop`。stop 不能抢在排队音频前发送，也不能把“服务器发完”直接等同于“设备已经播完”。

接口可以按职责分开：

```csharp
public interface IAsrService
{
    // 本接口约定输入已统一成 16 kHz、单声道的有效 PCM 采样。
    Task<string> TranscribeAsync(ReadOnlyMemory<short> pcm, CancellationToken ct);
}
public interface ILlmService
{
    Task<string> ReplyAsync(string userText, CancellationToken ct);
}
public interface ITtsService
{
    // 本接口约定输出为 24 kHz、单声道 PCM；真实实现还需流式与队列上限。
    Task<short[]> SynthesizeAsync(string text, CancellationToken ct);
}
```

这些是边界示例，不是可直接工作的 Provider。ASR 文件上传通常需要有效 WAV/受支持容器；LLM 的系统消息、工具调用和流式事件按实际端点适配；TTS 的音色和输出格式也要单独配置。

接收循环必须持续处理 abort，不能同步等待整轮 LLM 完成。每个回合使用独立 CancellationTokenSource，关联连接取消令牌，并让 ASR/LLM/TTS 的网络请求传播取消。

仅取消令牌仍不够：为结果附上回合编号，取消后清空待发送队列，发送前再次确认结果属于当前回合，丢弃晚到的旧结果。已经到达设备的音频需要设备配合停止/清空，不能从服务端撤回。收到 abort 后等待真实 listen 状态，不无条件假设设备立即恢复监听。

应测试：用户连续两次打断、断网后重连、TTS 超时、客户端慢读、服务端晚返回、非法消息和长时间静音。连接状态、语音回合与后台任务分别清理，避免旧设备会话的结果发到新连接。

## 7. 完整后端仍需补齐的能力

探针成功后，逐项实现实际 Codec、VAD、ASR、LLM、TTS、串行发送、有界音频缓存、独立回合取消和设备身份。Memory、MCP、视觉与家庭设备控制按需求再加；设备 advertised feature 不代表探针已经实现对应服务器功能。

推荐把协议解析、音频处理、Provider 与回合状态拆成模块。每次新增模块，都用上一阶段已确认的输出作为输入对照；最终必须通过设备上真实的说话、播放和打断验收，不能以服务器日志替代听觉结果。
