# 什么是技能？

> 代理技能是一种轻量级的开放格式，用于通过专门的知识和工作流扩展 AI 代理的能力。

技能的核心是一个包含 `SKILL.md` 文件的文件夹。这个文件包含元数据（至少包括 `name` 和 `description`）以及指示代理如何执行特定任务的说明。技能还可以捆绑脚本、模板和参考材料。

```
my-skill/ 
├── SKILL.md # 必需：说明 + 元数据 
├── scripts/ # 可选：可执行代码 
├── references/ # 可选：文档 
└── assets/ # 可选：模板、资源
```

## 技能如何工作

技能使用**渐进披露（progressive disclosure）**来有效管理上下文：

1. **发现**：启动时，代理仅加载每个可用技能的名称和描述，足以知道何时它可能相关。
2. **激活**：当任务与技能的描述匹配时，代理将完整的 `SKILL.md` 说明加载到上下文中。
3. **执行**：代理按照指示操作，根据需要加载引用的文件或执行捆绑的代码。

这种方法保持代理的高效，同时根据需要为它们提供更多的上下文。

## `SKILL.md` 文件

每个技能都以一个包含 YAML 前言和 Markdown 说明的 `SKILL.md` 文件开始：

```
---
name: pdf-processing 
description: 从 PDF 文件中提取文本和表格，填写表单，合并文档。
---

# PDF 处理

## 何时使用此技能

当用户需要处理 PDF 文件时使用此技能...

## 如何提取文本

1. 使用 pdfplumber 提取文本...

## 如何填写表单

...
```

`SKILL.md` 文件顶部需要包含以下前言：

- `name`：一个简短的标识符
- `description`：何时使用此技能

Markdown 体包含实际的说明，结构和内容没有具体的限制。

这种简单的格式具有一些关键优势：

- **自文档化**：技能的作者或用户可以阅读 `SKILL.md` 并理解其功能，使得技能易于审核和改进。
- **可扩展性**：技能的复杂性可以从仅仅是文本说明到可执行代码、资产和模板不等。
- **可移植性**：技能只是文件，因此它们易于编辑、版本管理和共享。

## 下一步

