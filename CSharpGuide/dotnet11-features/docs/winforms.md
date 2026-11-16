# Windows Forms：新样式和批量更新

这个独立项目需要 Windows、.NET 11 SDK 和 Windows Desktop 运行时。它会打开窗口；从仓库根目录运行，关闭窗口即可结束：

```powershell
dotnet run --project CSharpGuide/dotnet11-features/winforms -c Release
```

在自动化或不方便打开窗口的环境，可以使用 `--inspect`：它创建控件并打印设置，但不启动消息循环和显示窗口，因此不能证明视觉效果。

```powershell
dotnet run --project CSharpGuide/dotnet11-features/winforms -c Release -- --inspect
```

## 从应用默认样式到控件

代码先运行 `ApplicationConfiguration.Initialize()`，再设置 `Application.SetDefaultVisualStylesMode(VisualStylesMode.Net11)`，选择 .NET 11 新的控件呈现方式。控件可以继承父控件或应用的样式设置，也可单独配置。先初始化视觉样式很重要：本机验证中跳过初始化会使默认样式显示为 `Disabled`。

`CheckBox` 的 `Appearance.ToggleSwitch` 显示为开关，读写 `Checked` 的代码仍是复选框的语义。它只是控件外观，并不自动替业务保存设置。

`Application.SetDefaultFormRevealMode(FormRevealMode.Deferred)` 让窗口在初始布局和主题准备后再显示，以减少启动时的未设样式闪烁。实际效果取决于系统、主题和窗口内容。

## 一次加很多项时暂缓绘制

示例向 `ListBox` 增加 100 项，使用 `using (list.SuspendPainting(LayoutSuspendTraversal.TargetOnly))` 包围操作。进入范围后暂缓目标控件的绘制和布局相关更新；离开范围自动恢复。

这不影响列表最终包含的元素，也不意味着每次增加一项都必须暂停绘制。少量简单更新通常不需要此处理；对很多控件的大批量更新，可根据需要考虑 `TargetAndDescendants` 并在目标 UI 测量效果。暂停期间仍要避免在后台线程直接操作 Windows 控件。

参考：[Windows Forms .NET 11 更新](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/whats-new/net110)。该官方页面当前标注 RC 1；示例已用本机 SDK 与 Windows Desktop 运行时 11.0.0 构建并用 `--inspect` 执行，视觉效果需要交互桌面自行查看。
