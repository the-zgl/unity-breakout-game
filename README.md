# Unity 休闲小游戏（打砖块）+ 自动化测试

![CI](https://github.com/<你的用户名>/unity-breakout-game/actions/workflows/ci.yml/badge.svg)
![Unity](https://img.shields.io/badge/Unity-2022.3%20LTS-blue)
![C#](https://img.shields.io/badge/C%23-10%2B-239120)
![Tests](https://img.shields.io/badge/tests-30-brightgreen)

一个 **游戏测试开发** 方向的实践项目：用 **Unity 2D + C#** 做一个可直接游玩的休闲小游戏，
并把核心业务逻辑抽成纯 C# 类、用 **Unity Test Framework** 覆盖，接入 **GitHub Actions** 持续集成。

> 目标：`clone` 下来打开工程按 **Play** 就能玩；不装 Unity 也能用 `dotnet` 验证核心逻辑是对的。
>
> **当前状态（本机实测，非设想）**：Unity 2022.3.62f1 下 **EditMode 28 条 + PlayMode 2 条 = 30 条全绿**；
> 纯 C# 逻辑自检 `dotnet run --project tools/logic-smoke` **34 项断言全部通过**（无需 Unity License）。
> **未验证项**：没有在真机 / ARM 设备上验证（x86_64 模拟器不被 Unity Android 构建支持，见 README「为什么暂时不装 Android Build Support」）。

## 技术要点

本项目主要覆盖以下技术点：

| 技术点 | 实现方式 |
|---|---|
| C# | 全部游戏逻辑与测试为 C#；核心逻辑与表现层分离 |
| Unity 引擎 | GameObject/Component、Rigidbody2D 物理、Collider2D、uGUI、Sprite、场景装配 |
| 自动化测试 | Unity Test Framework（NUnit）30 条用例，含场景装配冒烟测试与 PlayMode 物理测试 |
| 账号、支付业务测试要点 | `PaymentService`：下单 → 支付回调 → 发货状态机，覆盖幂等/金额不符/掉单/退款 |
| Git、CI/CD | GitHub Actions：PR 阶段跑纯 C# 逻辑自检（无需 Unity License），可选跑官方 Test Runner |
| 客户端性能/适配意识 | 球速在 `FixedUpdate` 里 normalize、用完全弹性材质保证物理表现可预测（可断言） |

## 一句话介绍

一款 Unity 2D 打砖块小游戏：挡板、小球、55 块砖、计分、生命、金币商店（**模拟支付**购买生命）；
业务逻辑全部抽成纯 C# 类，配 30 条自动化测试，并在 CI 里做秒级逻辑校验。

## 目录结构

```
unity-breakout-game/
├── Assets/
│ ├── Scripts/
│ │ ├── GameState.cs # 纯 C# 状态：计分 / 生命 / 金币 / 购买（可单元测试）
│ │ ├── PaymentService.cs # 纯 C# 支付：下单 → 回调 → 发货 / 退款 / 幂等
│ │ ├── GameManager.cs # 总控：持有 GameState 与 PaymentService
│ │ ├── Bootstrap.cs # 运行时装配整个场景（相机/砖墙/挡板/小球/HUD/按钮）
│ │ ├── ShapeFactory.cs # 程序化生成 Sprite（仓库不放美术素材）
│ │ ├── PaddleController.cs # 挡板控制（键盘 + 鼠标）
│ │ ├── Ball.cs # 小球物理、出界判定
│ │ ├── Brick.cs # 砖块受击与销毁
│ │ ├── ShopManager.cs # 商店入口
│ │ └── UIManager.cs # HUD 刷新与提示条
│ ├── Editor/
│ │ └── SceneGenerator.cs # 菜单：Breakout → 生成主场景并设为启动场景
│ └── Tests/
│ ├── EditMode/
│ │ ├── GameStateTests.cs # 13 条：计分/金币/生命/购买/重置
│ │ ├── PaymentServiceTests.cs # 13 条：下单/回调/幂等/发货/退款/参数校验
│ │ ├── BootstrapSmokeTests.cs # 2 条：场景装配冒烟
│ │ └── Tests.asmdef
│ └── PlayMode/
│ ├── PlayModeSmokeTests.cs # 2 条：PlayMode 运行 + 球速恒定
│ └── PlayModeTests.asmdef
├── Packages/manifest.json # 依赖：test-framework、ugui、physics2d 等
├── ProjectSettings/ # 工程配置（ProjectVersion / BuildSettings）
├── tools/logic-smoke/ # 不装 Unity 也能跑的逻辑自检（34 项断言）
├── .github/workflows/ci.yml
└── README.md
```

## 环境准备

| 工具 | 版本建议 | 验证方式 |
|---|---|---|
| Unity Hub + Unity 编辑器 | 2022.3 LTS（与 `ProjectSettings/ProjectVersion.txt` 一致） | Unity Hub 里能看到已安装版本 |
| .NET SDK | 6.0 或更高 | `dotnet --version`（注意：Windows 上可能装在 `C:\Program Files (x86)\dotnet`，需要加进 PATH） |
| Git | 任意 | `git --version` |

> 装 Unity 时，**必须勾选** `Windows Build Support (IL2CPP)` 或对应平台的 Build Support；
> 只想跑测试和编辑器的话，`Microsoft Visual Studio` 组件不必装。

## 部署与运行

### 第 1 步：先用 30 秒验证逻辑是对的（不需要 Unity）

```powershell
cd tools\logic-smoke
dotnet run --project LogicSmoke.csproj
```

预期输出结尾：

```text
通过 34 项，失败 0 项。
逻辑自检全部通过 ✅
```

这一步的意义：把 `GameState` / `PaymentService` 这两个纯 C# 文件直接编进标准 .NET 工程，
证明「业务逻辑不依赖 Unity 也能跑」——这正是它们可被单元测试的前提。

### 第 2 步：用 Unity 打开工程并生成场景

1. Unity Hub → `Add` → 选择本仓库根目录（含 `Assets/`、`ProjectSettings/`、`Packages/` 的那一层）。
 - 若提示编辑器版本不一致，选「用当前已安装的 2022.3.x 打开」，让它升级工程即可。
2. 首次打开会自动导入资源、生成 `Library/`（比较慢，属正常）。
3. 菜单栏点击 **Breakout → 生成主场景并设为启动场景**。
 - 这会在 `Assets/Scenes/Main.unity` 生成场景，并写入 Build Settings。
 - 场景里只有**一个**挂着 `Bootstrap` 的空物体；其余对象全部由代码在运行时创建。
4. 按 **Play**。

### 第 3 步：玩起来

- `A` / `D` 或 `←` / `→` 移动挡板，也可以按住鼠标拖动
- 每砸掉一块砖得 100 分，每累计 10 分得 1 金币
- 右下角「购买生命 (20 金币)」按钮 = 模拟支付，金币足够才成功
- 小球掉出底部扣一条命，3 条命用完显示「游戏结束」
- 按 `R` 一键重开

### 第 4 步：跑测试

在 Unity 编辑器里：菜单 `Window → General → Test Runner`
- 切到 **EditMode** → `Run All` → 28 条通过
- 切到 **PlayMode** → `Run All` → 2 条通过

命令行方式（需要 Unity 已安装，`<UNITY_PATH>` 换成你的编辑器路径）：

```powershell
& "<UNITY_PATH>\Unity.exe" -batchmode -runTests -projectPath . `
 -testPlatform EditMode -testResults results-editmode.xml -logFile -
```

## 上传到 GitHub

```powershell
cd "D:\path\to\unity-breakout-game"
git init
git add .
git commit -m "feat: Unity 打砖块小游戏 + 单元测试 + CI"
git branch -M main
git remote add origin https://github.com/<你的用户名>/unity-breakout-game.git
git push -u origin main
```

**注意**：`.gitignore` 已排除 `Library/`、`Temp/`、`Build/` 等生成目录，但**保留**了
`tools/logic-smoke/LogicSmoke.csproj`（CI 需要它）。不要手工把 `Library/` 提交上去，
它会带来几百 MB 的无意义文件与冲突。

## CI 说明

| job | 触发条件 | 做什么 |
|---|---|---|
| `syntax-and-logic` | 每次 push / PR | 用 .NET 编译并运行 `tools/logic-smoke`，34 项断言全绿才算通过（**不需要 Unity License**） |
| `unity-editmode-tests` | 仓库 Variable `ENABLE_UNITY_CI=true` | 用 `game-ci/unity-test-runner` 在容器里跑 EditMode 用例，并上传测试结果 artifact |

第二个 job 默认跳过，因为它需要 Unity 授权。想开启：
1. `Settings → Secrets and variables → Actions → Variables` 新建 `ENABLE_UNITY_CI` = `true`
2. 在 `Secrets` 里添加 `UNITY_LICENSE`、`UNITY_EMAIL`、`UNITY_PASSWORD`
 （game-ci 官方文档有获取 `.ulf` 授权文件的步骤）

## 核心知识点

- **GameObject / Component / Transform**：Bootstrap 是如何用 `new GameObject` + `AddComponent`
 把整个场景搭出来的
- **Rigidbody2D 与 Collider2D**：小球为什么要在 `FixedUpdate` 里 normalize 速度（物理引擎会带来速度漂移）；
 完全弹性材质 + 零重力让弹跳可预测，测试断言才稳定
- **碰撞 vs 触发**：砖块用 `OnCollisionEnter2D`（实体碰撞），底部出界区用 `OnTriggerEnter2D` + `isTrigger`
- **为什么不用 Tag 判断碰撞对象**：Tag 依赖 `ProjectSettings/TagManager` 配置，克隆后容易漏配；
 改用组件判断（`GetComponent<Ball>`）与静态单例（`Ball.Instance`），更健壮
- **可测试性设计**：把计分/购买/支付从 `MonoBehaviour` 抽成纯 C# 类，才能被单元测试直接实例化断言
- **Arrange–Act–Assert**：所有单元测试都按这三段式组织，便于评审
- **支付测试要点**：渠道会重试（必须幂等）、金额可能不符、订单可能过期、货只能发一次
- **测试金字塔**：纯逻辑单测（快、多）→ 场景装配冒烟（中）→ PlayMode 物理测试（慢、少）

## 常见问题与设计取舍

1. 碰撞检测怎么做的？物理碰撞与触发器有什么区别？
2. 为什么把逻辑抽到 `GameState`，而不是全写在 `MonoBehaviour` 里？
3. 支付你是怎么处理的？真实项目要注意什么？
4. 怎么保证小球速度恒定？为什么要用 `FixedUpdate`？
5. 单元测试和集成测试的区别？这个项目里哪些算单元测试、哪些算集成测试？
6. Unity 游戏能不能用 Appium 做自动化？为什么？
 （答：Unity 用 OpenGL/Skia 渲染，Appium 依赖 Android 原生控件树，识别不到游戏画面内的 UI；
 游戏自动化要用 Unity Test Framework、Airtest/Poco 这类基于图像或引擎内注入的方案）

## 可扩展方向

- 加**道具掉落**与关卡/难度系统，并补对应边界用例
- 给 `PaymentService` 加**掉单补偿与对账**逻辑，模拟渠道回调丢失后的补单流程
- 用 `Unity Profiler` / PerfDog 采集帧率与内存，做一次**性能专项**并留下数据结论
- 增加一个「游戏内测试面板」，在真机上手动触发关键用例并展示通过率

## 相关文档

- 部署到 GitHub 的完整教程见上一级目录：`../教程-项目1-Unity游戏-部署到运行到GitHub.md`
- 逐行代码讲解见：`代码逐行讲解.md`
