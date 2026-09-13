# 构筑深度 · 武器与进化（v1）

> 状态：已落地（武器实体基座）
> **注意**：进化条件表 / 旧通用池出卡口径已被 `Gods-Select-v3.md` 取代；保留本文件作武器数值与抽象说明。
> 衔接：`EmberCombat` / `RunProgress` / `EmberRarity` / `EmberUpgradePanel` / `EmberChecks`
> 前置：`Difficulty-Waves-v2.md`（波次清完强制三选一）。落地时须同步 `Rarity-and-TMP.md` 词条表与 `Emberlight-Design.md`。

## 目标

- 解决"每局 build 相同"：武器从固定三件套 → 可插拔武器池 + 武器进化。
- 沿用现有**"词条即开关、叠加即升级"**模型（Orbits>0 才有环绕、TrailPower>0 才有拖尾）：新武器首次选取解锁、重复选取升级，**不引入武器等级 UI**。
- 一局仍 5 张卡（Wave1–5）；进化是"集中投入"的奖励，不是每局必得。

## 1. 武器抽象（纯重构，行为不变）

- 新目录 `Assets/Scripts/Combat/Weapons/`，抽象基类 `EmberWeapon`：`Tick(dt, ctx)`。
- 现有 火球 `Fire()` / 环绕 `UpdateOrbits()` / 拖尾 `UpdateTrail()` / 冲击波 pulse 迁移为 4 个子类；`EmberCombat` 持有 `List<EmberWeapon>` 统一 Tick。
- ctx 注入：玩家位置、敌人列表访问、`Damage(index, amount)` 回调、`EmberEffects`、`RunProgress`。武器不反向依赖 `EmberGame`。
- 本步骤为纯重构：所有现有数值与行为不变，`EmberChecks` 现有断言必须全绿。

## 2. 卡牌池扩展（Stat / Weapon / Evolution）

- `EmberOffer` 增加 `Kind`：`Stat`（现有 0–9）/ `Weapon`（10–19 预留）/ `Evolution`（20–29 预留）。
- 每 offer 仍抽 3 张、**同 offer 内不重复**；改为加权不放回抽样：

| 候选类别 | 权重 | 条件 |
|----------|------|------|
| Evolution | 6 | 进化条件已满足且未进化 |
| Weapon（解锁） | 3 | 到达解锁波且未拥有 |
| Weapon（升级） | 1.5 | 已拥有，重复选取 = 升级 |
| Stat（0–9） | 1 | 无 |

- **W1 只出 Stat 卡**（教学节奏）；武器解锁波：穿透火矢 W2+、回旋烬蝶 W3+、天降火雨 W4+。
- 武器卡稀有度按现有 `Roll(random, waveCleared)` 正常 roll：百分比类用 Magnitude 作武器基础伤害加成，Count 类词条（穿透火矢）用 CountBonus 加弹数。
- 进化卡**固定钻石稀有度**，不走 roll。
- 兜底：候选不足 3 张时用 Stat 池补齐，绝不空选。

## 3. 新武器数值

基准：火球 22 伤 / 0.8s 冷却；杂兵 HP `16+Wave*6`、tank `40+Wave*10`。

| Id | 武器 | 解锁波 | 冷却 | 伤害 | 行为 |
|----|------|--------|------|------|------|
| 10 | 穿透火矢 | W2 | 1.1s ÷ (1+AS) | 30 × (1+Dmg) | 朝最近可见敌直线穿透全部敌人，速度 12；CountBonus +1 弹（扇形 12°） |
| 11 | 回旋烬蝶 | W3 | 2.4s ÷ (1+AS) | 18 × (1+Dmg) / 段 | 飞出 6u 后返回玩家，**往返两段均可命中**；重复选取 +Magnitude 伤害 |
| 12 | 天降火雨（备选） | W4 | 3s | 45 × (1+Dmg) | 随机选可见敌，0.6s 预警圈后落 AOE（r=1.6）；WavePower>0 时半径 +WavePower×0.5 |

## 4. 武器进化

