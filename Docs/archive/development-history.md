# 开发历史（只读归档）

> **归档说明**：本文件是原 `Docs/progress.md` 的历史轮次记录，2026-10-14 移入归档。
> 它**不定义需求或架构**；其中的数值与结论可能已被后续口径覆盖。现行状态见 [`../progress.md`](../progress.md)，权威需求见 [`../requirements.md`](../requirements.md)，已裁定的历史冲突见 [`README.md`](README.md)。
> 保留本文件的价值在于：**每轮改动做了什么、实测输出是什么、踩过哪些坑**——尤其是工具链与字体的教训，避免重复踩。

## 时间线速览

| 日期 | 轮次 | 主题 | 结果 |
|------|------|------|------|
| 2026-09-15 | 字体与切图 | 中文字体改预烘静态图集；图集切为真 sprite sheet；选武器改滚动 | 批处理编译通过；EditMode 尚未建立 |
| 2026-09-23 | 第一轮 | 拆解上帝类 `EmberGame`（361 行） | 离线编译 53 文件 0 警告 |
| 2026-09-23 | 第二轮 | 拆 `RunProgress`，让 M2 真正零 `UnityEngine` | 离线编译 56 文件；Unity 批处理全量重编通过 |
| 2026-09-23 | 第三轮 | 拆 `EmberCombat`（509 行） | 离线编译 60 文件；抓到两个真实缺陷 |
| 2026-10-03 | 第四轮 | 建立自动化测试基础设施（首个真实测试套件） | EditMode 103/103 |
| 2026-10-13 | 可维护性与交付整理 | 依赖收窄、文案集中、卡面/说明修复、导出脚本 | EditMode 133/133；PlayMode 2/2 |
| 2026-10-13 | UIRoot 图片修复 | Prefab 图片持久化与修复工具 | EditMode 137/137；PlayMode 2/2 |
| 2026-10-14 | UIRoot 运行时接入 | 场景绑定 Prefab，运行时不再建 UI | EditMode 137/137；PlayMode 3/3 |
| 2026-10-14 | 加载页预制体化 | 新增 `IntroRoot.prefab` 与 `EmberIntroView` | EditMode 137/137；PlayMode 4/4 |
| 2026-10-14 | 提交前优化 | 屏外刷怪、UI 创建边界、HUD 缓存 | **EditMode 150/150；PlayMode 4/4** |
| 2026-10-14 | 注释精简与文档重构 | 精简冗余注释、修正文档过期断言 | EditMode 待重跑；离线编译 67 文件通过 |

## 2026-10-14 提交前优化（第 1、3、4 项）

普通/遭遇杂兵改用完整视口外沿带取点，地图边缘仍保证屏外；无合法区域时延后生成且不消耗配额。旧 UI 动态创建回退已删除，控件工厂和旧快照补建移至 Editor，运行时只绑定 Prefab。HUD 文字按对应值变化刷新，时间按秒更新，波次文案只由 Stage 写入。

新增 13 条 EditMode 回归，最终 150/150、图形 PlayMode 4/4，Unity 均退出 0；离线编译、字形覆盖与 Editor Prefab 配置检查通过。刷怪节奏、当前编辑器观感与手感待 jack 验收；debuff 方案由 jack 考虑，最终工程打包由 jack 完成。

## 2026-10-14 UIRoot 运行时引用

主场景已绑定 UIRoot Prefab，停止 Play 可预览主菜单；运行时复用视图与控件，保留字体闸门，菜单/开局选择/设置/升级替换最新回调。Editor 连接工具与持久图片已保存。离线编译、字形检查、EditMode 137/137、图形 PlayMode 3/3 通过。

加载页补齐：新增 IntroRoot Prefab 与场景引用，保留字体闸门、加载时序和继续输入，运行时绑定已有控件并在结束时隐藏。完整 EditMode 137/137、图形 PlayMode 4/4。Editor API 验证图片持久化及重连保留布局，渲染截图已检查。

## 2026-10-03 第四轮：补上自动化测试基础设施（第一套真实测试）

**动因**：前三轮重构的核心论据是"这些逻辑纯了、可以单测了"，但**一个测试都没写**——`RunProgress`、`EmberChoicePool`、`EmberWeaponCatalog`、`EmberRarityUtil` 已是零 `UnityEngine` 引用，却没有任何断言守着它们。

