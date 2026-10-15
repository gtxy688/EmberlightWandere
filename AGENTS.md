# EmberlightWandere · 项目根指令

本文件是本仓库的 AI 协作规则入口。**改代码或文档前先读本文件**，再按下面的路由进入对应文档。

## 工程事实

| 项 | 值 |
|----|----|
| Unity | **2022.3.62f3** |
| 主场景 | `Assets/Scenes/Emberlight.unity`（唯一启用场景） |
| 命名空间 | `Emberlight`；编辑器为 `Emberlight.Editor` |
| 脚本根 | `Assets/Scripts/{Core,Combat,UI,Visuals,Audio}` + `Assets/Editor` |
| 文档根 | `Docs/` |
| 程序集 | **有自建 asmdef**（2026-10-03 起）：`Emberlight.Runtime`（`Assets/Scripts`）→ `Emberlight.Editor`（`Assets/Editor`）+ `Emberlight.EditMode.Tests`（`Assets/Tests/EditMode`）+ `Emberlight.PlayMode.Tests`（`Assets/Tests/PlayMode`）。本项目代码已不在 `Assembly-CSharp` |
| 自动化测试 | `Assets/Tests/EditMode` 与 `Assets/Tests/PlayMode`；保留测试源码和运行工具 |
| 跑测试 | `powershell -ExecutionPolicy Bypass -File Tools/run-tests.ps1 -Platform EditMode`；PlayMode 换平台参数，带图形执行；脚本校验真实退出码与 XML 用例数 |
| 离线编译校验 | `powershell -ExecutionPolicy Bypass -File Tools/compile-check.ps1`（Unity 开着也能跑；**只覆盖 Runtime，不含测试程序集**） |
| 文档行号引用校验 | `powershell -ExecutionPolicy Bypass -File Tools/doc-line-check.ps1`（校验 `Docs/` 里 `` `File.cs:NN`（`标识符`） `` 一类引用是否仍指对地方；`-Fix` 可自动改写漂移的行号） |
| Unity 批处理编译 | `powershell -ExecutionPolicy Bypass -File Tools/unity-batch-verify.ps1`（需先关编辑器，且在普通终端运行） |
| GitHub | `https://github.com/gtxy688/EmberlightWandere.git` |

> ⚠️ 旧路径 `Assets/Emberlight/Scripts/...` **已废弃**，以 `Assets/Scripts/...` 为准。

## 文档路由

文档按职责分层，各有明确权威范围：

| 文档 | 回答 | 权威范围 |
|------|------|----------|
| `Docs/requirements.md` | 做什么、为什么、约束与数值 | **产品行为与约束** |
| `Docs/architecture/00-overview.md` | 有哪些模块、依赖与任务路由 | **模块边界与唯一任务索引** |
| `Docs/architecture/0X-*.md` | 如何实现、公开契约和影响范围 | **当前模块设计** |
| `Docs/progress.md` | 当前进度与阻塞 | **状态记录，不定义需求或架构** |
| `Docs/reference/*` | 素材来源与授权 | 参考资料 |
| `Docs/archive/*` | 历史决策与踩坑记录 | **只读历史，不要从归档实现功能** |

**不承担权威、只作说明**（与代码不一致时以代码和上面的权威文档为准）：

| 文档 | 用途 |
|------|------|
| `Docs/技术分析.md` | 面向阅卷人的设计取舍与边界说明 |
| `Docs/实现说明.md` | 实现要点、调用链和编辑器工具用法 |

### 新任务怎么开始

1. 打开 `Docs/architecture/00-overview.md` 的**任务索引**，用关键词定位主要模块。
2. 声明准备读取的最小文档集并继续；只有映射不唯一、缺少权威信息或影响范围不清楚时才等 jack 裁定。
3. 最小文档集通常包括：当前模块架构和它引用的 `requirements.md` 章节。
4. 存在显式依赖或回归影响时，只补读相关模块的「对外契约/影响范围」及相关测试源码。

> 不要因为"可能有用"而通读整个 `Docs/`，也不要因为"禁止跨模块"而拒绝读取已声明依赖的契约。

### 行号引用怎么写

文档里引用源码位置时，**必须带标识符**，否则行号漂了没人发现：

- ✅ `` `Assets/Scripts/Core/EmberGame.cs:101`（`cam.orthographic`） ``
- ❌ `` `Assets/Scripts/Core/EmberGame.cs:101` ``（动了代码就没人知道它还指不指对）

校验：`powershell -ExecutionPolicy Bypass -File Tools/doc-line-check.ps1`；漂移就加 `-Fix` 自动改写行号。**改完代码先跑这个脚本，再提交文档。**

## 冲突处理（重要）

现行文档与源码都需要核对，不能假设冲突只存在于归档。规则：

