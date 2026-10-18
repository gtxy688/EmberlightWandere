# 进度追踪

> 更新日期：2026-10-18
> 本文件只记录当前开发状态。需求见 [requirements.md](requirements.md)，模块边界见 [架构索引](architecture/00-overview.md)。

## 当前状态

- 主分支为 `main`，游戏保留现有熄灯与暗影主题。
- 核心单局、六把武器、波次/Boss、治疗掉落与选卡构筑已实现。
- 临时增伤、疾行掉落与射手迟缓技能已接入，共用 BuffConfig / Buff / BuffManager。
- Buff HUD 使用图标、径向计时环与剩余秒数；角色浮字显示效果修正与刷新结果，旧文字底板已移除。
- 治疗、增伤、疾行掉落已使用专用透明图集及持久 Sprite 切片。
- `codex/art-sample` 的玩家按新提灯小法师参考接入 Idle / Move 各两帧，已有 Keeper Prefab 原位绑定 Animator，按实际位移切状态和翻转，暂停与选卡冻结；普通怪仍沿用 A「清爽卡通」静态图、追击和对象池。`main` 分支保持原样。
- 当前石地、灯柱/碎墙/蜡烛、紧凑 HUD 边框与 Buff 浮字、图标环和秒数继续保留。其他兵种与 Boss 尚未扩展到 A 造型。
- `Build Player Sprite Animations` 导出和装配两帧玩家，`Use Clean Toon Characters` 应用当前卡通 Prefab；`Use Main Branch Characters` 可恢复原版角色。重复装配保留已有帧、动画、切片身份与外观布局。
- 单方向分件行走小样与透明帧已保留用于对比；jack 反馈步态不自然，当前停止扩展该路线。
- 低模 3D 庭院与角色小样保留用于对比；用户反馈人物与场景呆板，停止向正式游戏迁移。
- 生命结算、选卡流程、武器参数与共享燃地已按职责拆分。
- UIRoot 与 IntroRoot 已接入场景，运行时复用 Prefab 控件。
- 普通/遭遇杂兵的屏外生成与 HUD 文字刷新优化已实现。
- 保留技术分析、实现说明、需求与模块架构，以及自动化测试源码和运行工具。

## 后续安排

- 先由 jack 判断两帧玩家的实际步态、造型和背景可读性，再决定是否扩展其他兵种；地图、HUD 与 Buff 展示保持当前方案。
- 最终工程导出由 jack 处理，已有旧包需要重新导出。
- Android 构建、正式签名和商店素材按实际交付平台另行安排。

技术改动背景与工具链教训见 [开发历史](archive/development-history.md)。
