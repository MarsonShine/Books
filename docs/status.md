# 当前状态

更新：2026-11-16
Status: idle
Task: none
Next: 根据读者反馈继续调整示例讲解。

## 结果

- 重写 `CSharpGuide/dotnet11-features` 的 README 和学习文档，调整阅读顺序与文字表达。
- 示例代码和项目配置未修改；原有运行条件、API 限制及官方资料保留。
- 文档改动已提交，尚未推送。

## 验证

- `dotnet build CSharpGuide/dotnet11-features -c Release`：通过，0 警告、0 错误。
- `dotnet run --project CSharpGuide/dotnet11-features -c Release --no-build -- all`：通过。
- CBOR、EF SQLite、WinForms `--inspect` 实际运行通过；Web 项目构建通过。
- 13 篇 Markdown 相对链接与代码围栏检查通过；`git diff --check` 和 harness check 通过。
- 未新增单元测试；未重新验证桌面窗口视觉效果或其他操作系统上的行为。