1. **`requirements.md` 是玩法与可调表现数值的唯一权威**。架构写参数名、计算关系与归属，类型/Id/分支等结构说明可保留；维护边界见需求 §7。
2. 归档文档**只作历史**。引用归档里的数值当作现行口径是错误做法。
3. 发现不一致时：**暂停受影响的改动** → 列出双方位置与差异 → 说明裁定影响 → 拿到裁定后再改 → 同步更新权威文档。**不要静默选边。**
4. 代码与测试是**当前实现证据**，不会自动覆盖文档中的意图；反之文档也不等于代码现状。二者不符时同样走上面的流程。

### 已确定口径

玩法与数值以 `Docs/requirements.md` 为唯一需求入口；模块架构记录当前实现契约，归档只解释历史原因。保留现有主题、单武器攻击力/攻速、波次驱动和工程源码交付；不从旧文档恢复已删除功能。

## 硬约束（违反即事故）

1. **中文字体已改为预烘静态图集，改动前必读 `Docs/architecture/07-art-presentation.md`**。预烘曾因资产写入顺序和图集一致性导致 UI 失效，即使资产字段校验通过也不代表画面正确。**不要删掉 `EmberFonts.ValidateBaked` 那道运行时闸门**，也不要绕过它直接把未校验的字体资产塞给运行时——闸门是"资产坏了就回落动态图集"的唯一保证。**静态图集不能追加字形，改任何文案后必须重跑烘制**：
   ```
   python Tools/atlas_glyph_check.py      # 先查缺口，退出码非 0 表示有字没烘进去
   Unity.exe -batchmode -quit -projectPath <项目> -executeMethod Emberlight.Editor.EmberFontBake.Bake
   ```
   该脚本直接读编译后的 **`Library/ScriptAssemblies/Emberlight.Runtime.dll`**（2026-10-03 起游戏代码在本项目自建程序集里；脚本已同步默认路径，找不到时回落 `Assembly-CSharp.dll`）的字符串堆，能看见**方法体内的内联字面量**，因此是"文案全在盘上"的唯一可靠判断依据。它还会**显式检查 TMP 的两个特殊字符** `U+005F`（下划线）与 `U+2026`（省略号）——TMP 每次给 `TextMeshProUGUI` 赋字体都会去字体里找这两个，缺一个就每个 label 报一条 `The character used for Underline is not available` 警告。`U+005F` 是 ASCII，看着"显然存在"，但**没有任何游戏文案含它**，手写白名单和字面量扫描都抓不到，所以靠脚本兜。**批处理必须先关 Unity**，否则 `HandleProjectAlreadyOpenInAnotherInstance` 直接 crash。
2. **不要引入全局攻击力**，也不要把攻击力/攻速改回全局作用域——会让早期伤害膨胀。
3. **不要恢复局外成长、XP / 经验条、`Elapsed >= 480` Boss 触发、ScoreMult 随 N 变化**。见 `requirements.md` §6。
4. **开局不可跳过**：必须选 N 并单选 1 把武器；**禁止开局一次填满 N 槽**。
5. **不要在 `Assets/Scripts/Combat/Weapons/` 里引用 `EmberGame`**。武器只能通过 `EmberWeaponContext` 与 `RunProgress` 工作——这是本仓库最干净的边界。
6. **不要在 `Assets/Scripts/UI/` 里引用 `EmberGame`**。UI 通过方法参数接收只读数据。
7. **`Assets/Scripts/Combat/`（含 Boss/掉落）与 `Assets/Scripts/Visuals/` 也不得引用 `EmberGame`**（2026-09-23 起）。战斗走 `IEmberCombatHost`，表现通过 `Initialize` 注入阶段委托。`EmberGame` 是**组合根**：只做装配、阶段所有权、每帧顺序与**结果转译**（把别人算好的结果变成一次 UI 调用 / 音效 / 阶段切换请求），**不要在这里放数值规则或状态推进规则**。详见 `Docs/architecture/00-composition-root.md` 的「职责边界」。
8. **四个图集 `.meta` 现在是真正的 sprite sheet，切片权威在那里**。四个 `AssetPostprocessor` 导入器（`Ember*ArtImporter`）负责把 `textureType` 设为 `Sprite`、`spriteImportMode` 设为 `Multiple`，**不要让它们改回 `Default`**——历史上它们强制 `Default`，导致每次重新导入都把切片清掉、UI 退回运行时切图。切图本身用 Sprite Editor 做，格子与命名保持「整格均分 + `<atlas>_<index>`（行主序，左上起）」。
9. **新增音频后必须检查导入设置**为 `3D: 0` + `preloadAudioData: 1`（新 WAV 默认是 3D + 不预载，表现为"资源在但听不到"）。
10. **UI 与字体改动必须在编辑器里真进一局确认**。资产校验替代不了真跑一遍。
11. **改含中文的源文件不要用 PowerShell 的 `Get-Content -Raw` + `Set-Content`**：Windows PowerShell 5.1 按 ANSI 读 UTF-8 无 BOM 文件，会把中文注释写成乱码（2026-09-23 实际踩到）。用编辑工具，或显式 `-Encoding UTF8` + `[System.IO.File]::WriteAllText`。

