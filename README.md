# AR Solar System（VR Solar System）

基于 Unity + Vuforia 的 AR 太阳系科普 App：扫描实体卡片识别行星，在真实空间中展示可旋转、可交互的 3D 星球，并提供行星档案与测验。

## 环境

| 项 | 版本 |
| --- | --- |
| Unity | 6.3 LTS (6000.3.23f1) |
| 渲染管线 | URP 17.3.0 |
| AR SDK | Vuforia Engine 11.4.4（本地 tgz 安装） |
| 输入 | Input System 1.20 |
| 平台 | Android（Build Target: Android） |

## 运行

1. 用 Unity 6000.3.23f1 打开本目录。
2. 打开 `Assets/Scenes/App/LoginScene.unity`（Build Settings 第 0 个场景）。
3. 需要真机或 WebCam 播放模式才能跑 AR 识别。

已在 Build Settings 中的场景（13 个）：LoginScene → MainMenuScene → ScanScene，以及 Sun / Mercury / Venus / Earth / Mars / Jupiter / Saturn / Uranus / Neptune / SolarSystem 各行星详情场景。

## 目录

```
Assets/
  Scenes/App/        应用场景（登录、菜单、AR 扫描、各行星详情）
  Scripts/           运行时代码（AR 控制、数据、UI）
  Scripts/Editor/    场景/数据批量生成与修复工具
  Data/              行星 ScriptableObject 数据
  UI/                卡片、按钮等 UI 贴图
  Resources/         运行时 Resources（卡片贴图、Vuforia 配置）
  StreamingAssets/   Vuforia 识别库（.dat/.xml）
  Settings/          URP 管线与质量配置
  Prefabs/           土星环等预制体
  Inguz Media Studio/The Amazing Solar System/  星球贴图/网格/材质（第三方资源，已裁剪未使用部分）
```

## 仓库说明

- 二进制大文件（`*.psd`、`*.tgz`、`*.fbx` 等）走 **Git LFS**，克隆前先 `git lfs install`。
- `Library/`、`Build/`、`obj/`、`Logs/` 等生成物已在 `.gitignore` 中排除；首次打开会重建。
- `Assets/Resources/VuforiaConfiguration.asset` 内含 Vuforia License Key（客户端可见），公开仓库会泄露配额，建议保持仓库 **私有**。
- 开发时可用 [MCP for Unity](https://github.com/CoplayDev/unity-mcp) 让 AI 直接操作编辑器：`Packages/manifest.json` 中已指向官方 git URL（可选包，不影响构建；不需要可在 Package Manager 里移除）。