| 主题 | 内容 |
|------|------|
| **程序集边界** | 新增 3 个 asmdef：`Emberlight.Runtime`（`Assets/Scripts`）→ `Emberlight.Editor`（`Assets/Editor`）+ `Emberlight.EditMode.Tests`（`Assets/Tests/EditMode`）。**本项目代码已不在 `Assembly-CSharp`** |
| **EditMode 套件** | 4 个测试类共 **103 个用例**：`EmberWeaponCatalogTests` 18、`EmberRarityUtilTests` 25、`EmberChoicePoolTests` 25、`RunProgressTests` 35 |
| **覆盖范围** | M2 全覆盖：Id 分区与名册映射、品质权重三锚点与插值、五张通用词条数值表、`3+空位` 张数、`(Id, WeaponId)` 同屏唯一、满槽不出新武器、旧通用不回流、开局构筑与槽位钳制、攻击力/攻速单武器作用域、护盾叠加、刷新 2 次、护盾先于生命、计分 |
| **工具修复** | `Tools/atlas_glyph_check.py` 默认读取的程序集同步为 `Emberlight.Runtime.dll`（**否则无参调用会静默扫空集并报"覆盖完整"**），并补上"找不到程序集 / 字面量为空"的显式报错 |

### 本轮实测结果（2026-10-03）

| 检查 | 命令 | 结果 |
|------|------|------|
| **EditMode 测试** | `Unity.exe -batchmode -nographics -projectPath <项目> -runTests -testPlatform editmode -testResults Library/TestRun/editmode.xml` | **通过**：`total=103 passed=103 failed=0 skipped=0 result=Passed`，Unity 退出码 **0** |
| 字形覆盖（无参调用） | `python Tools/atlas_glyph_check.py` | **通过**：`MISSING (0)`，TMP 特殊字符齐全，退出码 0 |
| Unity 批处理编译 | `Tools/unity-batch-verify.ps1` | **通过**（asmdef 导入后重编，0 条 `error CS`） |
| 真进一局 | — | **待人工验收** |

### 测试暴露的三个既有问题（当时记录，未改产品行为）

| # | 现象 | 影响 | 处置 |
|---|------|------|------|
| 1 | **中间幸运值的品质权重和不是 100**：15 → `69/24/7/2 = 102`、45 → 102、75 → 101、90 → 101（四通道各自四舍五入所致）。锚点 0/60/120 恰好为 100 | `RollWeighted` 以权重和为分母，故这些幸运档的实际出率有约 1–2% 相对偏移 | 已用测试**钉住漂移 ≤ 2**；是否归一化仍**待裁定** |
| 2 | **`ConfigureRun` 不是"开局只给 1 把"的守卫**：传 2 把 + `slots=3` 会真的给 2 把 | 当前 UI 只传 1 把，线上行为不受影响；但从别处传多把会绕过硬约束 4 | 已用测试记录真实行为并注明"若加守卫则应红灯"；仍**待裁定** |
| 3 | **`RunProgress.AddShield` 是死代码**，且把护盾硬钳到 **250** = "护盾 /2"之前的旧口径 | 无运行时影响，但**是陷阱** | 已加 XML 警告并指向真正的发放点；**2026-10-14 已直接删除该方法** |

## 2026-09-23 第三轮：拆 `EmberCombat`

**动因**：`EmberCombat.cs` 509 行把波次状态机、刷怪参数、伤害结算、死亡处理、分裂与遭遇压在一个类里。其中 `ApplyDamageFrom` + `ResolveDeaths` + `TryBeginRebirth` 约 90 行是一条"扣血 → 闪白 → 击退 → 重生 → 行为 OnLethal → 判定死亡 → 掉落 → 记分 → 分裂子体 → 触发清波"的长链。

| 主题 | 内容 | 行数 |
|------|------|------|
| **波次推进抽离** | 新增 `EmberWaveDirector`：配额、刷怪间隔、Boss 预警、遭遇事件、清波判定、HUD 只读量。经 `Host` 契约请求刷怪，**自己不造敌人、不含伤害** | 216 |
| **敌人实例化抽离** | 新增 `EmberEnemyRoster`：种类/落点/颜色/体型/血量/词缀/部件，以及对象池的借出与归还 | 160 |
| **伤害与死亡结算抽离** | 新增 `EmberCombatDamage`（纯静态）：正面减伤、闪白击退、重生词缀、记分、掉落触发。经 `Outcome` 回报击杀数与 Boss 阵亡 | 143 |
| **敌人数据类型提升** | `EmberCombat.Enemy` 嵌套类 → 顶层 `EmberEnemy` | 62 |
| **战斗根瘦身** | `EmberCombat` 509 → 328 行，只保留：拥有武器、装配单元、每帧顺序、接回调 | 328 |

### 本轮抓到的两个真实缺陷（都在重构过程中发现并修掉）

