# 正点原子“小智盒子”局域网部署与开发指南

更新日期：2026-09-08。适用于使用 `78/xiaozhi-esp32` 兼容固件、可配置自定义 OTA 的正点原子 ESP32-S3 设备。**先核对精确板型和固件版本；厂商定制固件的页面、按键和协议可能不同。**

目标是先用 `xinnan-tech/xiaozhi-esp32-server` 跑通语音，再按需要开发自己的后端。本文的终端命令用于 **Ubuntu 24.04 x86_64 + Bash**；Windows 11 / Docker Desktop 的差异见后文。自研部分已拆到[小智协议与 .NET 调试服务器](xiaozhi_protocol_dotnet_reference.md)。

本文核对了上游文档与协议源码，但没有完成正点原子实机、家庭网络和云服务的端到端验收。硬件能力、可用模型和容器兼容性，应以选定版本及实际测试为准。

## 1. 架构、术语与数据去向

```text
设备：本地唤醒 → 麦克风采集 → Opus 编码
                         ↓ Wi-Fi / WebSocket
家庭服务器：Opus 解码 → VAD → ASR → LLM → TTS → Opus 编码
                         ↓ WebSocket
设备：Opus 解码 → 扬声器播放
```

- **OTA** 原意为空中升级；这里还承担启动配置与服务发现，告诉设备连接哪个后端。修改 OTA 地址不等于必然升级固件。
- **WebSocket** 是双向连接，承载控制 JSON 和二进制语音。
- **Opus** 是音频编码；**Claude Opus** 是模型名称，两者无关。
- **VAD** 判断是否有人说话；**ASR** 把语音转成文字；**LLM** 生成答复；**TTS** 把答复转成语音。VAD 不等于完整的语义断句。
- **PCM** 是未压缩的音频采样；采样率、声道、位深与字节序都必须明确。

