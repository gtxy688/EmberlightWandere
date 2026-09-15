# M8 音频 验收文档

> 对应架构：`08-audio.md`
> 对应需求：`../requirements.md` §7
> ⚠️ 项目当前**无测试基础设施**，见 `00-overview.md` §测试现状。

## 自动化测试计划与证据

分层依据：播放依赖 `AudioSource` 与帧推进 → **PlayMode**。**听感本身不可自动化**，只断言"确实播了、播的是对的那条、不叠音"。

| 编号 | 层级 | 拟测试路径与名称 | 行为 | 红灯原因或 N/A 理由 | 最近结果 |
|------|------|------------------|------|---------------------|----------|
| A1 | PlayMode（待建） | `AudioLoadTests::AllClipsResolveFromResources` | 全部 SFX/BGM 名能 `Resources.Load` 成功 | 资源路径写错会静默无声 | 未执行（无测试程序集） |
| A2 | PlayMode（待建） | `AudioLoadTests::MissingClipWarnsOnceAndDoesNotThrow` | clip 缺失时只警告一次、不抛异常、不中断游戏 | — | 未执行（无测试程序集） |
| A3 | PlayMode（待建） | `AudioLoadTests::SourcesAreForcedTo2D` | `music` 与 `sfx` 的 `spatialBlend == 0` | ⚠️ 回归点：导入为 3D 是历史"完全没声音"的首要原因 | 未执行（无测试程序集） |
| A4 | PlayMode（待建） | `AudioLoadTests::ListenerIsEnsuredWhenAbsent` | 场景无 `AudioListener` 时挂到 `Camera.main` | — | 未执行（无测试程序集） |
| A5 | PlayMode（待建） | `AudioWeaponRoutingTests::EachWeaponPlaysItsOwnClip` | 把其余 clip 置空后，6 把武器各自 Tick 仍能发声 | 证明走的是自己那条 clip，而非碰巧回退到通用音 | 未执行（无测试程序集） |
| A6 | PlayMode（待建） | `AudioWeaponRoutingTests::FallsBackToGenericClipWhenMissing` | 专属 clip 缺失时回退 `fire` / `meteor_impact` | — | 未执行（无测试程序集） |
| A7 | PlayMode（待建） | `AudioThrottleTests::FireIsThrottledAtFiftyMillis` | 每帧调用开火音 3 秒只播有限次 | 无限频会爆音 | 未执行（无测试程序集） |
| A8 | PlayMode（待建） | `AudioThrottleTests::BurnGroundThrottleIsShorterThanClip` | 燃地限频严格短于音效时长，不叠音成糊 | 历史问题：patch 每 ~0.45s 掉落 | 未执行（无测试程序集） |
| A9 | PlayMode（待建） | `AudioThrottleTests::OrbitIgnitesOnlyOnOrbCountChange` | 环火不在每帧发声，仅球数变化时点燃 | 每帧播放会持续噪音 | 未执行（无测试程序集） |
| A10 | PlayMode（待建） | `AudioThrottleTests::SplitArrowIsSilent` | `isSplit` 的分裂箭不发声 | 一次射击叠两声 | 未执行（无测试程序集） |
| A11 | PlayMode（待建） | `AudioThrottleTests::MeteorWarnsSilentlyAndSoundsOnImpact` | 预警圈静音，仅落地瞬间发声 | — | 未执行（无测试程序集） |
| A12 | PlayMode（待建） | `AudioContactTests::ContactSoundOnlyWhenHealthActuallyDrops` | 连续接触伤害不叠加通用命中音；生命实际下降才触发专属音 | 历史噪声源 | 未执行（无测试程序集） |
| A13 | PlayMode（待建） | `AudioVolumeTests::VolumeAppliesImmediatelyAndPersists` | set 后 `AudioSource.volume` 立即变化；写 `PlayerPrefs`；重进保持 | — | 未执行（无测试程序集） |
| A14 | PlayMode（待建） | `AudioVolumeTests::OneShotVolumeIsNotDoubleMultiplied` | 普通 `PlayOneShot` 音量只乘一次 | 历史缺陷：重复乘音量 | 未执行（无测试程序集） |
| A15 | PlayMode（待建） | `AudioMusicTests::SwitchesBetweenMenuAndCombatAndLoops` | 菜单/战斗切轨正确并 loop | — | 未执行（无测试程序集） |
| A16 | EditMode（待建） | `AudioImportSettingsTests::AllAudioImportsAre2DAndPreloaded` | 扫描 `.meta`，断言 `3D: 0` + `preloadAudioData: 1` | ⚠️ **新 WAV 默认是 `3D: 1` + `preload 0`**，是本模块最易复发的坑 | 未执行（无测试程序集） |
| A17 | N/A | 音色是否贴合奇幻火焰风、音量平衡是否舒服、武器间辨识度 | — | **无法合理自动化**：属听感判断 | N/A（列入手动验收 H1–H3） |

