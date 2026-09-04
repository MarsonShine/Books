# Agent 的上下文不是数据库：从 Context Window 到 Context Runtime

这四个问题表面上分别在问上下文压缩、Skills 发现、长上下文推理和 KV Cache；真正的共同点却是：我们是不是把太多职责都塞给了 Context Window。

一个 Agent 运行起来以后，系统里同时存在几类完全不同的信息：完整的对话和工具轨迹、跨会话的长期知识、当前任务的进度和决策、已经执行过的副作用操作、可供发现的工具与 Skill，以及本轮推理临时需要的材料。如果把它们都拼成一段越来越长的 prompt，系统当然可以先跑起来，但很快就会遇到失忆、重复执行、工具选择错误、上下文污染和缓存命中率下降。

更稳妥的出发点是：

> **Context Window 只是一次推理的工作内存。它不是数据库、日志系统，也不是状态机。**

这不是把 Context 降级，而是把职责放回正确的位置。模型只需要在当前时刻看到完成下一步所需的最小高信号信息；其余信息由外部 Runtime 保存、检索、校验，再编译成当前推理的上下文。

## 先把几种“记忆”分开

可以先用下面的划分建立边界：

| 组件 | 它保存什么 | 它解决什么问题 |
| --- | --- | --- |
| Context Window | 当前推理要看的指令、状态、历史片段和工具结果 | 让模型现在能做出下一步决策 |
| Event Store | 完整、追加写入的对话、Tool Call、Tool Result 和生命周期事件 | 审计、重放、调试和恢复 |
| Memory Store | 跨任务仍然有用的事实、偏好、知识和结构化笔记 | 长期记忆和知识复用 |
| Task State | 当前目标、计划、已确认事实、决策、TODO 和未决问题 | 任务连续性和会话交接 |
| Execution Ledger | 某个逻辑操作是否已执行、结果是否仍有效、是否允许重试 | 去重、幂等和副作用控制 |
| Capability Registry | 工具、Skill、输入输出类型、触发条件和版本 | 能力发现和按需加载 |

于是，Agent 不再是“把全部历史塞给模型”，而是一个循环：

```text
完整事件 / 长期记忆 / 任务状态 / 能力注册表
                    │
                    ▼
             Context Compiler
                    │
                    ▼
              当前工作集 Context
                    │
                    ▼
                    LLM
                    │
          提出计划、回答或 Tool Call
                    │
                    ▼
       Runtime 校验、执行、记录并更新状态
```

下面分别看四个问题。这四部分不是四套孤立的技巧，而是同一套 Context Runtime 的不同侧面。

## 一、如何同时处理滑动窗口、工具重复调用、上下文膨胀和 KV Cache

### 滑动窗口解决的是 token 数，不是任务连续性

最简单的策略是只保留最近 N 轮对话。它能控制长度，却无法回答几个关键问题：

- 被裁掉的历史里是否包含已经确认的约束？
- 某个工具是否已经执行过？
- 一个旧的工具结果现在是否仍然有效？
- 当前 TODO 是什么，下一步依赖哪些事实？

相反，持续重写一份 Summary 也不理想。Summary 每轮变化时，变化点之后的 token 前缀就不再相同；基于前缀的 KV Cache 只能复用变化点之前的部分。缓存并不是“只要内容大致相似就能复用”，而是依赖实际的 token 前缀相同。vLLM 的 Automatic Prefix Caching 和 OpenAI 的 Prompt Caching 都是这个基本模型的具体实现或产品化形式。

因此，正确的问题不是“历史要不要删除”，而是“哪些历史应该进入本轮的 Context 视图”。原始历史可以继续保存在外部 Event Store，Context 只拿它的一个工作集投影。

### 用四层 Context 组织一次推理

一个实用的布局可以分成四层：

```text
┌─────────────────────────────────────────────┐
│ L0 Stable Prefix                            │
│ Agent 身份、策略、基础协议、输出契约          │
│ 长时间保持不变                                │
├─────────────────────────────────────────────┤
│ L1 Frozen Checkpoint                        │
│ 当前任务状态、已确认事实、决策、TODO           │
│ 一个 Epoch 内保持不变                         │
├─────────────────────────────────────────────┤
│ L2 Working Set                              │
│ 最近对话、当前步骤依赖的事实和少量相关历史       │
├─────────────────────────────────────────────┤
│ L3 Ephemeral Context                        │
│ 本轮检索结果、临时 Skill、Tool Result、当前时间  │
└─────────────────────────────────────────────┘
```

