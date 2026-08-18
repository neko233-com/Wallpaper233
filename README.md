# Wallpaper233

Wallpaper233 是面向 Windows 的开源动态壁纸平台，目标是成为 Wallpaper Engine 的独立平替。

核心原则：

- 不依赖 Steam、Steamworks、Steam 登录或 Workshop API。
- Wallpaper Engine 仅作为可选的本地导入来源，不作为 Wallpaper233 的运行时依赖。
- 自有运行时、自有项目模型、自有包格式和自有插件边界。
- 应用层使用纯 C#；不维护 C++ 或 Rust 业务代码。
- 优先稳定性、可恢复性和可测试性，再扩展兼容范围。

## 技术路线

- .NET 10
- WinUI 3 / Windows App SDK：控制面板与系统集成
- Direct3D 12：高性能 GPU 渲染后端，计划通过 C# DirectX 绑定接入
- WebView2：网页壁纸兼容运行时
- Windows 原生媒体栈：视频播放与硬件解码
- Wallpaper233 Package：自有、可版本化、可校验的壁纸包格式

“纯 C#”指 Wallpaper233 的应用源码不使用 C++/Rust。Windows、显卡驱动和 WebView2 本身仍然是系统提供的原生组件。

## 当前状态

当前版本是工程骨架，已包含：

- WinUI 3 控制面板
- 核心运行时状态模型
- Wallpaper Engine 本地项目检测边界
- 基础构建与测试结构

## 构建

需要 Visual Studio 2022、.NET 10 SDK、Windows 10/11 SDK 和 Windows App SDK 工作负载。

```powershell
dotnet restore Wallpaper233.slnx -r win-x64
dotnet build Wallpaper233.slnx -c Debug
```

运行控制面板：

```powershell
dotnet run --project src/Wallpaper233.App/Wallpaper233.App.csproj -c Debug
```

## 许可

MIT License。第三方壁纸、字体、模型、音视频和社区内容不随本仓库分发。
