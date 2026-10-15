# 归档说明

本目录存放**已被取代的历史文档**。规则见根目录 `AGENTS.md`：

> **只读历史。不要从归档文档实现功能。** 归档里的数值与规则可能已被现行口径覆盖，引用它们当作现行要求是错误做法。

## 当前状态：目录内没有历史文件

历史上这里存过 40+ 篇平摊文档（`Emberlight-Design.md`、`Rarity-and-TMP.md`、`Survival-v1.md`、`DemoExpansion.md`、`VisualUpgrade.md`、`World-Doodle-Art.md`、`UI-Doodle-Art.md`、`Card-Doodle-Art.md`、`Card-Frames-Art.md`、`Floating-Actors.md`、`Agent.md` 等）。这些文档的内容已被逐项核对后**合并进现行的权威文档，原文件随后删除**，因此本目录当前是空的。

合并结果与「历史表述 → 正确值」的对照表保留在下列位置，不需要回到归档：

| 原文档 | 合并去处 | 历史偏差对照表 |
|--------|----------|----------------|
| `Emberlight-Design.md`、`Survival-v1.md` | `../requirements.md` | `../requirements.md` §6「已删除的系统」 |
| `Rarity-and-TMP.md` | `../requirements.md` §4.5、`02-run-progression-test.md` | `02-run-progression-test.md`「已修正的历史记录」 |
| `DemoExpansion.md` | `03-combat-waves.md`、`03-combat-waves-test.md` | `03-combat-waves-test.md`「已修正的历史记录」 |
| `VisualUpgrade.md`、`World-Doodle-Art.md`、`UI-Doodle-Art.md`、`Card-Doodle-Art.md`、`Card-Frames-Art.md`、`Floating-Actors.md` | `07-art-presentation.md`、`07-art-presentation-test.md` | `07-art-presentation-test.md`「已修正的历史记录」 |
| `Agent.md` | 根目录 `AGENTS.md` | — |
| `EMBERWORLD_LIMIT_PATCH.md`（原在仓库根目录） | 改动已落地，无需合入任何文档 | — |

## 已裁定的历史冲突

下面这些旧说法**已经被现行口径覆盖**，不要再翻案。完整表在 `AGENTS.md`「已知冲突」：

| 项 | 旧说法 | 现行权威值 |
|----|--------|------------|
| 怪物血量倍率 | `0.75 / 1 / 1.35`；或休闲 `0.5` | `0.25 / 0.5 / 0.675` |
| 幸运品质权重（幸运 0） | `60/28/10/2` | `72/22/5/1` |
| 攻速档位 | 与攻击力同档 `+10/20/30/40` | `+10/20/30/50`，且只作用于单武器 |
| 攻击力作用域 | 加算全局 | 只作用于对应已有武器 |
| Boss 触发 | `Elapsed >= 480` | 波次驱动（每 10 波 + 最终波） |
| 局长 | 6 波 | 25 / 50 波 |
| 粒子上限 | 180 | 120 |
| 护盾数值 | 除以 2 | 250 / 500 / 750 / 1250 |
| 死亡掉落 | 掉落余烬拾取物 | 已删除（击杀直接记 Score）；治疗掉落仍在 |
| 中文字体 | 「固定走动态图集，不要改回预烘」 | 预烘静态图集 + `ValidateBaked` 闸门 + 动态回落 |
| 程序集 | 「无 `.asmdef`」 | 有自建 asmdef（Runtime / Editor / EditMode.Tests / PlayMode.Tests） |

## 新增归档文档时

1. 在文件顶部加作废头注：写明**被谁取代**、**取代日期**；
2. 在本文件的「合并去处」表里补一行；
3. 确认现行权威文档已经吸收它仍然有效的内容——**不要只搬走文件而丢掉规则**。