L0 是适合缓存的稳定前缀；L1 是让任务跨越上下文重建仍能继续的状态快照；L2 是模型真正工作的近期材料；L3 是随请求变化最快、最容易被清除的临时信息。

这里的关键设计是 **Epoch Checkpoint**。不要每一轮都重写 Summary，而是在达到某个边界时生成一次新的 Checkpoint，例如：Context 使用率接近预算、近期历史超过一定长度，或者任务完成了一个阶段。

```text
Epoch 17
Stable Prefix
Checkpoint #17
Turn 1 ... Turn 12

        ↓ 到达压缩边界，只生成一次

Epoch 18
Stable Prefix
Checkpoint #18
Turn 13 ...
```

这样做不是为了永远不让缓存失效，而是把“每轮都改变前缀”变成“每个阶段改变一次前缀”。Checkpoint 不应只是模糊的自然语言摘要，至少应该保留：

- 当前目标和完成判定；
- 已确认事实及其证据引用；
- 已做出的决策和被排除的方案；
- 未完成的 TODO、阻塞项和下一步依赖；
- 重要 Artifact、文件或外部资源的引用；
- 不需要再次注入 Context 的旧 Tool Result 的 `ResultRef`。

### 不要让模型负责记住“某个工具已经执行过”

工具是否重复执行，不能依赖模型是否还看得到旧消息。应该在 Runtime 里单独维护 Execution Ledger。

```json
{
  "intent_hash": "...",
  "tool_name": "get_user_profile",
  "arguments_hash": "...",
  "status": "succeeded",
  "result_ref": "result://...",
  "resource_version": "user-123:v8",
  "executed_at": "...",
  "valid_until": "...",
  "idempotency_key": "..."
}
```

调用链可以是：

```text
LLM 提出 Tool Call
        │
        ▼
Runtime 对工具名和规范化参数计算 fingerprint
        │
        ▼
查询 Execution Ledger
   ├─ 已执行且结果仍有效：复用 ResultRef
   ├─ 正在执行：等待或共享同一个结果
   └─ 没有记录：执行工具并写入 Ledger
```

读操作和副作用操作的策略不同：

- Search、GET、读取配置等操作可以结合 TTL、资源版本或 ETag 复用结果；
- SendEmail、CreateOrder、扣款、删除文件等操作必须使用幂等键，并由目标系统配合去重；
- 如果外部 API 不支持幂等，而进程恰好在“副作用已经发生、Ledger 还没写入”之间崩溃，Ledger 本身不能凭空创造 exactly-once 语义。

所以更准确的原则是：

> **事实记忆可以允许不完美的召回；执行记忆必须由确定性的 Runtime 保护。**

## 二、Skills 的渐进式披露，如何应对“我不知道自己不知道什么”

渐进式披露是必要的：启动时只加载 Skill 的名称和描述，需要时再读取完整的 `SKILL.md`，更深的参考资料继续按需读取。这能避免一次性把所有规则塞进 Context，也能让 Skill 具备可扩展性。

但它有一个前提：模型必须先知道某个 Skill 可能相关。如果任务描述没有出现“PDF”“表格”这样的明显词语，或者模型不知道某个特殊领域存在专用 Skill，它就不会主动读取。这不是简单的提示词问题，而是能力发现的召回问题。

因此，Skill Discovery 不应该是：

```text
LLM 猜测是否存在相关 Skill
        ↓
决定是否加载
```

而应该是：

```text
硬规则触发
        +
高召回 Router
        +
LLM 精排
        +
运行时失败后的能力扩展
```

### 第一层：建立便宜的 Skill Manifest

Manifest 不放完整教程，只描述能力边界和触发线索：

```text
skill_id
name
capabilities
task_patterns
required_when
negative_patterns
input_types
output_types
keywords
examples
version
```

例如 PDF Skill 的 Manifest 可以声明：

```text
capabilities: 修改、合并、填写、生成 PDF
required_when: 用户要求产生或修改 PDF 文件
input_types: PDF、图片、表单数据
negative_patterns: 只要求提取纯文本且不涉及版式
```

