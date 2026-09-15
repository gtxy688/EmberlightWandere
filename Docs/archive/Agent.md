> # ⛔ 已归档 · 已被取代
> 本文件曾是项目的事实协作规范。
>
> **现行根指令：仓库根目录 `AGENTS.md`**（AI 工具会自动加载）
> **项目事实与数值：`../requirements.md`**
> **模块边界与任务索引：`../architecture/00-overview.md`**
>
> ⚠️ 本文引用的 `Docs/Emberlight-Design.md` 已归档；
> 「解耦目标（关卡/刷怪）」已完成——数值收敛进 `LevelConfig`，见 `../architecture/01-config-tuning.md`。
> 注意：本文提到的 Validate 自检项已随校验脚本删除，现行验收口径见 `AGENTS.md`。

---

# Agent.md — EmberlightWandere 协作规范

给所有参与本项目的 Agent / 人看。改代码或文档前先读本文件与 `Docs/Emberlight-Design.md`。

## 角色

| 角色 | 负责 | 不负责 |
|------|------|--------|
| jack | 拍板、试玩反馈、优先级 | — |
| xiaoProducer | 拆计划、标依赖/风险、指该找谁 | 不大段写 C#；不定数值终稿；不主动派工 |
| xiaoPlanner | 玩法/数值/波次/验收规格 | 不写大段引擎代码 |
| xiaoCoder | C#、系统、性能、可运行改动 | 不定玩法终稿 |
| xiaoLevel | 关卡节奏、路径、遭遇、场景 checklist | 不大改核心玩法代码 |
| xiaoUI | HUD/菜单/交互说明 | 不定数值 |

未指派时：文档归 Producer/Planner；代码归 Coder；关卡表归 Level；界面归 UI。

## 工程事实

- Unity **2022.3.62f3**，命名空间 `Emberlight`
- 主场景：`Assets/Scenes/Emberlight.unity`
- 脚本根：`Assets/Scripts/{Core,Combat,UI,Visuals}` + `Assets/Editor`
- 文档根：`Docs/`
- GitHub：`https://github.com/gtxy688/EmberlightWandere.git`（本地可能尚未首 commit）

## 改动约定

1. **先读再改**：动战斗节奏前读 `EmberCombat` / `EmberBoss` / `RunProgress`；动 UI 前读 `EmberMenuUi` / `EmberHud`。
2. **路径以仓库为准**：禁止再写 `Assets/Emberlight/Scripts/`。
3. **小步可回退**：一次只改一个闭环问题；改完在编辑器跑通菜单→战斗→升级→胜/负。
4. **数值与规则**：幅度、权重、Boss 出场条件以 Planner 规格为准；代码里的魔法数要集中到配置类（解耦目标）。
5. **安卓真机排最后**：未完成游戏闭环前，不主动扩 APK / 真机专项。
6. **文档同步**：改了公开行为（Boss 时间、稀有度、边界）必须更新对应 `Docs/*.md` 同一 PR/同一批改动。
7. **不要派工风暴**：除 jack 明确要求，Agent 之间不互相催办、不群发任务；需要协作时回 jack 点名。

## 解耦目标（关卡 / 刷怪）

现状：地图边界、刷怪间隔、Boss `480s`、敌人成长都揉在 `EmberCombat` / `EmberWorld`。

目标形状（实现归 Coder）：

- `LevelConfig`（或等价）：边界、刷怪曲线、Boss 触发条件（时间 **或** 波次）、敌人上限
- `EmberCombat` 只消费配置，不写死 `480`
- 场景里可挂 ScriptableObject / 纯数据，便于 Level 调参不改战斗逻辑

## 视觉

默认允许程序生成占位。若有 `Assets/Art/` 或 Resources 贴图，优先替换角色/Boss/地面，保持现有尺寸与锚点。

## 自检（改完至少做）

- [ ] Console 无报错进主菜单
- [ ] 能开一局、升级、暂停、胜或负
- [ ] 若动 Boss/刷怪：编辑器「试玩 Boss」与自然流程都符合当前 Docs 描述
