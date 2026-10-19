# 烬灯行者 · EmberlightWandere

Unity 2D 俯视角生存原型，保留熄灯与暗影主题。玩家移动躲避敌人，武器自动攻击，清波后选卡构筑，击败最终 Boss 或生命归零后结算。

## 打开与运行

1. 用 **Unity 2022.3.62f3** 打开工程根目录，等待 Package Manager 与资源导入完成。首次导入需要网络解析 `Packages/manifest.json` 中的依赖。
2. 打开 `Assets/Scenes/Emberlight.unity`，点击 Play，等待加载页就绪后按任意键。
3. 依次选择征程长度、难度、槽位数和**一把**起始武器，然后确认开战。
4. WASD / 方向键移动，也可拖动屏幕下半部使用虚拟摇杆；武器自动攻击，Esc 或右上按钮暂停。

敌人击杀后按概率掉落治疗、增伤或疾行物品，靠近后吸附拾取；临时 Buff 在 HUD 显示剩余时间。暗烛射手的青色技能弹会造成短时迟缓，可以躲避或拦截。构筑包含单武器强化、护盾、专属/质变和范围伤害。

项目使用 Built-in、旧 Input Manager、uGUI 和 TextMeshPro。运行代码、编辑器工具、EditMode 与 PlayMode 测试有各自的 asmdef。完整依赖版本以 Packages 中的清单为准。

## 源码与文档入口

| 路径 | 内容 |
|---|---|
| `Assets/Scripts/Core/EmberGame.cs` | 入口与装配、阶段所有权、每帧顺序 |
| `Assets/Scripts/Core/` | 生命结算、构筑、卡池、身份表、配置、输入 |
| `Assets/Scripts/Combat/` | 波次、敌人、伤害与死亡、治疗掉落、武器 |
| `Assets/Scripts/UI/` | 菜单、HUD、卡牌展示与交互 |
| `Assets/Scripts/Visuals/`、`Assets/Scripts/Audio/` | 表现与音频 |
| `Assets/Editor/`、`Assets/Tests/`、`Tools/` | 编辑器工具、自动化测试、检查与打包脚本 |

内部开发文档：[技术分析](Docs/技术分析.md)记录设计思路与取舍，[实现说明](Docs/实现说明.md)用于查调用链、改功能和找工具。需求与数值看 [requirements.md](Docs/requirements.md)，模块入口看 [架构索引](Docs/architecture/00-overview.md)，素材来源在 `Docs/reference/`。

## 开发检查

```powershell
powershell -ExecutionPolicy Bypass -File Tools/compile-check.ps1 -UnityExe "<Unity 安装目录>/Editor/Unity.exe"
powershell -ExecutionPolicy Bypass -File Tools/run-tests.ps1 -UnityExe "<Unity 安装目录>/Editor/Unity.exe" -Platform EditMode
powershell -ExecutionPolicy Bypass -File Tools/run-tests.ps1 -UnityExe "<Unity 安装目录>/Editor/Unity.exe" -Platform PlayMode
powershell -ExecutionPolicy Bypass -File Tools/doc-line-check.ps1
```

离线编译需要先导入一次工程，只检查 Runtime；测试脚本需先关闭此工程的 Unity 编辑器。PlayMode 用图形模式执行，输出保存在 Library。

## 工程与源码交付

```powershell
powershell -ExecutionPolicy Bypass -File Tools/export-project.ps1
```

生成 `Delivery/EmberlightWandere-project.zip`，包含 **Assets（含源码与 .meta）、Packages、ProjectSettings、Tools、Docs、README**。Library、Temp、Logs、个人配置和 Git 历史不随工程提交。字体、图片、音频必须是实际资源文件，脚本会拒绝 Git LFS 指针占位文件。

交付完整 Unity 工程及源码，可执行程序可另附用于试玩。修改源码后请重新导出工程包。
