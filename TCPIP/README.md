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
- **ACK**：确认（一种表明数据已经成功到达接收端的提示，适用于协议栈的多个层次）

## <a id="B">B</a>

- **BAP**：带宽分配协议
- **BACP**：带宽分配控制协议
- **BOD**：按需带宽
- **BPDU**：Bridge PDU，网桥协议数据单元
- **BPSK**：Binary phase shift keying，二进制相移键控；使用两个信息相位调制二进制数
- **BSD**：伯克利软件套件（加州大学伯克利分校的UNIX版本，包括首个广泛使用的TCP/IP实现版本）
- **BSS**：basic service set，基本服务集合；

## <a id="C">C</a>

- **CBC**：cipher block chaining 密码块链接（一种利用XOR运算来链接已加密块的加密模式，用于抵御重排攻击）
- **CCM**：counter mode，带CBC消息认证码的计算器模式（一种认证加密模式，结合了CTR模式加密和CBC-MAC）
- **CCMP**：Counter Mode with CBC-MAC Protocol，带CBC-MAC协议的计数器模式（用于WPA2加密；源自IEEE 802.11i 协议；是WPA的继承者）
- **CCP**：压缩控制协议
- **CHAP**：查询-握手认证协议
- **CTS**：clear to send，明确发送
- **CTR**：Counter，计数器（一种加密模式，在并行的对多个块进行加密或解密时使用一个计数器的值来维护被加密块间的顺序）
- **CIDR**：无类别域间路由，classless inter-domain routing

## <a id="D">D</a>

- **<a id="dcf">DCF </a>**：distributed coordinate function，分布式协调功能
- **DIFC**：distributed inter-frame spacing，分布式帧间间隔
- **DHCP**：dynamic host configuration protocol，动态主机配置协议：BOOTP 演变而来，使用配置信息来建立系统，例如租用的IP地址、默认路由器以及DNS服务器的IP地址

## <a id="E">E</a>

- **EAP**：Extensible Authentication Protocol，扩展身份验证协议（支持各种框架身份验证）
- **EDCA**：enhanced [DCF](#def) channel access 增强型 DCF 信道访问
- **EQM**：Equal Modulation，平等调制（对不同的数据流同时使用相同的调制方案）

## <a id="F">F</a>

- **FEC**：Forward Error Correction，转发纠错（使用冗余位来纠正数据位中的错误）

## <a id="G">G</a>
## <a id="H">H</a>

- **HC**：hybrid coordinate，混合协调
- **HCF**：hybrid coordinate function，混合协调功能
- **HCCA**：HFCA-controlled channel access，HFCA 控制信道访问
- **HDLC**

## <a id="I">I</a>

- **ISM**：industry science medicine，工业、科学和医疗（世界上许多地区免许可证的频段，为 Wi-Fi 所用）

## <a id="J">J</a>
## <a id="K">K</a>
## <a id="L">L</a>

- **LCP**：链路控制协议（在 PPP 中，用于建立一条链路）

## <a id="M">M</a>

- **MAC**：Media Access Control，介质访问控制，对一个共享网络介质的访问控制；通常作为链路层协议的一部分
- **MAC**：Message Authentication Code，消息认证码；用于协助认证消息完整性的数学函数
- **MIMO**：multiple input multiple output，多输入多输出
- **MCS**：modulation and coding scheme，调制和编码方案（结合调制和编码 ，802.11n 中有很多可用的组合）

## <a id="N">N</a>

- **NAT**：Network Address Translation，网络地址转换；在IP数据报中重写地址的机制；主要用于减少全球可路由IP地址的使用量；通常与私有IP地址共同使用；也支持防火墙功能
- **NACK**：否定确认（表示未收到或不接受的标识）
- **NCP**：

## <a id="O">O</a>

- **OFDM**：Orthogonal Frequency Division Multiplexing，正交频分复用（一种复杂的调制方案，在某个指定的宽带下同时调制多个频率的载波，以实现高吞吐量；用于DSL、80211a/g/n、802.16e，以及包含LTE的高级蜂窝数据标准）
- 

## <a id="P">P</a>

- **PAP**：密码认证协议
- **PDU**：protocol data unit，描述在协议层的信息；在非正式情况下，有时与数据包、帧、数据报、报文段以及消息等名词通用
- **PFF**：
- **PLCP**：物理层汇聚程序；802.11 用于编码与决定帧类型及无线电参数的方法
- **PCO**：分阶段共存操作；
- **PoE**：以太网供电
- **PSK**：预共享密钥
- **PPP**：

## <a id="Q">Q</a>

- **QSTA**：
- **QPSK**：正交相移键控
- **QAM**：正交幅度调制

## <a id="R">R</a>

- **RAS**：远程访问服务器
- **RSTP**：rapid spanning tree protocol，快速生成树协议
- **RSN**：强健网络安全
- **RSNA**：强健安全网络访问
- **RTS**：请求发送

## <a id="S">S</a>

- **SAP**：session announcement protocol，会话通告协议；携带实验性的组播会话通告
- **STP**：spainning tree protocol，生成树协议
- **STA**：站点（一个接入点或相关无线主机的 IEEE 802.11 术语）

## <a id="T">T</a>

- **TXOP**：Transmission Opportunity，传输机会；在 802.11 中 允许一个站点发送一个或多个帧的模式
- **TSPEC**：
- **TKIP**：临时密钥完整性协议

## <a id="U">U</a>

- **UP**：user priorities，用户优先级
- **UEQM**：不平等调制

## <a id="V">V</a>

- **VLSM**：Variable-Length Subnet Masks 可变长度子网掩码

## <a id="W">W</a>

- **WEP**：有线等效保密加密算法
- **WPA**：Wi-Fi 保护访问

## <a id="X">X</a>
## <a id="Y">Y</a>
## <a id="Z">Z</a>