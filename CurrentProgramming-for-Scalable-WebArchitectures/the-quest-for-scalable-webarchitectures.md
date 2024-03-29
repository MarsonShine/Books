# 寻求可伸缩的网络架构

本章将对提供网络应用程序的架构进行深入探讨。在研究最新的基于云的架构之前，我们将概述为动态网络内容提供服务而出现的几种技术。然后，我们将了解这些技术是如何集成到常见架构中的。之后，我们将讨论负载均衡，这是网络架构中一种成熟而重要的可伸缩机制。然后，我们将了解流行的云供应商提供的基础设施和服务。

在此基础上，我们将提出自己的可伸缩网络应用通用架构模型，作为本章的重要贡献。该模型是我们进一步考虑并发问题的基础。将不同的架构组件分离开来，不仅能为每个组件提供更清晰的可伸缩性路径，还能帮助我们推理特定的可伸缩性问题。它还有助于我们推理不同组件固有的特定并发挑战。

## 传统网络架构

尽管当前的网络架构与最初的Web 服务器所使用的应用协议几乎相同，但其内部结构却发生了很大变化。特别是动态网络内容的兴起对架构概念产生了合理的影响。随着网络的发展，出现了分层架构，将架构组件的不同责任分开。不断发展的架构还要求有扩展网络应用程序的方法，而负载均衡已成为一种合适的机制。现在，我们通过对不同技术的概述，来了解将动态内容集成到网络应用程序以及对服务器造成的影响。然后，我们将研究分层架构和负载均衡的概念。

### 动态网页内容的服务器端技术

90 年代初，第一批Web 服务器是仅通过 HTTP 访问静态文件的Web 服务器。在很短的时间内，对更多动态内容的需求不断增加。例如，人们要求用可变的服务器状态来丰富静态 HTML 文件、即时生成完整的 HTML 文件或动态响应表单提交，以改善用户体验。其中一种方法是修改Web 服务器，将动态内容创建的机制深入到 Web 服务器代码中。当然，这种方法很繁琐，并且混淆了 Web 服务器的内部机制和 Web 应用程序的编程。因此，需要更通用的解决方案，并很快出现了一些解决方案，例如通用网关接口（CGI）。

#### CGI

[通用网关接口（Common Gateway Interface，CGI）]()[Rob04] 是一种标准化接口，用于将网络请求委托给处理请求并生成响应的外部应用程序。当 Web 服务器和外部应用程序都支持该接口时，就可以使用 CGI。实际上，这些应用程序大多是用脚本语言实现的。对于映射到 CGI 应用程序的 URI 的每个传入请求，Web 服务器都会生成一个新进程。该进程执行 CGI 应用程序，并通过环境变量提供特定变量，如请求头和服务器变量。CGI 进程可通过 `STDIN` 读取请求实体，并将生成的响应（包括标题和响应实体）写入 `STDOUT`。生成响应后，CGI 进程终止。只使用外部进程和通过环境变量进行通信，`STDIN` 和 `STDOUT` 提供了一个简单的接口。只要支持这些基本机制，任何应用程序都能处理和生成网络内容。特别是 Perl 和其他脚本语言（如 PHP），后来被广泛用于构建基于 CGI 的网络应用程序。这些语言可以直接嵌入到现有的 HTML 标记中，并在每次请求时执行添加的代码。另外，它们还提供了生成 HTML 文档的方法，通常使用模板引擎。

然而，CGI 模式有几个问题，特别是可伸缩性和性能。如前所述，进程是任务的重量级结构。创建进程需要合理的开销和资源。因此，将每个动态请求映射到一个要生成的新进程是一个非常昂贵的操作。它不仅会因进程的创建而增加延迟，还会因生成开销而浪费服务器资源，并限制可处理的并发请求数量。鉴于大多数 CGI 应用程序都是脚本文件，每次执行时都必须对其进行解释，因此平均延迟会进一步恶化。通过 `STDIN/STDOUT` 进行通信会带来另一个严重问题。进程间的这种通信方式限制了组件的可分配性，因为两个进程必须位于同一台机器上。在使用 CGI 时，无法以分布式方式将两个组件解耦。

#### FastCGI

FastCGI 通过指定通过本地套接字或 TCP 连接使用的接口协议，缓解了 CGI 的主要问题。因此，网络服务器和生成响应的应用程序是分离的，可以位于不同的机器上。由于对并发模型没有限制，后端应用程序可以作为具有内部多线程的长期运行进程来实现。实际上，按请求创建进程的开销已不复存在，并发性可以大大提高。

#### Web 服务器拓展模块

CGI 的另一种替代方法是服务器扩展模块。Web 服务器提供内部模块接口，允许插入模块，而不是外部接口。这些模块经常用于在服务器上下文中嵌入脚本语言解释器。特别是，服务器的并发模型经常应用于脚本的执行。因此，请求处理和动态内容生成可在同一线程或进程中执行。与服务器的紧密集成可以提高性能，通常比基于 CGI 的脚本速度更快。不过，这种模式也会阻碍松散耦合，使 Web 服务器和后端应用程序的分离变得更加困难。

#### Web 应用程序容器

最初的 CGI 模式并不适合 Java 等语言。基于每个请求的专用进程模型和 JVM 的启动时间使其完全无法使用。因此，出现了一些替代方法，如 Java Servlet 规范。该标准规定了一个容器，用于托管和执行网络应用程序，并将接收到的请求分派给线程池和相应的对象来处理请求。使用的特殊类（`javax.servlet.Servlet`）提供了特定于协议的方法。`HttpServlet` 类提供了 `doGet` 或 `doPost` 等方法来封装 HTTP 方法。JSP 提供了另一种语法，允许将代码内嵌到 HTML 文件中。启动时，这些文件会自动转换成常规的 Servlet 类。与 CGI 相比，内部多线程提供了更好的性能和可扩展性，Web 应用程序容器和 Web 服务器也可以解耦。在这种情况下，需要使用 [Apache JServ 协议]()[Sha00]之类的连接协议。

