# 开发文档 / Development Guide

## 项目概览 / Overview

AR Solar System 是使用 Unity 和 Vuforia 开发的 Android 增强现实学习应用，面向博物馆等体验场景。用户扫描实体天体卡片，查看三维模型和介绍，并通过双行星实验比较重力、昼长、公转周期和大小。

AR Solar System is an Android augmented reality learning app built with Unity and Vuforia for hands-on experiences such as museum visits. Visitors scan physical celestial-body cards to explore 3D models and descriptions, then compare two planets through gravity, day length, orbital period, and size experiments.

## 开发环境 / Requirements

| 组件 / Component | 版本或要求 / Version or requirement |
| --- | --- |
| Unity Editor | 6000.3.23f1 (Unity 6.3 LTS) |
| Render pipeline | Universal Render Pipeline 17.3.0 |
| AR | Vuforia Engine 11.4.4，项目内本地包 / local package |
| Input | Unity Input System 1.20.0 |
| Target | Android；Unity Hub 中安装 Android Build Support、SDK、NDK 和 OpenJDK / Install Android Build Support, SDK, NDK and OpenJDK via Unity Hub |
| Assets | Git LFS |

安装 Git LFS 并拉取项目中的大文件，然后使用指定版本的 Unity 打开项目根目录，等待包解析和资源导入完成。

Install Git LFS and pull the repository's large files, then open the project root with the specified Unity version and wait for package resolution and asset import.

```sh
git lfs install
git lfs pull
```

`Packages/manifest.json` 引用本地的 `Packages/com.ptc.vuforia.engine-11.4.4.tgz`。若包无法解析，先检查 LFS 文件是否已完整下载。部署前应配置自己的 Vuforia 许可证，并检查其使用限制；客户端中的许可证配置不可视为秘密。

`Packages/manifest.json` references the local `Packages/com.ptc.vuforia.engine-11.4.4.tgz`. If resolution fails, check that the LFS file was downloaded. Configure your own Vuforia license before deployment and check its usage limits; client-side license configuration is not secret.

## 运行与交互 / Running and interaction

1. 打开 `Assets/Scenes/App/MainMenuScene.unity` 并进入 Play 模式；点击首页扫描入口进入扫描场景。/ Open `Assets/Scenes/App/MainMenuScene.unity`, enter Play mode, and select the scan control.
2. 对准受支持的实体卡片，显示对应模型；点击模型或名称查看介绍，拖动旋转，双指缩放。/ Point the camera at a supported physical card to display its model. Tap the model or name for information, drag to rotate, and pinch to scale.
3. 同时识别两张行星卡片后，可在实验面板中选择 JUMP、DAY、YEAR 或 SIZE，先预测，再观看演示和结果。三张或更多卡片时可切换比较对象。/ Track two planet cards to choose JUMP, DAY, YEAR, or SIZE in the lab; make a prediction, then watch the demonstration and result. With three or more cards, switch the compared pair.

识别需要兼容的 Android 摄像头，或在编辑器中配置摄像头。模拟跟踪可辅助检查逻辑，但不能代替实体卡片和真机测试。太阳和太阳系总览卡不参与行星重力比较。

Recognition requires a compatible Android camera or a configured editor webcam. Simulated tracking helps exercise logic but does not replace physical-card and device testing. The Sun and Solar System overview cards are excluded from planet gravity comparisons.

## 结构与职责 / Structure and responsibilities

| 路径 / Path | 职责 / Responsibility |
| --- | --- |
| `Assets/Scenes/App/` | 首页、共享扫描场景及保留的单天体场景 / Home, shared scan scene, and retained individual-body scenes |
| `Assets/Scripts/AR/` | 跟踪、模型、手势、标注、面板布局和实验 / Tracking, models, gestures, annotations, panel layout, and experiments |
| `Assets/Scripts/Data/` | 天体数据、实验规则与重力计算 / Celestial data, experiment rules, and gravity calculations |
| `Assets/Scripts/App/`, `UI/` | 场景导航与界面控制 / Navigation and UI controllers |
| `Assets/Scripts/Editor/` | 场景生成、修复和构建辅助工具 / Scene generation, repair, and build helpers |
| `Assets/Tests/EditMode/` | 编辑器模式测试 / EditMode tests |
| `Assets/Data/`, `Resources/`, `StreamingAssets/Vuforia/` | 数据资源、运行时资源及识别数据库 / Data assets, runtime resources, and tracking databases |
| `Assets/Settings/`, `Prefabs/`, `UI/` | 渲染设置、预制体与界面素材 / Render settings, prefabs, and UI assets |

`ScanController` 处理跟踪更新；`ScanPlanetContent` 创建模型并提供稳定的显示边界；`PlanetAnnotations` 和 `PlanetPanelLayout` 负责标注与面板；`PlanetTapTarget` 和 `PlanetPinchScale` 处理触摸；`GravityJumpComparison` 协调实验，`PlanetLab` 和 `GravityGuessRound` 分别提供实验规则与回合状态。

