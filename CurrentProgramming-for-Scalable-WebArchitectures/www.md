# 万维网

互联网和 WWW 已成为现代社会和经济的支柱。WWW 是互联网中一个由相互连接的超媒体资源组成的分布式系统。它以一系列不同的相关技术为基础。本节将简要介绍最重要的协议和格式，包括统一资源标识符（URI）、超文本传输协议(HTTP) 和 HTML。

## 统一资源标识符

URI 目前由 (RFC 3986) [^1] 规定，是用于引用资源的字符串。就分布式系统而言，URI 有三种不同的作用--命名、寻址和识别资源。我们将重点讨论在 WWW 中识别资源的 URI，尽管它们也可用于其他抽象或物理实体。根据规范，URI 由五个部分组成：`schema`、`authority`、`path`、`query` 和 `fragment`。不过，只有 `schema` 和 `path` 是必须的，其他部分都是可选的。`scheme` 声明 URI 的类型，因此决定了 URI 其他部分的含义。如果使用，`authority` 指向引用资源的负责机构。在使用 http 作为方案的情况下，这一部分成为必填部分，包含托管该资源的网络服务器的主机。可选择包含端口号（http 默认为 80）和验证数据（已废弃）。必选路径部分用于在 `schema`（和 `authority`）范围内对资源进行寻址。它通常采用分层结构。可选的 `query` 部分提供非分层数据，作为资源标识符的一部分。`fragment` 可用于指向资源中的某一部分。下面的示例改编自 RFC 3986，使用了所有五个部分：

```
   http://example.com:8080/over/there?search=test#first
    \_/   \______________/\_________/ \_________/ \__/
     |           |            |            |        |
   scheme     authority      path        query   fragment
```

它标识了一个网络资源（`scheme` 为 http），该资源托管在 `example.com`（端口为 `8080`）上。资源 `path` 为 /over/there，`query` 组件包含 key/value 对 `search=test`。此外，还引用了资源的 `first` 片段。

##  超文本传输协议

HTTP 是一种应用级协议，是 WWW 在 TCP/IP 基础上的通信基础。根据 RFC 2616 [^2] 的定义，HTTP 是一种无状态协议，符合客户机/服务器架构和请求/响应通信模型。服务器托管由 URI 标识的资源，客户端可以访问这些资源。客户端向服务器发出 HTTP 请求，服务器则提供 HTTP 响应。该通信模型将可能的信息模式限制为总是由客户端发起的单个请求/响应循环。除了客户端和服务器，HTTP 还描述了可选的中介，即所谓的代理。这些组件提供缓存或过滤等附加功能。代理结合了客户端和服务器的特点，因此在通信方面对客户端和服务器来说通常是透明的。

HTTP 请求和响应具有共同的结构。两者都以请求行开始，分别是状态行。接下来的部分包含一组标题行，分别包含请求、响应和实体的相关信息。

实体是 HTTP 消息的可选主体，包含有效载荷，如资源的表示。HTTP 报文的前两个部分是基于文本的，而实体则可以是任意一组字节。HTTP 请求行包含一个请求 URI 和一个方法。如表 2.1 所示，不同的 HTTP 方法在应用于资源时具有不同的语义。

| 方法      | 使用                                                         | 安全 | 幂等 | 可缓存 |
| :-------- | :----------------------------------------------------------- | :--- | :--- | :----- |
| `GET`     | 这是 WWW 最常用的方法。它用于获取资源表示                    | ✓    | ✓    | (✓)    |
| `HEAD`    | 从本质上讲，这种方法与 `GET` 相同，只是在响应中省略了实体。  | ✓    | ✓    | (✓)    |
| `PUT`     | 这种方法用于创建或更新具有新表述的现有资源。                 |      | ✓    |        |
| `DELETE`  | 可使用 `DELETE` 删除现有资源                                 |      | ✓    |        |
| `POST`    | `POST` 用于创建新资源。由于缺乏幂等性和安全性，它也经常被用来触发任意操作。 |      |      |        |
| `OPTIONS` | 方法提供有关资源和可用表示的元数据。                         | ✓    | ✓    |        |

表 2.1：官方 HTTP 1.1 方法表。根据 RFC 2616，当使用该方法的请求不会改变服务器上的任何状态时，该方法就是安全的。如果一个请求的多次派发比单次派发产生相同的副作用，那么该请求语义就被称为幂等请求（idempotent）。如果请求方法提供了缓存功能，客户端就可以根据 HTTP 缓存语义存储响应。

在随后的 HTTP 响应中，服务器会使用预定义的状态代码告知客户端请求的结果。状态代码的类别见表 2.2。

| **Range** | **Status Type** | **Usage**                                                    | **Example Code**          |
| :-------- | :-------------- | :----------------------------------------------------------- | :------------------------ |
| `1xx`     | 信息            | Preliminary response codes                                   | `100 Continue`            |
| `2xx`     | 成功            | The request has been successfully processed.                 | `200 OK`                  |
| `3xx`     | 转发            | The client must dispatch additional requests to complete the request. | `303 See Other`           |
| `4xx`     | 客户端异常      | Result of an erroneous request caused by the client.         | `404 Not Found`           |
| `5xx`     | 服务器异常      | A server-side error occured (not caused by this request being invalid). | `503 Service Unavailable` |

表 2.2：HTTP 响应代码范围表。第一位数字表示状态类型，最后两位数字表示确切的响应代码。

下面列出了一个简单的请求/响应交换示例：

