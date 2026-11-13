# Monochrome

Monochrome 是面向 tModLoader 的库模组，主要服务于 Fargo's Souls Phantasm Mode。

## tml-build 构建

`MonoPhantasm` 已采用在 `MP-tml-build-evaluation` 验证的构建方案。Monochrome 和 FargosPhantasmMode 都使用 `Tomat.Terraria.ModLoader.Sdk/2.1.2`、`TmlVersion=stable`、`net8.0`。SDK 下载官方稳定版并生成 NuGet 引用，不再跟随 Steam 当前安装的分支；独立源码生成器仍为普通 .NET 工程。

首次使用或切换版本后，先在仓库根目录还原，再重新加载 VS 中的两个项目：

```powershell
dotnet restore .\FargosPhantasmMode\FargosPhantasmMode.csproj --disable-parallel -m:1
```

SDK 2.1.2 首次下载同一版本时存在并发解压冲突；串行还原可避开。还原同时覆盖 Monochrome 项目引用。

在 VS 中，将 **Monochrome** 或 **FargosPhantasmMode** 设为启动项目，选择 **Terraria Client** 后按 F5；服务端使用 **Terraria Server**。这两个配置直接运行稳定版缓存的 `tModLoader.dll`，不经过 SDK Bootstrap。**Terraria Client (tml-build experimental)** 使用同一稳定版，通过 Bootstrap 启用开发功能；experimental 指启动方式，tML 版本仍是 stable。

也可以在仓库根目录直接运行：

```powershell
dotnet build .\Monochrome\Monochrome.csproj -c Debug
```

原构建脚本仍可使用：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Monochrome\Tools\BuildMonochrome.ps1
```

默认由 SDK 将模组包写入它检测到的 tML 存档的 Mods 目录，并可能更新该目录的 `enabled.json`。游戏正在加载该模组时，应先卸载模组或退出游戏再构建。

只生成独立测试包，不安装到 Mods：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Monochrome\Tools\BuildMonochrome.ps1 -TmodOutputPath .\Monochrome\bin\Monochrome.tmod
```

脚本现在串行还原 stable，然后构建；不再接受 `-TmlDirectory` 或设置 Steam 安装目录环境变量。

`Tools/TmlBuild.Local.props` 规范缓存目录，`Tools/TmlBuild.Local.targets` 从还原后的 targets 取得启动路径。Windows 默认缓存为 `%APPDATA%\tml-build\versions\<版本>\`；编译引用通过 tml-build 本地 NuGet feed 还原。上游属性 `tMLSteamPath` 在 stable 模式下实际指向这个缓存目录，并不代表正在使用 Steam 版本。根目录 `tModLoader.targets` 作为旧工程兼容入口，也动态导入同一 stable 缓存的 `tMLMod.targets`，不再写死盘符。

官方 GitHub 压缩包不带 .NET 运行时。启动器依次采用 `MonochromeTmlDotNet` 覆盖、缓存目录自带的 dotnet、SDK 已知 Steam 路径的 dotnet，最后采用系统 dotnet。本机通过被 Git 忽略的根目录 `tModLoader.Local.props` 继续使用此前验证正常的 Steam 自带 .NET 8.0.0；它只指定 dotnet 启动器，不会改变运行的 tML 版本。其他电脑可创建同名文件：

```xml
<Project>
  <PropertyGroup>
    <MonochromeTmlDotNet>C:\path\to\dotnet.exe</MonochromeTmlDotNet>
  </PropertyGroup>