设备通常只连接语音后端，由后端选择模型和工具。设备端控制功能还取决于固件暴露的能力，服务端配置不能凭空增加硬件能力。[固件项目](https://github.com/78/xiaozhi-esp32)

**局域网部署不等于离线运行。** 云端 ASR 会收到语音，LLM 会收到转写与上下文，TTS 会收到待朗读文本。模型下载、镜像拉取和在线服务也需要网络。完全离线需要逐项替换 ASR、LLM、TTS，并验证所有外部依赖；这不在首次联调的验收范围内。

推荐按以下顺序推进：

| 阶段 | 目标 | 通过条件 |
| --- | --- | --- |
| 硬件基线 | 使用匹配板型的现有固件验证设备 | 麦克风、扬声器、唤醒、网络稳定 |
| xinnan 全模块 | 建立自建语音后端 | 设备完成一轮 ASR → LLM → TTS |
| 模型切换 | 配置第二个兼容 Provider | 不刷设备即可切换，流式与打断正常 |
| 自研后端 | 逐步实现协议和音频链路 | 分层测试通过，再与设备联调 |

官方服务可作为硬件基线，但不是必经步骤；也可用厂商自检、串口日志或已知可用的自建服务验证。官方服务不可达还可能是网络、账号或激活问题，不能直接判定硬件坏了。旧后端成功只提供对照，切换后网络与配置仍需重新检查。

## 2. 确认板型，保存恢复信息

记录商品完整型号、PCB 丝印、Board Name、固件版本、Flash/PSRAM 配置、Device ID 和当前 OTA 地址。需要查看 PCB 时按厂商说明操作，不必为抄录信息贸然拆机。

正点原子有 BOX、BOX0、BOX2 Wi-Fi/4G、BOX3 等变体。相同 ESP32-S3 芯片不代表屏幕、麦克风、引脚和音频 Codec 相同。只选对应板型的固件，刷写偏移与是否擦除按对应型号说明执行。

核对时 [Releases](https://github.com/78/xiaozhi-esp32/releases) 的 latest 指向 [v2.4.2](https://github.com/78/xiaozhi-esp32/releases/tag/v2.4.2)。它只说明检索当时的发布状态，不代表所有厂商固件都已兼容，也不代表后续仍是最新版本。

已有固件能配网并设置自定义 OTA 时，优先保留。需要刷写时，先准备匹配的恢复镜像并记录原配置。升级可能改变分区布局或清除配网数据。

厂商资料：[BOX3 产品介绍](https://wiki.alientek.com/docs/Boards/IoT/DNESP32S3B3/start-guide/dnesp32s3-box3-introduction/)、[BOX3 烧录说明](https://wiki.alientek.com/docs/Boards/IoT/DNESP32S3B3/start-guide/firmware-flash/)。其他型号应查自己的说明。

## 3. 网络地址与端口

以下地址全部是例子，请替换为真实 LAN 地址：

| 访问者 | 目标 | 示例 |
| --- | --- | --- |
| 设备 | 全模块 OTA | `http://192.168.50.10:8002/xiaozhi/ota/` |
| 设备 | 语音 WebSocket | `ws://192.168.50.10:8000/xiaozhi/v1/` |
| 浏览器 | 智控台 | `http://192.168.50.10:8002` |
| Python 语音容器 | manager-api | `http://xiaozhi-esp32-server-web:8002/xiaozhi` |
| 需要视觉功能的客户端 | Python HTTP 接口 | `http://192.168.50.10:8003`，具体路径见选定版本 |
| 设备 | 自研调试 OTA / WS | `http://192.168.50.10:5000/xiaozhi/ota/` / `ws://192.168.50.10:5000/xiaozhi/v1/` |

`127.0.0.1` 指访问者自己：在容器里指容器，在设备里指设备。Compose 服务名只能用于相应 Docker 网络内。绑定监听地址可以使用 `0.0.0.0`，但下发给设备的地址必须可路由，不能使用 `0.0.0.0`。

在路由器设置 DHCP 地址保留，使服务器 LAN IP 稳定。查看地址：

```bash
ip -br addr
hostname -I
```

从结果中选择家庭网卡地址，排除 Docker、VPN 和虚拟网卡。设备与服务器需要能互通；访客网、AP 隔离、VLAN 或主机防火墙都可能阻断。可以通过路由规则开放必要访问，不应为此盲目关闭整个网络的隔离。

服务器规格取决于模型是否本地运行及并发量。2 核/4 GB 可作为低并发、主要调用云 API 的起点；本地语音识别要额外预留内存与模型磁盘空间。这不是完整本地大模型的硬件保证。[上游全模块部署文档](https://github.com/xinnan-tech/xiaozhi-esp32-server/blob/main/docs/Deployment_all.md)注明其发行镜像自 0.8.2 起面向 x86，ARM64 应检查目标镜像架构或自行构建。

## 4. Ubuntu 安装 Docker

新安装按 [Docker Ubuntu 官方文档](https://docs.docker.com/engine/install/ubuntu/)进行。已有 Docker 或发行版容器包时，先按官方说明检查冲突，不要直接覆盖现有环境。

```bash
sudo apt update
sudo apt install -y ca-certificates curl git nano openssl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg   -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
```

用 `sudo nano /etc/apt/sources.list.d/docker.sources` 写入以下内容；此段专用于 Ubuntu 24.04 amd64：

```text
Types: deb
URIs: https://download.docker.com/linux/ubuntu
Suites: noble
Components: stable
Architectures: amd64
Signed-By: /etc/apt/keyrings/docker.asc
```

然后安装并验证：

```bash
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io   docker-buildx-plugin docker-compose-plugin
sudo systemctl enable --now docker
sudo docker run --rm hello-world
sudo docker compose version
```

下文统一保留 `sudo docker`。如果选择把用户加入 `docker` 组，该用户将获得近似 root 的主机控制能力，并非普通的“免密码便利”。

## 5. 准备 xinnan 全模块部署

完整部署包含 Python 语音服务、管理 API/网页、MySQL 和 Redis。只需要最小语音链路时也可使用简化部署，但配置来源和 OTA 端口不同，不要混用两套教程。

### 5.1 固定源码和镜像

```bash
mkdir -p ~/xiaozhi-lan
cd ~/xiaozhi-lan
git clone https://github.com/xinnan-tech/xiaozhi-esp32-server.git upstream
cd upstream
git rev-parse HEAD
```

记录输出的提交号；如有经过验证的 tag，先切到该 tag。把同一版本的配置放到独立运行目录，避免更新源码时覆盖运行配置：

```bash
cd ~/xiaozhi-lan
mkdir -p runtime/data runtime/models/SenseVoiceSmall
cp upstream/main/xiaozhi-server/docker-compose_all.yml runtime/compose.yaml
cp upstream/main/xiaozhi-server/config_from_api.yaml runtime/data/.config.yaml
cd runtime
```

以上复制步骤只用于全新目录。已有部署应先备份，并比较新旧配置，不能重新执行复制命令覆盖密钥。

检查 `compose.yaml` 中的镜像。上游可能使用 `server_latest`、`web_latest`、`mysql:latest` 等可变化标签；记录 Git 提交并不能固定这些镜像。选择兼容的发布标签，或首次验证后记录并固定镜像 digest（内容标识），再升级。不要单独替换 MySQL 大版本。

### 5.2 模型挂载文件必须真实存在

保留上游 SenseVoiceSmall 文件挂载时，先准备模型：

```bash
curl --fail --location --retry 3   'https://modelscope.cn/models/iic/SenseVoiceSmall/resolve/master/model.pt'   -o models/SenseVoiceSmall/model.pt
test -s models/SenseVoiceSmall/model.pt
ls -lh models/SenseVoiceSmall/model.pt
sha256sum models/SenseVoiceSmall/model.pt
```

这里的哈希用于记录本次文件；仅计算哈希不证明来源或完整性，应与发布方提供的校验信息核对。下载失败时不要用空文件替代。短格式 bind mount 的源路径不存在，Docker 可能创建目录，随后出现“文件/目录类型不匹配”。

换云 ASR 后能否去掉模型文件，取决于镜像启动逻辑、模型选择与挂载配置，应一并修改并验证，不能仅删除文件。

### 5.3 首次启动前修改密码与端口

编辑 `compose.yaml`，将管理端的 `SPRING_DATASOURCE_DRUID_PASSWORD` 与数据库的 `MYSQL_ROOT_PASSWORD` 都改为同一个强随机值。可以把两处值改成 `${MYSQL_ROOT_PASSWORD:?set MYSQL_ROOT_PASSWORD}`，然后在运行目录的 `.env` 中设置变量。

`.env` 示例结构如下，运行前必须替换占位符：

```dotenv
MYSQL_ROOT_PASSWORD=REPLACE_WITH_RANDOM_HEX_PASSWORD
```

可用 `openssl rand -hex 32` 生成密码，经编辑器保存。不要复用公开示例密码。设置文件权限：

```bash
chmod 600 .env data/.config.yaml
```

**已有数据库时，修改环境变量不会自动修改数据库内的密码。** 需要按当前 MySQL 版本修改数据库账号，再同步管理端配置；先备份并安排停机，不要用删除数据目录来“重置密码”。

将主机端口绑定到实际 LAN IP，例如：

```yaml
# 分别修改语音服务和管理服务已有的 ports，不要新增重复映射。
# 语音服务：
ports:
  - "192.168.50.10:8000:8000"
# 管理服务：
# ports:
#   - "192.168.50.10:8002:8002"
```

只做语音时可先不发布 8003。MySQL 的 3306 和 Redis 的 6379 保留容器网络访问，不增加宿主机发布端口。上游还有 `seccomp:unconfined` 等容器安全配置，应认识到它放宽了隔离，并在所选镜像兼容性测试后决定是否保留。[上游 Compose](https://github.com/xinnan-tech/xiaozhi-esp32-server/blob/main/main/xiaozhi-server/docker-compose_all.yml)

`sudo docker compose config` 会展开密钥，检查时使用：

```bash
sudo docker compose -f compose.yaml config --quiet
sudo docker compose -f compose.yaml config --services
```

## 6. 启动、注册管理员并配置模块

```bash
cd ~/xiaozhi-lan/runtime
sudo docker compose -f compose.yaml up -d
sudo docker compose -f compose.yaml ps
sudo docker compose -f compose.yaml logs --tail 100 xiaozhi-esp32-server-web
```

容器在运行不等于应用就绪。首次启动涉及数据库初始化，需看管理服务启动日志；语音服务此时因尚未配置 secret 而报错，不必立即重装。

打开 `http://192.168.50.10:8002`。全新数据库中的首个注册用户具有管理员身份，应在受控网络中立即完成注册。账号和 API Key 都属于自建智控台，与官方小智账号不同。

### 6.1 让语音服务读取管理端配置

在参数管理中取得 `server.secret`，把它填入 `data/.config.yaml`：

```yaml
manager-api:
  url: http://xiaozhi-esp32-server-web:8002/xiaozhi
  secret: "REPLACE_WITH_MANAGER_SERVER_SECRET"
```

**四类凭据不要混用：** 数据库密码用于管理服务访问 MySQL；`server.secret` 用于语音服务访问管理 API；设备令牌用于设备连接；模型 API Key 用于服务器调用云模型。它们不应使用同一个值。

### 6.2 配齐语音链路，再选择智能体使用的配置

在模型管理中设置可用的 ASR、LLM、TTS，并检查 VAD 等模块的默认选择。模型条目已保存，不代表设备对应智能体已经选用它；还要进入智能体配置选择对应模型/声音/提示词。

第一次只接入一个 LLM，并单独验证 ASR 与 TTS。项目默认值可能需要下载本地模型或访问外部服务，不能只填一个 LLM Key 就认为其余模块必然可用。

### 6.3 设置下发给设备的地址

在参数管理中设置：

```text
server.websocket = ws://192.168.50.10:8000/xiaozhi/v1/
server.ota       = http://192.168.50.10:8002/xiaozhi/ota/
```

修改完成后：

```bash
sudo docker compose -f compose.yaml restart xiaozhi-esp32-server
sudo docker compose -f compose.yaml logs --tail 100 xiaozhi-esp32-server
```

如果修改的是 Compose 环境变量或端口，应重新执行 `up -d` 使容器重建，单纯 `restart` 不会应用这些更改。具体配置项与账号流程以[同版本部署文档](https://github.com/xinnan-tech/xiaozhi-esp32-server/blob/main/docs/Deployment_all.md)为准。

## 7. OTA、设备绑定和真正的验收

### 7.1 GET 可达与 POST 配置是两项检查

先从同一 LAN 的另一台电脑测试：

```bash
curl --max-time 10 -i http://192.168.50.10:8002/xiaozhi/ota/
```

GET 可能返回提示文字、页面或其他响应，取决于管理端版本。它只能帮助检查路径，不证明设备 POST 会得到正确配置。

全模块 8002 的 OTA 由 manager-api 处理；Python 的 `core/api/ota_handler.py` 属于另一个 HTTP 实现，不能用它的 GET 响应推断全模块行为。设备会携带身份头并发送固件信息，POST 的简化排障请求如下：

```bash
read -rp 'Device-Id（使用你自己设备的 MAC）: ' DEVICE_ID
read -rp 'Client-Id（使用设备日志中的值）: ' CLIENT_ID
read -rp '已安装固件版本: ' FW_VERSION
export FW_VERSION
python3 -c 'import json,os; print(json.dumps({"application":{"version":os.environ["FW_VERSION"]}}))'   | curl --fail-with-body --max-time 15       -H "Device-Id: $DEVICE_ID" -H "Client-Id: $CLIENT_ID"       -H 'Content-Type: application/json'       --data-binary @- http://192.168.50.10:8002/xiaozhi/ota/
```

这是**最小探测请求**；若管理端还要求板型、激活或设备上报字段，需按设备真实请求补齐，不能据此认定服务故障。使用真实设备身份可能创建待绑定记录；响应可能含令牌，不要把完整内容贴到公开日志。

检查响应的激活要求、WebSocket 地址及协议配置。真实设备日志确认 POST 成功后，再看 WebSocket 是否建立。固件的请求/解析行为见 [ota.cc](https://github.com/78/xiaozhi-esp32/blob/main/main/ota.cc)。

### 7.2 修改设备 OTA

按对应板型说明进入 Wi-Fi 配网模式，连接设备热点，在配网页面的“高级选项”填写自建 OTA 地址，保存 Wi-Fi 设置并重启。上游给出了[预编译固件配置方式](https://github.com/xinnan-tech/xiaozhi-esp32-server/blob/main/docs/firmware-setting.md)，厂商固件不一定提供相同页面。

没有高级选项时，先核对厂商配置方式，再考虑匹配板型的预编译固件或自行编译。编译依赖、Flash 分区、唤醒模型与烧写参数，应读取**目标 tag** 的 README 和板型文档；不要把 main 分支的 ESP-IDF 要求套到旧版固件上。[固件编译与板型说明](https://github.com/78/xiaozhi-esp32/blob/main/docs/custom-board_zh.md)

### 7.3 在自建智控台绑定

设备若显示或播报绑定码，在 `http://192.168.50.10:8002` 的设备管理中绑定至对应智能体。先确认它连的是自建 OTA，而不是官方服务器。

正常语音验收逐层记录：

1. 实际设备完成 OTA 配置/激活。
2. 后端收到设备 WebSocket 连接与 hello。
3. 后端收到二进制音频，ASR 转写与讲话一致。
4. 选定 LLM 返回可见答复。
5. TTS 成功产生音频，设备实际听到完整答复。
6. 连续对话、断网重连和打断都能恢复，旧回复不会串入下一轮。

只看到“设备连接”或几个二进制包，不算完整跑通。先由成人使用非敏感测试内容联调，再考虑家庭持续使用。

## 8. DeepSeek、OpenAI 与 Claude 接入

“OpenAI-compatible”通常指兼容某组请求/响应格式，不代表每个模型、流式事件或工具调用都兼容。先确认所选 Provider 使用 Chat Completions、Responses 还是原生 Messages，之后再选支持该端点的模型。

| 服务 | 地址/协议 | 配置重点 |
| --- | --- | --- |
| DeepSeek | `https://api.deepseek.com`，Chat Completions | model ID、鉴权、流式输出 |
| OpenAI | `https://api.openai.com/v1` | 模型必须支持 Provider 实际调用的端点 |
| Anthropic | 原生 `/v1/messages`，或通过协议网关 | 原生 Provider 支持情况、消息与工具格式转换 |

DeepSeek 当前文档列有 `deepseek-v4-flash` 等模型；把它当作核对时的例子，不作为永不变化的依赖。[DeepSeek API](https://api-docs.deepseek.com/zh-cn/)

以下测试用环境变量保存临时 Key，避免把真实 Key 写成命令历史中的字面量。请求会产生正常 API 用量：

```bash
read -rsp 'DeepSeek API Key: ' DEEPSEEK_API_KEY
echo
export DEEPSEEK_API_KEY
read -rp '当前账号可用的模型 ID: ' DEEPSEEK_MODEL
export DEEPSEEK_MODEL
python3 - <<'PY'
import json, os, urllib.request
body = json.dumps({
    "model": os.environ["DEEPSEEK_MODEL"],
    "messages": [{"role": "user", "content": "请只回答：测试成功"}]
}).encode()
request = urllib.request.Request(
    "https://api.deepseek.com/chat/completions", data=body,
    headers={"Authorization": "Bearer " + os.environ["DEEPSEEK_API_KEY"],
             "Content-Type": "application/json"})
with urllib.request.urlopen(request, timeout=60) as response:
    data = json.load(response)
print(data["choices"][0]["message"].get("content"))
PY
unset DEEPSEEK_API_KEY DEEPSEEK_MODEL
```

测试成功后，再把相同地址、模型 ID 和 Key 配入智控台，并用语音实测流式答复。Shell 环境变量也不是加密保险箱，不要使用调试跟踪输出凭据。

OpenAI ASR 使用音频转写端点，支持的文件类型与模型能力按[转写文档](https://developers.openai.com/api/docs/guides/speech-to-text)核对；TTS 格式按[语音生成文档](https://developers.openai.com/api/docs/guides/text-to-speech)核对。不要把 Responses-only 模型直接填入只实现 Chat Completions 的适配器。

### 可选：用 LiteLLM 转接 Claude

先检查所选 xinnan 版本的 Provider 列表和实现；README 没列出某个 Provider，不足以证明它不存在。若无合适的原生 Anthropic 适配器，可用 LiteLLM 的 OpenAI-compatible 入口转换协议。

在同一运行目录创建 `litellm_config.yaml`，替换模型占位符：

```yaml
model_list:
  - model_name: claude-voice
    litellm_params:
      model: anthropic/YOUR_CLAUDE_MODEL_ID
      api_key: os.environ/ANTHROPIC_API_KEY
general_settings:
  master_key: os.environ/LITELLM_MASTER_KEY
```

在 `compose.yaml` 的 `services` 下新增服务，替换镜像为实际核验过的版本或 digest：

```yaml
litellm:
  image: ghcr.io/berriai/litellm:REPLACE_WITH_VERIFIED_TAG
  command: ["--config", "/app/config.yaml", "--port", "4000"]
  restart: unless-stopped
  volumes:
    - ./litellm_config.yaml:/app/config.yaml:ro
  environment:
    ANTHROPIC_API_KEY: ${ANTHROPIC_API_KEY:?set ANTHROPIC_API_KEY}
    LITELLM_MASTER_KEY: ${LITELLM_MASTER_KEY:?set LITELLM_MASTER_KEY}
  ports:
    - "127.0.0.1:4000:4000"
```

在受保护的 `.env` 中补上两个独立密钥；网关主密钥按 LiteLLM 要求生成，不使用公开示例值。不要把真实凭据加入 Git。

```bash
sudo docker compose -f compose.yaml config --quiet
sudo docker compose -f compose.yaml up -d litellm
sudo docker compose -f compose.yaml logs --tail 100 litellm
```

同一 Compose 网络中的 xinnan 使用 `http://litellm:4000/v1`、模型别名 `claude-voice` 和网关主密钥。宿主机测试使用 `http://127.0.0.1:4000/v1`；容器内的 localhost 仍不是宿主机。

如选定 LiteLLM 版本的主密钥管理要求数据库或其他组件，应按该版本[部署文档](https://docs.litellm.ai/docs/proxy/docker_quick_start)补齐，不能通过取消鉴权绕过。转换后的流式分块、工具参数、错误和取消行为都要实测；API Key 和模型订阅/账号权限也要分别确认。

## 9. 防火墙与 Windows 差异

Docker 发布端口的流量可能绕过普通 UFW INPUT 规则，所以“执行 ufw allow/deny 后就是 LAN-only”不成立。[Docker 与 UFW](https://docs.docker.com/engine/network/packet-filtering-firewalls/)

Linux 上应同时检查主机绑定 IP、路由器端口转发、IPv6 暴露和 Docker 对应防火墙后端的转发规则。绑定 LAN IP 缩小入口范围，但不是来源地址白名单；需要限制来源时按所用 iptables/nftables 后端配置并从允许与拒绝的网络分别测试。

只有直接运行的 .NET 调试服务等普通主机端口，可按实际网段配置 UFW，例如：

```bash
sudo ufw status
sudo ufw allow from 192.168.50.0/24 to any port 5000 proto tcp
```

不要在远程 SSH 会话里未经检查就启用防火墙；先保证当前 SSH 端口仍可用。公网访问涉及 TLS、身份、限流与网络边界，反向代理本身不等于安全评估，本指南不包含公网生产部署。

Windows 11 使用 [Docker Desktop 的 Linux containers](https://docs.docker.com/desktop/setup/install/windows-install/)。Ubuntu 命令需在相应 WSL/Linux 环境执行；设备应访问 Windows 主机 LAN 地址及发布端口，不能使用 WSL 内部临时 IP。检查 Windows 防火墙、私有网络配置和主机睡眠。Docker Desktop 开机启动通常依赖用户登录，不应直接当作无人值守服务器的自启保证。

## 10. 自研后端的下一步

完整 .NET 协议探针、启动方式、音频参数、状态机与打断设计见[配套文档](xiaozhi_protocol_dotnet_reference.md)。

按以下顺序验收：HTTP 健康检查 → OTA POST → WebSocket 鉴权与 hello → Opus 解码 → 分段 ASR → LLM → TTS 转码 → 设备播放 → 打断和重连。

自研探针使用 5000 端口，不实现真实 ASR/LLM/TTS。切回已知可用的后端有利于对照，但每次改变 OTA 后应检查设备实际 POST 响应和连接日志；固件可能保留 NVS 配置，不能只凭配网页面填写成功判断切换完成。

## 11. 家庭与儿童使用

提示词可以约束表达，但不能单独保证儿童内容安全。先由成人测试年龄适配、错误回答、意外唤醒、断网提示和音量，再逐步开放使用。

可作为起点的提示词：

```text
你是 AI 语音助手，不是真人。用适合孩子年龄的语言回答。
先给简短解释，再按追问展开；不确定时明确说明。
不索取姓名、住址、学校、电话等身份信息。
不提供可直接执行的危险步骤；紧急或受伤情况提醒联系身边可信成年人。
不引导购买或充值，不擅自控制家庭设备。
```

可把第一轮回答控制在约 80～180 个中文字符作为体验试验，但不在句中硬切，也不能截掉必要提醒。费用、时段、设备控制权限和最大播放时长由服务端配置保证。

原始录音默认不持久化的建议，需要落实到 ASR/TTS 临时文件、容器卷、转写日志、数据库历史、备份和云服务保留策略中。排障确需录音时使用非敏感内容，限定用途、数量、期限和访问权限。长期记忆须可查看、关闭和清理，不以“放在家里”推断数据没有离开家庭。

## 12. 故障定位

| 现象 | 优先检查 | 不能据此推出什么 |
| --- | --- | --- |
| 手机能打开 8002，盒子不能 | Wi-Fi 隔离、设备 LAN 路由、真实 OTA 请求 | 手机可达不证明设备可达 |
| OTA GET 正常，设备未连接 | POST 状态、激活要求、下发 WS URL | GET 正常不证明设备配置正确 |
| WS 建立但 hello 超时 | JSON 类型、协议版本、参数、服务器错误 | HTTP 101 不代表小智握手完成 |
| 收到音频，无 ASR | v1/v2/v3 封包、解码结果、VAD、ASR 选择 | 二进制包存在不证明录到了有效人声 |
| ASR 正常，LLM 无回答 | API 权限、余额、网络、模型端点、流式解析 | 换 Key 未必能解决协议不兼容 |
| 有文字，无声音 | TTS 成功、容器格式、重采样、Opus、下发时序 | TTS 返回文件不证明 ESP32 能播放 |
| 打断后仍播旧话 | 取消传播、发送队列、回合编号、设备缓存 | 取消 LLM 不会自动清空已经发出的音频 |
| 自己的声音被再次识别 | 回声、监听模式、AEC 与硬件条件 | 全双工不是只开一个配置即可实现 |

AEC 是回声消除，需要设备或服务端获取合适的参考信号。首版可采用半双工：播放时不把回声送入本轮 ASR，但保持接收控制消息以支持中断；其具体唤醒表现要按固件验收。

## 13. 备份、升级与常用命令

本指南复制出来的运行目录与源码分离。上游 Compose 的持久化路径包括 `data/`、`mysql/data/` 和 `uploadfile/`，实际以自己选定配置为准；不是 `data/mysql`。

简单的**停机备份**可对运行目录整体归档。先确认磁盘空间和目录，再执行：

```bash
cd ~/xiaozhi-lan/runtime
sudo docker compose -f compose.yaml stop
mkdir -p ../backups
chmod 700 ../backups
sudo tar -czf "../backups/runtime-$(date +%Y%m%d-%H%M%S).tar.gz"   -C "$PWD" .
sudo docker compose -f compose.yaml start
```

备份包含数据库、Key 与会话数据，须保护和按需加密。备份期间服务停止；若归档失败，先记录错误再恢复服务。不要直接复制运行中的 MySQL 数据目录作为一致性备份。在线备份应采用所选 MySQL 版本支持的工具与一致性策略。

恢复时使用独立目录和兼容的数据库镜像，先验证恢复副本可启动、用户/设备配置和数据完整，再考虑切换。不要对现有数据目录直接覆盖，也不要用 `down -v` 充当重启。

常用命令（在运行目录执行）：

```bash
sudo docker compose -f compose.yaml ps
sudo docker compose -f compose.yaml logs --tail 100 xiaozhi-esp32-server
sudo docker compose -f compose.yaml logs --tail 100 xiaozhi-esp32-server-web
sudo docker compose -f compose.yaml restart xiaozhi-esp32-server
sudo docker compose -f compose.yaml stop
sudo docker compose -f compose.yaml start
```

升级先备份，核对固件、板型、协议、服务端源码、镜像与模型 ID，再小范围验证。记录已验证镜像：

```bash
sudo docker compose -f compose.yaml images
sudo docker image inspect --format '{{json .RepoDigests}}' YOUR_IMAGE_REFERENCE
```

将真实 digest 填回运行 Compose；记录原版本和数据库迁移要求。服务重启、机器重启后的恢复，以及关闭 Docker Desktop/退出登录后的行为，应分别验收。

## 资料核对入口

- 固件：[主项目](https://github.com/78/xiaozhi-esp32)、[发布版本](https://github.com/78/xiaozhi-esp32/releases)、[OTA_URL 定义](https://github.com/78/xiaozhi-esp32/blob/main/main/Kconfig.projbuild)。
- 协议：[WebSocket 说明](https://github.com/78/xiaozhi-esp32/blob/main/docs/websocket_zh.md)、[客户端实现](https://github.com/78/xiaozhi-esp32/blob/main/main/protocols/websocket_protocol.cc)、[OTA 实现](https://github.com/78/xiaozhi-esp32/blob/main/main/ota.cc)。
- 后端：[xinnan 项目](https://github.com/xinnan-tech/xiaozhi-esp32-server)、[全模块部署](https://github.com/xinnan-tech/xiaozhi-esp32-server/blob/main/docs/Deployment_all.md)、[Compose](https://github.com/xinnan-tech/xiaozhi-esp32-server/blob/main/main/xiaozhi-server/docker-compose_all.yml)、[配置模板](https://github.com/xinnan-tech/xiaozhi-esp32-server/blob/main/main/xiaozhi-server/config_from_api.yaml)。
- 接口：[DeepSeek](https://api-docs.deepseek.com/zh-cn/)、[OpenAI 模型目录](https://developers.openai.com/api/docs/models)、[Anthropic Messages](https://platform.claude.com/docs/en/api/messages)、[LiteLLM](https://docs.litellm.ai/docs/proxy/docker_quick_start)。

这些链接跟随上游更新。实际复现时应记录所用提交/tag 和镜像 digest；日期标题本身不是版本锁定。