| Id | 进化 | 条件（全局词条阈值） | 效果 |
|----|------|----------------------|------|
| 20 | 燎原 | DamageBonus ≥ 1.0 且 TrailPower ≥ 0.4 | 火球体积 ×1.5、伤害 ×1.5、命中点在路径留燃地（半功率火焰） |
| 21 | 日冕 | Orbits ≥ 5 且 AttackSpeedBonus ≥ 0.8 | 环绕半径 1.6→2.3、转速 2.5→3.6、DPS 65→90、火种附带灼烧拖尾 |

- 条件满足 → 进化卡入池；**选择后才替换**武器形态（构筑选择权留给玩家，不自动触发）。
- 阈值按 5 卡预算设计：燎原最少 3 卡（金 0.65+银 0.4 伤害 + 银拖尾），日冕最少 3 卡（钻 3+金 2 环绕 + 钻攻速）。全力投入才够得着。

## 5. RunProgress 改动

- 新增 `OwnedWeapons` / `Evolved` 集合；`Choose()` 处理 id ≥ 10（武器解锁/升级、进化置位）。
- `Choices()` 重写为第 2 节的加权不放回抽样；Stat 部分逻辑与数值不变。
- 进化卡选择后从候选池永久移除（已进化不再出现）。

## 6. UI / 文案

- 复用 `EmberUpgradePanel`，无新界面；武器卡/进化卡仅描述文案不同。
- 进化卡徽章显示「进化」，固定钻石色；武器卡徽章显示「新武器」或「升级」。

## 7. 验收

- 回归：现有 10 卡数值与行为不变，`EmberChecks` 现有断言全绿。
- 池断言：W1 无 Weapon/Evolution 卡；W2 起穿透入池；进化条件满足时候选含对应进化卡且稀有度恒为钻石；候选不足时 Stat 补齐不空选。
- 武器断言：穿透单次命中多敌；烬蝶去程/回程各结算一次；火雨预警结束才结算伤害。
- 进化断言：阈值未满足时池中无进化卡；选择后武器行为替换且不再出现。
- 手测闭环：主菜单 → 5 波清波选卡 → Boss → 胜/负，同一 offer 三张卡互不相同。

## 落地顺序（每步独立可运行、可回退）

1. 武器抽象 + 迁移现有 4 武器（纯重构回归）
2. 卡牌池加权 + 穿透火矢
3. 回旋烬蝶（+ 视手感决定天降火雨）
4. 进化 ×2（燎原 / 日冕）
5. `EmberChecks` 扩展断言 + 同步 `Rarity-and-TMP.md`、`Emberlight-Design.md`


## 实现备注
- Step1：`Assets/Scripts/Combat/Weapons/` 抽象 `EmberWeapon` + `EmberWeaponContext`；Basic/Orbit/Trail/Pulse 迁移，数值不变（22/0.8、轨道 DPS65、拖尾、pulse）。
- `EmberCombat` 持有 `List<EmberWeapon>`，Begin 时注册 4 基础 + Pierce/Boomerang/Meteor；解锁门控在武器 Tick / `OwnedWeapons`。
- Step2：`EmberOffer.Kind` = Stat/Weapon/Evolution；`Choices` 加权不放回；W1 Stat-only；Id10 穿透火矢。
- Step3：Id11 回旋烬蝶（往返两段）；Id12 天降火雨（0.6s 预警后 AOE）。
- Step4：Id20 燎原 / Id21 日冕；条件门控入池；选择后 `Evolved` 永久移除；行为替换在 Basic/Orbit Tick。
- Step5：`EmberChecks.ValidateBuildDepth`：W1 无武/进化、W2 可出穿透、进化钻石、阈值未满足无进化、Choose 后消失、补齐 Stat；无 `AddExperience`。
- UI 徽章：新武器 / 升级 / 进化（`\u` 转义）；`UpgradeNames/Details` 扩至 Id21。
- 保持 Difficulty-Waves-v2：清波强制选卡、无 XP、Score 余烬、Boss 2200、稀有度 20/40/65/90。