</Project>
```

SDK 的 `.props` 和 `.targets` 仍在工程中显式导入，无需修改 SDK 包或全局环境变量。若需要自定义缓存根目录，在启动 VS/命令行之前设置 `TML_BUILD_CACHE_DIRECTORY`，然后重新还原。普通构建与启动均从还原后的版本取得路径；stable 别名不是永久锁定版本，更新缓存并还原后可能升级到新的稳定版。

2026-11-13 切换验证：stable 解析为 `2026.9.3.1`；CLI 与 VS MSBuild 编译通过，两份独立测试包打包通过，六个启动配置的文件与工作目录均存在。测试包位于仓库临时目录，未覆盖 Mods；本次没有重新启动客户端验收。

当前配置：

- 启用 SDK 打包和 HJSON 校验；启动配置由本项目维护，关闭 SDK 自动覆盖。
- Shader 编译与资源访问代码生成交给 SDK；`/monoshader reload` 只显示使用说明。
- 两个模组的 Shader 位于各自 `Assets/Effects`；FPM 不再将自有效果交给 Luminance 自动加载。
- 普通客户端与服务端直接启动。实验配置启用 `tomat.enablemod;tomat.assethotreloading;lolxd87.splashskip`，用于开发期热重载；保留用户当前选择的跳过启动画面功能。
- 保留 SDK 默认的 Publicizer；没有将现有反射或资源访问代码改写为新接口。

2026-11-12，用户报告通过 VS 启动后进入模组列表导致整机无响应。系统事件包含黑屏快捷键触发的 `LiveKernelEvent 1a8/1b8` 诊断转储以及异常关机记录；它们不能证明某个插件、Shader 或硬件是根因。最后一次 `client.log` 为零字节，现存日志没有可归因的异常堆栈。

当时先停用 `tomat.environmentmodlist`（修改模组列表）与 `lolxd87.splashskip`（跳过预先 JIT 与部分初始化），并将 `Terraria Client` 改为直接启动，作为隔离措施。当前用户已自行重新启用 splashskip，environmentmodlist 仍关闭。2026-11-13 用户反馈禁用几个可能冲突的第三方模组后不再卡死；尚未逐个归因。本次切换 stable 保留这些插件选择。

本次验证：Monochrome 编译和重复打包通过；测试包校验和正确、内含的 DLL 与构建输出一致，Shader、贴图及本地化资源存在，开发脚本和启动配置已排除。FargosPhantasmMode 对 Monochrome 的项目引用编译通过。用户已确认从 Steam 启动游戏测试正常；VS 路径修复和更换启动配置后，VS 自带 MSBuild 的还原、编译、打包均通过。新的直接启动配置已检查路径与参数；F5 实际启动、断点及热重载仍需在 VS 中验收。

2026-11-13，用户确认多数 tml-build 功能实际运行正常，保留当前配置并合入 `MonoPhantasm`。`tomat.environmentmodlist` 的模组列表增强及 SDK 虚拟条目会出现闪现，保持关闭；当前配置没有启用该插件。实验分支保留，合并使用独立 merge commit，便于整体回退；代码回退后还需重新生成模组，Mods 中已安装的包不会随 Git 自动恢复。

## Asset/Effect Shader 迁移

`MonoShader` 通过 `MonoEffectResource` 解析当前 Asset，保留参数、纹理与绘制批次接口；不再包含编译器或监听器。
`MonoShaderManager` 只加载 Monochrome 内置效果。其他模组创建自己的 `MonoShaderLibrary`，在
`PostSetupContent` 中显式 `Import`，并在 `Unload` 中 `Dispose`。FPM 的导入策略在
`Core/Systems/PModeShaders.cs`：自有资源借用 Asset，FSS 白名单中的 12 个资源使用独立克隆。
完整 API、状态约定和游戏验收见 [图形文档](Core/Graphics/README.md#55-shaders--着色器)。

离线构建可使用 `dotnet build FargosPhantasmMode/FargosPhantasmMode.csproj -p:TmlBuildPackageMod=false`。
只设置 `BuildMod=false` 不会关闭 SDK 打包，需要同时关闭 `TmlBuildPackageMod`。
FPM 生成代码输出到 `obj/Generated`，避免旧 `Generated` 目录的文件重复编译。

普通 Steam/VS 直启不会启用资源热重载。实验配置启动后修改自有 `.fx` 可以触发 SDK 编译；
修改参数类型、增删参数或更改 Pass 后，仍需重新编译 C# 并重载模组。
SDK 2.1.2 会监听所有可找到 SourceFolder 的模组，没有逐模组白名单；外部导入 API 的白名单
只约束 Monochrome 导入哪些资源，不约束 Bootstrap 的文件监听范围。

迁移验证：强制重编全部 28 个 Shader、Monochrome/FPM C# 构建和资源所有权离线测试。
`Grain` 与 `TwinsBackgroundShader` 超过 SDK 编译选项下的 ps_2_0 指令限制，改为 ps_3_0；
完整 Effect 文件目标仍为 fx_2_0。隔离客户端已实际加载全部 28 个效果，验证外部克隆参数隔离、混合批次与自动分块像素；
实验 Bootstrap 连续两次源码修改均更新运行中的 Effect（0.2 → 0.6 → 0.9）。
具体 Boss 战斗画面、UI/融合球的主观效果以及 VS 断点仍需人工验收。

离线资源所有权测试：`dotnet run --project Monochrome/Tests/ShaderResources/ShaderResources.csproj`。
FPM 的 Assets/Music 从 SDK AdditionalFiles 中排除，避免 SDK 2.1.2 生成的音乐 Loader
与 tML AutoloadMusic 重复注册；音乐资源仍正常打包，由 tML 加载。