1. **Boss 被逐帧双推**：把 Boss 单独 Tick 了一次，同时它仍在名册里由 `TickEnemies` 的 `ai` 分支推进——效果是 Boss 招式速度翻倍。已删除多余调用，并在代码里留注释警告。
2. **`ResolveDeaths` 的"待分裂"标志是多余的**：真正决定"生成多少子体"的是 `pendingBroods` 里的落点条数，标志位只用在一个 `if` 条件里，而两个分支内容相同。已删掉该字段。

### 本轮实测结果（2026-09-23）

| 检查 | 命令 | 结果 |
|------|------|------|
| 离线编译 | `Tools/compile-check.ps1` | **通过**：60 源文件、0 错误、0 警告 |
| **Unity 批处理全量重编** | `Tools/unity-batch-verify.ps1`（先删 `Library/ScriptAssemblies`） | **通过**：`Assembly-CSharp.dll` 重新生成，0 条 `error CS` |
| 字形覆盖 | `Tools/atlas_glyph_check.py` | **通过**：`MISSING (0)`，TMP 特殊字符齐全 |
| 边界（源码扫描） | `Combat/`、`Visuals/`、`UI/` 的 `EmberGame` 引用 | **通过**：三处均零代码引用 |
| 真进一局 | — | **待人工验收** |

> ⚠️ 本轮动的是战斗主循环、刷怪与死亡结算，是三轮里**运行时风险最高**的一轮。

## 2026-09-23 第二轮：拆 `RunProgress`，并让 M2 真正零 `UnityEngine`

**动因**：`RunProgress.cs` 528 行同时承担三件事——词条 Id 空间、候选卡生成、构筑状态推进。

| 主题 | 内容 | 验证 |
|------|------|------|
| **Id 空间抽离** | 新增 `EmberWeaponCatalog`：0–4 通用 / 10–15 武器 / 20–25 质变 / 30–35 专属的唯一出处 | 编译通过 |
| **候选池抽离** | 新增 `EmberChoicePool`：基础 3 张 + 空位补新武器、权重抽卡、同屏唯一键、补位与兜底 | 编译通过；**随机数消耗顺序逐字保留** |
| **`RunProgress` 只留状态** | 528 → 380 行；`OfferCountExpected` / `FullRoster` 改为转发 | 编译通过 |
| **消除名册数组重复** | `EmberGodsSelectPanel` 的本地 `Roster` 数组改为读 `EmberWeaponCatalog.Roster` | 源码扫描：全仓已无重复名册字面量 |
| **M2 真正零 `UnityEngine`** | `EmberRarityUtil` 的 `Mathf.Lerp/RoundToInt` 改 `System.Math`；配色由返回 `Color` 改为返回 `float[3]`，UI 侧新增 `EmberRarityTint.ToColor()` | 源码扫描通过 |

### 本轮实测结果（2026-09-23）

| 检查 | 命令 | 结果 |
|------|------|------|
| 离线编译 | `Tools/compile-check.ps1` | **通过**：56 源文件、0 错误、0 警告 |
| **Unity 批处理全量重编** | `Tools/unity-batch-verify.ps1` | **通过**：`Assembly-CSharp.dll` 重新生成，0 条 `error CS` |
| 字形覆盖 | `Tools/atlas_glyph_check.py` | **通过**：`MISSING (0)` |
| 真进一局 | — | **待人工验收** |

> ⚠️ **订正一条既有文档断言**：`02-run-progression-test.md` 原先声称"`RunProgress` 与 `EmberRarityUtil` 零 `UnityEngine` 依赖"，但当时 `EmberRarityUtil` 既用 `Mathf` 又返回 `Color`，**该断言不成立**。已按上面两项改动使其成立。

## 2026-09-23 第一轮：拆解上帝类 `EmberGame`

**动因**：全项目只有一个入口脚本 `EmberGame`，361 行里承担 5 种职责（11 态状态机 + 开局装配/销毁 + 摇杆与键盘输入 + 相机跟随 + 血量存亡 + HUD 推送 + 选卡编排）；M3（战斗）与 M7（表现）反向依赖 `EmberGame` 类型，使这两块无法脱离场景验证。

