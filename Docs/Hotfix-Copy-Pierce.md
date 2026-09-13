# 文案白话 + 穿透狙击（Hotfix）

> jack 2026-09-13：局外先不做；选武器说明白话；穿透改为站住才开火。

## 局外
`Meta-Progression-v1` **暂停**。灯油/灯铺可留代码，不再排期扩展图鉴/成就/解锁树。

## 选武器说明（RosterDescs）
| 武器 | 文案 |
|------|------|
| 火球 | 自己会打火球，好上手 |
| 环火 | 火团围着你转，碰到就烫 |
| 燃地 | 走过的路着火，怪踩了掉血 |
| 穿透火矢 | 站住才开火，一箭穿一串 |
| 回旋烬蝶 | 丢出去再飞回来，来回都能打 |
| 天降火雨 | 先画圈再砸火，打一片 |

禁止：基线 0.20、往返结算、预警 AOE、DPS 等术语。

## 穿透手感
- **仅当玩家基本静止时才开火**（移动中不进入射击；已飞出的箭继续飞）。
- 静止判定：建议速度 < 0.15 世界单位/秒，或本帧位移 < 阈值阈值。
- 文案与专属/质变保持白话。
- 目的：狙击手感 + 削超模。

## 落地
1. xiaoPlanner：文案已改 RosterDescs / UpgradeDetails；本文档  
2. xiaoCoder：`EmberWeaponContext` 提供是否静止；`EmberPierceWeapon` 开火门控  
3. xiaoUI：若开局说明另有控件，跟 RosterDescs

## 程序落地
- `EmberWeaponContext.PlayerSpeed` / `PlayerStanding`（阈值 0.15）
- `EmberPierceWeapon`：`cd` 仍走，开火仅 `PlayerStanding`；在飞箭不受影响。
