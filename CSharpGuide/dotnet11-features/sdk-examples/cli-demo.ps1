param(
    [string] $WorkspaceRoot = [System.IO.Path]::GetTempPath()
)

$ErrorActionPreference = 'Stop'
$demoDirectory = Join-Path $WorkspaceRoot ("dotnet11-sdk-demo-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $demoDirectory | Out-Null

function Run-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet 命令失败，退出码 $LASTEXITCODE" }
}

Push-Location $demoDirectory
try {
    # 只需要 SDK 自带框架，避免机器上的私有包源影响演示。
    @'
<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear /></packageSources></configuration>
'@ | Set-Content NuGet.Config -Encoding utf8

    New-Item -ItemType Directory -Path App/Properties, Core | Out-Null
    @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net11.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>MyDemoApp</AssemblyName>
  </PropertyGroup>
</Project>
'@ | Set-Content App/App.csproj -Encoding utf8
    @'
Console.WriteLine($"启动参数：{string.Join(" ", args)}");
Console.WriteLine($"传入的环境变量：{Environment.GetEnvironmentVariable("SDK_DEMO_MESSAGE")}");
'@ | Set-Content App/Program.cs -Encoding utf8
    @'
{
  "profiles": {
    "Demo": {
      "commandName": "Project",
      "commandLineArgs": "--name $(AssemblyName)"
    }
  }
}
'@ | Set-Content App/Properties/launchSettings.json -Encoding utf8
    @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup>
</Project>
'@ | Set-Content Core/Core.csproj -Encoding utf8
    @'
namespace DemoCore;
public static class Message
{
    public static string SayHello() => "来自被引用的 Core 项目";
}
'@ | Set-Content Core/Message.cs -Encoding utf8

    Write-Host "`n[1] 在父解决方案中加入两个项目"
    Run-Dotnet new sln --name Demo --format slnx
    Run-Dotnet sln Demo.slnx add App/App.csproj Core/Core.csproj

    Write-Host "`n[2] 创建筛选器：先加入、列出、再移除 Core"
    Run-Dotnet new slnf --name Focus --parent-solution Demo.slnx
    Run-Dotnet sln Focus.slnf add App/App.csproj Core/Core.csproj
    Run-Dotnet sln Focus.slnf list
    Run-Dotnet sln Focus.slnf remove Core/Core.csproj
    Run-Dotnet sln Focus.slnf list
    Write-Host "父解决方案仍包含两个项目："
    Run-Dotnet sln Demo.slnx list
    Write-Host "构建筛选器中的项目："
    Run-Dotnet build Focus.slnf

    Write-Host "`n[3] 将 MSBuild 属性展开到启动参数，并用 -e 给子进程传环境变量"
    Run-Dotnet run --project App/App.csproj --launch-profile Demo -e SDK_DEMO_MESSAGE=HelloDotNet11

    # 对临时副本运行 reference 命令：它会修改被指定的 app.cs。
    @'
#!/usr/bin/env dotnet
#:property PublishAot=false
using DemoCore;
Console.WriteLine(Message.SayHello());
'@ | Set-Content app.cs -Encoding utf8

    Write-Host "`n[4] 给文件式应用添加项目引用"
    Run-Dotnet reference add --file app.cs Core/Core.csproj
    Write-Host "修改后的 app.cs："
    Get-Content app.cs
    Run-Dotnet run app.cs
    Write-Host "移除引用后再次列出："
    Run-Dotnet reference remove --file app.cs Core/Core.csproj
    Run-Dotnet reference list --file app.cs

    Write-Host "`n[5] 在项目目录中省略 --project 来添加和移除引用"
    Push-Location App
    try {
        Run-Dotnet reference add ../Core/Core.csproj
        Run-Dotnet reference list
        Run-Dotnet reference remove ../Core/Core.csproj
    }
    finally {
        Pop-Location
    }

    Write-Host "`n[6] 文件式应用直接包含编译后的 DLL，并设置含空格的属性"
    Run-Dotnet build Core/Core.csproj
    @'
#!/usr/bin/env dotnet
#:property PublishAot=false
#:property AssemblyTitle="SDK Demo Title"
#:include Core/bin/Debug/net11.0/Core.dll
using System.Reflection;
using DemoCore;
Console.WriteLine(Message.SayHello());
Console.WriteLine($"程序集标题：{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()?.Title}");
'@ | Set-Content app-dll.cs -Encoding utf8
    Run-Dotnet run app-dll.cs

    Write-Host "`n示例文件保存在 $demoDirectory，可打开查看；完成后可自行删除该目录。"
}
finally {
    Pop-Location
}