| 主题 | 内容 | 验证 |
|------|------|------|
| **组合根瘦身** | `EmberGame` 361 → 约 300 行，只保留装配、阶段所有权与每帧顺序 | 离线编译通过 |
| **阶段提为顶层类型** | `EmberGame.Mode` → `EmberRunPhase`（取值与顺序不变） | 离线编译通过 |
| **本局状态抽离** | 新增 `EmberRunState`（构筑/关卡/计时/击杀/世界与相机引用） | 离线编译通过 |
| **生命结算抽离** | 新增 `EmberKeeperVitals`：难度倍率只乘一次、护盾先吸收、0.65s 无敌帧、治疗溢出丢弃 | 离线编译通过 |
| **选卡推进抽离** | 新增 `RunFlowController`：选卡次数、新武器补偿（不连锁）、免费刷新 | 离线编译通过 |
| **战斗解除对 `EmberGame` 的依赖** | 新增 `IEmberCombatHost`；`EmberCombat` / `EmberBoss` / `EmberHealingDrops` 改用它 | 源码扫描：`Combat/` 下零 `EmberGame` 引用 |
| **表现层解除反向依赖** | `EmberKeeperAnimation` 去掉 `FindObjectOfType<EmberGame>()`；与 `EmberEffects` 一起改为注入阶段委托 | 源码扫描：`Visuals/` 下零 `EmberGame` 引用 |
| **输入 / 位移 / 相机抽离** | 新增 `EmberInput`（经 `Presenter` 回调）、`EmberKeeperMotion` | 离线编译通过 |
| **HUD 推送点抽离** | 新增 `EmberHudPresenter`；只在 `Playing` 阶段推送 | 离线编译通过；**待人工验收** |
| **安全区轮询改事件** | 新增 `EmberSafeAreaWatcher` | 离线编译通过 |
| **离线编译校验工具** | 新增 `Tools/compile-check.ps1`：用 Unity 自带 Roslyn 编译，**不启动编辑器** | 基线 42 文件 → 现状 53 文件，均 0 错误 0 警告 |

### 本轮实测结果（2026-09-23）

| 检查 | 命令 | 结果 |
|------|------|------|
| 离线编译 | `powershell -ExecutionPolicy Bypass -File Tools/compile-check.ps1` | **通过**：53 源文件、0 错误、0 警告 |
| **Unity 批处理导入 + 编译** | `Tools/unity-batch-verify.ps1`（需先关 Unity） | **通过**：退出码 0、0 条 `error CS`、`Assembly-CSharp.dll` 重新生成且含全部 12 个新类型 |
| 字形覆盖 | `python Tools/atlas_glyph_check.py Library/RefactorCheck/Emberlight.dll` | **通过**：`MISSING (0)` |
| 边界（源码扫描） | `Combat/`、`Combat/Weapons/`、`Visuals/` 下零 `EmberGame` 类型引用 | **通过** |
| 真进一局 | — | **待人工验收** |

> ⚠️ 编译通过（无论离线 Roslyn 还是 Unity 批处理）只证明**工程能导入、代码能编译**，**不能替代**进一局。

## 2026-09-15 字体与切图

| 主题 | 内容 | 验证 |
|------|------|------|
| **中文字体改为预烘静态图集** | 新增 `Assets/Editor/EmberFontBake.cs` 与 `Resources/Fonts/NotoSansCJKsc-Regular SDF.asset`。`EmberFonts` 预烘优先 + `ValidateBaked` 闸门 + 动态图集回落 | **通过** |
| **加载页文字第 0 帧可见** | `EmberIntro` 原先把 `CreateChinese()` 排在 0.5 s 动画之后，导致中文"慢半拍"；现改为字体优先、动画在后 | 批处理编译通过；**待人工验收** |
| **图集切为真正的 sprite sheet** | 四张图集 `textureType: Sprite` + `spriteMode: Multiple`；四个 `Ember*ArtImporter` 不再强制 `Default`（此前每次重导入都会清掉切片） | 资产结构核验通过；**待人工验收** |
| **取图三级链** | `EmberUiArt` / `EmberCardFrames` / `EmberCardIcons` / `EmberWorldArt` 改为「子 Sprite 优先 → 运行时 `Sprite.Create` 兜底 → 程序化占位」 | 同上 |
| **选武器改滚动** | `EmberGodsSelectPanel` 去掉 3 张一页的翻页，改为 `ScrollRect` | **待人工验收** |
| UI 文案与排版 | 难度页三档各占一行；武器卡说明与开局选武器文案逐字一致；菜单文案提为命名常量 | 文案已提取并比对 |
| 每帧 `UpdateSafeArea` 轮询 | 改为仅在分辨率/安全区变化时更新 | 编译通过 |

> **未做（有意决定）**：纹理压缩（`doodle-*` 仍为 Uncompressed，约 24 MB VRAM）；UI prefab 化当时未开始（后续已于 2026-10-14 完成）。

---

# 过程教训（跨轮次，仍然有效）

