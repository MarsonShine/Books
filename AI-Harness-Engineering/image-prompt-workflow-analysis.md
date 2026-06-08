# AI Prompt 工程化设计最佳实践

> 一份面向软件工程师的 Prompt 设计方法论，适用于任何需要系统化、工程化提升 LLM 输出质量的场景。
> 涵盖从简单问答到复杂的多阶段生成流水线的通用原则。

---

## 目录

1. [核心理念：把 Prompt 当作代码](#1-核心理念把-prompt-当作代码)
2. [原则一：Plan-and-Prompt 分离](#2-原则一plan-and-prompt-分离)
3. [原则二：多阶段流水线](#3-原则二多阶段流水线)
4. [原则三：Schema 即约束](#4-原则三schema-即约束)
5. [原则四：Prompt 模块化组装](#5-原则四prompt-模块化组装)
6. [原则五：代码覆写 LLM 输出](#6-原则五代码覆写-llm-输出)
7. [原则六：输入分类 + 模板路由](#7-原则六输入分类--模板路由)
8. [原则七：约束排序与密度控制](#8-原则七约束排序与密度控制)
9. [原则八：防御性降级](#9-原则八防御性降级)
10. [原则九：可观测性设计](#10-原则九可观测性设计)
11. [反模式清单](#11-反模式清单)
12. [实施检查清单](#12-实施检查清单)

---

## 1. 核心理念：把 Prompt 当作代码

大多数开发者与 LLM 交互时，是把 prompt 当作"自然语言对话"来写的：

```
❌ "帮我生成一张图片，内容是一个苹果，风格可爱"
```

工程化的做法是把 prompt 当作**代码**来管理——有输入、有处理逻辑、有输出格式、有错误处理：

```
✅ 输入 → 分类 → 路由到对应模板 → 结构化参数注入 → 组装 → 最终 prompt
```

> **核心心态转变**：你不是在和 AI "聊天"，你是在**编程驱动 AI**。

这引出了第一条也是最根本的原则。

---

## 2. 原则一：Plan-and-Prompt 分离

### 问题

当 LLM 直接生成最终 prompt 时，你无法控制：
- 哪些内容应该出现、哪些不应该
- 约束是否被遵守
- 输出格式是否一致

### 模式

```
┌──────────────────────────────────────────────────┐
│                  你的代码                          │
│                                                   │
│   输入 ──→ Plan阶段(LLM) ──→ 结构化结果            │
│                  │                                │
│                  ▼                                │
│           Build阶段(纯代码)                         │
│                  │                                │
│                  ▼                                │
│            最终 Prompt                             │
│                  │                                │
│                  ▼                                │
│          执行阶段(LLM / 图像模型 / ...)             │
│                                                   │
└──────────────────────────────────────────────────┘
```

- **Plan 阶段**（LLM）：将高层次需求转为**结构化 JSON**。LLM 只做"语义转换"。
- **Build 阶段**（纯代码）：将结构化规格**确定性**地组装成最终 prompt。不经过 LLM。
- **Execute 阶段**（目标模型）：将最终 prompt 发送给图像/文本/代码模型。

### 为什么这样做？

| 维度 | LLM 直接生成 prompt | Plan-and-Prompt 分离 |
|------|---------------------|----------------------|
| 可控性 | 低——LLM 可能忽略约束 | 高——代码控制最终 prompt 的每个词 |
| 可调试性 | 低——不知道是"理解错了"还是"表达错了" | 高——可以分别检查 Plan 和 Final Prompt |
| 约束遵守 | 不可靠——LLM 经常"忘记"负向约束 | 代码保证约束一定出现在最终输出中 |
| 一致性 | 低——每次输出格式可能不同 | 代码保证输出格式始终一致 |
| 可测试性 | 难——只能端到端测试 | 易——Build 阶段可单独单元测试 |

### 代码示例

```python
# ❌ 反模式：让 LLM 直接生成最终 prompt
response = llm.chat(f"Generate an image prompt for: {user_input}")
final_prompt = response.text  # 不可控

# ✅ Plan-and-Prompt 分离
plan = llm.chat_json(
    system="Extract visual semantics from the input as JSON.",
    user=user_input
)
# plan = {"mainSubject": "a red apple", "style": "flat illustration", ...}

final_prompt = build_prompt(plan)  # 纯代码，确定性
# "A simple flat illustration of a red apple. Clean lines, centered..."
```

### 适用场景

- 任何需要精确控制最终 prompt 的场景（文生图、文生视频、代码生成、文档生成）
- 需要遵守敏感词、合规、品牌安全规则的场景
- 需要支持 A/B 测试 prompt 变体的场景
- Agent / Tool-calling 的中间步骤规划

---

## 3. 原则二：多阶段流水线

### 问题

单一 LLM 调用试图完成"理解输入 + 规划内容 + 格式化输出"三件事，每一步的失败都会污染下一步。

### 模式

将复杂任务拆分成**独立、可替换、可单独测试**的阶段：

```
Stage 1          Stage 2          Stage 3          Stage 4
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  Parse   │───▶│ Classify │───▶│   Plan   │───▶│  Build   │
│  (纯代码) │    │  (纯代码) │    │  (LLM)   │    │ (纯代码)  │
└──────────┘    └──────────┘    └──────────┘    └──────────┘
     输入            类型             规格              最终格式
```

### 每个阶段的职责

| 阶段 | 执行者 | 职责 | 输入 → 输出 |
|------|--------|------|------------|
| Parse | 纯代码 | 提取结构化信息 | 原始字符串 → 结构化对象 |
| Classify | 纯代码 | 判断类型/意图 | 结构化对象 → 分类标签 |
| Plan | LLM | 语义转换、内容规划 | 结构化对象 + 分类 → JSON 规格 |
| Build | 纯代码 | Prompt 组装 | JSON 规格 → 最终 prompt |

### 判断是否需要拆分的经验法则

| 问题 | 答案 |
|------|------|
| 这个判断能用 if/else 做吗？ | → 不要用 LLM，用代码 |
| 这个转换需要理解语义吗？ | → 用 LLM |
| 这个格式化有固定格式要求吗？ | → 用代码，不要依赖 LLM 记忆格式 |
| 这个约束绝对不能违反吗？ | → 用代码硬编码，不交给 LLM |

### 代码示例

```python
# ❌ 反模式：一个 prompt 做所有事
response = llm.chat(f"""
    Analyze this input: {user_input}
    Classify its type
    Plan the visual content
    Output a final image prompt
""")

# ✅ 多阶段流水线
parsed = parse_input(user_input)        # Stage 1: 纯代码解析
category = classify(parsed)             # Stage 2: 纯代码分类
plan = llm_plan(parsed, category)       # Stage 3: LLM 语义规划
prompt = build_prompt(plan, category)   # Stage 4: 纯代码组装
```

---

## 4. 原则三：Schema 即约束

### 核心洞察

> **LLM 输出的 JSON Schema 不只是数据格式——它是对 LLM 行为的最强约束。**

Schema 里有什么字段，LLM 就会去思考什么；Schema 里没有的字段，LLM 就不会考虑。你可以通过**选择性地暴露或隐藏字段**来精确控制 LLM 的注意力范围。

### 实践：按场景使用不同 Schema

```python
# ❌ 反模式：万能 Schema——包含所有场景的字段
universal_schema = {
    "overlayText": "...",        # 字母卡才需要
    "allowVisibleText": True,    # 大部分场景不需要
    "sceneDescription": "...",
    "characterCount": 0,         # 只有人物场景需要
    # ... 10+ 个字段
}
# → LLM 会为所有字段分配注意力，包括不相关的

# ✅ 按场景使用精简 Schema
if category == "letter_card":
    schema = {
        "letter": "...",
        "illustration": "...",
        "allowText": True
    }
elif category == "scene_card":
    schema = {
        "action": "...",
        "setting": "...",
        "mood": "..."
    }
    # 没有 allowText 字段 → LLM 根本不会考虑"要不要写字"
```

### 为什么字段的"存在与否"比"值为 false"更有效？

- 如果 schema 中有 `allowText: false`，LLM 仍然看到了 `allowText` 这个字段名，**已经被提示了"文字"这个概念**
- 如果 schema 中**根本没有** `allowText` 字段，LLM 的注意力完全不会被引导到"文字"方向
- 同样的逻辑适用于任何你不希望 LLM 考虑的维度：政治、暴力、品牌、价格……

### Schema 设计检查清单

- [ ] 每个字段都是**当前任务必需的**吗？不需要的就删掉
- [ ] 字段名是否在**引导正确的思维方向**？（避免 `text`, `label`, `caption` 等词当任务不需要文字时）
- [ ] 是否有**冗余字段**可以用一个字段替代？
- [ ] 字段顺序是否反映了优先级？（LLM 通常更关注前面的字段）

---

## 5. 原则四：Prompt 模块化组装

### 问题

把整个 prompt 写成一个长字符串或一个模板，难以：
- 增删某个约束
- 根据条件切换某一段
- 单独调试某一部分
- 做 A/B 测试

### 模式：Section 化

将 prompt 分解为**语义独立的 Section**，每个 Section 是 List 中的一个元素：

```python
sections = [
    # Section 1: 全局格式（硬约束，前置——最重要！）
    "The output must be a valid JSON array.",

    # Section 2: 风格指令（正向引导）
    "Use professional, concise language.",

    # Section 3: 条件性指令（仅在需要时加入）
    *(["Include citations for each claim."] if require_citations else []),

    # Section 4: 负向约束（放在后面——安全网角色）
    "Do not include personal opinions. Do not speculate.",
]

# 组装：过滤空字符串，用换行符连接
final_prompt = "\n".join(s for s in sections if s)
```

### Section 的组织原则

| 位置 | 内容类型 | 原因 |
|------|----------|------|
| **最前（前25%）** | 全局格式要求、关键硬约束 | LLM 对 prompt 前半部分关注度更高 |
| **中间** | 任务描述、正向引导 | 告诉 LLM "要做什么" |
| **条件** | 根据上下文动态添加的指令 | 按业务逻辑判断是否加入 |
| **靠后** | 负向约束、禁止列表 | 防止稀释主体指令；充当"安全网" |
| **最后** | 输入数据 / 用户内容 | 避免被误解释为指令的一部分 |

### 为什么不用模板引擎？

模板引擎（Jinja / Handlebars）适合"文本填空"，但不适合 prompt 工程：
- 条件逻辑写在模板里会变得难以阅读（`{% if %}` 嵌套）
- 模板不容易做 Section 级别的 A/B 测试
- 模板的空白符控制经常出问题

**推荐做法**：在代码中构建 `list[str]`，最后 `join`。每个 Section 是一行代码，清晰、可调试、可单测。

### 代码示例：条件性 Section

```python
def build_system_prompt(user_role: str, task_type: str, include_examples: bool) -> str:
    sections = ["You are a helpful assistant. Be concise and accurate."]

    # 按角色定制
    if user_role == "expert":
        sections.append("Use technical terminology appropriate for domain experts.")
    elif user_role == "beginner":
        sections.append("Explain concepts in simple terms. Avoid jargon.")

    # 按任务类型定制
    if task_type == "creative":
        sections.append("Be imaginative and explore multiple angles. Up to 500 words.")
    elif task_type == "analytical":
        sections.append("Be rigorous and evidence-based. Cite sources. Up to 300 words.")

    # 可选示例
    if include_examples:
        sections.append("Include 1-2 concrete examples in your response.")

    return "\n\n".join(sections)
```

---

## 6. 原则五：代码覆写 LLM 输出

### 核心原则

> **LLM 提供"建议"，代码做"裁决"。永远不要信任 LLM 输出的权限/安全/合规相关字段。**

LLM 的角色是"内容顾问"——它可以建议 `mainSubject`、`tone`、`style`，但它**无权决定**是否允许文字、是否允许外链、内容是否安全。

### 哪些字段必须代码覆写？

| 字段类别 | 示例 | 为什么不能信任 LLM |
|----------|------|---------------------|
| **权限字段** | `allowText`, `canMentionBrands`, `isPublic` | 安全策略不容 LLM 决定 |
| **长度/数量限制** | `maxWords`, `imageCount` | 成本控制，且 LLM 不擅长数字 |
| **内容安全判断** | 是否包含敏感话题、违规内容 | LLM 的自评不可靠——它经常判断错误 |
| **格式字段** | 输出格式是否匹配预期 schema | 代码比 LLM 更擅长格式校验 |
| **业务规则** | 是否符合年龄分级、地区限制 | 业务逻辑应集中管理，不能散落在 LLM 判断中 |

### 代码示例

```python
# ❌ 危险：信任 LLM 的安全自评
response = llm.chat_json("Generate content and mark it as safe if appropriate.")
if response["is_safe"]:            # ← 不能相信这个值！
    publish(response["content"])

# ✅ 正确：独立的安全检查 + 代码覆写
response = llm.chat_json("Generate content about: " + topic)
plan = response

# 代码覆写——不管 LLM 说了什么
plan["allow_external_links"] = False        # 硬编码安全策略
plan["max_output_tokens"] = 500             # 硬编码成本控制
plan["content"] = safety_filter(plan["content"])  # 独立安全检查
plan["flags"] = run_content_classifier(plan["content"])

if all_checks_pass(plan):
    publish(plan["content"])
```

### 核心心态

```
LLM 的输出 = 建议
代码的覆写 = 裁决
建议可以被采纳，但裁决不容商量。
```

---

## 7. 原则六：输入分类 + 模板路由

### 问题

用一个万能 prompt 处理所有类型的输入，必然在边缘情况翻车。不同类型的输入需要**不同的指令策略**。

### 模式

```
输入 → 分类器(纯代码) → 路由到对应模板 → 组装 prompt
```

分类器**必须用纯代码**（正则、关键词、长度判断），不用 LLM：
- 零延迟、零成本
- 确定性——不会分错类
- 可以记录分到哪个类，用于后续效果分析和 A/B 测试

### 分类维度设计示例

| 业务场景 | 分类维度 | 分类方法 |
|----------|----------|----------|
| 客服机器人 | 意图：投诉 / 咨询 / 售后 | 关键词 + 正则 |
| 内容生成 | 类型：短文本 / 长文 / 代码 / 表格 | 长度 + 特征检测 |
| 翻译 | 语言对 + 领域：技术 / 文学 / 口语 | 语言检测 + 术语库匹配 |
| 图片生成 | 语义粒度：字母 / 单词 / 短语 / 句子 / 抽象 | 单词计数 + 标点检测 |
| 代码审查 | 语言 + 变更类型：新功能 / Bug修复 / 重构 | 文件扩展名 + diff 分析 |

### 代码示例

```python
def classify_input(text: str) -> str:
    """纯代码分类，零 LLM 调用"""
    text = text.strip()

    # 按优先级从特殊到一般
    if re.match(r'^[A-Za-z]$', text):
        return "single_letter"

    word_count = len(text.split())
    has_punctuation = bool(re.search(r'[!?.]', text))

    if word_count >= 4 or has_punctuation:
        return "sentence"

    if word_count >= 2:
        return "phrase"

    if re.match(r'^[A-Za-z]+$', text):
        return "word"

    return "abstract"  # 兜底


# 每类路由到不同的 builder
TEMPLATES = {
    "single_letter": build_letter_prompt,
    "sentence":      build_sentence_prompt,
    "phrase":        build_phrase_prompt,
    "word":          build_word_prompt,
    "abstract":      build_abstract_prompt,
}

def process(input_text: str) -> str:
    category = classify_input(input_text)
    plan = llm_plan(input_text, category)       # LLM 知道分类，可针对性规划
    return TEMPLATES[category](plan)            # 选择对应模板组装
```

---

## 8. 原则七：约束排序与密度控制

### 核心洞察

> **LLM 对 prompt 前半部分的关注度显著高于后半部分。** 最重要的约束应该出现在前 25% 的位置。

### 排序策略

```
┌─────────────────────────────────────────┐
│  1. 角色 / 格式硬约束（最前，最高权重）     │  ← 不可违反的规则
│  2. 核心任务描述（正向引导）              │  ← 告诉 LLM 要做什么
│  3. 风格 / 语气要求                      │  ← 质量要求
│  4. 条件性指令（按需）                    │  ← 场景特化
│  5. 负向约束 / 禁止列表（靠后）           │  ← 安全网，不要放在最前面
│  6. 用户输入数据（最后）                  │  ← 避免被 LLM 误解释为指令
└─────────────────────────────────────────┘
```

### 约束密度的陷阱

堆砌大量负向约束是最常见的错误之一：

```
❌ 过度密集的负向约束：

不要做A。不要做B。不要做C。不要做D。不要做E。不要做F。不要做G。不要做H。
不要做I。不要做J。不要做K。不要做L。不要做M。不要做N。不要做O。不要做P。
```

两个问题：
1. **认知稀释**：约束太多，每条都不突出，LLM 倾向于全部忽略
2. **注意力劫持**：LLM 在处理长否定列表时消耗大量注意力，正向引导被边缘化

### 改进策略

```python
# ❌ 20 条零散的负向约束
negatives = ["Do not do A.", "Do not do B.", ..., "Do not do T."]

# ✅ 精简为 3-5 条分组约束
constraints = [
    # 分组 1: 格式
    "Output must be valid JSON with exactly these fields: [...]",
    # 分组 2: 内容安全（合并同类项）
    "Do not include personal opinions, speculation, unverified claims, or political commentary.",
    # 分组 3: 风格
    "Maintain a neutral, factual tone. Use professional language only.",
]
```

### 约束设计经验法则

| 约束类型 | 建议数量 | 表示方式 |
|----------|----------|----------|
| 硬约束（不可违反） | 1-3 条 | 放在最前面，每条单独一行 |
| 正向引导 | 2-5 条 | 放在中间，描述"要做什么" |
| 负向禁止 | 3-6 条，同类分组 | 放在靠后，同类合并为一句话 |
| 示例（few-shot） | 1-3 个 | 质量 > 数量 |

---

## 9. 原则八：防御性降级

### 问题

任何依赖 LLM 的系统都必须面对一个事实：**LLM 调用可能失败**（超时、限流、格式错误、返回空内容、模型不可用）。

**系统不应因 LLM 故障而完全不可用。**

### 降级策略层次

| 优先级 | 策略 | 适用场景 |
|--------|------|----------|
| 1 | 重试（exponential backoff + jitter） | 临时性故障（限流、网络抖动） |
| 2 | 使用缓存结果（相同输入的之前成功结果） | 幂等操作、重复请求 |
| 3 | 降级到更小/更快的模型（如 GPT-4 → GPT-4o-mini） | 主模型不可用或超预算 |
| 4 | 使用规则引擎替代 LLM（模板 + 关键词匹配） | LLM 完全不可用 |
| 5 | 返回安全的默认值 | 最终兜底 |
| 6 | 返回错误并记录（比返回错误结果更安全） | 无可用降级路径时 |

### 代码示例

```python
async def generate_with_fallback(user_input: str) -> str:
    try:
        # 主路径：完整 Plan + Build
        plan = await llm_plan(user_input)
        return build_prompt(plan)
    except LLMError as e:
        logger.warning(f"LLM plan failed: {e}, degrading to fallback")

        # Fallback: 跳过 Plan 阶段，直接用简化模板
        # 不依赖 LLM，仍能返回可用的 prompt
        category = classify_input(user_input)
        return build_simple_prompt(user_input, category)
```

### 关键原则

- **Fallback 路径必须极简化**——只做必要的最小处理，不要再引入复杂的 LLM 调用链
- **记录每次降级**——用于监控 LLM 服务质量和优化重试策略
- **不要让降级本身成为新的故障点**——Fallback 逻辑应尽量是纯代码

---

## 10. 原则九：可观测性设计

### 问题

Prompt 系统上线后，你如何知道它在"想什么"？哪个阶段出了问题？

### 模式：在每个阶段边界打结构化 Log

```python
# 每个阶段记录输入和输出
logger.info("[Stage:Parse]", extra={
    "trace_id": trace_id,
    "input_snippet": raw_input[:100],
    "output": json.dumps(parsed)
})

logger.info("[Stage:Plan]", extra={
    "trace_id": trace_id,
    "input": json.dumps(plan_input),
    "output": json.dumps(plan),           # ← 最关键！查看 LLM 的规划
    "model": "gpt-4o-mini",
    "latency_ms": elapsed_ms,
})

logger.info("[Stage:Build]", extra={
    "trace_id": trace_id,
    "category": category,
    "final_prompt": final_prompt,          # ← 最关键！查看最终发给模型的 prompt
})

logger.info("[Stage:Execute]", extra={
    "trace_id": trace_id,
    "prompt_length": len(final_prompt),
    "model": "dashscope",
    "latency_ms": elapsed_ms,
})
```

### 为什么 Plan 和 Final Prompt 的 Log 最关键？

| 你看到的 | 能判断什么 |
|----------|-----------|
| Plan 正确，Final Prompt 有问题 | Bug 在 `build_prompt()` 代码逻辑 |
| Plan 错误，Final Prompt 跟着错 | Bug 在 LLM Planner 的 system prompt 或 payload |
| Plan 为空 / 格式错误 | LLM 调用失败——检查网络、限流、模型可用性 |
| Plan 和 Final Prompt 都正确，最终效果差 | 问题在目标模型侧，不是 prompt 问题 |

### 结构化日志的建议字段

```python
log_entry = {
    "trace_id": str(uuid.uuid4()),       # 全链路追踪
    "stage": "plan" | "build" | "execute",
    "category": "word",                   # 分类结果
    "input_snippet": "...",               # 截断后的输入
    "output_snippet": "...",              # 截断后的输出
    "model": "gpt-4o-mini",              # 使用的模型
    "latency_ms": 342,                    # 耗时
    "token_usage": {"input": 120, "output": 45},
    "error": None,                        # 错误信息（如有）
}
```

---

## 11. 反模式清单

以下是 prompt 工程中常见的设计错误，按严重程度排序：

| # | 反模式 | 为什么危险 | 如何修正 |
|---|--------|-----------|---------|
| 1 | **让 LLM 直接生成最终输出** | 不可控，每次结果不一致，约束可能被忽略 | Plan-and-Prompt 分离 |
| 2 | **一个万能 prompt 处理所有输入** | 质量低下，边缘情况不处理 | 分类 + 模板路由 |
| 3 | **在 prompt 中把用户输入和目标指令混在一起** | LLM 可能把用户输入当指令执行（prompt injection） | 用户输入放最后，用分隔符隔开 |
| 4 | **信任 LLM 的安全/权限自评** | 不安全——LLM 容易在自评中出错 | 代码独立校验 |
| 5 | **负向约束放在 prompt 最前面** | 稀释正向引导，LLM 被"不要做X"占据注意力 | 正向引导在前，负向约束靠后 |
| 6 | **堆砌 20+ 条零散的禁止规则** | 认知稀释——每条都不突出，LLM 倾向于全部忽略 | 精简至 3-6 条，同类合并为一句话 |
| 7 | **Schema 包含不需要的字段** | LLM 为无用字段分配注意力，甚至被误导 | 每个场景用不同的精简 Schema |
| 8 | **LLM 调用失败时系统崩溃** | 可用性差，用户体验糟糕 | 实现降级策略（重试 → 缓存 → 规则引擎 → 默认值） |
| 9 | **不记录中间阶段的输出** | 出问题无法定位是哪个阶段 | 每个阶段打结构化 Log |
| 10 | **用 LLM 做可以用代码完成的判断** | 浪费延迟和成本，且不确定性强 | 能用 if/else 就不要用 LLM |

---

## 12. 实施检查清单

在设计或审查一个 Prompt 系统时，逐项检查：

### 架构层面
- [ ] 是否将复杂任务拆成了多个独立阶段？
- [ ] 每个阶段是纯代码还是 LLM 调用？代码能做的绝不交给 LLM
- [ ] 是否有 Fallback 路径？LLM 故障时系统是否仍能返回可用结果？

### Plan 阶段
- [ ] Plan 的输出是否是**结构化 JSON**（不是自然语言）？
- [ ] Schema 是否针对当前场景**精简**了？是否移除了不需要的字段？
- [ ] 字段名是否在引导**正确的思维方向**？
- [ ] 是否区分了不同场景使用**不同的 Plan Schema**？

### Build 阶段
- [ ] 最终 prompt 是否由**代码确定性组装**？
- [ ] 是否用 `list[str]` + 条件 Add + Join 的模式？
- [ ] 权限/安全/合规相关字段是否由**代码覆写**（而非信任 LLM）？

### 约束设计
- [ ] 最重要的硬约束是否放在了 prompt **前 25%** 的位置？
- [ ] 负向约束是否精简到 6 条以内？是否同类合并了？
- [ ] 是否有"正向引导过弱，负向约束过密"的问题？
- [ ] 用户输入数据是否与系统指令用分隔符隔开，且放在 prompt **最后**？

### 分类与路由
- [ ] 是否有输入分类逻辑？不同输入走不同模板？
- [ ] 分类器是用纯代码规则还是 LLM？
- [ ] 是否有"兜底"分类（default/abstract）处理未知输入？

### 可观测性
- [ ] 每个阶段的输入输出是否打了结构化 Log？
- [ ] Plan 的结果（JSON）和最终 Prompt 是否都记录了？
- [ ] 是否有全链路 `trace_id` 便于排查？

### 防御性
- [ ] LLM 调用是否设置了超时？
- [ ] 是否有重试策略（exponential backoff）？
- [ ] 是否有独立于 LLM 的内容安全检查？

---

## 附录

本文档的设计模式和原则提炼自生产环境中经过验证的 Prompt 工程实践，适用于但不限于以下场景：

- **文生图 / 文生视频 Prompt 工程**：多阶段 Plan-Build 流水线，精确控制视觉输出
- **对话系统 (Chatbot)**：意图分类 + 模板路由 + 上下文管理 + 安全降级
- **内容审核与安全**：Schema 约束 + 代码覆写 + 独立安全检查层
- **RAG 系统**：Query 改写 → 检索 → 重排序 → 生成，每个阶段可独立优化
- **Agent / Tool Calling**：Plan 阶段决定调用哪些工具，Build 阶段组装工具参数
- **代码生成**：分类代码类型 → Plan 架构 → Build 代码结构 → 生成

---

*文档版本：v2.0*
*最后更新：2026-06-08*

> 这是一份活的文档。每当你发现一个新的有效模式或踩了一个新的坑，更新它。
