# Books
读书笔记索引

1. [C# 异步](AsyncInCSharp/docs/)
2. [架构整洁之道](CleanArchitecture/)
3. [代码整洁之道](CleanCode/)
4. [编写高性能 .NET 代码](WHPerformanceDotNet/)
5. [C# 并发编程实战](ConcurrencyInCSharpCookbook/03ParallerBasic/)
6. [C# 指南](CSharpGuide/docs/)
7. [深入理解计算机系统（CSAPP）](CSAPP/)
8. [深入理解C#](CSharpInDepth/)
9. [设计模式](DesignPattern/DesignPatternCore/)
10. [EffectiveJava](EffectiveJava/)
11. [Linux 相关](linux/doc/)
12. [TCPIP](TCPIP/)
13. [Java 编程思想](ThinkingInJava/docs/)
14. [Typescript](ts/)
15. [DDIA](DDIA/)
16. [垃圾回收算法与实现](GarbageCollection/)
17. [博客系列（翻译，阅读资料等）](blogs/)

---

## 本地预览与部署

本站使用 [MkDocs](https://www.mkdocs.org/) + [Material 主题](https://squidfunk.github.io/mkdocs-material/) 构建，通过 GitHub Actions 自动部署到 GitHub Pages。

### 安装依赖

```bash
python -m pip install "mkdocs<2" mkdocs-material
```

当前 `Material for MkDocs` 还未支持 MkDocs 2，因此这里显式约束到 `MkDocs 1.x`，避免本地和 CI 安装到不兼容版本。

### 本地预览

```bash
mkdocs serve
```

启动后访问 http://127.0.0.1:8000/ 即可预览站点。

构建前会自动通过 `hooks/prepare_docs.py` 生成 `_mkdocs_docs/`，把 `docs/` 下的链接映射展开为真实目录，因此在 Windows 上也不需要额外处理 Git symlink。

### 构建静态文件

```bash
mkdocs build
```

构建产物输出到 `site/` 目录。

### 部署到 GitHub Pages

推送到 `main` 分支后，GitHub Actions 工作流（`.github/workflows/pages.yml`）将自动构建并部署到 GitHub Pages，无需手动操作。