## 改动约定

1. **先读再改**：动战斗节奏前读 `EmberCombat` / `EmberBoss` / `LevelConfig`；动选卡前读 `RunProgress` / `EmberRarity`；动 UI 前读 `EmberMenuUi` / `EmberHud` / `EmberUpgradePanel`。
2. **数值集中在配置类**：新增可调数值应进 `LevelConfig`（或对应模块的配置归属），不要在战斗逻辑里写魔法数。
3. **小步可回退**：一次只改一个闭环问题。
4. **同步文档**：改了公开行为或数值，必须同一批改动内更新 `requirements.md` 对应章节和该模块架构的「对外契约/影响范围」。现行文档只描述当前可用实现，及时清理未实现提案、已删除 API 和已完成的待办。
5. **数值与规则**以 `requirements.md` 与 xiaoPlanner 规格为准。
6. **安卓真机排最后**：未完成游戏闭环前不主动扩 APK / 真机专项。
7. **不要派工风暴**：除 jack 明确要求，Agent 之间不互相催办、不群发任务；需要协作时回 jack 点名。

## 开发检查与交付

- 按 jack 的要求，不生成独立验收文档、符合性核查报告或维护验收记录。
- 保留自动化测试源码和检查工具；修改代码时仍运行相关检查，回复中如实说明实际执行的结果。
- 修改 UI 或字体后，在编辑器里进一局检查实际显示与交互；手感和审美由 jack 判断。
- 最终交付完整 Unity 工程及源码，包含资源、`.meta`、Packages、ProjectSettings 和必要说明。

## 自检（改完至少做）

按实际改动范围执行。纯开发文档改动检查引用、路径、契约与差异格式，无需为此启动游戏或重烘字体。

- [ ] **若改了源码，先跑离线编译**：`powershell -ExecutionPolicy Bypass -File Tools/compile-check.ps1`（退出码 0）
- [ ] **跑 EditMode 测试**（改了 M2 / 纯逻辑时必做）：见上表命令，要求 `failed=0` 且 Unity 退出码 0
- [ ] 若改了运行时可见文案：`python Tools/atlas_glyph_check.py`（退出码 0，开发文档不适用，见硬约束 1）
- [ ] Unity 编译无报错；Console 进主菜单无错误
- [ ] 能开一局：选长度 → 选难度 → 选槽位 → 选武器 → 升级 → 暂停 → 胜或负
- [ ] 若动 Boss / 刷怪：波次推进与 Boss 出场符合 `requirements.md` §3
- [ ] 若动选卡：候选数量、同屏唯一、补偿选卡、刷新次数符合 `requirements.md` §4
- [ ] 若动 UI / 字体：在编辑器里真进一局，中文无方框、Console 干净
- [ ] 若动 `Scripts/Combat` 或 `Scripts/Visuals`：确认其中零 `EmberGame` 引用（硬约束 7）
- [ ] **若动了被文档引用行号的源码**：跑 `Tools/doc-line-check.ps1`（退出码 0）；漂移就用 `-Fix` 改写
- [ ] 已回写受影响的文档章节

## 角色

| 角色 | 负责 | 不负责 |
|------|------|--------|
| jack | 拍板、试玩反馈、优先级、手感与审美判断 | — |
| xiaoProducer | 拆计划、标依赖/风险、指该找谁 | 不大段写 C#；不定数值终稿；不主动派工 |
| xiaoPlanner | 玩法/数值/波次规格 | 不写大段引擎代码 |
| xiaoCoder | C#、系统、性能、可运行改动 | 不定玩法终稿 |
| xiaoLevel | 关卡节奏、遭遇、场景 checklist | 不大改核心玩法代码 |
| xiaoUI | HUD/菜单/交互说明 | 不定数值 |

未指派时：文档归 Producer/Planner；代码归 Coder；关卡表归 Level；界面归 UI。

## 文档健康度（每次交付前检查）

- [ ] 模块文档引用的 `requirements.md` 章节仍然存在
- [ ] 「对外契约」与实现一致
- [ ] 影响范围反映本次行为变化及相关模块
- [ ] 新任务关键词已加入 `00-overview.md` 的**唯一**任务索引
- [ ] 调参表只在 `requirements.md` 维护；模块正文没有并列的历史数值表
- [ ] Markdown 链接、锚点以及反引号中的具体文件路径有效，Tools 下说明也纳入检查
- [ ] 有效契约合入对应正文，已完成变更不再按日期追加第二套说明
- [ ] `progress.md` 没有被当成需求来源
- [ ] 归档文档没有被当作现行口径引用
