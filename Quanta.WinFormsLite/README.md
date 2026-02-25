# Quanta.WinFormsLite

这是一个独立的 WinForms 重构项目（不依赖 WPF UI 树），目标是作为 Quanta 的低内存版本基础。

## 已实现能力
- 全局热键呼出（默认 Alt+R）
- 命令搜索与执行（URL / Shell 路径模板）
- 顶层目录文件搜索（桌面 + 下载）
- 统一 ThemeManager（避免主题逻辑散落）
- 单实例运行

## 目标内存
- 目标：空闲工作集尽量压到 50MB 附近（最终取决于 .NET Runtime 与系统版本）。
- 当前提交为重构起点，需在 Windows 真机上使用 Release 构建测量。

## 运行（Windows）
```bash
dotnet run --project Quanta.WinFormsLite/Quanta.WinFormsLite.csproj -c Release
```

## 配置
程序会在可执行目录生成 `lite.config.json`。
