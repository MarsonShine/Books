# TCP/IP 协议详解

这本书里面有很多专业术语的简称，虽说书里面第一次提到的时候有些是什么意思，但是当我继续往后面看的时候，再次碰到这个缩写简称术语时，经常忘记而回过头花大量的时间找到当初这个术语第一次出现的地方。这样很浪费时间，所以才有这个文档。来记录缩写的意思

## 快速导航

| [A](#a) | [B](#b) | [C](#c) | [D](#d) | [E](#e) | [F](#f) |
| :-----: | :-----: | :-----: | :-----: | :-----: | :-----: |
| [G](#g) | [H](#h) | [I](#i) | [J](#j) | [K](#k) |  [L]()  |
| [M](#m) | [N](#n) | [O](#o) | [P](#p) | [Q](#q) | [R](#r) |
| [S](#s) | [T](#t) | [U](#u) | [V](#v) | [W](#w) | [X](#x) |
| [Y](#y) | [Z](#z) |         |         |         |         |



## <a id="A">A</a>

- **AES**：Advanced Encryption Standard 高级加密标准算法
- **AP**：access point，接入点，（802.11 标准，通常用于连接无线和有线网络部分）
- **ACCM**：Asynchronous Control Character Map，简称 asyncmap；异步控制字符映射（在PPP中，压缩地址和控制字段以减少开销）
- **ACFC**：Address and Control Field Compression，地址和控制字段压缩（在 PPP 中，压缩地址和控制字段以减少开销）
- **ACK**：Acknowledgment，确认（一种表明数据已经成功到达接收端的提示，适用于协议栈的多个层次）

## <a id="B">B</a>

- **BAP**：Bandwidth Allocation Protocol，带宽分配协议（在一个 MPPP 绑定中，一种用于配置链路的协议）
- **BACP**：Bandwidth Allocation Control Protocol，带宽分配控制协议（与 PPP 一起，一种用于配置 BoD 的协议）
- **BoD**：Bandwidth on Demand,按需带宽（动态调整可用链路带宽的能力）
- **BPDU**：Bridge PDU，网桥协议数据单元（由 STP 使用的 PDU；通过交换机与网桥进行交换）
- **BPSK**：Binary phase shift keying，二进制相移键控；使用两个信息相位调制二进制数
- **BSD**：伯克利软件套件（加州大学伯克利分校的UNIX版本，包括首个广泛使用的TCP/IP实现版本）
- **BSS**：basic service set，基本服务集合（IEEE 802.11接入点和相关站的术语）

## <a id="C">C</a>

- **CBC**：cipher block chaining 密码块链接（一种利用XOR运算来链接已加密块的加密模式，用于抵御重排攻击）
- **CBCP**：Callback Control Protocol，回拨控制协议（在 PPP 中建立一个回叫号码）
- **CCM**：counter mode，带CBC消息认证码的计算器模式（一种认证加密模式，结合了CTR模式加密和CBC-MAC）
- **CCMP**：Counter Mode with CBC-MAC Protocol，带CBC-MAC协议的计数器模式（用于WPA2加密；源自IEEE 802.11i 协议；是WPA的继承者）
- **CCP**：Compression Control Protocol，压缩控制协议（在 PPP 中，建立使用的压缩方法）
- **CHAP**：Challenge-Handshake Authentication Protocol，查询-握手认证协议（协议要求每一次查询到对应一个响应；易受到中间人攻击）
- **CTS**：clear to send，清除发送（授权 RTS 发送者进行发送的消息）
- **CTR**：Counter，计数器（一种加密模式，在并行的对多个块进行加密或解密时使用一个计数器的值来维护被加密块间的顺序）
- **CIDR**：无类别域间路由，classless inter-domain routing（一种解决地址 ROAD 问题的步骤，通过移除 IP 地址的类边界实现，但要求域间路由使用一个关联的 CIDR 掩码）
- **CSLIP**：压缩串行线路 IP（一种来源于旧的点到点协议）

## <a id="D">D</a>

- **<a id="dcf">DCF </a>**：distributed coordinate function，分布式协调功能（CSMA/CA 介质访问控制方法，用于 802.11 网络）
- **DIFCS**：distributed inter-frame spacing，分布式帧间间隔（802.11 DCF 帧之间的时间）
- **DHCP**：dynamic host configuration protocol，动态主机配置协议：BOOTP 演变而来，使用配置信息来建立系统，例如租用的IP地址、默认路由器以及DNS服务器的IP地址
- **DTCP**：动态隧道配置协议

## <a id="E">E</a>

- **EAP**：Extensible Authentication Protocol，扩展身份验证协议（支持各种框架身份验证）
- **EDCA**：enhanced [DCF](#def) channel access 增强型 DCF 信道访问
- **EQM**：Equal Modulation，平等调制（对不同的数据流同时使用相同的调制方案）

## <a id="F">F</a>

- **FCS**：Frame Check Sequence，帧校验序列（用于检查位错误的数据位的通称）
- **FEC**：Forward Error Correction，转发纠错（使用冗余位来纠正数据位中的错误）

## <a id="G">G</a>

- **GRE**：Generic Routing Encapsulation，通用路由封装（在 IP 数据报中的通用封装）

## <a id="H">H</a>

- **HC**：hybrid coordinate，混合协调
- **HCF**：hybrid coordinate function，混合协调功能（同时支持优先级与基于竞争的 802.11 信道访问的协调功能）
- **HCCA**：HFCA-controlled channel access，HFCA 控制信道访问
- **HDLC**：High-level Data Link Control，高级数据链路控制（一种流行的的 ISO 数据链路协议标准，是大多数流行的 PPP 变种的基础）

## <a id="I">I</a>

- **ICMP**：Internet Control Message Protocol，Internet 控制报文协议（一种信息与错误报告协议，被视为 IP 协议的一部分）
- **ICPC**：*TODO：文章中并没有说明，不知道是什么意思*
- **IPCP**：IP 控制协议（在 PPP 中一种用于配置 IPv4 网络链路的 NCP）
- **IPV6CP**：IP 控制协议（在 PPP 中一种用于配置 IPv6 网络链路的 NCP）
- **ISM**：industry science medicine，工业、科学和医疗（世界上许多地区免许可证的频段，为 Wi-Fi 所用）

## <a id="J">J</a>
## <a id="K">K</a>
## <a id="L">L</a>

- **L2TP**：Layer 2 Tunneling Protocol，第 2 层隧道协议（IETF 标准的链路层隧道协议）
- **LCP**：Link Control Protocol，链路控制协议（在 PPP 中，用于建立一条链路）
- **LQR**：Link Quality Reports，链路质量报告（在 PPP 中，关于链路质量的测量报告，包括接收、发送以及因错误而被拒绝接收的数据包数量）

## <a id="M">M</a>

- **MAC**：Media Access Control，介质访问控制，对一个共享网络介质的访问控制；通常作为链路层协议的一部分
- **MAC**：Message Authentication Code，消息认证码；用于协助认证消息完整性的数学函数
- **MIMO**：multiple input multiple output，多输入多输出
- **MCS**：modulation and coding scheme，调制和编码方案（结合调制和编码 ，802.11n 中有很多可用的组合）
- **MP**：Mesh Point，Mesh（网格） 点（在 IEEE 802.11s 中以 Mesh 方式进行配置的节点名称）
- **MRU**：Maximum Receive Unit，最大接收单元（接收方能够接收的最大数据包 / 消息的大小）
- **MRRU**：Multilink Maximum Received Reconstructed，多链路最大接收重构单元（多条 MP 链路重构之后的最大接收单元）
- **MPPE**：Microsoft’s Point-to-Point Encryption，微软点对点加密（用于 PPP）
- **MTU**：Maximum Transmission Unit，最大传输单元（一个网路所能传输的最大帧大小）

## <a id="N">N</a>

- **NAT**：Network Address Translation，网络地址转换；在IP数据报中重写地址的机制；主要用于减少全球可路由IP地址的使用量；通常与私有IP地址共同使用；也支持防火墙功能
- **NACK**：Negative Acknowledgment，否定确认（表示未收到或不接受的标识）
- **NCP**：Network Control Protocol，网络控制协议（在 PPP 中，用来建立网络协议层）

## <a id="O">O</a>

- **OFDM**：Orthogonal Frequency Division Multiplexing，正交频分复用（一种复杂的调制方案，在某个指定的宽带下同时调制多个频率的载波，以实现高吞吐量；用于DSL、80211a/g/n、802.16e，以及包含LTE的高级蜂窝数据标准）
- 

## <a id="P">P</a>

- **PAP**：Password Authentication Protocol，密码认证协议（使用明文密码的协议，容易受到中间人的攻击或窃听）
- **PDU**：protocol data unit，描述在协议层的信息；在非正式情况下，有时与数据包、帧、数据报、报文段以及消息等名词通用
- **PFC**：Protocol Field Compression协议字段压缩（在 PPP 中，删除了协议字段以减少开销）
- **PLCP**：Physical Layer Convergence Procedure，物理层汇聚程序；802.11 用于编码与决定帧类型及无线电参数的方法
- **PCO**：Phased Coexistence Operation，分阶段共存操作（802.11 接入点切换信道宽度的一种方法，以减少对旧设备的负面影响）
- **PoE**：Power over Ethernet，以太网供电（通过以太网布线为设备供电）
- **PSK**：Pre-Shared Key，预共享密钥（预先设置加密密钥，不使用动态密钥交换协议）
- **PPP**：Point-to-Point Protocol，点对点协议（一个链路层配置与数据封装协议，能够承载多种网络层协议，并能够使用多种底层物理链路）
- **PMTU**：Path MTU（在发送方到接收方的路径上所经过链路的最小 MTU 值）
- **PMTUD**：PMTU Discovery，路径MTU发现（确定 PMTU 的过程；通常依赖于 ICMP PTB 消息）
- **PPTP**：Point-to-Point Tunneling Protocol，点对点隧道协议（Microsoft 的链路层隧道协议）

## <a id="Q">Q</a>

- **QSTA**：基于 QoS 的STA（一种支持 Qos 功能的 802.11 STA）
- **QPSK**：Quadrature Phase Shift Keying，正交相移键控（一般利用四个信号相位对每个符号进行两位调制，在更高级的版本中每个符号可能对应更多位）
- **QAM**：Quadrature Amplitude Modulation，正交幅度调制（相位和幅度调制的组合）
- **QoS**：Quality of Service，服务质量（描述如何处理流程的通用术语，通常基于不同的配置参数有更好或更坏的延迟或丢弃优先级）

## <a id="R">R</a>

- **RAS**：Remote Access Server，远程访问服务器（一台服务器，用于处理远程用户的身份认证、访问控制等）
- **RSTP**：rapid spanning tree protocol，快速生成树协议（STP 的较少延迟版本）
- **RSN**：Robust Security Network，强健网络安全（针对 IEEE 802.11i/WPA 的安全改进；已包含于 802.11 标准中）
- **RSNA**：RSN Association，强健安全网络访问（RSN 的完整利用 / 实施）
- **RTS**：Request To Send ，请求发送（表明希望发送后续消息的消息）
- **ROHC**：鲁棒性头部压缩（允许同时对多个头部进行压缩）协议头部压缩的当前一代标准

## <a id="S">S</a>

- **SAP**：session announcement protocol，会话通告协议；携带实验性的组播会话通告
- **SDLC**：Synchronous Data Link Control，同步数据链路控制（HDLC 协议的前身，SNA 的链路层）
- **SNA**：Systems Network Architecture，系统网格体系结构（IBM 公司的网络体系结构）
- **STP**：spainning tree protocol，生成树协议
- **STA**：Station，站点（一个接入点或相关无线主机的 IEEE 802.11 术语）

## <a id="T">T</a>

- **TCP**：Transmission Control Protocol，传输控制协议（一种面向连接的无消息边界的可靠流协议，包括了流量与拥塞控制）
- **TXOP**：Transmission Opportunity，传输机会；在 802.11 中 允许一个站点发送一个或多个帧的模式
- **TSPEC**：Traffic Specification，流量规范（为 802.11 Qos 指明流量参数的一个结构）
- **TKIP**：Temporal Key Integrity Protocol，临时密钥完整性协议（用 WPA 替换 WEP 的加密算法）

## <a id="U">U</a>

- **UDL**：Unidirectional Link，单向链路（只提供一个方向通信的链路）
- **UDP**：User Datagram Protocol，用户数据报协议（一种尽力而为的消息协议，带有消息边界，不支持拥塞或流量控制）
- **UP**：user priorities，用户优先级（802.11 优先级；基于来自 802.1d 的相同术语）
- **UEQM**：Unequal Modulation，不平等调制（对不同的数据流同时使用不同的调制方案）

## <a id="V">V</a>

- **VLSM**：Variable-Length Subnet Masks 可变长度子网掩码
- **VLAN**：Virtual LAN，虚拟局域网（通常用于在共享的线路上模拟多个不同的局域网）
- **VPN**：Virtual Private Network，虚拟专用网络（实际上隔离的网络；通常加密）

## <a id="W">W</a>

- **WEP**：Wired Equivalent Privacy，有线等效保密加密算法（原始的 Wi-Fi 加密；被证实非常脆弱）
- **WPA**：WiFi Protected Access，Wi-Fi 保护访问（802.11 加密方法）

## <a id="X">X</a>
## <a id="Y">Y</a>
## <a id="Z">Z</a>