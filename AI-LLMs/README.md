记录平常接触到的AI领域相关概念与知识点，以及一些论文翻译

- 张奇著作的[《大规模语言模型：从理论到实践》](books/大规模语言模型：从理论到实践.pdf)

## AI Agent 与 Harness

- [如何评估 Agent：从一次成功到可靠交付](Agent-Evaluation-Explained.md)：结合文件归档示例，解释成功指标、评估环境、评分与归因、回归测试、成本和发布决策。
- [模型后训练：怎样把示范和反馈变成能力](Model-Post-Training-Explained.md)：第八章导读，解释中期训练、SFT、RL、奖励设计、LoRA 与蒸馏，以及如何选择训练方法。
- [Agent 的持续进化：怎样让经验改善下一次任务](Agent-Continual-Evolution-Explained.md)：第九章导读，解释经验提炼、四类更新位置、独立验证、版本发布与长期整理。
- [多 Agent 协作：怎样分工、交接和验证](Multi-Agent-Collaboration-Explained.md)：第十章导读，解释上下文隔离、协作结构、通信调度、并发冲突、独立审核与 A2A。
- [Agent 的上下文不是数据库：从 Context Window 到 Context Runtime](Context-Engineering-Agent-Harness.md)：从上下文工作集、长期记忆、执行账本、Skill / Tool 发现和 KV Cache 出发，梳理 Agent 的 Context Runtime 与 Harness 设计。
- [《深入理解 AI Agent：设计原理与工程实践》](https://github.com/bojieli/ai-agent-book)：围绕“Agent = LLM + 上下文 + 工具”展开，覆盖上下文工程、用户记忆与知识库、工具、评估、持续进化和多 Agent 协作，并提供配套实验。
- [Learn Harness Engineering](https://github.com/walkinglabs/learn-harness-engineering)：以项目实践学习如何构建可靠的 AI Coding Agent 环境，重点讨论 Instructions、State、Verification、Scope 和 Session Lifecycle。
