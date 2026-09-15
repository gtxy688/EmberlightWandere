# M7 美术与表现 验收文档

> 对应架构：`07-art-presentation.md`
> 对应需求：`../requirements.md` §7（技术约束）
> ⚠️ 项目当前**无测试基础设施**，见 `00-overview.md` §测试现状。

## 自动化测试计划与证据

分层依据：图集取图与回退链依赖 Unity 资源导入与 `Sprite` 创建 → **PlayMode**（EditMode 亦可覆盖静态取图）。**美术观感不可自动化**，只断言"取到图、不崩溃、不漂移"。

| 编号 | 层级 | 拟测试路径与名称 | 行为 | 红灯原因或 N/A 理由 | 最近结果 |
|------|------|------------------|------|---------------------|----------|
| A1 | EditMode（待建） | `SpriteFallbackTests::CardFramesFallBackThroughThreeLevels` | `doodle-card-frames` 缺失时回退手工切分，再缺失回退 `EmberArt.Panel`，**不返回 null** | 空 Sprite 会让卡面不可见且不报错 | 未执行（无测试程序集） |
| A2 | EditMode（待建） | `SpriteFallbackTests::CardIconsMapEveryOfferId` | 全部现行词条 id 都能映射到图集格，未知 id 回落占位而非 null | 新增词条忘配图标会静默空白 | 未执行（无测试程序集） |
| A3 | EditMode（待建） | `SpriteFallbackTests::UiArtFallbackRectsAreNormalized` | `EmberUiArt` 6 个回退 rect 均在 [0,1] 内 | 越界 rect 会产生错误切片 | 未执行（无测试程序集） |
| A4 | EditMode（待建） | `SpriteFallbackTests::WorldArtSpriteCountMatchesSheet` | `wanderer-sheet` 解析出 12 格，且各格非空 | — | 未执行（无测试程序集） |
| A5 | EditMode（待建） | `BakedArtTests::BakedSpritesMatchRuntimeRasterization` | 5 个烘焙 PNG 与现场重新光栅化逐像素一致（`maxDelta == 0`） | 改了一处忘另一处会导致美术漂移 | 未执行（无测试程序集） |
| A6 | EditMode（待建） | `BakedArtTests::MissingBakedAssetFallsBackToRasterization` | 烘焙资源缺失时回退运行时逐像素生成，不崩溃 | — | 未执行（无测试程序集） |
| A7 | PlayMode（待建） | `EffectsTests::ParticleCapIsOneHundredTwenty` | 同屏粒子不超过 **120** | ⚠️ 历史文档写 180，实现为 120；回退会让移动端掉帧 | 未执行（无测试程序集） |
| A8 | PlayMode（待建） | `EffectsTests::EmitsOnlyWhilePlaying` | 暂停/选卡/结算态不发射新粒子 | 暂停时仍在放特效会显得未暂停 | 未执行（无测试程序集） |
| A9 | PlayMode（待建） | `EffectsTests::PoolExhaustionDropsSilently` | 池空时丢弃粒子且不抛异常、不无限分配 | — | 未执行（无测试程序集） |
| A10 | PlayMode（待建） | `EffectsTests::NewRunClearsPreviousEffects` | 重新开局后旧特效世界被清理 | — | 未执行（无测试程序集） |
| A11 | 已有（编辑器批处理脚本） | `Tools/atlas_glyph_check.py`（外部工具，非 Test Runner） | 读 `Assembly-CSharp.dll` 的 `#US` 堆取全部字面量 + 读 `.asset` 的 `m_Unicode`，差集必须为空；并显式断言 TMP 特殊字符 `U+005F`、`U+2026` 存在。**非空时退出码 1** | 静态图集不能追加字形，漏字直接表现为游戏内空心方框 | **通过**：`MISSING (0)` / `TMP special chars : all present` / 退出码 0 |
| A12 | 已有（编辑器批处理方法） | `EmberFontBake.CheckRuntimeLoad` | `EmberFonts.CreateChinese()` 必须返回**预烘静态图集**（`atlasPopulationMode == Static`），且 `atlasTextures.Length == 1`、`max atlasIndex == 0` | 多图集会产生无纹理的 atlas 槽，运行时 `GetFallbackMaterial` 读它即 `NullReferenceException`（历史事故三） | **通过**：`1 atlas texture(s), 529 characters, 529 glyphs, max atlasIndex 0` |
| A12b | 已有（同 A12） | `EmberFontBake.CheckRuntimeLoad`（输出含 FALLBACK 分支） | 预烘资产被破坏时 `ValidateBaked` 必须拒绝并回落动态图集，**不得崩溃** | 这是"资产坏了不拖挂 UI"的唯一保证 | **通过**（闸门存在且被 A12 覆盖；破坏路径未构造用例） |
| A13 | 已有（编辑器批处理方法） | `EmberFontBake.Bake` 的 `CoversEveryCharacter` | 每个请求字符都能在烘出的图集里查到，否则换更小采样点重试，全部失败则拒绝写盘 | 关掉多图集后装不下的字**不进 `glyphTable`**，"没有字形越界"依然成立而字数已缺 | **通过**：`72 px -> 529 chars ... complete=True` |
| A14 | 已有（编辑器批处理方法） | `EmberFontBake.VerifyReloaded` | 从磁盘**重新加载**资产后真渲染 64 字采样，必须有几何体产出；并校验每个字形的 `atlasIndex` 落在 `atlasTextures` 范围内 | 历史两次事故都是"字段校验全绿、首次绘制才炸"，只有真绘制能拦住 | **通过**：`reloaded 529 glyphs / 1 atlas texture(s) / max atlasIndex 0` |
| A15 | PlayMode（待建） | `KeeperAnimationTests::FreezesWhileNotPlaying` | 暂停时角色动画停止，恢复后继续 | — | 未执行（无测试程序集） |
| A16 | PlayMode（待建） | `KeeperAnimationTests::MovesOnlyVisualChildren` | 动画只影响视觉子节点，角色坐标与碰撞不变 | 动到根节点会破坏移动与碰撞 | 未执行（无测试程序集） |
| A17 | EditMode（待建） | `ImporterTests::AtlasImportersApplySlicing` | 4 个 `AssetPostprocessor` 对目标图集应用切片并关闭 mipmap | 重新导入资源后切片丢失 | 未执行（无测试程序集） |
| A18 | EditMode（待建） | `ImporterTests::AndroidCompressionOverrideIsUncompressed` | 三张 doodle 图集的 Android 平台覆写为 Uncompressed | 回退 ETC2 会让描边起块状伪影 | 未执行（无测试程序集） |
| A19 | N/A | 简笔画风格是否讨喜、色彩是否协调、动画节奏是否舒服 | — | **无法合理自动化**：属审美判断 | N/A（列入手动验收 H1–H3） |