## 工具链

1. **PowerShell 5.1 会按 ANSI 读 UTF-8 无 BOM 文件**：用 `Get-Content -Raw` + `Set-Content` 批量改含中文注释的文件，会把注释写成乱码（曾污染 `EmberMenuFlow.cs` 与 `EmberCombat.cs`）。
   结论：**改含中文的源文件只用编辑工具，或显式 `-Encoding UTF8` + `[System.IO.File]::WriteAllText`**。已升级为 `AGENTS.md` 硬约束 11。
2. **`$PSScriptRoot` 在 `powershell -File` 下可能为空**：`compile-check.ps1` 已改为从 `$MyInvocation.MyCommand.Definition` 回退。
3. **执行策略会挡住 `.ps1`**：需 `-ExecutionPolicy Bypass`。
4. **Unity 授权客户端需要命名管道**：在沙箱内批处理必定以退出码 199 失败（`IPC channel to LicensingClient doesn't exist; aborting`）。必须在普通终端跑，且**先把 Unity 编辑器关掉**——否则撞 `HandleProjectAlreadyOpenInAnotherInstance` 直接 crash。
5. **Unity 进程返回后仍在刷日志**：立刻检查日志会读到"文件不存在/为空"的假失败；脚本已加等待。
6. **Unity 退出 ≠ 产物写好**：`Library/ScriptAssemblies/*.dll` 是 Bee 在进程结束前才拷贝出来的。**判断"是否编译过"要看产物时间戳轮询，不要只看日志措辞。**
7. **增量导入时日志不会出现 `CompileScripts`**：没东西要编时 Unity 直接跳过，此时"日志里没有编译记录"是正常现象，不是失败。要拿到真实编译信号，先删 `Library/ScriptAssemblies`。
8. **改 asmdef 会静默打断依赖产物路径的工具**：`atlas_glyph_check.py` 默认读 `Assembly-CSharp.dll`，代码搬进 `Emberlight.Runtime` 后，无参运行会扫到空集并打印 "coverage complete"——**看着通过，其实什么都没查**。
   **结论：把"空输入"当成失败，而不是当成通过。**

## 测试

9. **测试断言要符合统计学**：最初写了"幸运 0 时连抽 10 次必须全青铜"——但白银权重有 22%，抽到白银是正常结果，那条断言本身就是错的（红灯后已改为比较两个幸运档的实际分布）。
10. **我写错成员名**：测试里用了 `EmberWeaponCatalog.WeaponOrbit`（沿用 `RunProgress` 的命名），实际是 `EmberWeaponCatalog.Orbit`；Unity 报 15 个 `CS0117` 才暴露。**离线编译检查不含测试程序集，所以必须真跑 Unity 测试才知道测试编不编得过。**

## 字体（本轮最贵的教训）

静态图集**不会告诉你少了什么**——只会静默画方框或打警告。2026-09-15 那轮连续踩到四类漏字，各自成因不同：

| # | 漏的是什么 | 为什么常规手段抓不到 |
|---|------------|----------------------|
| 1 | 难度页整句文案 | 是方法体**内的内联字面量**，反射与数组遍历都够不到 |
| 2 | 设置页 / 敌人图鉴 / 结算文案 | 同上，散落在多个方法体 |
| 3 | `_`（U+005F） | **TMP 的内部要求**，不来自任何文案；又是 ASCII，看着"显然存在" |
| 4 | 图集只烘了卡面字 | `AllGlyphs` 的来源是人工枚举的数组清单，漏一个来源就整片缺字 |

因此把判断依据从"代码里有哪些来源"换成**"编译产物里有哪些字面量"**：`Tools/atlas_glyph_check.py` 读编译程序集的 `#US` 堆，能看见方法体内的字符串，并额外显式检查 TMP 的两个特殊码点。**这是目前唯一不依赖人记得同步的检查。**

## 2026-10-13 可维护性与工程交付整理

- 生命结算与选卡宿主缩小依赖；武器身份表、基础参数与实现注册集中；共享燃地独立，保持原时序。
- 卡面展示模型与 uGUI 创建分离，修正武器说明 ID 错位与过期预览；补回死亡结果到整局击杀计数的已有回调。
- 离线编译输出隔离，测试脚本拒绝 0 用例；补工程源码导出脚本和 README / 技术分析 / 实现说明。
- 基线 EditMode 103/103，最终 133/133；图形 PlayMode 2/2，无 Library 工程重新导入后 133/133。
- 屏幕外刷怪保证（已于 2026-10-14 完成）、通用 debuff、完整胜利与 Android 专项当时尚未完成。
