# Windows Forms：设置新样式，批量更新列表

示例窗口上方是一个通知开关，下方是包含 100 项的列表。它使用 .NET 11 的控件样式，并在添加列表项时暂缓绘制。代码在 [winforms/Program.cs](../winforms/Program.cs)。

运行需要 Windows、.NET 11 SDK 和 Windows Desktop 运行时。在仓库根目录执行后会打开窗口，关闭窗口即可结束：

```powershell
dotnet run --project CSharpGuide/dotnet11-features/winforms -c Release
```

如果只想检查代码能否创建控件，可以加 `--inspect`。程序会打印默认样式、列表项数和开关状态，然后退出；这个模式不显示窗口，也不运行窗口消息循环，不能用它判断外观是否正确。

```powershell
dotnet run --project CSharpGuide/dotnet11-features/winforms -c Release -- --inspect
```

## 应用默认样式和开关外观

程序先调用 `ApplicationConfiguration.Initialize()`，然后设置：

```csharp
Application.SetDefaultVisualStylesMode(VisualStylesMode.Net11);
```

这让应用选择 .NET 11 的控件呈现方式。控件可以继承父控件或应用的样式，也可以单独配置。初始化顺序会影响结果：在本机的检查中，如果跳过初始化，打印出的默认样式会是 `Disabled`。

开关仍然是 `CheckBox`，只是把 `Appearance` 设为 `ToggleSwitch`。代码仍通过 `Checked` 读取和修改状态，也可以继续使用 `CheckedChanged` 等事件；状态不会因为外观变成开关就自动保存。

例如读取 `toggle.Checked` 可以知道当前是开还是关。用户点击时可以在 `CheckedChanged` 里更新界面；如果希望下次启动程序时仍记住状态，还需要自己把它写入配置或其他存储。

示例还设置了 `FormRevealMode.Deferred`。窗口启动时，应用可以等初始布局和主题准备好再显示，减少窗口先以未设样式的状态闪现。最终视觉效果与 Windows 版本、主题和窗口内容有关。

## 添加 100 项时，先暂停绘制

逐项修改列表时，控件可能不断更新布局和画面。示例把添加操作放进 `SuspendPainting` 的范围：

```csharp
using (list.SuspendPainting(LayoutSuspendTraversal.TargetOnly))
{
    for (int i = 1; i <= 100; i++)
        list.Items.Add($"通知 {i}");
}
```

进入 `using` 后，目标控件暂缓绘制和相关布局更新；离开时释放作用域，恢复更新。`TargetOnly` 只针对当前目标，处理嵌套控件时还可以按需要考虑 `TargetAndDescendants`。

`using` 在这里管理的是一段临时状态：无论循环正常结束还是中途抛出异常，离开作用域时都会释放返回的对象，让控件恢复更新。它不是等 100 项添加完才开始往列表里保存；列表项仍在循环中一项项加入，只是暂缓了期间的画面和布局处理。

少量列表项未必需要这样做。是否减少闪烁或耗时，可以在自己的窗口和数据量下观察。它也不会改变 Windows Forms 的线程要求：后台工作线程不能因此直接修改 UI 控件。

参考：[Windows Forms .NET 11 更新](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/whats-new/net110)。该官方页面目前标注 RC 1；本项目此前已用 Windows 上的 SDK 和 Desktop 运行时 11.0.0 执行过 `--inspect`，窗口外观仍需在交互式桌面查看。