## 自动化运行记录

| 日期 | Unity 版本与环境 | 命令 | 结果 | 结论 |
|------|------------------|------|------|------|
| 2026-09-15 | 2022.3.62f3，`-batchmode -quit`（**须在沙箱外且项目未被编辑器打开**） | `python Tools/atlas_glyph_check.py` | `assembly literals : 420` / `distinct visible : 489` / `atlas holds : 529` / `MISSING (0)` / `TMP special chars : all present` / 退出码 **0** | **通过** |
| 2026-09-15 | 同上 | `-executeMethod Emberlight.Editor.EmberFontBake.Bake` | `baked 529 glyphs of 863 requested (531 distinct) at 72 px into ONE 2048x2048 atlas` / `VERIFY OK` / `complete=True` | **通过** |
| 2026-09-15 | 同上 | `-executeMethod Emberlight.Editor.EmberFontBake.CheckRuntimeLoad` | `pre-baked atlas validated: 1 atlas texture(s), 529 characters, 529 glyphs, max atlasIndex 0` / `PASS: the pre-baked atlas is in use` | **通过** |

> `531 distinct − 2`（`\r`、`\n` 控制字符，TMP 不为其生成字形）`= 529`，与烘入数一致。

## 手动验收前置条件

- 场景、Prefab 与配置：`Assets/Scenes/Emberlight.unity`；图集已导入且无 Console 导入警告
- 依赖模块状态：M6 面板可显示、M5 敌人可生成
- 目标设备与画质档位：Editor + Android 真机（中端机）

## 手动验收

| 编号 | 操作步骤 | 可观察预期结果 | 环境与构建 | 执行者/日期 | 状态 | 证据或备注 |
|------|----------|----------------------------|------------|-------------|------|------------|
| H1 | 观察开局武器库与升级选卡 | 六把武器与全部词条图标可辨认，风格统一为简笔画 | Editor | — | 待人工验收 | 审美项 |
| H2 | 观察四档卡框 | 铜/银/金/钻在**颜色与形状**上都能区分（不只靠颜色） | Editor | — | 待人工验收 | 审美项 |
| H3 | 战斗全程观察特效与角色 | 打击反馈清晰不糊；角色浮动/倾斜/提灯摇晃自然；暂停时完全静止 | Editor | — | 待人工验收 | 审美项 |
| H4 | 观察中文显示 | 全流程无缺字方框、无字体抖动；**加载页文字第一眼即完整**（无空白后补）；首次打开各面板无可见卡顿 | Editor + Android | — | 待人工验收 | 对应 A11/A13 |
| H4b | 观察字体清晰度 | 72 px 采样点下，各面板 11–40 px 字号的中文边缘不发虚、可读 | Editor + Android | — | 待人工验收 | 单图集容量迫使采样点由 90 px 降至 72 px；**这是审美取舍，需人判断** |
| H5 | 大波次战斗（第 20 波后）观察帧率 | 同屏 150 敌人 + 粒子 + 暗弹 + 掉落时无明显掉帧 | Android 真机 | — | 待人工验收 | 对应 A7；依赖 M9 |
| H6 | Android 真机观察描边质量 | 图集描边无块状伪影（验证 Uncompressed 覆写生效） | Android APK | — | 待人工验收 | 对应 A18 |
| H7 | 切后台再回前台 | 动画状态正确恢复，无卡在静止或暴走 | Android 真机 | — | 待人工验收 | — |

