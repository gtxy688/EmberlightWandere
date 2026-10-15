# 开发历史（技术背景）

本文件保留技术改动与工具链教训。现行需求见 [requirements.md](../requirements.md)，模块设计见 [架构索引](../architecture/00-overview.md)。

## 主要改动

- 2026-09-15：中文字体改为预烘静态图集，保留 `ValidateBaked` 与动态回落；图集改为真正的 Sprite Sheet，导入器保留切片；开局选武器改用滚动列表。
- 2026-09-23：`EmberGame` 收敛为组合根，抽离本局状态、生命、选卡、输入/移动与 HUD 推送；Combat 使用宿主契约，Visuals 注入阶段委托。
- 2026-09-23：从 `RunProgress` 拆出 `EmberWeaponCatalog` 与 `EmberChoicePool`，配色改为返回 RGB 数组，消除 M2 对 UnityEngine 的依赖。
- 2026-09-23：战斗拆为 `EmberCombat`、`EmberWaveDirector`、`EmberEnemyRoster` 与 `EmberCombatDamage`；修正 Boss 重复推进，分裂只按待生成落点处理。
- 2026-10-03：建立 Runtime、Editor 与测试程序集边界，字形检查默认读取 `Emberlight.Runtime.dll`，并将空输入视为错误。
- 2026-10-13：收窄生命与选卡依赖，集中武器身份/参数/注册，独立共享燃地；卡面展示与控件创建分离，修正说明映射与击杀计数回报。
- 2026-10-14：主界面和加载页迁移到 UIRoot、IntroRoot，图片持久化；普通/遭遇杂兵改用完整视口外沿带取点，HUD 缓存显示值。

## 工具链教训

- Windows PowerShell 5.1 默认编码会损坏 UTF-8 无 BOM 文件中的中文。编辑中文源文件使用编辑工具，或明确 UTF-8 读写。
- Unity 授权客户端依赖命名管道；批处理需要正常授权环境。同一工程不能同时由两个 Unity 编辑器打开。
- Unity 退出与产物写入完成可能存在时间差；检查实际进程退出码和结果文件，不能仅凭日志措辞判定。
- 离线编译只覆盖 Runtime，测试程序集需要由 Unity 编译。
- asmdef 会改变程序集产物路径。依赖 DLL 的工具必须同步更新路径，并拒绝空输入。
- 保留随机数消耗顺序，才能让相同种子的构筑流程继续复现。

## 字体教训

- 静态图集不能追加字形，改文案后需要重新烘制。
- 方法体中的内联字符串无法靠手写文案清单完整收集，字形检查读取编译后程序集的字符串堆。
- TMP 内部使用下划线与省略号，即使游戏文案未出现，也需要纳入字符集。
- 字体资产字段正确不代表真实画面正确，保留 `EmberFonts.ValidateBaked` 与实际渲染检查。