- [查看规范说明](https://agentskills.io/specification) 以了解完整的格式。
- [向您的代理添加技能支持](https://agentskills.io/integrate-skills) 来构建兼容的客户端。
- [查看示例技能](https://github.com/anthropics/skills) 在 GitHub 上。
- [阅读编写最佳实践](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/best-practices) 以编写有效的技能。
- [使用参考库](https://github.com/agentskills/agentskills/tree/main/skills-ref) 来验证技能并生成提示 XML。

------

> 要查找此文档中的导航和其他页面，请获取 llms.txt 文件，网址为： https://agentskills.io/llms.txt

# 规格说明

> 代理技能的完整格式规范。

本文档定义了代理技能的格式。

## 目录结构

技能是一个包含至少 `SKILL.md` 文件的目录：

```
skill-name/
└── SKILL.md          # 必需
```

## `SKILL.md` 格式

`SKILL.md` 文件必须包含 YAML 前言，后跟 Markdown 内容。

### 前言（必需）

```
------
## name: skill-name 
description: 描述该技能的功能以及何时使用该技能。
------
```

可选字段：

```
---
name: pdf-processing 
description: 从 PDF 文件中提取文本和表格，填写表单，合并文档。 
license: Apache-2.0 
metadata: 
  author: example-org 
  version: "1.0"
---
```

| 字段            | 必需 | 限制条件                                                     |
| --------------- | ---- | ------------------------------------------------------------ |
| `name`          | 是   | 最多 64 个字符。仅限小写字母、数字和连字符。不能以连字符开头或结尾。 |
| `description`   | 是   | 最多 1024 个字符。非空。描述技能的功能以及何时使用该技能。   |
| `license`       | 否   | 许可证名称或对捆绑许可证文件的引用。                         |
| `compatibility` | 否   | 最多 500 个字符。指示环境要求（目标产品、系统包、网络访问等）。 |
| `metadata`      | 否   | 任意键值对，用于额外的元数据。                               |
| `allowed-tools` | 否   | 空格分隔的已预批准工具列表（实验性）。                       |

#### `name` 字段

必需是 `name` 字段：

- 必须为 1-64 个字符
- 只能包含小写字母、数字和连字符（`a-z` 和 `-`）
- 不能以 `-` 开头或结尾
- 不能包含连续的连字符（`--`）
- 必须与父目录名称匹配

有效示例：

```
name: pdf-processing
```

```
name: data-analysis
```

```
name: code-review
```

无效示例：

```
name: PDF-Processing # 不允许使用大写字母
```

```
name: -pdf # 不能以连字符开头
```

```
name: pdf--processing # 不允许连续使用连字符
```

#### `description` 字段

必需的 `description` 字段：

- 必须为 1-1024 个字符
- 应该描述技能的功能以及何时使用该技能
- 应包括帮助代理识别相关任务的具体关键词

良好的示例：

```
description: 从 PDF 文件中提取文本和表格，填写 PDF 表单，合并多个 PDF 文件。用于处理 PDF 文档或用户提到 PDF、表单或文档提取时。
```

不良示例：

```
description: 帮助处理 PDF 文件。
```

#### `license` 字段

可选的 `license` 字段：

- 指定应用于技能的许可证
- 我们建议将其简短（可以是许可证名称或捆绑许可证文件的名称）

示例：

```
license: 专有. LICENSE.txt 文件包含完整条款
```

#### `compatibility` 字段

可选的 `compatibility` 字段：

- 如果提供，必须为 1-500 个字符
- 只有在技能具有特定环境要求时才应包含
- 可以指示目标产品、所需的系统包、网络访问需求等

示例：

```
compatibility: 设计用于 Claude Code（或类似产品）
```

```
compatibility: 需要 git、docker、jq 和互联网访问
```

#### `metadata` 字段

可选的 `metadata` 字段：

- 一个从字符串键到字符串值的映射
- 客户端可以使用它来存储 Agent Skills 规范未定义的附加属性
- 我们建议让您的键名尽可能独特，以避免意外冲突

示例：

```
metadata: 
  author: example-org 
  version: "1.0"
```

#### `allowed-tools` 字段

可选的 `allowed-tools` 字段：

- 由空格分隔的已预批准工具列表
- 实验性。不同代理实现对该字段的支持可能有所不同

示例：

```
allowed-tools: Bash(git:*) Bash(jq:*) Read
```

### 正文内容

前言之后的 Markdown 正文包含技能说明。没有格式限制。写下任何有助于代理有效执行任务的内容。

推荐的章节：

- 步骤说明
- 输入和输出示例
- 常见的边缘情况

请注意，一旦决定激活技能，代理将加载整个文件。考虑将较长的 `SKILL.md` 内容拆分到引用文件中。

## 可选目录

### scripts/

包含代理可以执行的代码。脚本应：

- 是自包含的，或清楚地文档化依赖关系
- 包含有用的错误消息
- 能够优雅地处理边缘情况

支持的语言取决于代理实现。常见选项包括 Python、Bash 和 JavaScript。

### references/

包含代理在需要时可以读取的附加文档：

- `REFERENCE.md` - 详细的技术参考
- `FORMS.md` - 表单模板或结构化数据格式
- 特定领域的文件（`finance.md`、`legal.md` 等）

保持单个[参考文件](#文件引用)的专注。代理按需加载这些文件，因此较小的文件可以减少上下文的使用。

### assets/

包含静态资源：

- 模板（文档模板、配置模板）
- 图像（图表、示例）
- 数据文件（查找表、架构）

## 渐进披露（Progressive disclosure）

技能应按上下文高效使用结构化：

1. **元数据**（约 100 个字节）：启动时加载所有技能的 `name` 和 `description` 字段
2. **说明**（建议少于 5000 个字节）：当技能被激活时，加载完整的 `SKILL.md` 正文
3. **资源**（按需加载）：仅在需要时加载文件（例如，来自 `scripts/`、`references/` 或 `assets/` 中的文件）

保持主 `SKILL.md` 文件不超过 500 行。将详细的参考材料移到单独的文件中。

## 文件引用

在技能中引用其他文件时，使用相对路径从技能根目录开始：

```
请参阅 [参考指南](references/REFERENCE.md) 获取详细信息。

运行提取脚本： scripts/extract.py
```

保持文件引用不超过一层深度。避免深度嵌套的引用链。

## 验证

使用 [skills-ref](https://github.com/agentskills/agentskills/tree/main/skills-ref) 参考库验证您的技能：

```
skills-ref validate ./my-skill
```

这将检查您的 `SKILL.md` 前言是否有效，并遵循所有命名约定。

# 将 skills 集成到您的代理中

> 如何将代理技能支持添加到您的代理或工具中。

本指南解释了如何将技能支持添加到 AI 代理或开发工具中。

## 集成方法

集成技能的两种主要方法是：

**基于文件系统的代理**在计算机环境中运行（如 bash/unix），并且是最强大的选项。当模型发出类似 `cat /path/to/my-skill/SKILL.md` 的 shell 命令时，技能被激活。捆绑的资源通过 shell 命令进行访问。

**基于工具的代理**在没有专用计算机环境的情况下工作。相反，它们实现了允许模型触发技能并访问捆绑资产的工具。具体的工具实现由开发者决定。

## 概述

兼容技能的代理需要：

1. **发现** 配置目录中的技能
2. **加载元数据**（名称和描述）在启动时
3. **匹配** 用户任务与相关技能
4. **激活** 技能，加载完整的说明
5. **执行** 脚本并根据需要访问资源

## 技能发现

技能是包含 `SKILL.md` 文件的文件夹。您的代理应该扫描配置的目录以查找有效的技能。

## 加载元数据

在启动时，只解析每个 `SKILL.md` 文件的前言部分。这可以保持初始上下文使用的低开销。

### 解析前言

```
function parseMetadata(skillPath):
    content = readFile(skillPath + "/SKILL.md")
    frontmatter = extractYAMLFrontmatter(content)

    return {
        name: frontmatter.name,
        description: frontmatter.description,
        path: skillPath
    }
```

### 注入上下文

将技能元数据包含在系统提示中，以便模型知道哪些技能是可用的。

按照您平台的系统提示更新指南。例如，对于 Claude 模型，推荐的格式使用 XML：

```
<available_skills>
  <skill>
    <name>pdf-processing</name>
    <description>从 PDF 文件中提取文本和表格，填写表单，合并文档。</description>
    <location>/path/to/skills/pdf-processing/SKILL.md</location>
  </skill>
  <skill>
    <name>data-analysis</name>
    <description>分析数据集，生成图表并创建总结报告。</description>
    <location>/path/to/skills/data-analysis/SKILL.md</location>
  </skill>
</available_skills>
```

对于基于文件系统的代理，包含 `location` 字段，并提供 `SKILL.md` 文件的绝对路径。对于基于工具的代理，可以省略 `location` 字段。

保持元数据简洁。每个技能应大约增加 50-100 个令牌到上下文中。

## 安全考虑

脚本执行会带来安全风险。考虑以下措施：

- **沙盒化**：在隔离环境中运行脚本
- **允许名单**：仅执行来自可信技能的脚本
- **确认**：在执行可能危险的操作之前，询问用户
- **日志记录**：记录所有脚本执行操作，以便审计

## 参考实现

[skills-ref](https://github.com/agentskills/agentskills/tree/main/skills-ref) 库提供了用于处理技能的 Python 工具和 CLI。

例如：

**验证技能目录：**

```
skills-ref validate <path>
```

**生成 `<available_skills>` XML 以用于代理提示：**

```
skills-ref to-prompt <path>...
```

使用该库的源代码作为参考实现。
