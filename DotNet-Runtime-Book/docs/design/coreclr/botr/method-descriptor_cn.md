1. 方法描述符 (Method Descriptor)
2. ==============================
3. 
4. 作者: Jan Kotas ([@jkotas](https://github.com/jkotas)) - 2006
5. 
6. 简介
7. ====
8. 
9. MethodDesc（方法描述符）是托管方法的内部表示。它有几个用途：
10. 
11. - 提供唯一的方法句柄，可在整个运行时使用。对于普通方法，MethodDesc 是 <模块, 元数据标记, 实例化> 三元组的唯一句柄。
12. - 缓存从元数据计算昂贵的常用信息（例如方法是否为静态）。
13. - 捕获方法的运行时状态（例如是否已为该方法生成代码）。
14. - 拥有方法的入口点。
15. 
16. 设计目标和非目标
17. ----------------
18. 
19. ### 目标
20. 
21. **性能：** MethodDesc 的设计针对大小进行了大量优化，因为每个方法都有一个 MethodDesc。例如，当前设计中普通非泛型方法的 MethodDesc 为 8 字节。
22. 
23. ### 非目标
24. 
25. **丰富性：** MethodDesc 不缓存有关方法的所有信息。对于较少使用的信息（例如方法签名），预计必须访问底层元数据。
26. 
27. MethodDesc 的设计
28. =================
29. 
30. MethodDesc 的种类
31. -----------------
32. 
33. 有多种类型的 MethodDescs：
34. 
35. **IL**
36. 
37. 用于常规 IL 方法。
38. 
39. **Instantiated**
40. 
41. 用于具有泛型实例化或在方法表中没有预分配槽的不常见 IL 方法。
42. 
43. **FCall**
44. 
45. 在非托管代码中实现的内部方法。这些是 [标记为 MethodImplAttribute(MethodImplOptions.InternalCall) 属性的方法](corelib.md)，委托构造函数和 tlbimp 构造函数。
46. 
47. **PInvoke**
48. 
49. P/Invoke 方法。这些是标记为 DllImport 属性的方法。
50. 
51. **EEImpl**
52. 
53. 其实现由运行时提供的委托方法（Invoke, BeginInvoke, EndInvoke）。参见 [ECMA 335 Partition II - Delegates](../../../project/dotnet-standards.md)。
54. 
55. **Array**
56. 
57. 其实现由运行时提供的数组方法（Get, Set, Address）。参见 [ECMA Partition II – Arrays](../../../project/dotnet-standards.md)。
58. 
59. **ComInterop**
60. 
61. COM 接口方法。由于非泛型接口默认可用于 COM 互操作，因此这种类型通常用于所有接口方法。
62. 
63. **Dynamic**
64. 
65. 没有底层元数据的动态创建方法。由 Stub-as-IL 和 LKG（轻量级代码生成）生成。
66. 
67. 替代实现
68. --------
69. 
70. 虚方法和继承将是用 C++ 实现各种 MethodDesc 的自然方式。虚方法会向每个 MethodDesc 添加 vtable 指针，浪费大量宝贵空间。vtable 指针在 x86 上占用 4 个字节。相反，虚拟化是通过基于 MethodDesc 种类（适合 3 位）进行切换来实现的。例如：
71. 
72. ```c++
73. DWORD MethodDesc::GetAttrs()
74. {
75.     if (IsArray())
76.         return ((ArrayMethodDesc*)this)->GetAttrs();
77. 
78.     if (IsDynamic())
79.         return ((DynamicMethodDesc*)this)->GetAttrs();
80. 
81.     return GetMDImport()->GetMethodDefProps(GetMemberDef());
82. }
83. ```
84. 
85. 方法槽 (Method Slots)
86. ---------------------
87. 
88. 每个 MethodDesc 都有一个槽，其中包含方法的当前入口点。所有方法都必须存在该槽，即使是像抽象方法那样从不运行的方法。运行时中有多个位置依赖于入口点和 MethodDescs 之间的映射。
89. 
90. 每个 MethodDesc 逻辑上都有一个入口点，但我们不会在 MethodDesc 创建时急切地分配这些入口点。不变的是，一旦方法被识别为要运行的方法，或者用于虚拟重写，我们将分配入口点。
91. 
92. 槽位于 MethodTable 中或 MethodDesc 本身中。槽的位置由 MethodDesc 上的 `mdcHasNonVtableSlot` 位决定。
93. 
94. 对于需要通过槽索引进行有效查找的方法（例如虚方法或泛型类型上的方法），槽存储在 MethodTable 中。在这种情况下，MethodDesc 包含槽索引以允许快速查找入口点。
95. 
96. 否则，槽是 MethodDesc 本身的一部分。这种安排提高了数据局部性并节省了工作集。此外，对于动态创建的 MethodDesc（例如由编辑并继续添加的方法、泛型方法的实例化或 [动态方法](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Reflection/Emit/DynamicMethod.cs)），甚至并不总是能够预先在 MethodTable 中预分配槽。
97. 
98. MethodDesc 块 (Chunks)
99. ----------------------
100. 
101. MethodDesc 按块分配以节省空间。多个 MethodDesc 往往具有相同的 MethodTable 和元数据标记的高位。MethodDescChunk 是通过将公共信息提升到多个 MethodDesc 数组的前面而形成的。MethodDesc 仅包含其自身在数组中的索引。
102. 
103. ![Figure 1](images/methoddesc-fig1.png)
104. 
105. 图 1 MethodDescChunk 和 MethodTable
106. 
107. 调试
108. ----
109. 
110. 以下 SOS 命令对于调试 MethodDesc 非常有用：
111. 
112. - **DumpMD** – 转储 MethodDesc 内容：
113. 
114. 		!DumpMD 00912fd8
115. 		Method Name: My.Main()
116. 		Class: 009111ec
117. 		MethodTable: 00912fe8md
118. 		Token: 06000001
119. 		Module: 00912c14
120. 		IsJitted: yes
121. 		CodeAddr: 00ca0070
122. 
123. - **IP2MD** – 查找给定代码地址的 MethodDesc：
124. 
125. 		!ip2md 00ca007c
126. 		MethodDesc: 00912fd8
127. 		Method Name: My.Main()
128. 		Class: 009111ec
129. 		MethodTable: 00912fe8md
130. 		Token: 06000001
131. 		Module: 00912c14
132. 		IsJitted: yes
133. 		CodeAddr: 00ca0070
134. 
135. - **Name2EE** – 查找给定方法名称的 MethodDesc：
136. 
137. 		!name2ee hello.exe My.Main
138. 		Module: 00912c14 (hello.exe)
139. 		Token: 0x06000001
140. 		MethodDesc: 00912fd8
141. 		Name: My.Main()
142. 		JITTED Code Address: 00ca0070
143. 
144. - **Token2EE** – 查找给定标记的 MethodDesc（用于查找具有奇怪名称的方法的 MethodDesc）：
145. 
146. 		!token2ee hello.exe 0x06000001
147. 		Module: 00912c14 (hello.exe)
148. 		Token: 0x06000001
149. 		MethodDesc: 00912fd
150. 		8Name: My.Main()
151. 		JITTED Code Address: 00ca0070
152. 
153. - **DumpMT** – MD – 转储给定 MethodTable 中的所有 MethodDesc：
154. 
155. 		!DumpMT -MD 0x00912fe8
156. 		...
157. 		MethodDesc Table
158. 		   Entry MethodDesc      JIT Name
159. 		79354bec   7913bd48   PreJIT System.Object.ToString()
160. 		793539c0   7913bd50   PreJIT System.Object.Equals(System.Object)
161. 		793539b0   7913bd68   PreJIT System.Object.GetHashCode()
162. 		7934a4c0   7913bd70   PreJIT System.Object.Finalize()
163. 		00ca0070   00912fd8      JIT My.Main()
164. 		0091303c   00912fe0     NONE My..ctor()
165. 
166. 在调试版本中，MethodDesc 具有包含方法名称和签名的字段。这对于在运行时状态严重损坏且 SOS 扩展不起作用时进行调试非常有用。
167. 
168. 预代码 (Precode)
169. ================
170. 
171. 预代码是一小段代码，用于实现临时入口点和存根的高效包装器。预代码是针对这两种情况的小众代码生成器，生成尽可能高效的代码。在理想的世界中，运行时动态生成的所有本机代码都将由 JIT 生成。考虑到这两种情况的具体要求，这种情况下这是不可行的。x86 上的基本预代码可能如下所示：
172. 
173. 	mov eax,pMethodDesc // Load MethodDesc into scratch register
174. 	jmp target          // Jump to a target
175. 
176. **高效的存根包装器：** 某些方法（例如 P/Invoke、委托调用、多维数组设置器和获取器）的实现由运行时提供，通常作为手写的汇编存根。预代码提供了存根上的空间高效包装器，以便为多个调用者复用它们。
177. 
178. 存根的工作代码由预代码片段包装，该片段可以映射到 MethodDesc 并跳转到存根的工作代码。这样，存根的工作代码可以在多个方法之间共享。这是用于实现 P/Invoke 编组存根的重要优化。它还在 MethodDescs 和入口点之间建立了 1:1 映射，从而建立了一个简单高效的低级系统。
179. 
180. **临时入口点：** 方法必须在 JIT 编译之前提供入口点，以便 JIT 编译的代码具有调用它们的地址。这些临时入口点由预代码提供。它们是存根包装器的一种特殊形式。
181. 
182. 这种技术是 JIT 编译的一种延迟方法，它在空间和时间上都提供了性能优化。否则，方法的传递闭包需要在执行之前进行 JIT 编译。这将是一种浪费，因为只有执行的代码分支（例如 if 语句）的依赖项才需要 JIT 编译。
183. 
184. 每个临时入口点都比典型的方法体小得多。它们需要很小，因为即使以性能为代价，它们的数量也很多。临时入口点在生成方法的实际代码之前仅执行一次。
185. 
186. 临时入口点的目标是 PreStub，这是一种触发方法 JIT 编译的特殊类型的存根。它原子地将临时入口点替换为稳定的入口点。稳定的入口点必须在方法生命周期内保持不变。这是保证线程安全所必需的不变性，因为方法槽总是在不采用任何锁定的情况下访问。
187. 
188. **稳定的入口点**是本机代码或预代码。**本机代码**是 JIT 编译的代码或保存在 NGen 映像中的代码。当我们实际上是指本机代码时，通常会说 JIT 编译的代码。
189. 
190. ![Figure 2](images/methoddesc-fig2.png)
191. 
192. 图 2 入口点状态图
193. 
194. 如果在执行实际方法体之前需要进行工作，则方法可以同时拥有本机代码和预代码。这种情况通常发生在 NGen 映像修复中。在这种情况下，本机代码是一个可选的 MethodDesc 槽。这是以廉价统一的方式查找方法的本机代码所必需的。
195. 
196. ![Figure 3](images/methoddesc-fig3.png)
197. 
198. 图 3 Precode、Stub 和 Native Code 最复杂的情况
199. 
200. 单次调用与多次调用入口点
201. ------------------------
202. 
203. 调用方法需要入口点。MethodDesc 公开了封装逻辑的方法，以获取给定情况下的最有效入口点。关键区别在于入口点是仅用于调用方法一次，还是用于多次调用方法。
204. 
205. 例如，使用临时入口点多次调用方法可能是一个坏主意，因为它每次都会经过 PreStub。另一方面，使用临时入口点仅调用一次方法应该没问题。
206. 
207. 从 MethodDesc 获取可调用入口点的方法有：
208. 
209. - `MethodDesc::GetSingleCallableAddrOfCode`
210. - `MethodDesc::GetMultiCallableAddrOfCode`
211. - `MethodDesc::TryGetMultiCallableAddrOfCode`
212. - `MethodDesc::GetSingleCallableAddrOfVirtualizedCode`
213. - `MethodDesc::GetMultiCallableAddrOfVirtualizedCode`
214. 
215. 预代码的类型
216. ------------
217. 
218. 有多种专门的预代码类型。
219. 
220. 预代码的类型必须能够从指令序列中廉价地计算出来。在 x86 和 x64 上，预代码的类型是通过获取常量偏移量处的字节来计算的。当然，这对用于实现各种预代码类型的指令序列施加了限制。
221. 
222. **StubPrecode**
223. 
224. StubPrecode 是基本的预代码类型。它将 MethodDesc 加载到暂存寄存器<sup>2</sup>中，然后跳转。为了使预代码工作，必须实现它。当没有其他专门的预代码类型可用时，它用作回退。
225. 
226. 所有其他预代码类型都是可选的优化，平台特定文件通过 HAS\_XXX\_PRECODE 定义开启。
227. 
228. StubPrecode 在 x86 上如下所示：
229. 
230. 	mov eax,pMethodDesc
231. 	mov ebp,ebp // dummy instruction that marks the type of the precode
232. 	jmp target
233. 
234. "target" 最初指向 prestub。它被修补为指向最终目标。最终目标（存根或本机代码）可能会也可能不会使用 eax 中的 MethodDesc。存根经常使用它，本机代码不使用它。
235. 
236. **FixupPrecode**
237. 
238. 当最终目标不需要暂存寄存器<sup>2</sup>中的 MethodDesc 时，使用 FixupPrecode。FixupPrecode 通过避免将 MethodDesc 加载到暂存寄存器中来节省几个周期。
239. 
240. 大多数使用的存根都是更高效的形式，如果不要求专门形式的 Precode，目前我们可以将此形式用于除互操作方法之外的所有内容。
241. 
242. x86 上 FixupPrecode 的初始状态：
243. 
244. 	call PrecodeFixupThunk // This call never returns. It pops the return address
245. 	                       // and uses it to fetch the pMethodDesc below to find
246. 	                       // what the method that needs to be jitted
247. 	pop esi // dummy instruction that marks the type of the precode
248. 	dword pMethodDesc
249. 
250. 一旦它被修补为指向最终目标：
251. 
252. 	jmp target
253. 	pop edi
254. 	dword pMethodDesc
255. 
256. <sup>2</sup> 在暂存寄存器中传递 MethodDesc 有时被称为 **MethodDesc 调用约定**。
257. 
258. **ThisPtrRetBufPrecode**
259. 
260. ThisPtrRetBufPrecode 用于为返回值类型的开放实例委托切换返回缓冲区和 this 指针。它用于将 MyValueType Bar(Foo x) 的调用约定转换为 MyValueType Foo::Bar() 的调用约定。
261. 
262. 此预代码始终作为实际方法入口点的包装器按需分配，并存储在表 (FuncPtrStubs) 中。
263. 
264. ThisPtrRetBufPrecode 如下所示：
265. 
266. 	mov eax,ecx
267. 	mov ecx,edx
268. 	mov edx,eax
269. 	nop
270. 	jmp entrypoint
271. 	dw pMethodDesc
272. 
273. **PInvokeImportPrecode**
274. 
275. PInvokeImportPrecode 用于非托管 P/Invoke 目标的延迟绑定。此预代码是为了方便并减少平台特定的管道代码量。
276. 
277. 除了常规预代码外，每个 PInvokeMethodDesc 都有 PInvokeImportPrecode。
278. 
279. PInvokeImportPrecode 在 x86 上如下所示：
280. 
281. 	mov eax,pMethodDesc
282. 	mov eax,eax // dummy instruction that marks the type of the precode
283. 	jmp PInvokeImportThunk // loads P/Invoke target for pMethodDesc lazily
284.