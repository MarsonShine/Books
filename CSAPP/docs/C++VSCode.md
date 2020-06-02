# VSCode 运行 C++

https://code.visualstudio.com/docs/languages/cpp

https://code.visualstudio.com/docs/cpp/config-mingw

tasks.json:

```json
{
    "tasks": [
        {
            "type": "shell",
            "label": "g++.exe build active file",
            "command": "C:\\Program Files\\C++\\mingw64\\bin\\g++.exe",
            "args": [
                "-g",
                "${file}",
                "-o",
                "${fileDirname}\\lib\\${fileBasenameNoExtension}.exe"
            ],
            "options": {
                "cwd": "C:\\Program Files\\C++\\mingw64\\bin"
            }
        }
    ],
    "version": "2.0.0"
}
```

settings.json:

```json
{
    "files.associations": {
        "vector": "cpp",
        "iostream": "cpp",
        "string": "cpp",
        "ostream": "cpp",
        "atomic": "cpp",
        "*.tcc": "cpp",
        "cstddef": "cpp",
        "cstdint": "cpp",
        "exception": "cpp",
        "algorithm": "cpp",
        "memory": "cpp",
        "memory_resource": "cpp",
        "type_traits": "cpp",
        "utility": "cpp",
        "initializer_list": "cpp",
        "iosfwd": "cpp",
        "istream": "cpp",
        "new": "cpp",
        "streambuf": "cpp",
        "typeinfo": "cpp"
    }
}
```

launch.json:

```json
{
    // ctr + shift + B 触发的配置
    // Use IntelliSense to learn about possible attributes.
    // Hover to view descriptions of existing attributes.
    // For more information, visit: https://go.microsoft.com/fwlink/?linkid=830387
    "version": "0.2.0",
    "configurations": [
        {
            "name": "g++.exe - 生成和调试活动文件",
            "type": "cppdbg",
            "request": "launch",
            "program": "${fileDirname}\\lib\\${fileBasenameNoExtension}.exe",
            "args": [],
            "stopAtEntry": false,
            "cwd": "${workspaceFolder}",
            "environment": [],
            "externalConsole": false,
            "MIMode": "gdb",
            "miDebuggerPath": "C:\\Program Files\\C++\\mingw64\\bin\\gdb.exe",
            "setupCommands": [
                {
                    "description": "为 gdb 启用整齐打印",
                    "text": "-enable-pretty-printing",
                    "ignoreFailures": true
                }
            ],
            "preLaunchTask": "g++.exe build active file"
        }
    ]
}
```