## 失败路径与边界

| 编号 | 前置状态与操作 | 预期保护或失败行为 | 自动/手动 | 状态 | 证据或备注 |
|------|----------------|--------------------|-----------|------|------------|
| E1 | 图集资源缺失或未导入 | 逐级回退到程序化占位，**不崩、不空白** | EditMode 待建 | 未验证 | 对应 A1–A3 |
| E2 | 烘焙 PNG 缺失 | 回退运行时光栅化 | EditMode 待建 | 未验证 | 对应 A6 |
| E3 | 新增词条 id 未配图标 | 回落占位图，不返回 null | EditMode 待建 | 未验证 | 对应 A2 |
| E4 | 粒子池耗尽 | 静默丢弃，不无限分配 | PlayMode 待建 | 未验证 | 对应 A9 |
| E5 | 预烘字体资产缺失 | `CreateChinese()` 回落运行时动态图集，中文仍可显示（代价：首屏慢半拍） | 手动（删资产） | 未验证 | 对应 A12b |
| E6 | 预烘字体资产被破坏（如多图集产生无纹理的 atlas 槽） | `ValidateBaked` 检出并**回落动态图集**，不崩溃；`EmberFontBake.VerifyReloaded` 在烘制阶段就应拦下，拒绝写盘 | 手动（构造坏资产） | 未验证 | **代码级保护已就位**：`ValidateBaked` 校验图集纹理、字符表、材质、每字形的 `glyphIndex` 与 `atlasIndex` |

## 回归范围

| 受影响模块或契约 | 复验项 | 原因 | 状态 | 证据或备注 |
|------------------|--------|------|------|------------|
| M6 界面 | 全部面板精灵、卡框、图标 | 图集由本模块供给 | 待人工验收 | — |
| M5 敌人系统 | 共享剪影与粒子服务 | 本模块提供 | 待人工验收 | — |
| M4 武器系统 | 弹体外观重定向 | `EmberWorldArt.Projectile` | 待人工验收 | — |
| M3 战斗 | 打击特效与暂停冻结 | `EmberEffects` 门控 | 待人工验收 | — |
| 构建产物 | 包体大小（字体 16.4 MB + 图集） | 资源体积直接影响 APK | 待人工验收 | 见 M9 |

## 交付结论

- **已验证**：A11（字形覆盖，外部脚本）、A12（运行时加载预烘单图集）、A13（烘制覆盖校验）、A14（重启后真渲染校验）。四条均为 2026-09-15 批处理实跑通过，命令与输出见「自动化运行记录」。
- **不适用**：A19 美术风格与动画节奏的审美判断——不做自动化。
- **待人工验收**：H1–H7、H4b（新增：72 px 采样点的清晰度取舍）。
- **未验证**：A1–A10、A15–A18（阻塞原因：项目无 asmdef 与测试程序集）；A12b、E5、E6 的**破坏路径**（闸门存在且已被正向路径覆盖，但未构造坏资产用例）。
- **未通过**：无。

> ⚠️ A11–A14 走的是**编辑器批处理脚本**，不是 Unity Test Runner。项目仍无测试程序集，`00-overview.md` §测试现状所述的阻塞未解除。

## ⚠️ 已修正的历史记录

本文件由六篇已归档的美术文档（`VisualUpgrade`、`World-Doodle-Art`、`UI-Doodle-Art`、`Card-Doodle-Art`、`Card-Frames-Art`、`Floating-Actors`）合并整理而来，原始内容与生成提示词保留在 `../archive/`。合并时发现并**已订正**的偏差：

| 历史表述 | 实际值 |
|----------|--------|
| 「粒子上限 **180**」（`VisualUpgrade.md`） | **120**（`EmberEffects.cs:15`） |
| 「角色按实际移动距离播放图集走路帧，向上移动切背面」 | **已改为不播放走路帧**，改为连续浮动 + 移动倾斜 + 衣摆呼吸 + 提灯摇晃（`Floating-Actors.md`，较新） |
| 「玩家恢复原程序造型基础，不再播放图集走路帧」 | 现行口径，以上条为准 |
| 「字体预烘已放弃——试过两次，都崩，不要再试」「`EmberFonts` 固定走动态图集」 | **已改为预烘静态图集**。三次失败的根因均已定位并修正（写入顺序 / 多图集 null 槽 / 缺覆盖与渲染校验），现行实现带 `ValidateBaked` 运行时闸门与动态图集回落。详见 `07-art-presentation.md` §中文字体 |
| 「图集为 Default 类型，运行时按格 `Sprite.Create`」 | **四个图集已切成真正的 sprite sheet**（`textureType: Sprite` + `spriteMode: Multiple`），取图入口为「子 Sprite 优先 → 运行时 `Sprite.Create` 兜底」三级链 |
