# M10 Buff 系统

> 相关需求：`../requirements.md` §4.10（效果、掉落、合成与生命周期）
> 代码：`Assets/Scripts/Core/Buffs/`

## 职责边界

负责玩家临时 Buff 与 Debuff 的配置、独立倒计时、刷新、移除和属性汇总。不负责掉落概率、敌人技能命中、世界视图、生命结算或 HUD 排版。选卡构筑仍由 M2 管理。

## 依赖

核心是普通 C#，无 UnityEngine、场景、UI 或游戏入口依赖。M0 持有并推进，M3 在伤害入口读取倍率，M5 经宿主请求施加，M6 只读取生效列表。

## 设计与数据流

`EmberBuffCatalog` 保存共享的不可变 `BuffConfig`。每个 `BuffManager` 持有自己的 `Buff` 对象，配置可共享，剩余时间分别维护。

```text
敌人最终死亡 → EmberDrops → 拾取 → IEmberCombatHost.TryAddBuff
射手技能命中 → EmberEnemyProjectiles → IEmberCombatHost.TryAddBuff
                                      ↓
                               Run.Buffs.Add
                                      ↓
Playing 帧 → Tick → 到期移除 → 汇总移速 / 伤害倍率
                             ├─ EmberKeeperMotion
                             ├─ EmberCombatDamage.Apply
                             └─ EmberHudPresenter → 只读列表 → HUD
```

## 关键机制

- 同 Id 刷新时间，不增加层数；同 Id 必须使用同一份共享配置，拒绝运行时偷换规则。
- 施加、移除、到期和清空时重新汇总属性。普通计时不重算倍率，UI 只在显示秒或种类变化时重建文案。
- 不修改基础属性或选卡字段，因此效果交错到期不会误恢复其他 Buff 的贡献。
- 组合根在输入阶段检查后、玩家移动前推进 Buff；拾取与技能命中后新获得的效果供后续调用读取，移速最迟下一帧生效。
- 伤害倍率集中在实际命中入口应用，武器无需分别判断增伤 Buff；已有弹体也按命中时的 Buff 结算。
- 迟缓弹先记录命中前的无敌帧许可，再扣血和请求施加；死亡后的请求由宿主拒绝。弹体回池清除 Buff 元数据，普通弹不会继承迟缓。

## 对外契约

| 名称 | 契约 |
|---|---|
| `BuffConfig` | 不可变；Id、名称必填，属性有效，修正为有限数，持续时间为有限正数 |
| `Buff` | 公开只读的配置与剩余时间；刷新与计时仅管理器调用 |
| `BuffManager.Add` | 添加返回 true，刷新返回 false，供调用方转译反馈；相同 Id 的冲突配置报错 |
| `Tick` / `Remove` / `Clear` | 推进并到期、按 Id 移除、清空；非法时间拒绝，清空后倍率回到中性值 |
| `Active` | 缓存的只读列表；外部不能修改集合或计时 |
| `MoveSpeedMultiplier` / `DamageMultiplier` | 已汇总、已钳制的修正倍率，具体规则与参数见需求 §4.10 |

## Unity 装配

`EmberRunState` 创建玩家 BuffManager。`EmberGame` 仅接线、驱动 Tick、清空和转译音效/UI；效果规则留在管理器。伤害 context 绑定当前管理器；UI 图标与浮字来自 UIRoot 的序列化引用，无运行时控件创建。新增 / 刷新结果经 Presenter 转成浮字，图标环与秒数读取当前 Buff；UI 自己管理浮字显示时间。新局与回营地清空，普通换波保留。

## 影响与回归范围

- M0：Playing 门控、移动前顺序、回营地和新局清理。
- M3/M4：所有伤害路径均经过统一入口且倍率只乘一次，持续区域与飞行弹体的时机一致。
- M5：治疗与 Buff 独立掉落、分裂/重生不重复掉落、吸附/过期/回池、迟缓弹命中和拦截。
- M6/M7：Prefab 引用、倒计时、暂停与升级期间不消耗时间、字体覆盖与实际显示。

核心与接线检查位于 `Assets/Tests/EditMode/BuffManagerTests.cs`、`Assets/Tests/EditMode/BuffIntegrationTests.cs`，场景行为见 `Assets/Tests/PlayMode/EmberRuntimeSmokeTests.cs`。