```
GET /html/rfc1945 HTTP/1.1
Host: tools.ietf.org
User-Agent: Mozilla/5.0 (Ubuntu; X11; Linux x86_64; rv:9.0.1) Gecko/20100101 Firefox/9.0.1
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8
Accept-Language: de-de,de;q=0.8,en-us;q=0.5,en;q=0.3
Accept-Encoding: gzip, deflate
Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.7
Connection: keep-alive
If-Modified-Since: Sun, 13 Nov 2011 21:13:51 GMT
If-None-Match: "182a7f0-2aac0-4b1a43b17a1c0;4b6bc4bba3192"
Cache-Control: max-age=0

HTTP/1.1 304 Not Modified
Date: Tue, 17 Jan 2012 17:02:44 GMT
Server: Apache/2.2.21 (Debian)
Connection: Keep-Alive
Keep-Alive: timeout=5, max=99
Etag: "182a7f0-2aac0-4b1a43b17a1c0;4b6bc4bba3192"
Content-Location: rfc1945.html
Vary: negotiate,Accept-Encoding
```

现在，我们将仔细研究 HTTP 的两个高级功能，它们对我们后面的讨论很有意义，即连接处理和分块编码。

### HTTP 连接处理

如前所述，HTTP 使用 TCP/IP 作为底层传输协议。现在我们将研究 HTTP 请求中 TCP 套接字的具体用法。以前的 HTTP 规范建议为每个请求/响应周期建立单独的套接字连接。为每个请求增加建立 TCP 连接的开销会导致性能低下，并且失去现有连接的可重用性。非标准的连接：Keep-Alive 标头是一种临时的解决方法，但目前的 HTTP 1.1 规范已经详细解决了这个问题。HTTP 1.1 默认引入了持久连接。也就是说，HTTP 请求的底层 TCP 连接会在后续 HTTP 请求中重复使用。请求流水线（Request pipelining）进一步提高了持久连接的吞吐量，因为它允许分派多个请求，而无需等待之前请求的响应。然后，服务器以相同的顺序响应所有传入请求。这两种机制都提高了网络应用程序的性能，减少了延迟问题。但是，正如我们在[第 4 章](webserver-architectures-for-highconcurrency.md)中看到的那样，多个开放连接的管理和流水线请求的处理给网络服务器带来了新的挑战。

### HTTP 块传输转码

HTTP 报文必须包含其实体的长度（如果有）。在 HTTP 1.1 中，这是确定报文总长度和检测持久连接的下一条报文所必需的。有时，实体的确切长度无法事先确定。这对于动态生成的内容或即时压缩的实体尤为重要。因此，HTTP 提供了其他传输编码。使用分块传输编码时，客户端或服务器会按顺序串流实体的分块。下一个分块的长度会被添加到实际分块的前面。分块长度为 0 表示实体结束。这种机制允许传输任意长度的生成实体。

### Web Formats

HTTP 并不限制用于实体的文档格式。然而，WWW 的核心理念是基于超媒体，因此大多数格式都支持超媒体。最重要的一种格式是 HTML 及其后代。

#### 超文本标记语言

HTML [^3] 是一种源于 SGML [^4] 并受 XML [^5] 影响的标记语言。HTML 提供了一系列元素、属性和规则，用于以文本方式描述网页。浏览器解析 HTML 文档，利用其结构语义为人类呈现可视化表示。HTML 通过超链接和交互式表格支持超媒体。此外，HTML 文档中还可以使用图像等媒体对象。HTML 文档的外观和样式可通过 CSS 进行定制。为了实现更动态的用户界面和交互行为，可以在 HTML 文档中嵌入脚本语言代码，如 JavaScript。例如，它可用于在后台以编程方式加载新内容，而无需完全重新加载页面。这种技术也被称为 AJAX，是实现更灵敏的用户界面的关键之一。因此，它能使网络应用程序的界面与传统桌面应用程序的界面相似。

#### HTML5

HTML 标准的第五次修订[^5]引入了多项标记改进（如语义标记）和更好的多媒体内容支持，但最值得注意的是引入了一套丰富的新应用程序接口。这些应用程序接口涉及各种功能，包括客户端存储、离线支持、用于上下文感知的设备传感器以及改进的客户端性能。Web Sockets API [^6]通过基于 WebSocket 协议的低延迟、双向、全双工套接字对传统 HTTP 请求/响应通信模式进行了补充。这对于实时网络应用程序来说尤其有趣。

#### 通用格式

除了专有和定制格式外，网络服务还经常使用 XML 和 JSON 等通用结构化格式 。XML 是一种全面的标记语言，提供丰富的相关技术，如验证、转换或查询。JSON 是轻量级的替代格式之一，只专注于结构化数据的简洁表示。虽然人们对网络服务和信息的轻量级格式越来越感兴趣，但 XML 仍然提供了最广泛的工具集和支持。

[^1]: [ietf.org/rfc/rfc3986.txt](https://www.ietf.org/rfc/rfc3986.txt)
[^2]: [ietf.org/rfc/rfc2616.txt](https://www.ietf.org/rfc/rfc2616.txt)
[^3]: http://www.w3.org/TR/1999/REC-html401-19991224
[^4]: [https://en.wikipedia.org/wiki/Standard_Generalized_Markup_Language]()
[^5]: [HTML 5 (w3.org)](https://www.w3.org/TR/2009/WD-html5-20090825/)
[^6]: http://www.w3.org/TR/2009/WD-websockets-20091222/
[^7]: 