Runtime 可以用文件扩展名、MIME 类型、规则、BM25、Embedding 或轻量分类器召回 Top-K。这里目标是高召回，而不是一次就精准命中。多给模型两个候选 Skill 的成本，通常低于漏掉一个有关键约束的 Skill。

### 第二层：让模型在候选集合里做判断

模型面对的应该是：

```text
可能相关：PDF、Spreadsheet、Document
```

而不是让它回答“宇宙中是否存在一个我尚未见过的能力”。候选生成和候选选择是两个不同的问题，前者更适合由外部系统承担。

### 第三层：对前置能力使用硬触发

有些能力不需要模型决定：

```text
.pdf  → 确保 PDF Skill 可用
.xlsx → 确保 Spreadsheet Skill 可用
.pptx → 确保 Slides Skill 可用
部署到 Netlify → 确保 Netlify 部署能力可用
```

这类规则属于 capability prerequisite。它们不代表模型一定要使用该能力，只代表 Runtime 不能让模型在缺少必要契约的情况下自行猜测。

### 第四层：从失败信号反推能力缺口

第一次没有加载正确 Skill 并不意味着系统已经结束。Runtime 可以监听这些信号：

- 工具调用返回“缺少必要参数”或“格式不支持”；
- 模型开始自己猜测未知 API 的字段；
- 计划中出现 Manifest 没有声明的能力；
- 连续重试同一种失败操作；
- 输出中出现大量“可能”“应该是”，但任务需要确定的契约。

这些信号可以触发 Capability Gap Detector，再次检索 Skill 或 Tool Registry。

所以，“模型需要知道自己不知道什么”不应该被当作一个必须由模型独自解决的元认知问题。更可操作的改写是：

> **让外部系统负责能力召回，让模型负责候选判断，让 Runtime 负责前置条件和失败恢复。**

这也解释了为什么 Agent Skills 的 progressive disclosure 和大规模 Tool Search 是同一个方向：都在把“完整能力集合”放到模型上下文之外，再通过索引和按需加载把相关部分带进来。

## 三、“上下文学习本质是检索而不是推理”到底对不对

这个说法需要修正。

如果“上下文学习”指模型读入上下文后完成归纳、比较、规划和推理，那么它当然不只是检索；模型仍然在进行真实的组合和推断。但如果讨论的是长上下文 Agent 的系统瓶颈，这句话抓住了一个重要事实：**信息没有被正确寻址，后面的推理能力就没有施展空间。**

可以把信息进入行动的过程拆成五步：

```text
信息存在
   ↓
信息被寻址 / 找到
   ↓
信息被理解
   ↓
信息参与推理
   ↓
推理结果被正确执行
```

“信息在 Context 里”只证明了第一步，不等于它被模型有效找到，更不等于它被理解和使用。`Lost in the Middle` 一类研究显示，在信息内容不变的情况下，仅仅改变相关信息在长上下文中的位置，也可能改变模型表现。这说明 Context Size 不等于 Effective Memory。

因此，更准确的判断是：

> **长上下文 Agent 首先受制于信息寻址，其次才受制于推理；但寻址和推理会交替发生，而不是互相替代。**

### 用分层内存理解 Agent Context

把 Context 看成 CPU Cache 是一个有用但不完全等价的类比：

| 层级 | 典型内容 | 特征 |
| --- | --- | --- |
| Context Window | 当前工作集 | 容量有限，访问快，直接影响本轮行为 |
| Task Memory | Checkpoint、结构化笔记、任务状态 | 容量更大，按任务加载 |
| Knowledge Index | 向量索引、倒排索引、知识图谱 | 负责定位相关信息 |
| Event Store | 完整轨迹和原始结果 | 适合审计、重放和恢复 |
| 外部系统 | 数据库、文件系统、互联网和业务 API | 信息最丰富，但访问成本和不确定性更高 |

这个类比的重点不是给每一层强行规定某种存储技术，而是提醒我们：不要让最昂贵、最有限的那一层承担所有存储工作。

### 从 RAG 走向 Context Compiler

传统 RAG 常常以 Query 为中心：找和问题语义相似的文本，再把结果放进 prompt。对长任务来说，这还不够，因为当前步骤的依赖关系比整句 Query 的相似度更重要。

更完整的编译输入应该包括：