### 分层架构

用于远程访问、交互式应用程序和分离关注点的模式，如模型-视图-控制器或表现-抽象-控制模式，早在网络出现之前就已经开发出来了。在这方面，网络的一个重要架构模式是[多层架构]()的概念[Fow02]。它描述了作为客户-服务器架构一部分的不同组件或组件组（component groups）的分离。这种分离通常有两种方式，一种是描述应用程序及其功能的逻辑分解，另一种是描述部署组件的技术分割。这种分离还有不同的粒度。指定专门用途的层级（如业务流程管理）或进一步分解层级（如分割数据访问和数据存储）会产生额外的层级。我们现在来看看网络架构中最常见的分离方式，即将关注点在逻辑上分为三个不同的层次。

- **表现层**：表现层负责显示信息。就 Web 应用程序而言，可以通过基于 HTML 的图形用户界面（网站）或提供结构化数据表示（网络服务）来实现。
- **应用逻辑层**：该层通过处理来自表现层的查询并提供适当的（数据）响应来处理应用程序逻辑。因此，需要访问持久层。应用层封装了应用程序的业务逻辑和以数据为中心的功能。
- **持久化层**：最后一层用于存储和检索应用数据。存储通常是持久性的，即数据库。

在将这些逻辑层映射到应用程序组件时，通常有多种不同的可能性。传统的 web 应用程序将所有层都分配到服务器端，只有 HTML 页面的渲染是在浏览器中进行的。这类似于传统的瘦客户机架构。现代浏览器技术（如网络存储 API 或 IndexedDB）现在允许应用程序完全位于客户端，至少在离线使用期间是这样。这会暂时将所有概念层推入浏览器，类似于胖客户端。在当前的大多数 web 应用程序中，层级是平衡的，呈现主要是浏览器的任务。现代网络应用程序通常会尝试在客户端提供尽可能多的功能，以获得更好的用户体验，并在浏览器不支持的情况下依赖服务器端功能。这被称为[优雅降级（gracemain degradation）](http://accessites.org/site/2007/02/graceful-degradation-progressive-enhancement/)，是从[容错系统设计]()中借用的术语[Ran78]。在某种程度上，客户端也有应用逻辑，但大部分功能都在服务器上。有时，功能也是冗余提供的。这对于输入验证等安全关键任务尤为重要。持久性被分配给服务器端，只有暂时离线使用等少数例外情况。

以服务器端架构为重点，各层提供了一套基本组件。表现层、应用层和持久层的组件可以放在一台机器上，也可以部署到专用节点上。我们将在本章稍后部分阐述基于组件的更详细架构模型。

### 负载均衡

垂直扩展的局限性迫使我们在一定规模上部署多个网络服务器。因此，我们需要一种机制来平衡来自多个可用服务器的输入请求的工作量。因此，我们希望有效利用所有服务器的资源（这是负载均衡的主要目标，而不是我们在[第 2 章](scalability.md)中看到的高可用性）。处理一个请求是一项耗时较短的任务，但由于并行请求的数量巨大，如何将请求适当分配到服务器上就成了一项严峻的挑战。为了解决这个问题，我们已经开发了几种策略，很快我们就会看到。在实施负载均衡解决方案时，另一个决定因素是[连接转发技术]()实现水平[Sch06]。

#### 网络层与应用层负载均衡

在 HTTP 中，网络服务器是通过主机名和端口来区分的。主机名通过 DNS 协议解析为 IP 地址。但是，一个 IP 地址不能同时分配给多个在线主机。将单个主机名映射到多个服务器的第一种方法是 DNS 条目包含多个 IP 地址，并保持一个轮换列表。实际上，这是一种幼稚的负载平衡方法，因为 DNS 有几个不受欢迎的特性，如很难移除崩溃的服务器或较长的更新传播时间。频繁更改主机名到 IP 地址会干扰通过 SSL/TLS 进行的安全连接。虽然基于 DNS 的均衡可以在一定程度上提供帮助（例如在多个负载均衡器之间进行平衡），但我们通常需要更强大、更复杂的机制。参照 ISO/OSI 模型，应用层和更底层都是合理的方法。

##### 2/3/4 层

在第 3/4 层运行的负载均衡器要么是网络交换机--专用的专有网络设备（“黑盒”），也可以是在通用服务器硬件上运行的 IP 虚拟服务器。它们的功能类似于反向 NAT 映射或路由。它们不是通过单个 IP 将 Internet 访问映射到多个私有节点，而是提供一个单一的外部可访问的 IP 映射到一组私有服务器。第 2 层负载均衡器使用链路聚合将多个服务器合并为单个逻辑链路。所有这些方法都使用了透明的头部修改、隧道、交换或路由等机制，但在不同的层级上。专用的网络设备在吞吐量方面可以提供令人瞩目的性能，但价格昂贵。基于在常规硬件上运行的 IP 虚拟服务器的解决方案通常提供了一个更经济实惠的解决方案，在一定规模范围内具有合理的性能。

##### 7 层

在应用层操作的负载均衡器在 HTTP 方面实质上是反向代理。与在较低层面工作的负载均衡器相反，应用层负载均衡器可以利用明确的协议知识。这会带来明显的性能损失，因为需要解析从传输层到应用层的流量。这种技术的好处包括可以进行基于 HTTP 的负载均衡决策、潜在的缓存支持、透明的 SSL 终止和其他 HTTP 特定功能。与 IP 虚拟服务器类似，应用层负载均衡器的性能较低于网络交换机。但是，由于它们托管在通用硬件上，可以实现良好的横向扩展性。

#### 均衡策略

目前已开发出多种负载均衡策略，在云计算时代，设计有效的均衡算法仍是学术界关注的问题。策略面临的一个主要挑战是难以预测未来的请求。先验知识的缺失限制了策略只能根据最近的负载做出一些假设（如果有的话）。

##### Robin 轮询

所有服务器被放置在一个概念性的环中，在每个请求中进行轮转。这种简单机制的一个缺点是它无法感知到负载过重的服务器。

##### 最少连接数

按照这个策略，负载均衡器管理一个服务器列表及其活动连接数。根据这个信息，新的连接将被转发。这个策略的基本思想是连接占用机器资源，连接数最少或积压最小的机器仍然具有最多的可用容量。

##### 最小响应时间

这个策略与前面的策略类似，但是使用响应时间而不是连接数。通过响应时间来衡量服务器负载的理由在于，特别是当连接的工作负载不同时，延迟对于服务器负载的表达能力更强。

##### 随机

在随机策略中，负载均衡器通过随机选择一个后端 Web 服务器。由于概率分布的作用，这种机制可以取得令人惊讶的好结果。

##### 资源感知

资源感知策略利用关于服务器利用率的外部知识，以及连接数和响应时间等指标。然后，将这些值结合起来对所有服务器进行加权，并相应地分配负载。

除了这些基本策略外，还有各种高级算法，它们往往结合了不同的方法。此外，当同时部署多个负载均衡器时，负载均衡策略会变得更加困难。有些策略会根据转发决定来估算利用率。多个负载均衡器可能会干扰个别假设。因此，通常需要在均衡器之间共享知识的合作策略。

根据 [Schlossnagle]() [Sch06]，在大型架构中，每台服务器 70% 的利用率是一个值得尊敬的目标。更高的利用率是不现实的，原因在于任务的短暂性、高吞吐率以及未来对传入请求知识的缺失。

#### 会话粘性

会话粘性（Session stickiness）是一种将访问 Web 应用程序的某个特定用户映射到同一后端 Web 服务器的技术，以便在其浏览会话期间仅需在相应服务器上存储会话状态。虽然在单个服务器设置中可以自动提供会话粘性，但在负载均衡方案中实现这个概念非常困难。基本上，会话粘性要求负载均衡器根据会话信息转发请求（例如解析 Cookie、读取会话变量或自定义 URI）。结果是，负载均衡器开始将会话分发到服务器而不是单个连接和请求。尽管这种设置对于 Web 应用程序开发人员来说非常方便，但从可伸缩性和可用性的角度来看，它代表了严峻的挑战。**如果丢失一个服务器，就相当于丢失了与之相关的会话**。当需求增加时，单个请求的粒度可以实现有效的分发机制。可以向架构中添加新的服务器，但拆分和重新分配已绑定到不同机器的现有会话是一个复杂的过程。因此，有效的资源利用和会话数据分配的概念不应混淆，否则可伸缩性和可用性将会受到威胁。在负载均衡方面，会话粘性通常被认为是一个误解，应尽量避免。相反，Web 架构应提供从不同服务器访问会话数据的方法。更多地采用无状态通信，由客户端管理会话状态，可以进一步减轻会话粘性的问题。

## 云架构

近年来，随着大规模架构的不断出现，云计算时代应运而生。

### 云计算

关于云计算，存在着大量不同的定义。“云”这个词本身是从将互联网抽象为云的比喻中衍生而来的。对于云计算的定义从将其简化为“下一个炒作术语”，到聚焦于特定方面的务实定义，再到将云计算视为信息架构的一种普遍范式转变的定义，各种不同的定义都有。正如 [2009年 ACM CTO 圆桌会议]()[Cre09]所显示的那样，对“云”所赋予的各种特征中，可伸缩性、按需付费的公用模型和虚拟化是最常见的共同特点。

云计算的关键概念是服务资源化。这些服务通常可以分为三个垂直层：

#### 基础设施即服务（IaaS）

虚拟化硬件的提供，通常以虚拟计算实例的形式出现，被称为基础设施即服务。这种形式的云服务的特点是能够按需和随时更改这些实例。这包括生成新的计算实例，通过调整大小或重新分配资源来修改现有实例，或者动态关闭不需要的实例。IaaS 的基本工作单元是虚拟镜像，它们在云中实例化和部署，并在虚拟机中执行。它们包含操作系统和实施应用程序的附加软件。由于完全虚拟化，IaaS 提供商的数据中心的物理基础架构对其客户完全透明。

#### 平台即服务（PaaS）

PaaS 通过提供完整的运行时环境为基于云的应用程序提供了额外的抽象层。PaaS 通常提供一个专用组件的软件栈，应用程序可以在其中执行，并提供了简化开发过程的工具。该平台向开发人员隐藏了所有可伸缩性工作，因此对用户来说，它似乎是一个单一的系统，尽管它可以透明地进行扩展。大多数平台通过复杂的监控和计量来实现这个特性。一旦应用程序上传到平台上，系统会自动在 PaaS 的数据中心节点中部署它。当应用程序负载过重时，将自动添加新节点进行执行。如果需求下降，多余的节点将从应用程序中分离并返回到可用资源池中。

#### 软件即服务（SaaS）

SaaS 提供基于 Web 的应用程序。由于各种技术趋势，基于 Web 的应用程序逐渐成熟，并成为完全在用户浏览器中运行的强大软件。通过替代在本地运行的传统桌面应用程序，SaaS 提供商能够仅在 Web 上发布他们的应用程序。云架构使他们能够应对大量在线用户。用户可以根据需求随时访问这些应用程序，而且只需按使用量付费，无需购买任何产品或许可证。这体现了将软件供应视为一种服务的理念。

根据云计算的术语，可扩展的 Web 应用程序本质上是需要适当的执行/托管环境（PaaS/IaaS）的 SaaS。

### PaaS 和 IaaS 供应商

下面，我们将考虑一些样板托管服务提供商，并概述其服务特点。

#### 亚马逊网络服务

亚马逊是最早提供专用、按需和按使用量付费的网络服务的提供商之一。它目前在多租户云计算市场上占据主导地位。他们的主要产品是**弹性计算云（EC2）**，这是一种提供不同虚拟化私有服务器的服务。提供可按比例扩展的虚拟机数量形成了他们大多数客户的架构基础。EC2 客户可以在几秒钟内启动新的机器实例，以适应不断变化的需求，而不是扩展和维护自己的基础设施。这种传统的 IaaS 托管服务还配备了一套其他可扩展的服务，代表了典型的架构组件。

_弹性块存储（EBS）_为 EC2 实例提供块级存储卷。简单存储服务（S3）是一个用于文件的键值Web存储。还有更复杂的可用数据存储系统，如 SimpleDB、DynamoDB 和关系数据库服务（RDS）。前两个服务代表了具有有限功能集的非关系型数据库管理系统。RDS 目前支持基于 MySQL 和 Oracle 的关系数据库。

_弹性负载均衡（ELB）_提供传输协议级别（如 TCP）和应用程序级别（如 HTTP）的负载均衡功能。作为消息队列，简单队列服务（SQS）可用于此。对于复杂的基于 MapReduce 的计算，亚马逊推出了弹性 MapReduce。

_ElastiCache_ 是一种内存缓存系统，有助于加速 Web 应用程序。CloudFront 是一个内容分发网络（CDN），通过按地域复制项目对 S3 进行补充。出于监控目的，亚马逊推出了中央实时监控网络服务 CloudWatch。

除了这些服务之外，亚马逊还推出了自己的 PaaS 栈，称为 _Elastic Beanstalk_。它实质上是 EC2、S3 和 ELB 等现有服务的捆绑，可以部署和扩展基于 Java 的 Web 应用程序。此外，还有其他涵盖会计或计费等业务服务的附加服务。

#### 谷歌应用引擎

[谷歌应用引擎（Google App Engine）](https://appengine.google.com/)是一个面向网络应用的 PaaS 环境。它目前支持 Python、Go 和 Java 等几种基于 JVM 的语言。应用程序在谷歌管理的数据中心托管和执行。其主要功能之一是自动扩展应用程序。应用程序的部署对用户是透明的，遇到高负载时，应用程序会自动部署到更多的机器上。

谷歌为应用引擎提供免费使用配额，限制流量、带宽、内部服务调用次数和存储大小等。超过这些限制后，用户可以决定增加收费资源，从而获得额外的容量费用。

应用程序的运行环境是沙箱式的，有几种语言功能受到限制。例如，基于 JVM 的应用程序不能生成新线程、使用套接字连接和访问本地文件系统。此外，一个请求的执行时间不得超过 30 秒。这些限制通过修改 JVM 和更改类库来执行。

除应用程序容器外，应用引擎还提供多种服务和组件作为平台的一部分。这些服务和组件可通过一组应用程序接口访问。下面我们将简要介绍当前的 Java API。

_数据存储 API_ 是应用引擎的基本存储后端。它是无模式对象数据存储，支持查询和原子事务执行。开发人员可以使用 Java Data Objects、Java Persistence API 接口或通过专门的底层 API 访问数据存储。专用 API 支持对数据存储的异步访问。对于较大的对象，尤其是二进制资源，应用引擎提供了 Blobstore API。出于缓存目的，可以使用 Memcache API。可以使用底层 API 或 JCache 接口。

Capabilities API 能以编程方式访问预定的服务停机时间，可用于开发自动为功能不可用做好准备的应用程序。多租户应用程序接口（Multitenancy API）为在应用引擎中运行的应用程序的多个独立实例提供支持。

可以使用专用的图像 API 对图像进行操作。邮件 API 允许应用程序发送和接收电子邮件。同样，XMPP API 允许基于 XMPP 进行信息交换。Channel API 可用于通过 HTTP 与客户端建立高级通道，然后向客户端推送信息。

远程应用程序接口（Remote API）可打开应用引擎应用程序，以便使用外部 Java 应用程序进行可编程访问。使用 URLFetch API 可以访问其他网络资源。

借助任务队列应用程序接口（Task Queue API），可以为与请求处理分离的任务提供一些支持。请求可以将任务添加到队列中，而工作者则异步执行后台工作。用户 API 提供了几种验证用户的方法，包括 Google 账户和 OpenID 标识。

## 可伸缩的网络基础设施的架构模型

可伸缩的 Web 架构的需求要比被归类为云计算的一组概念要早得多。先驱的网络公司在用户对其服务的兴趣逐渐增加并且现有容量被利用时，就开始吸取经验教训。他们被迫设计和建立持久的基础设施，以满足不断增长的需求。以下是一些更重要的准则和要求。然后，我们介绍一种可伸缩的 Web 基础架构的架构模型，该模型源自现有的云基础架构，并满足这些要求。所得到的模型将为我们进一步的考虑提供概念基础，并允许我们在后续章节中调查特定组件的并发性。

### 设计指南和要求

让我们先对基础设施的总体设计做一些假设。我们的目标是可扩展性，因此需要提供适当的可扩展性路径。虽然垂直可扩展性在这里可以帮助我们，但它并不是我们进行扩展的主要工具。将单核 CPU 替换为四核机器时，我们可能会将该节点的整体性能提高四倍（如果有的话）。但是，我们很快就会遇到技术上的限制，尤其是在进一步扩大规模时会遇到成本效益的限制。因此，我们的主要增长方式是横向扩展。在理想情况下，这使我们能够通过提供更多节点来线性扩展我们的基础设施能力。横向扩展本质上迫使我们进行某种分配。由于分布式系统比常规系统复杂得多，因此需要注意许多额外的问题和设计问题。其中最重要的是系统内部故障的受领和优美处理。这两项要求构成了基础设施的基本设计基础：我们必须针对规模和故障进行设计。要扩展一个未经设计的运行系统是非常困难的。如果一个复杂的系统由于混杂的子组件的部分故障而失效，要修复它也并非易事。因此，我们现在将介绍一些从头开始构建可扩展基础设施的指导原则。

在设计规模时，我们的目标是横向可扩展性。因此，系统分区和组件分配在一开始就是至关重要的设计决策。我们首先要将组件分配为松耦合的单元，并对容量和资源进行过度规划。组件的解耦可以防止（意外地）开发出不可复制的混杂和高度复杂的系统。相反，解耦的架构提出了一种简化模式，便于容量规划，降低组件之间的一致性要求。隔离组件和子服务可使它们独立扩展，并使系统架构师能够为组件应用不同的扩展技术，如克隆、拆分和复制。这种方法可以防止过度设计一个单体系统，并有利于更简单、更容易的组件。[Abbott 等人](https://dl.acm.org/doi/abs/10.1145/2088883.2139179)建议在设计、实施和部署过程中有意识地过度规划容量。作为一个大概的数字，他们建议在设计阶段使用 20 倍的系数，在实施阶段使用 3 倍的系数，在实际部署系统时至少使用 1.5 倍的系数。

故障设计对体系结构有多种影响。一个显而易见的说法是避免单点故障。复制和克隆等概念有助于建立冗余组件，在不影响可用性的情况下容忍单个节点的故障。从分布的角度来看，[Abbott 等人](https://dl.acm.org/doi/abs/10.1145/2088883.2139179)进一步建议划分故障域。这些域将组件和服务分组，使其中的故障不会影响或升级到其他故障域。故障域有助于检测和隔离系统中的部分故障。所谓的故障隔离泳道（fault isolative swim lanes）概念将这一理念发挥到了极致，完全禁止域之间的同步调用，也不鼓励异步调用。针对不同用户组的完整架构分片就是故障隔离泳道的一个例子。

接下来，我们需要一种组件间通信的方式。[通常的做法](https://dl.acm.org/doi/10.5555/2810078)是使用消息传递，因为它为松散耦合组件的集成提供了适当的通信机制。RPC 等替代方法则不太适用，因为它们要求组件之间具有更强的耦合性和一致性。

可扩展架构的另一套建议涉及时间和状态，这是分布式系统面临的基本挑战。根据经验，应尽可能放宽系统的所有时间限制，并尽可能避免全局状态。当然，这些建议的适用性显然取决于实际用例。例如，大型网上银行系统甚至不能暂时容忍无效的账户余额。另一方面，许多应用描述的用例允许在一定程度上弱化时间约束和一致性。当社交网络用户上传图片或撰写评论时，如果需要几秒钟的时间才能让其他用户看到，也不会造成服务质量下降。如果存在放宽时间约束、使用异步语义和防止分布式全局状态的手段，无论如何都应该考虑使用。强制同步行为和协调多个节点之间的共享状态是分布式系统中最困难的问题之一。在可能的情况下避开这些挑战，极大地增加了可扩展性的前景。这些建议不仅对实现有影响，而且对一般的架构也有影响。除非有充分的理由需要同步语义，否则应该使用异步行为和消息传递。削弱全局状态有助于防止单点故障，并促进服务的复制。

缓存是另一种有助于在不同地点提供可扩展性的机制。从根本上说，缓存就是将需求较高的数据副本存储在应用程序内实际需要的地方。缓存还可以通过存储和重用结果来防止幂等操作的多次执行。组件可以提供自己的内部缓存，或者专用的缓存组件可以将缓存功能作为服务提供给架构的其他组件。尽管缓存可以加速应用程序并提高可扩展性，但重要的是要考虑适当的替换算法、一致性和无效化缓存条目。

在实践中，将日志、监控和测量设施纳入系统非常重要。如果没有关于利用率和负载的详细数据，就很难预测日益增长的需求，也很难在瓶颈问题变得严重之前发现它们。在采取增加新节点和部署额外组件单元等应对措施时，应始终以一套全面的规则和实际测量结果为基础。

### 组件

根据这些指导方针，我们现在介绍一种可扩展网络基础设施的架构模型。该模型基于分离的组件，这些组件提供专用服务并可独立扩展。我们将它们划分为具有共同功能的层。这些组件是松耦合的，并使用消息传递进行通信。

对于每一类组件，我们都描述了其任务和目的，并概述了合适的可扩展性策略。此外，我们还列举了一些符合特定类别的实际例子，并参考了前面提到的流行云平台的例子。

![](./asserts/web_arch.svg)

图 3.1：可扩展的网络基础设施架构模型。各组件松耦合，可独立扩展。

#### HTTP 服务器

我们模型的第一个组件是实际的 HTTP 服务器。它是一个 web 服务器，负责接受和处理传入的 HTTP 请求并返回响应。HTTP 服务器与应用服务器是分离的。应用服务器是处理请求的真正业务逻辑的组件。这两个组件的解耦有几个优点，主要是 HTTP 连接处理和应用程序逻辑执行的分离。这样，HTTP 服务器就可以应用特定功能，如持久连接处理、透明 SSL/TLS 加密（注：适用性取决于所使用的负载平衡机制）或即时压缩内容，而不会影响应用服务器的性能。它还能将连接和请求分离开来。如果客户端使用持久连接向网络服务器发送多个请求，这些请求仍可在应用服务器前分离。同样，这种分离也允许两个组件根据各自的要求进行独立扩展。例如，移动用户比例较高的网络应用程序必须处理许多慢速连接和高延迟问题。移动链接会导致数据传输缓慢，从而造成服务器拥塞，降低了其为其他客户提供服务的能力。通过分离应用服务器和 HTTP 服务器，我们可以在上游部署额外的 HTTP 服务器，并通过卸载应用服务器来从容应对这种情况。

解耦 HTTP 服务器和应用服务器需要某种路由机制，将请求从 web 服务器转发到应用服务器。这种机制可以是两类服务器之间消息传递组件的透明功能。另外，web 服务器也可以采用类似于负载均衡器的分配策略。某些 web 服务器的另一项任务是提供图像和样式表等静态资产。在这种情况下，使用本地或分布式文件系统提供内容，而不是由应用服务器提供动态生成的内容。

_可扩展性策略_： HTTP 服务器的基本解决方案是克隆。在我们的案例中，由于服务器不保存任何状态，因此克隆工作非常简单。

_实际案例_： Apache HTTP 服务器是目前最流行的网络服务器，不过也有越来越多的替代品能提供更好的可扩展性，如 [nginx](http://nginx.org/) 和 [lighttpd](http://www.lighttpd.net/)。

_基于云的示例_： 谷歌应用引擎内部使用 [Jetty](http://jetty.codehaus.org/jetty/)，这是一种基于 Java 的高性能网络服务器。

可扩展性和并发性对大量并行连接和请求的影响是[第 4 章](webserver-architectures-for-highconcurrency.md)的主要内容。

#### 应用服务器

应用服务器是在应用层处理请求的专用组件。接收到的请求通常是经过预处理的高层结构，会触发业务逻辑操作。因此，会生成并返回适当的响应。

应用服务器是 HTTP 服务器的后盾，也是架构的核心组件，因为它是唯一能整合大多数其他服务以提供逻辑的组件。应用服务器的典型任务包括参数验证、数据库查询、与后端系统通信和模板渲染。

_可扩展性策略_：理想情况下，应用服务器采用无共享方式，即除了共享数据库外，应用服务器不直接共享任何平台资源。无共享风格使每个节点都是独立的，并允许通过克隆进行扩展。如有必要，应用服务器之间的协调和通信应外包给共享的后台服务。如果由于固有的共享状态而导致应用服务器实例之间的耦合更加紧密，那么在达到一定规模时，可扩展性就会变得非常困难和复杂。

_现实世界的例子_：流行的 web 应用环境包括专用脚本语言，如 Ruby（on Rails）、PHP 或 Python。在 Java 领域，RedHat 的 JBoss Application Server 或 Oracle 的 GlassFish 等应用容器也非常流行。

_基于云的实例_： 谷歌应用引擎和亚马逊的 Elastic Beanstalk 支持 Java Servlet 技术。应用引擎还可以托管基于 Python 和 Go 的应用程序。

应用服务器内部的编程和并发处理是[第 5 章](concurrency-concepts-for-applications-and-business-logic.md)的主题。

#### 消息队列系统

有些组件需要特殊形式的通信，如基于 HTTP 的接口（如网络服务）或基于套接字的低级访问（如数据库连接）。对于架构组件之间的所有其他通信，_消息队列系统_或_消息总线_是主要的集成基础设施。消息系统既可以有一个中央消息代理，也可以完全分散运行。消息传递系统在组件之间提供不同的通信模式，如请求-回复、单向、发布-订阅或推拉（扇出/扇入）、不同的同步语义和不同程度的可靠性。

_可扩展性策略_： 当分散式基础架构没有单点故障并专为大规模部署而设计时，它能提供更好的可扩展性。带有消息代理的面向消息的中间件系统需要更复杂的扩展方法。这可能包括对消息传递参与者进行分区和复制消息代理。

_现实世界中的例子_：[AMQP](https://www.amqp.org/sites/amqp.org/files/amqp.pdf) 是一种流行的消息传递协议，有多个成熟的实现，如 [RabbitMQ](http://www.rabbitmq.com/)。[ØMQ](http://www.zeromq.org/)是一种流行的无代理和分散式消息传递系统。

_基于云的实例_：亚马逊提供消息队列解决方案 SQS。谷歌应用引擎（Google App Engine）提供了一种基于队列的解决方案，用于处理后台任务和专用的 XMPP 消息传递服务。然而，所有这些服务的消息传递延迟都较高（可达数秒），因此是为其他目的而设计的。将这些服务用作 HTTP 请求处理的一部分是不合理的，因为这样的延迟是不可接受的。不过，一些基于 EC2 的定制架构已经推出了基于上述产品（如 ØMQ）的消息传递基础设施。

#### 后端数据存储

这些组件能够以持久、耐用的方式存储结构化、非结构化和二进制数据以及文件。根据数据类型，这包括关系数据库管理系统、非关系数据库管理系统和分布式文件系统。

_可扩展性策略_：我们将在[第 6 章]()中了解到，数据存储的扩展是一项具有挑战性的任务。复制、数据分区（即去规范化、垂直分区）和分片（水平分区）是传统的方法。

_真实案例_： [MySQL](http://mysql.com/) 是一种支持集群的流行关系数据库管理系统。[Riak](https://github.com/basho/riak)、[Cassandra](http://cassandra.apache.org/) 和 [HBase](http://hbase.apache.org/) 是可扩展的非关系型数据库管理系统的典型代表。[HDFS](http://hadoop.apache.org/hdfs/)、[GlusterFS](http://www.gluster.org/) 和 [MogileFS](http://danga.com/mogilefs/) 是大规模网络架构中使用的分布式文件系统的杰出代表。

_基于云的实例_：谷歌应用引擎提供了数据存储和 blob 存储。亚马逊为基于云的数据存储（如 RDS、DynamoDB、SimpleDB）和文件存储（如 S3）提供了多种不同的解决方案。

[第 6 章](concurrent-scalable-storage-backends.md)讨论了存储系统的并发性和可扩展性所面临的挑战。

#### 缓存系统

与持久存储组件相比，缓存组件提供的是易失性存储。缓存可实现对高需求对象的低延迟访问。在实践中，这些组件通常是基于键/值的内存存储，设计用于在多个节点上运行。有些缓存还支持高级功能，如针对某些键的发布/订阅机制。

_可扩展性策略_：从本质上讲，分布式缓存是一种基于内存的键/值存储。因此，可以通过为机器提供更多内存来实现纵向扩展。通过克隆和复制节点以及划分密钥空间，可以实现更持久的扩展。

_真实案例_：[Memcached](http://memcached.org/) 是一种流行的分布式缓存。[Redis](http://redis.io/) 是另一种内存缓存，支持结构化数据类型和发布/订阅通道。

_基于云的示例_：谷歌应用引擎支持 Memcache API，亚马逊提供了名为 ElastiCache 的专用缓存解决方案。

[第 6 章](concurrent-scalable-storage-backends.md)中的一些注意事项也适用于分布式缓存系统。

#### 后台作业服务

计算密集型任务不应由应用服务器组件执行。例如，对上传的视频文件进行转码、生成图像缩略图、处理用户数据流或运行推荐引擎，都属于占用 CPU 资源的密集型任务。这些任务通常是异步的，因此可以在后台独立执行。

_可扩展性策略_：在后台工作池中添加更多的资源和节点，通常会加快计算速度，或由于并行性，允许执行更多的并发任务。从并发的角度来看，如果作业是没有依赖关系的小型孤立任务，则更容易扩展工作池。

_真实案例_：[Hadoop](http://hadoop.apache.org/) 是 MapReduce 平台的开源实现，允许在大型数据集上并行执行某些算法。在实时事件处理方面，Twitter 发布了 [Storm 引擎](https://github.com/nathanmarz/storm)，这是一个分布式实时计算系统，主要针对流处理。[Spark](https://dl.acm.org/doi/10.5555/1863103.1863113) 是为内存集群设计的数据分析开源框架。

_基于云的实例_：谷歌应用引擎提供了任务队列 API，允许向一组后台工作者提交任务。亚马逊提供了一种基于 MapReduce 的定制服务，名为 Elastic MapReduce。

#### 集成外部服务

特别是在企业环境中，经常需要集成更多的后端系统，如客户关系管理（CRM）/机构资源规划（ERP）系统或流程引擎。这可以通过专用集成组件（即所谓的企业服务总线）来解决。ESB 也可以是网络架构的一部分，甚至可以取代用于集成的简单消息传递组件。另一方面，后端企业架构通常与网络架构分离，而使用网络服务进行通信。网络服务还可用于访问外部服务，如信用卡的验证服务。

_可扩展性策略_：外部服务的可扩展性首先取决于其自身的设计和实施。由于这不是我们内部网络架构的一部分，因此我们不会进一步考虑。至于与我们网络架构的整合，最好将重点放在无状态、可扩展的通信模式和松散耦合上。

_现实世界的例子_：[Mule](http://www.mulesoft.org/) 和 [Apache ServiceMix](http://servicemix.apache.org/) 是两种提供 ESB 和集成功能的开源产品。

_基于云的示例_：我们提到的两个提供商都只在较低层次上提供外部服务集成机制。谷歌应用引擎允许通过 URLFetch API 访问外部网络资源。此外，XMPP API 可用于基于消息的通信。同样，亚马逊的消息服务也可用于集成。

### 模型的关键反思

现在，我们已经介绍了具有扩展能力的网络基础设施的一般架构模型。然而，对于直接实施和部署而言，所建议的模型并不完整。我们忽略了并发性考虑所不需要的组件，但这些组件确实是成功运行所必需的。这包括完整架构的日志记录、监控、部署和管理组件。此外，由于简单起见，我们忽略了安全和身份验证所需的组件。在构建实际基础设施时，将这些组件纳入其中非常重要。

另一点批评意见针对的是刻意分割组件的做法。在实践中，功能分配可能会有所不同，这往往会导致组件数量减少。尤其是在当今使用的某些服务器应用程序中，网络服务器和应用服务器的划分可能显得有些武断。然而，混为一谈的设计会影响我们对并发性的考虑，往往会在可扩展性方面带来更棘手的问题。

## 可拓展 Web 应用程序

到目前为止，我们已经从架构的角度探讨了网络应用程序的可扩展性。在接下来的章节中，我们将根据网络服务器、应用服务器和后端存储系统内部并发性的使用情况，重点讨论网络架构内部的可扩展性。

不过，还有其他因素会影响 web 应用程序的可扩展性和感知性能。因此，我们将对 web 站点设置和客户端 web 应用程序设计相关的因素进行简要概述。本概述总结了[相关书籍]()[^1][^2][^3]和雅虎开发者网络的一篇博客文章中概述的重要策略。

从用户的角度来看，当 web 应用程序不受并发用户数量和负载的影响，仍能提供相同的服务和相同的服务质量时，它就是可扩展的。理想情况是，用户无法从与应用程序的交互体验中推断出实际的服务负载。因此，持续、低延迟的响应对用户体验非常重要。在实际应用中，单次请求/响应周期的低往返延迟至关重要。更复杂的 web 应用程序可通过防止完全重新加载和动态行为（如异步加载新内容和部分更新用户界面）（如 AJAX）来减轻负面影响。此外，完善可靠的应用功能和优雅的降级对于用户的接受度也至关重要。

### 优化通信和内容传输

首先，尽可能减少 HTTP 请求的数量非常重要。网络往返时间（可能在此之前还有建立连接的开销）会大大增加延迟，降低用户体验。因此，web 应用程序中应尽量减少必要的请求次数。一种流行的方法是使用 CSS sprites 或图像地图。这两种方法都是加载包含多个较小图像的单个图像文件。然后，客户端对图像进行分割，小块图像可以独立使用和渲染。这种技术可以提供包含所有按钮图像和图形用户界面元素的单一图像，作为组合图像的一部分。减少请求的另一种方法是内联外部内容。例如，[数据 URI 方案](https://datatracker.ietf.org/doc/html/rfc2397)允许将任意内容嵌入为 base64 编码的 URI 字符串。这样，较小的图像（如图标）就可以直接内联到 HTML 文档中。按照 [RFC 2616](https://datatracker.ietf.org/doc/html/rfc2616)的要求，浏览器通常会限制与某一主机的并行连接数。因此，当需要访问多个资源时，为同一服务器提供多个域名（如 static1.example.com、static2.example.com）会有所帮助。这样，就可以使用不同的域名来识别资源，客户端也可以在加载资源时增加并行连接的数量。

另一个重要策略是尽可能使用缓存。根据经验，静态资产应注明不会过期。静态资产是用于补充网站的图像、样式表文件、JavaScript 文件和其他静态资源。这些资产上线后一般不再进行编辑。相反，当网站更新时，它们会被其他资产取代。这就为静态资产提供了一些不可改变的特性，使我们能够积极地进行缓存。因此，在一段时间后，静态资产缓存将网络浏览器和网络服务器之间的流量几乎完全限制在动态生成的内容上。对于动态内容，网络服务器应提供有用的标头（如 ETag、Last-Modified），允许在后续请求中使用条件语义。作为网络架构一部分的反向缓存代理也可以缓存生成的内容，加快响应速度。

使用 CDN 有助于减轻提供静态资产的机器的负担，缩短响应时间。由于 CDN 不仅仅是缓存反向代理，它们还提供地理分布，并将请求路由到离客户端最近的节点。为 JavaScript 库（如 jQuery）等流行资产使用公共资源库也有好处。这些资源库不仅有大型 CDN 的支持，使用它们还能增加被缓存的机会。

对内容进行即时压缩可进一步减小文件大小，而 CPU 开销则微乎其微。这对基于文本的格式很有帮助，对语法冗长的格式（如 HTML、XML）尤其有效。

### 提高网站性能

此外，还有一些关于 HTML 文件结构的建议，可改善用户体验。最好在 HTML 文件顶部引用外部样式表，在底部引用 JavaScript 文件。在解析和呈现网站时，浏览器会根据允许的并行请求数量（见上文），先加载样式表，然后再加载脚本。这种顺序有助于逐步渲染页面，然后加入交互式元素。此外，JavaScript 和 CSS 资源不应内联到 HTML 文件中，而应外部化到文件中。这有助于缓存工作，并最大限度地减少 HTML 文件的大小。样式表和 JavaScript 文件等基于文本的资产也应通过删除空格和将变量名重命名为尽可能短的名称来进行文本精简，从而有效减少生产环境中的文件大小。不同的内容加载时间也会影响用户体验。依赖基于 AJAX 内容的丰富用户界面通常会在首次加载时提供基本的用户界面。之后，访问实际内容并将其填充到用户界面中。这种顺序导致网站逐步组成，与完全重新加载的大型 HTML 文件相比，响应速度似乎更快。与后载入内容相反，预载入技术也能加快用户体验。HTML5 提供了 `prefetch` 链接关系，允许浏览器提前加载链接资源。随着基于网络的丰富界面越来越多地使用浏览器来实现应用功能和部分业务逻辑，高效处理计算和并发任务也变得非常重要。[Web Worker API](http://www.w3.org/TR/2009/WD-workers-20091222/)解决了这一问题，它允许生成后台工作者。在通信和协调方面，工作者使用消息传递，他们不共享任何数据。

关于应用程序，明确区分不同的状态很有帮助。理想情况下，服务器处理应用程序资源的持久状态，而客户端处理自己的会话状态，通信则是无状态的。传统上，完全的无状态通信是很困难的，因为 HTTP cookie 一直是有状态网络应用的主要方式。与 HTML5 相关的 [Web Storage API](http://www.w3.org/TR/2009/WD-webstorage-20091222/)为客户端状态处理提供了另一种机制。该 API 本质上允许在浏览器中通过 JavaScript 存储键/值对。该 API 可为每个域或单个浏览器会话提供持久状态。与 HTTP cookie 不同的是，Web Storage API 的操作不会放在 HTTP 标头中，因此能保持通信的无状态性。这种浏览器存储还可用于在客户端应用层中缓存数据。

## 总结

我们已经看到，提供动态内容与提供静态文件面临着不同的挑战。为了整合动态内容，CGI、FastCGI 和专用网络服务器模块等技术得到了发展，脚本语言在这方面也非常流行。另外，还可以使用承载动态网络应用程序的容器进行部署，这是基于 Java 的应用程序的首选方式。网络架构的分层视图不仅有助于拆分特定的部署组件，还能提供网络架构的逻辑分隔。它还可以通过对表现形式、应用逻辑和持久性进行分层，在逻辑上将关注点分开。我们还了解到，负载均衡是网络架构可扩展性的一个基本概念，因为它可以将负载分配给多个服务器。

然后，我们介绍了云计算的一般原理，即利用虚拟化技术，以按使用付费的实用模式按需提供服务。云计算平台和基础设施具有无缝可扩展性，因此与我们的可扩展网络架构非常相关。

基于现有云服务中的类似组件，我们引入了可扩展网络基础设施的架构模型。我们的模型不采用单一的单体架构，而是采用一组具有松散耦合的专用服务组件。这种分离使我们能够使用适当的扩展策略来扩展不同的服务组件，并使整体架构更加稳健、可靠和灵活。网络服务器、应用服务器和后端存储系统的隔离也使我们能够更准确地了解每种组件类型内部的并发挑战。

我们还提到了其他一些提高性能和可扩展性的尝试，目的是优化通信、改善内容交付和加快网站速度。不过，这些尝试并不在后续章节的讨论范围之内。



[^1]: https://www.oreilly.com/library/view/web-operations/9781449377465/
[^2]: https://www.oreilly.com/library/view/scalability-rules-50/9780132614016/
[^3]: https://www.oreilly.com/library/view/art-of-scalability/9780134031408/