`ScanController` handles tracking updates; `ScanPlanetContent` creates models and supplies stable display bounds; `PlanetAnnotations` and `PlanetPanelLayout` position annotations and panels; `PlanetTapTarget` and `PlanetPinchScale` handle touch; `GravityJumpComparison` coordinates experiments, while `PlanetLab` and `GravityGuessRound` provide experiment rules and round state.

## 重力模型 / Gravity model

两名跳跃者使用相同的初速度（约 3.13 m/s），假设重力恒定且忽略空气阻力。播放速度为 0.45 倍；视觉模型尺寸不代表真实比例。气态及冰巨星的比较是在参考高度上的假想演示。

Both jumpers use the same initial velocity (approximately 3.13 m/s), assuming constant gravity and no air resistance. Playback runs at 0.45× speed; visual model sizes are not to scale. Comparisons on gas and ice giants are hypothetical demonstrations at a reference level.

```text
h(t) = max(0, v*t - 0.5*g*t²)
peak = v² / (2*g)
flight time = 2*v / g
```

数据来源及模型背景见 [NASA Planetary Fact Sheet](https://nssdc.gsfc.nasa.gov/planetary/factsheet/index.html?level=1)。/ See the [NASA Planetary Fact Sheet](https://nssdc.gsfc.nasa.gov/planetary/factsheet/index.html?level=1) for source data and context.

## 构建与测试 / Build and test

在 Unity Build Profiles 中切换到 Android，确认 `MainMenuScene` 和 `ScanScene` 已启用，然后使用 Build / Build and Run，或选择 `Solar System > Setup > Step6 Build Android APK`。当前构建设置仅启用这两个场景；其他单天体场景保留在项目中，但未加入构建。

Switch to Android in Unity Build Profiles, confirm that `MainMenuScene` and `ScanScene` are enabled, and use Build / Build and Run, or select `Solar System > Setup > Step6 Build Android APK`. The current build settings enable only these two scenes; individual-body scenes remain in the project but are not included.

通过 **Window > General > Test Runner > EditMode** 运行测试。测试覆盖重力计算、回合逻辑、名称查找和部分布局等；跟踪丢失、选对切换、旋转时锚点和首页导航仍需人工验证。发布前应在目标设备上测试实体卡片识别、不同光照和屏幕尺寸、触摸操作及性能。

Run tests via **Window > General > Test Runner > EditMode**. Tests cover gravity calculations, round logic, name lookup, and parts of layout; tracking loss, pair switching, anchors during rotation, and home navigation still need manual verification. Before deployment, test physical card recognition, lighting, screen sizes, touch, and performance on target devices.

## 维护注意事项 / Maintenance notes

- 修改卡片识别或数据时，同时检查 Vuforia 数据库、目标名称映射与场景绑定。/ When changing card recognition or data, check the Vuforia database, target-name mapping, and scene bindings together.
- 场景生成和修复工具可能修改场景或资源；执行前先保存工作并检查版本控制差异。/ Scene generation and repair tools can modify scenes or assets; save work first and inspect version-control changes afterward.
- `Library/`、构建输出等由 Unity 生成；大型资源使用 Git LFS。第三方资源遵循各自许可。/ `Library/` and build outputs are generated by Unity; large assets use Git LFS. Third-party assets retain their own licenses.
- 更多交互细节、已知限制及数据说明见 [README.md](README.md)。/ See [README.md](README.md) for further interaction details, limitations, and data notes.

## 从零理解项目 / Understanding the project from scratch

### 1. 项目要解决的问题 / Problem and goal

本项目把印刷的天体卡片作为交互入口：观众把卡片放在摄像头前，应用识别图像并在其位置显示三维内容。单卡用于了解天体，多卡用于对比行星。系统的主要挑战是将真实卡片的位置持续映射到虚拟模型，同时在移动设备的小屏幕上保留可读的说明和可操作的实验界面。

The project uses printed celestial-body cards as interaction points. The camera recognizes a card and places 3D content at its tracked position. A single card introduces a body; multiple cards enable comparisons. The main technical challenge is maintaining the mapping between physical cards and virtual models while keeping information and controls usable on a small mobile screen.

### 2. 技术如何配合 / How the technology fits together

Unity 负责场景、渲染、界面和交互；Vuforia 从摄像头画面识别图像目标并更新跟踪状态；C# 脚本把跟踪结果转换为模型显示、触摸操作和实验状态；URP 负责渲染。Android 提供设备摄像头和触摸输入。

Unity manages scenes, rendering, UI, and interactions. Vuforia recognizes image targets from the camera and updates tracking state. C# scripts turn those updates into model visibility, touch interactions, and experiment state. URP renders the scene, while Android supplies camera and touch input.

```text
实体卡片 / Physical card
        ↓ camera
Vuforia Image Target → VuforiaTargetController
        ↓ found / lost
ScanController → ScanPlanetContent → 3D 模型 / model
        ├→ PlanetAnnotations → 名称与介绍 / labels and profiles
        └→ GravityJumpComparison → 双行星实验 / two-planet lab
```

### 3. 第一次打开项目 / First-time setup

1. 安装 Git、Git LFS 和 Unity Hub；在 Hub 中安装 Unity `6000.3.23f1` 及 Android Build Support。/ Install Git, Git LFS, and Unity Hub; install Unity `6000.3.23f1` with Android Build Support.
2. 克隆仓库，在仓库根目录运行 `git lfs install` 和 `git lfs pull`。确认 `Packages/com.ptc.vuforia.engine-11.4.4.tgz` 已下载为实际文件。/ Clone the repository, run `git lfs install` and `git lfs pull` at its root, and confirm the local Vuforia package is present as an actual file.
3. 在 Unity Hub 中添加项目根目录并打开。首次导入可能需要一段时间；等待包解析和资源导入完成，查看 Console 中是否有阻断性错误。/ Add the project root in Unity Hub and open it. Initial import can take time; wait for package resolution and import, then check the Console for blocking errors.
4. 检查 Vuforia 配置、许可证和摄像头环境。打开首页场景进入 Play 模式；要验证真实识别，请使用摄像头和项目对应的实体目标卡。/ Check Vuforia configuration, licensing, and camera availability. Open the home scene and enter Play mode; use a camera and the matching physical targets to verify real recognition.
5. 从 Test Runner 运行 EditMode 测试，再切换 Android 平台并在设备上构建。/ Run the EditMode tests in Test Runner, then switch to Android and build on a device.

### 4. 一次扫描的生命周期 / Lifecycle of a scan

1. **识别 / Recognition:** Vuforia 根据项目中的图像目标数据库识别卡片，并更新目标的跟踪状态。/ Vuforia recognizes the card using the project's image target database and updates its tracking state.
2. **通知 / Dispatch:** `VuforiaTargetController` 将目标发现或丢失通知给 `ScanController`。/ `VuforiaTargetController` reports found or lost targets to `ScanController`.
3. **解析 / Resolution:** 控制器通过目标名称查找对应的行星数据和显示内容。/ The controller resolves the target name to planet data and display content.
4. **展示 / Presentation:** `ScanPlanetContent` 管理模型及其稳定边界，标注系统据此安排名称和说明。/ `ScanPlanetContent` manages the model and stable bounds used by the annotation system to place names and descriptions.
5. **交互 / Interaction:** 点击、拖动和缩放分别触发信息、旋转和尺寸调整；识别到足够的行星后，实验面板允许选择比较对象。/ Tap, drag, and pinch control information, rotation, and scale. Once enough planets are tracked, the lab allows the visitor to choose a pair.
6. **丢失 / Loss:** 卡片离开跟踪范围时，相应内容隐藏；若影响当前实验对象，实验回合会重置。/ When tracking is lost, the corresponding content is hidden; the round resets if the selected pair is affected.

### 5. 实验状态与扩展 / Lab state and extension

实验的基本流程是选择预测、开始演示、等待完成、显示结果，再重新开始。`GravityGuessRound` 管理回合状态，`PlanetLab` 保存各实验的数值、判断规则和文字，`GravityJumpComparison` 负责界面与动画。演示期间锁定预测和部分切换操作，以免结果与动画不同步。

A round proceeds through prediction, demonstration, result, and replay. `GravityGuessRound` manages round state, `PlanetLab` supplies experiment values, comparison rules, and text, and `GravityJumpComparison` coordinates presentation and animation. Prediction and some switching controls are locked while a demonstration runs so that the result remains consistent with it.

新增实验时，应先确定比较数据、胜负与平局规则，再实现演示及复位行为，检查切换卡片、丢失跟踪和重新开始时的状态，并为纯计算和状态转换添加有意义的 EditMode 测试。

When adding an experiment, define its data and win/tie rules first, then implement its demonstration and reset behavior. Check pair changes, tracking loss, and replay, and add meaningful EditMode tests for pure calculations and state transitions.

### 6. 日常开发与交付 / Development and delivery workflow

建议先在编辑器中修改和运行针对性测试，再用真实设备检查摄像头、卡片、触摸和性能。提交前检查场景和资源差异，避免把 `Library`、临时文件或本机构建产物作为源文件提交。生成 APK 后，在目标设备上安装验证完整流程；仓库中的现有 APK 不应被视为当前源码已验证的构建。

Make changes and run focused tests in the editor first, then check camera tracking, cards, touch, and performance on real hardware. Review scene and asset changes before committing; avoid committing generated `Library`, temporary files, or local build outputs as source. After producing an APK, install it on the target device and verify the complete flow. The existing APK in the repository should not be treated as a verified build of the current source.