```text
Current Goal
+ Current Plan
+ Current Step
+ Current State
+ Missing Information
+ Dependency Graph
+ Evidence Requirements
```

输出的不是“最相似的几段文本”，而是完成当前步骤所需的 Task-specific Context。记忆也应该尽可能结构化，例如：

```text
Facts
Constraints
Decisions
OpenQuestions
Artifacts
ToolExecutions
Entities
Dependencies
Evidence
```

于是 Agent 的工作循环变成：

```text
Retrieve → Reason → Write State → Retrieve → Reason
```

每次推理不只消费上下文，还要把新的事实、决策、结果引用和未决问题写回外部状态。这样下一次上下文重建时，Runtime 可以根据当前 Step 的依赖重新编译工作集，而不必把整个历史重新播放给模型。

## 四、工具、时间戳和动态环境如何尽量不破坏 KV Cache

这里可以先记住一个布局原则：

> **越稳定的内容越靠前，越动态的内容越靠后；能不进入 prompt 的动态数据，就不要进入 prompt。**

一个适合前缀缓存的逻辑布局如下：

```text
1. Immutable Core
   Agent 身份、安全边界、核心协议、输出契约
2. Stable Infrastructure Tools
   search_capabilities、load_skill、invoke_tool、read_memory、write_memory
3. Frozen Task Checkpoint
4. 稳定的任务指令与近期工作集
5. 本次按需加载的 Skill 和 Tool Schema
6. 动态运行时信息
   时间、搜索结果、临时状态、短期 Tool Result
```

准确的消息排列还要服从具体模型 API 的协议，但一般原则不变：把稳定内容组成尽可能长的公共前缀，把请求级变量放到后面。

### 不要把一千个业务工具全部放进 System Prompt

把所有工具定义预先注入 Context，会同时造成三种问题：token 开销、工具选择歧义和缓存前缀不稳定。更适合的方式是只暴露少量稳定的 Meta Tools：

```text
search_capabilities(query)
get_tool_schema(tool_id)
invoke_tool(tool_id, arguments)
search_skills(query)
load_skill(skill_id)
```

GitHub、Slack、Jira、Google Drive 或内部业务 API 的完整定义放在外部 Registry。模型先搜索能力，再按需加载具体 Schema，最后通过统一入口调用。Anthropic 的 Tool Search 采用的就是类似思路：延迟加载的工具不会进入初始上下文，核心系统提示和基础工具因此更容易保持可缓存。

不过，Tool Search 不是所有场景都值得引入。工具数量很少、所有工具都会被频繁使用，或者一次搜索的延迟高于省下的上下文成本时，直接加载反而更简单。它应该由测量结果驱动，而不是成为新的固定模板。

### 时间应该是工具，而不是每次变化的 System Prompt 字段

如果每次请求都把秒级时间戳写入 System Prompt：

```text
System + 17:24:31
System + 17:24:32
```

从时间戳开始的前缀就不同了。更好的做法是提供 `get_current_time()`，只在任务涉及“今天”“是否过期”“当前营业时间”或“距离会议还有多久”时调用。

同理，Request ID、Trace ID、服务器版本、Token 统计和随机数如果只是 Runtime 内部使用，就不要让模型看到。它们只有在需要模型参与诊断、关联外部资源或生成用户可见结果时，才应该以明确的字段进入 Context。

### 缓存优化应该被当作一个可观测指标

“把稳定内容放前面”只是布局策略，最终还要观察：

- 每轮命中了多少 `cached_tokens`；
- 前缀在哪个组件发生了变化；
- Prefill 延迟和生成延迟分别是多少；
- 工具 Schema、Checkpoint 和动态结果各自占了多少 token；
- 为了节省缓存是否引入了额外的搜索延迟或错误率。

KV Cache 主要帮助重复前缀的 prefill。它不能替代上下文筛选，也不能消除长输出本身的生成成本。真正的目标不是“缓存命中率越高越好”，而是在准确性、延迟、成本和状态新鲜度之间取得可测量的平衡。

## 四个问题最后会汇合到 Harness

如果只看 prompt，我们会把上述问题描述成“如何让模型记住更多、知道更多、调用更多工具”。如果从 Harness 看，问题会变成“Runtime 如何为模型提供一个可恢复、可验证、可控的工作环境”。

