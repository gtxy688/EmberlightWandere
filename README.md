# 烬灯行者 · EmberlightWandere

Unity 2D 俯视角生存原型。玩家移动躲避敌人，武器自动攻击，清波后选卡构筑，击败最终 Boss 或生命归零后结算。

## 打开与运行

1. 用 **Unity 2022.3.62f3** 打开工程根目录，等待 Package Manager 与资源导入完成。首次导入需要网络解析 `Packages/manifest.json` 中的依赖。
2. 打开 `Assets/Scenes/Emberlight.unity`，点击 Play，等待加载页结束后按任意键。
3. 依次选择征程长度、难度、槽位数、**一把**起始武器，然后确认开战。
4. WASD / 方向键移动，也可拖动屏幕下半部使用虚拟摇杆；自动攻击；Esc 或右上按钮暂停。

项目使用 Built-in、旧 Input Manager、uGUI 和 TextMeshPro。运行代码、编辑器工具、EditMode 与 PlayMode 测试有各自的 asmdef。完整依赖版本以 `Packages/manifest.json` 与 `packages-lock.json` 为准。

## 源码入口

| 路径 | 内容 |
|---|---|
| `Assets/Scripts/Core/EmberGame.cs` | 入口与装配、阶段所有权、每帧顺序 |
| `Assets/Scripts/Core/` | 生命结算、构筑、卡池、身份表、配置、输入 |
| `Assets/Scripts/Combat/` | 波次、敌人、伤害与死亡、治疗掉落、武器 |
| `Assets/Scripts/UI/` | 菜单、HUD、卡牌展示与交互 |
| `Assets/Scripts/Visuals/`、`Audio/` | 表现与音频 |
| `Assets/Editor/`、`Assets/Tests/`、`Tools/` | 导入/字体/构建工具、测试、校验与打包脚本 |

简短说明：[技术分析](Docs/技术分析.md)、[实现说明与验证记录](Docs/实现说明.md)。需求与数值权威为 [requirements.md](Docs/requirements.md)；维护路由见 [架构索引](Docs/architecture/00-overview.md)。素材来源见 `Docs/reference/`。

## 对照笔试题 2

题目要求 **2D 俯视角生存原型**：WASD/摇杆移动 + 摄像机跟随、丧尸从屏幕外随机生成并追踪玩家、玩家自动攻击、敌人死亡掉落物品；进阶 buff / debuff / aoe 与可扩展性；另需技术分析与实现文档。

逐项核查结论、源码证据位置与剩余改进点见 **[笔试题 2 符合性核查](Docs/笔试题2符合性核查.md)**。要点：

| 题目要求 | 判定 |
|----------|------|
| 2D 俯视角、WASD/摇杆移动、摄像机跟随 | 符合 |
| 丧尸从屏幕外随机生成并追踪玩家 | 符合（Boss 与分裂子体按需求保留特殊落点） |
| 玩家自动攻击 | 符合（六把武器各自 `Tick`） |
| 敌人死亡掉落物品 | 符合（治疗掉落，概率触发） |
| 进阶：buff / aoe | 符合 |
| 进阶：debuff | **不完整**——只有区域持续伤害，没有附着在目标上的限时状态 |
| 可扩展性 / 可维护性 | 符合，是本项目的强项 |
| 技术分析与实现文档 | 符合 |

## 验证

```powershell
powershell -ExecutionPolicy Bypass -File Tools/compile-check.ps1 -UnityExe "<Unity 安装目录>/Editor/Unity.exe"
powershell -ExecutionPolicy Bypass -File Tools/run-tests.ps1 -UnityExe "<Unity 安装目录>/Editor/Unity.exe" -Platform EditMode
powershell -ExecutionPolicy Bypass -File Tools/run-tests.ps1 -UnityExe "<Unity 安装目录>/Editor/Unity.exe" -Platform PlayMode
```

离线编译需要先导入一次工程，只检查 Runtime；测试脚本需先关闭此工程的 Unity 编辑器。PlayMode 用图形模式执行。实际结果及未覆盖范围见实现说明，测试通过不代表手感与审美验收完成。

## 工程与源码交付

```powershell
powershell -ExecutionPolicy Bypass -File Tools/export-project.ps1
```

生成 `Delivery/EmberlightWandere-project.zip`，包含 **Assets（含源码与 .meta）、Packages、ProjectSettings、Tools、Docs、README**。不包含 Library、Temp、Logs、构建缓存、个人配置或 Git 历史。字体、图片、音频必须是实际资源文件；脚本会拒绝 Git LFS 指针占位文件。

交付的是可重新导入的完整 Unity 工程；可执行程序只能作为另附的试玩版本。修改源码后请重新导出，不能把旧 zip 当作最新工程。

## 笔试题 2 仍需关注

- **已有敌人死亡治疗掉落**，不是完全没有掉落；目前不是通用战利品系统。
- 普通与遭遇杂兵已改为从完整视口外沿带生成，边界处理后仍保证屏外；Boss/分裂保留特殊位置。还需确认题目所说的「丧尸」是否接受当前暗影敌人外观。
- 已有属性增益、护盾、专属/质变以及范围持续伤害；**尚无通用 buff/debuff 系统，减速、易伤、定时状态等未实现**——这是进阶项唯一的实质缺口。
- 提交前仍需人工试玩完整胜负流程、确认画面与操控；Android 真机与性能数据尚未验证。
- 本轮附带的注释精简与文档重构**尚未在编辑器里真进一局确认**，也未重新导出 `Delivery/` 工程包。
