# Emberlight 设计（策划与开发记录，同步 2026-09-13）

目标：Unity 2022.3.62f3 可打安卓包；先把**游戏闭环**做完，再谈资源与上架。
项目路径：`E:/Unity/Projects/DemoProjects/EmberlightWandere`（文件夹名少一个 r）。GitHub：`gtxy688/EmberlightWandere`。

## 已确定方向

灯芯卡通暖色；触控优先；虚拟摇杆移动、自动射击、占位美术；升级三选一；稀有度影响强度；击杀 Boss 胜利，灯芯熄灭失败。第一版不做关卡、联网、伤害数字。

## 当前优先级（jack 确认）

1. **游戏闭环**：战斗、难度、胜负、成长循环优先
2. **选卡流程（Gods-Select-v3，诸神对齐）** ← 当前现行
3. 美术可迭代：结构稳定后再替换贴图
4. 结构可改：关卡/刷怪与战斗逻辑分开
5. **安卓导出 / APK** 排最后（编辑器内闭环完成后再做）

## 原型范围

- 1 灯芯主角、1 张有装饰边界的地图；普通 / 快速 / 高血怪、暗烛射手、烬胎与烬虫、铁灯卫、引线虫 + 最终 Boss「长夜守卫」
- **开局诸神式选卡（v3）**：先选槽位 N=1…5（默认 2），再整库翻页填槽（火球/环火/燃地/穿透/烬蝶/火雨）；至少 1 把；Skip=火球×1+N=2
- 局内：清波出卡 `3+空位`；通用仅攻击/攻速/幸运/增幅；每武器 1 金专属 + 1 钻质变；新武器补偿再选；2 次免费刷新
- 冲击波不占槽；敌人多样性已接入：灼热/疾风/重生词缀，W3+ 精英小队/围杀圈遭遇，中文提示
- 虚拟摇杆、选卡、临时战场暂停
- 主菜单、开局两步（N+整库）、战斗 HUD（武器数/N + 幸运）、升级、暂停、胜负结算（原始 Score）、重新开始

## 工程结构（以仓库实装为准）

```
Assets/
  Scenes/Emberlight.unity
  Scripts/
    Core/     EmberGame, RunProgress, EmberRarity, LevelConfig, EmberFonts
    Combat/   EmberCombat, EmberBoss, EmberWorld
    Combat/Weapons/  EmberWeapon, EmberWeaponContext, Basic/Orbit/Trail/Pulse/Pierce/Boomerang/Meteor
    UI/       EmberHud, EmberMenuUi, EmberIntro, EmberUpgradePanel, EmberGodsSelectPanel, EmberCardMotion
    Visuals/  EmberArt, EmberEffects, EmberVisuals
  Editor/     EmberChecks, EmberExpansionChecks
  Resources/Fonts/
Docs/
  Emberlight-Design.md             # 本文件
  Rarity-and-TMP.md
  Difficulty-Waves-v3.md           # 现行难度/节奏（波次选牌）
  Difficulty-Waves-v1.md           # 已作废
  Build-Depth-v1.md                # 武器实体基座
  Gods-Select-v3.md                # 现行选卡（诸神对齐）
  Gods-Select-v2.md                # 作废
  Gods-Select-v1.md                # 作废
  Enemy-Variety-v1.md              # 已接入，试玩验收由 jack 负责
```

> 旧路径 `Assets/Emberlight/Scripts/...` **已废弃**，以 `Assets/Scripts/...` 为准。

## 现行难度

见 **`Docs/Difficulty-Waves-v3.md`**。v1 作废。

## 构筑深度

见 **`Docs/Build-Depth-v1.md`**（武器实体）。进化条件表以 **Gods-Select-v3** 专属/质变表为准（旧阈值作废）。

## 选卡流程（Gods-Select-v3）

见 **`Docs/Gods-Select-v3.md`**：N + 整库填槽；通用四词条 + 幸运权重；专属金/质变钻；`3+空位`；补偿选；2 刷新。v2/v1 作废。

## 工作顺序（更新）

1. 工程可运行
2. 战斗闭环 + 难度/Boss 节奏 ← 已落地
3. UI 状态切换 ← 已落地
4. 构筑深度（武器池基座）← Build-Depth-v1 已落地
5. 诸神式开局 v1/v2 ← 已作废
6. **诸神对齐选卡** ← Gods-Select-v3 现行
7. 敌人多样性 ← **已接入代码，待 jack 试玩**（Enemy-Variety-v1）
8. 资源替换（贴图，可选）
9. **最后**：安卓导出 / APK

## 实现备注（无死亡掉落）
- **死亡掉落已删除**：击杀直接 AddScore（小怪 1 / Boss 30），不再生成小余烬拾取物；PickupBonus 停用。
| Weapon-Balance-v1.md | 武器相对 DPS 档（收超模）


## 征程与难度（现行）

开局先选择 25/50 波，再选择休闲/标准/困难，之后选择武器槽位和起始武器。每 5 波精英挑战、每 10 波阶段 Boss，最终波为最终 Boss。前 10 波每波选卡，之后每 2 波选卡，阶段 Boss 额外选一次；最终波直接结算。详见 Docs/Difficulty-Waves-v3.md。

局外成长已删除：无灯油、灯铺或永久加成；不再读取/写入旧成长存档。所有成长在单局内完成。

## 护盾与回血

通用护盾词条现已加入：青铜/白银/黄金/钻石分别为 250/500/750/1250。新增概率掉落回血补给（每个 20 HP），不恢复护盾，不带入下一局。详见 Docs/Survival-v1.md。原先无死亡掉落的说明仅适用于余烬，回血道具为本次新增。

## 2026-09-14
- 攻击力/攻速=单武器；伤害增幅=全体；同屏唯一按 (Id,WeaponId)。详见 `Hotfix-Weapon-AtkAs-v1.md`。
