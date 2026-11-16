using System.Windows.Forms;

ApplicationConfiguration.Initialize();
Application.SetDefaultVisualStylesMode(VisualStylesMode.Net11);
Application.SetDefaultFormRevealMode(FormRevealMode.Deferred);
using var form = new Form { Text = ".NET 11 WinForms：样式与批量更新", Width = 480, Height = 380 };
using var toggle = new CheckBox { Text = "接收通知", Appearance = Appearance.ToggleSwitch, Checked = true, Dock = DockStyle.Top };
using var list = new ListBox { Dock = DockStyle.Fill };
form.Controls.Add(list);
form.Controls.Add(toggle);
using (list.SuspendPainting(LayoutSuspendTraversal.TargetOnly))
{
    for (int i = 1; i <= 100; i++) list.Items.Add($"通知 {i}");
}
if (args.Contains("--inspect"))
{
    Console.WriteLine($"默认视觉样式={Application.DefaultVisualStylesMode}; 列表项={list.Items.Count}; 开关={toggle.Checked}");
    return;
}
Application.Run(form);
