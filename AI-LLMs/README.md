记录平常接触到的AI领域相关概念与知识点，以及一些论文翻译

- 张奇著作的[《大规模语言模型：从理论到实践》](books/大规模语言模型：从理论到实践.pdf)

## AI Agent 与 Harness

- [第七章读书笔记：Agent 的评估](Agent-Evaluation-Explained.md)：成功与可靠性指标、评估环境、验证器、失败归因、统计判断和成本权衡。
- [第八章读书笔记：模型后训练](Model-Post-Training-Explained.md)：参数更新、预训练与中期训练、SFT、强化学习、奖励设计和蒸馏。
- [第九章读书笔记：Agent 的持续进化](Agent-Continual-Evolution-Explained.md)：从轨迹提炼经验，将经验写入知识、指令、程序或参数，并验证、发布和维护。
- [第十章读书笔记：多 Agent 协作](Multi-Agent-Collaboration-Explained.md)：上下文与拓扑、任务交接、通信控制、并发故障、独立审核和 Agent 社会。
- [Agent 的上下文不是数据库：从 Context Window 到 Context Runtime](Context-Engineering-Agent-Harness.md)：从上下文工作集、长期记忆、执行账本、Skill / Tool 发现和 KV Cache 出发，梳理 Agent 的 Context Runtime 与 Harness 设计。
- [《深入理解 AI Agent：设计原理与工程实践》](https://github.com/bojieli/ai-agent-book)：围绕“Agent = LLM + 上下文 + 工具”展开，覆盖上下文工程、用户记忆与知识库、工具、评估、持续进化和多 Agent 协作，并提供配套实验。
- [Learn Harness Engineering](https://github.com/walkinglabs/learn-harness-engineering)：以项目实践学习如何构建可靠的 AI Coding Agent 环境，重点讨论 Instructions、State、Verification、Scope 和 Session Lifecycle。