这正是 Harness Engineering 和单纯 Prompt Engineering 的分界线。可以把四个问题映射到一组更大的系统职责：

| 需求 | 不应主要依赖 | 应由 Harness / Runtime 提供 |
| --- | --- | --- |
| 任务跨上下文继续 | 完整聊天记录 | 持久化 State、Checkpoint 和 Session Handoff |
| 不重复执行副作用 | 模型记忆旧 Tool Call | Execution Guard、Ledger 和幂等 API |
| 发现未知能力 | 模型的自我意识 | Manifest、Router、Registry 和失败恢复 |
| 长上下文仍然可用 | 无限增大 Context | Context Compiler、分层记忆和按需检索 |
| 结果确实完成 | 模型说“完成了” | 测试、验证、Smoke Run 和可观察证据 |
| 长任务不失控 | 一个无限循环 | Scope、生命周期、阶段边界和人工接管点 |

`learn-harness-engineering` 把可靠的 Agent 环境归纳为 Instructions、State、Verification、Scope 和 Session Lifecycle 五个子系统；这个划分很适合拿来检查上面的设计是否缺了一块。`ai-agent-book` 则从“Agent = LLM + 上下文 + 工具”出发，把上下文工程、记忆、工具、评估和持续进化放到同一个工程体系里。两条线合在一起看，会发现 Context Runtime 只是 Harness 的核心部件之一，Harness 还要负责状态边界、执行约束和结果验证。

## 一个可落地的最小架构

如果要把这套思路实现成系统，可以按下面的顺序拆分，而不必一开始就建设一个复杂的“Agent Operating System”：

1. 先把 Event Store、Task State 和 Execution Ledger 分开；至少不要让它们共享一段不可解析的 Summary。
2. 定义 Context Compiler 的输入和输出，明确每一轮为什么加载某条记忆、某个工具或某个 Skill。
3. 为高风险 Tool Call 增加规范化参数、资源版本、幂等键和结果引用。
4. 为 Skill 和 Tool 建立 Manifest / Registry，先做高召回检索，再让模型选择和精排。
5. 固定 Stable Prefix，把动态时间、搜索结果和临时 Schema 放到后面，并记录缓存命中指标。
6. 最后补上 Verification、Scope 和 Session Lifecycle，让 Agent 只能在证据满足时宣布完成。

这套顺序的好处是，每一步都对应一个可观察的失败模式：状态丢失、重复执行、能力漏召回、缓存失效或错误完成。系统可以针对失败增加机制，而不是靠继续加长 System Prompt 来碰运气。

## 结语：把 Context 从“世界的副本”变成“当前的工作集”

四个问题的共同答案可以浓缩成一句话：

> **下一代 Agent 的关键不是把所有东西都放进更大的上下文，而是把 Context 变成一个由 Memory、Cache、Scheduler、Registry、Execution Log 和 Verification 共同管理的计算工作集。**

模型负责在当前工作集中理解、推理和提出行动；Runtime 负责保存它看不到的世界、决定下一步给它什么、阻止危险的重复执行，并用可运行的证据判断任务是否真的完成。

这也是从 Prompt Engineering 走向 Context Engineering，再走向 Context Runtime / Agent Harness 的自然演进： prompt 仍然重要，但它只是整个系统对模型可见的那一小部分。

## 参考资料

- [深入理解 AI Agent：设计原理与工程实践](https://github.com/bojieli/ai-agent-book)
- [Learn Harness Engineering](https://github.com/walkinglabs/learn-harness-engineering)
- [Effective context engineering for AI agents](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)，Anthropic
- [Equipping agents for the real world with Agent Skills](https://www.anthropic.com/engineering/equipping-agents-for-the-real-world-with-agent-skills)，Anthropic
- [Introducing advanced tool use on the Claude Developer Platform](https://www.anthropic.com/engineering/advanced-tool-use)，Anthropic
- [Automatic Prefix Caching](https://docs.vllm.ai/en/latest/features/automatic_prefix_caching/)，vLLM
- [Prompt Caching in the API](https://openai.com/index/api-prompt-caching/)，OpenAI
- [Lost in the Middle: How Language Models Use Long Contexts](https://arxiv.org/abs/2307.03172)
- [MemGPT: Towards LLMs as Operating Systems](https://arxiv.org/abs/2310.08560)
