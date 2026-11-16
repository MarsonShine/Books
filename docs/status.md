# 当前状态

更新：2026-11-16
Status: idle
Task: none
Next: 根据实际平台补充 WPF、MAUI、浏览器等尚未演示的功能；参考特性地图。

## 结果

- 补充 .NET 11 主项目基础库、JSON 源生成、异步验证、SIMD 排列和环境示例。
- 新增独立 ASP.NET Core、EF Core SQLite、增量 CBOR、Windows Forms 和 SDK CLI 示例及中文文档。
- 更新 `CSharpGuide/dotnet11-features/docs/release-map.md`，保留尚无可运行示例的条目标识。
- 变更按主题拆分为本地提交；未推送。

## 验证

- .NET SDK 11.0.100，Windows x64：主项目 Release Rebuild 成功，0 警告 0 错误；`dotnet run --project CSharpGuide/dotnet11-features -c Release --no-build -- all` 成功；新增向量排列随后单独运行成功。
- `environment process`、`environment http` 以及系统 DNS `environment dns example.com` 成功；未测试自定义 DNS 服务器。
- 本机 HTTP 验证 union 200、异步验证通过 200/失败 400、参数绑定 400、健康端点及缓存命中/未命中指标。
- 独立 CBOR、EF SQLite 和 WinForms `--inspect` 运行成功；未观察交互式窗口视觉效果。
- `pwsh -NoProfile -File CSharpGuide/dotnet11-features/sdk-examples/cli-demo.ps1` 成功；它将文件保留在系统临时目录供查看。
- Markdown 本地链接与代码围栏检查通过；harness check 通过。
- 按用户要求未编写单元测试；未验证 macOS/Linux、Arm SVE、移动设备、QUIC、WPF 或 MAUI 环境。
