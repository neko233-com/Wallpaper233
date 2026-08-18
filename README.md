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
- Direct3D 12：独立渲染进程的高性能 GPU 后端，计划通过 C# DirectX 绑定接入
- WebView2：网页壁纸兼容运行时
- Windows 原生媒体栈：视频播放与硬件解码
- Wallpaper233 Package：自有、可版本化、可校验的壁纸包格式

“纯 C#”指 Wallpaper233 的应用源码不使用 C++/Rust。Windows、显卡驱动和 WebView2 本身仍然是系统提供的原生组件。

## 当前状态

当前版本是可体验的 alpha，已包含：

- WinUI 3 控制面板
- 核心运行时状态模型
- 本地壁纸库：拖拽或选择图片/视频导入
- 本地图片/视频库、拖拽导入、双击预览
- Windows 桌面后置预览宿主（实验性）
- Windows x64 自包含发布与 CI
- Steam Workshop 可选桥接：打开官方页面、扫描本机已下载缓存
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

发布 Windows x64 可运行版本：

```powershell
dotnet restore Wallpaper233.slnx -r win-x64
dotnet publish src/Wallpaper233.App/Wallpaper233.App.csproj -c Release -r win-x64 -p:Platform=x64 -p:PublishAot=false -p:PublishTrimmed=false -p:SelfContained=true -p:PublishDir=bin/Release/win-x64-release/ --no-restore
```

控制面板不强行 NativeAOT：当前 Windows App SDK + WinUI 3 组合在 NativeAOT 启动阶段会崩溃。最终架构是“WinUI 控制面板 JIT 自包含 + 独立 D3D12/视频渲染进程 NativeAOT”，这样同时保留稳定 UI、AOT 渲染性能和进程级故障隔离。`tools/publish-aot.ps1` 仅用于显式实验。

Steam Workshop 下载由 Steam Client 和 Steamworks API 控制。Wallpaper233 不绕过 Steam 下载，也不分发 Steam Workshop 第三方资源；用户可以打开官方页面，或扫描本机已下载的 Wallpaper Engine 媒体缓存。

## 许可

MIT License。第三方壁纸、字体、模型、音视频和社区内容不随本仓库分发。