## 自动化运行记录

| 日期 | Unity 版本与环境 | 命令或 Test Runner 过滤器 | 结果文件 | 结论 |
|------|------------------|---------------------------|----------|------|
| — | — | 尚无可执行的测试程序集 | — | **未验证** |

> 历史记录：`Weapon-Sfx-v1` 时期曾在 Play Mode 手工实测 6 项（clip 可加载、`Play*` 使 `sfx` 进入 `isPlaying`、隔离测试、伤害产出、燃地限频只播 1 次、通用回退有效）。该实测**结论已过期**（v2 已替换素材与限频），且**未留存可重跑的自动化产物**。

## 手动验收前置条件

- 场景、Prefab 与配置：`Assets/Scenes/Emberlight.unity`；音频资源已导入且 `.meta` 为 2D
- 依赖模块状态：M6 设置页可用、M4 六把武器可用
- 目标设备与画质档位：Editor + Android 真机（含耳机与扬声器）

## 手动验收

| 编号 | 操作步骤 | 可观察预期结果 | 环境与构建 | 执行者/日期 | 状态 | 证据或备注 |
|------|----------|----------------|------------|-------------|------|------------|
| H1 | 进主菜单 → 开战 | 进菜单即有营地 BGM；开战切战斗 loop；切换干净无重叠 | Editor | — | 待人工验收 | 对应 A15 |
| H2 | 逐把武器单独开一局听音 | 六把武器音色可区分，风格统一为奇幻火焰，无科幻电子味 | Editor | — | 待人工验收 | 需 jack 听感确认 |
| H3 | 高攻速构筑连续开火 30 秒 | 不爆音、不糊成一片；燃地与环火不持续噪音 | Editor | — | 待人工验收 | 对应 A7–A9 |
| H4 | 暂停 → 拖动音乐/音效滑条 | 实时生效；音效滑条拖动有预览点击声；0 为静音 | Editor | — | 待人工验收 | — |
| H5 | 杀进程重进游戏 | 音量设置保持 | Editor + Android | — | 待人工验收 | 对应 A13 |
| H6 | Android 真机全程试听 | 扬声器与耳机下音量正常；切后台再回前台声音状态正确 | Android APK | — | 待人工验收 | — |
| H7 | 真机混音整体平衡 | BGM 不压过 SFX，SFX 不刺耳 | Android 真机 | — | 待人工验收 | 未做真人听感验收 |

## 失败路径与边界

| 编号 | 前置状态与操作 | 预期保护或失败行为 | 自动/手动 | 状态 | 证据或备注 |
|------|----------------|--------------------|-----------|------|------------|
| E1 | 音频资源缺失 | 静默跳过 + 警告一次，不崩、不刷屏 | PlayMode 待建 | 未验证 | 对应 A2 |
| E2 | 专属武器音效缺失 | 回退通用 `fire` / `meteor_impact` | PlayMode 待建 | 未验证 | 对应 A6 |
| E3 | 场景无 `AudioListener` | 自动挂到 `Camera.main` | PlayMode 待建 | 未验证 | 对应 A4 |
| E4 | 新 WAV 导入为 3D | ⚠️ 无代码级保护，表现为"资源在但听不到" | EditMode 待建 | 未验证 | 对应 A16；**易复发** |
| E5 | 首帧导入未完成时请求播放 | `Play*` 内再次 `TryLoadFromResources` | PlayMode 待建 | 未验证 | — |
| E6 | 丢焦点 / 切后台 | BGM 与 SFX 不因暂停而无声 | PlayMode 待建 | 未验证 | `ignoreListenerPause` |

## 回归范围

| 受影响模块或契约 | 复验项 | 原因 | 状态 | 证据或备注 |
|------------------|--------|------|------|------------|
| M4 武器系统 | 6 条武器音效接线与分裂箭静音 | 触发点在武器内 | 待人工验收 | 对应 A5/A10 |
| M3 战斗 | 受击音、接触伤害去重 | `DealContactDamage` 返回值驱动 | 待人工验收 | 对应 A12 |
| M6 界面 | 交互音、滑条预览、BGM 切换 | 触发点在 UI | 待人工验收 | — |
| M5 敌人系统 | 陨石落地与爆炸音 | `PlayMeteorImpact` | 待人工验收 | — |

## 交付结论

- **已验证**：无。接线与节流参数已**逐项对照代码**确认，但尚无任何可执行检查通过。
- **不适用**：A17 音色与音量平衡的听感判断——不做自动化。
- **待手动验收**：H1–H7（BGM 切换、六武器音色辨识、高频不爆音、滑条即时生效、设置持久化、真机试听、整体混音平衡）。**全部需 jack 确认。**
- **未验证**：A1–A16 全部（阻塞原因：项目无 asmdef 与测试程序集）。
- **未通过**：无。已知的 3D 导入设置复发风险见 E4。
