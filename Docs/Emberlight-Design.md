# Emberlight 设计（策划与开发记录，同步 2026-09-13）

目标：Unity 2022.3.62f3 可打安卓包；先把**游戏闭环**做完，再谈资源与上架。
项目路径：`E:/Unity/Projects/DemoProjects/EmberlightWandere`（文件夹名少一个 r）。GitHub：`gtxy688/EmberlightWandere`。

## 已确定方向

灯芯卡通暖色；触控优先；虚拟摇杆移动、自动射击、占位美术；升级三选一；稀有度影响强度；击杀 Boss 胜利，灯芯熄灭失败。第一版不做关卡、联网、伤害数字。

## 当前优先级（jack 确认）

1. **游戏闭环**：战斗、难度、胜负、成长循环优先
2. 美术可迭代：结构稳定后再替换贴图
3. 结构可改：关卡/刷怪与战斗逻辑分开
4. **安卓导出 / APK** 排最后（编辑器内闭环完成后再做）

## 原型范围

- 1 灯芯主角、1 张无边界大图、普通 / 精英 / 快速怪 + 最终 Boss「长夜守卫」
- 火球、环绕火种、燃地拖尾；可回血；拾取范围扩展
- 10 条强化（见 `Rarity-and-TMP.md`）；稀有度铜/银/金/钻影响强度
- 重复选项不再出现；每次三选一三个不同选项；全屏时暂停
- 虚拟摇杆、三选一、临时战场暂停
- 主菜单、战斗 HUD、升级、暂停、胜负结算、重新开始

## 工程结构（以仓库实装为准）

```
Assets/
  Scenes/Emberlight.unity
  Scripts/
    Core/     EmberGame, RunProgress, EmberRarity, EmberFonts
    Combat/   EmberCombat, EmberBoss, EmberWorld
    UI/       EmberHud, EmberMenuUi, EmberIntro, EmberUpgradePanel, EmberCardMotion
    Visuals/  EmberArt, EmberEffects, EmberVisuals
  Editor/     EmberChecks, EmberExpansionChecks
  Resources/Fonts/
Docs/
  Emberlight-Design.md             # 本文件
  DemoExpansion.md
  Rarity-and-TMP.md
  VisualUpgrade.md
  Difficulty-Waves-v2.md           # 现行难度/节奏（波次选牌）
  Difficulty-Waves-v1.md           # 已作废
  Agent.md
```

> 旧路径 `Assets/Emberlight/Scripts/...` **已废弃**，以 `Assets/Scripts/...` 为准。

## 已知现状 / 待改实现

- 自然 Boss：旧逻辑 `Elapsed >= 480`（约 8 分钟）→ **按 v2 删除，改 Wave6**
- 菜单「试玩 Boss」可提前刷
- 地图边界、围墙、刷怪节奏、Boss AI 写在脚本里，**尚无独立关卡配置/资源驱动**
- 视觉多为程序化几何（`EmberArt` / `EmberEffects`）

## 现行难度

见 **`Docs/Difficulty-Waves-v2.md`**（波次清完强制三选一；去掉经验升级；删 480s；幅度 20/40/65/90 + 波次稀有度门控）。v1 作废。

## 工作顺序（更新）

1. 工程可运行
2. 战斗闭环 + 难度/Boss 节奏 ← 按 v2 落地
3. UI 状态切换（HUD 去经验条、加波次）
4. 结构理清（关卡配置 vs 战斗核）
5. 资源替换（贴图，可选）
6. **最后**：安卓导出 / APK