# 美术样板来源

场景样板素材于 2026-10-17、玩家拆件于 2026-10-18 使用内置 imagegen 生成，用于用户确认的可玩样板。方向来自用户提供的暗色手绘遗迹参考图：蓝灰石地、暖橙提灯角色、灰紫幽灵与克制的金属边框。参考截图没有直接作为游戏贴图使用。

工程文件在 `Assets/Art/HandPaintedSample/Textures/`，保留生成 PNG 的原始像素；没有通过程序抠图、重绘、拼接或缩放源文件。Unity 导入可限制实际纹理尺寸，Sprite 切片和九宫格边框由 Editor API 写入 `.meta`。这些原图、Prefab 与 `.meta` 需一并交付，运行游戏不依赖生成工具。

| 源文件 | 原始尺寸 | 内容与用途 |
|---|---|---|
| `keeper.png` | 1230×1278，RGBA | 深蓝兜帽提灯玩家，完整独立角色 |
| `keeper-parts.png` | 1254×1254，RGBA | 九格透明拆件：头、躯干、披风、双腿、空手臂、提灯上臂、前臂和提灯 |
| `shade.png` | 1230×1278，RGBA | 灰紫斗篷与红眼的普通幽灵 |
| `courtyard-floor.png` | 1254×1254，RGB | 低饱和蓝灰石板地，重复平铺 |
| `lantern-pillar.png` | 1024×1536，RGBA | 橙色灯火、铁灯罩与破损石座 |
| `ruin-rubble.png` | 1774×887，RGBA | 横向碎墙、碎石和少量草丛 |
| `candle-cluster.png` | 1536×1024，RGBA | 蜡烛、石块与少量蓝色晶体 |
| `hud-panel.png` | 2172×724，RGBA | 无文字的深蓝金属面板，薄铜金边框 |

## 生成约束摘要

以下是各次生成的设计约束摘要，用于后续延续风格，不作为完整对话记录：

- 玩家：俯视游戏用的完整小比例兜帽角色，深蓝斗篷、细金边、琥珀眼和铜提灯；轮廓清晰，不生成脚下火环、外围火球或场景。
- 玩家拆件：2026-10-18 以工程内 `keeper.png` 为参照生成，再修正为九格留白布局。保持原色彩、金边和朝向，补全被遮挡的连接处；前臂去掉重复提灯环。独立关节和动画由 Unity 制作，非逐帧独立重绘。`Animations/KeeperWalkFrames` 为 Unity 相机渲染的透明帧，源图未做程序像素修改。
- 普通敌人：灰紫兜帽幽灵，暗色面孔、红眼、破损衣摆，姿态与玩家保持同一游戏视角；不包含血条、文字和地面。
- 石地：低对比、暗蓝灰的不规则石板与细裂缝，适于连续平铺；不画角色、道具、明显中心图案或 UI。
- 灯柱：独立的破损石座与铁灯罩，暖橙火焰，保持完整轮廓和透明背景。
- 碎墙：低矮的横向石墙残段、碎石和少量草；低饱和，控制亮度，透明背景。
- 蜡烛：独立的小组象牙色蜡烛、石屑和少量蓝色晶体，暖光克制，透明背景。
- HUD：横向空白深蓝面板，细铜金边、转角装饰和少量菱形节点；不包含文字、数字、图标、计时环或血条，让这些元素由实际 UI 控件提供。

当前美术分支保留这批资源中的地图背景、场景装饰和 HUD 边框，玩家采用下述两帧小法师，敌人采用下述无脚悬浮皮肤；第一版手绘整体和拆件角色保留作对比。Buff 继续使用现有浮字、图标环与秒数，不能把参考图中未实现的内容当作当前工程状态。

## 两帧提灯小法师

2026-10-18，用户提供 [黑暗中的提灯小法师](黑暗中的提灯小法师.png) 作为严格角色参考，并把动作范围确定为 Idle、Move 各两帧。使用内置 imagegen 生成真正透明的完整角色动作原稿；移动稿修正了重复迈同一只脚的问题，最终只取两次相反落脚，未采用整图位移或缩放冒充步态。待机保持双脚支撑，只有细微衣摆与摆灯差异。

- 生成原稿：`Assets/Art/CleanToonSample/Player/Source/Player_Idle_Generated.png`（1774×887）与 `Player_Move_Generated.png`（1254×1254）。Move 源稿有四格，当前仅使用左上、左下两个落脚姿势；其余源格不进入动画。
- 交付帧：同级 `Player_Idle_0.png`、`Player_Idle_1.png`、`Player_Move_0.png`、`Player_Move_1.png`，均为相同画布的 RGBA PNG。两张 `Player_Idle_Sheet.png`、`Player_Move_Sheet.png` 是横向双格图集；尺寸、脚底与导入数值统一见需求 §7。
- 后处理：`EmberPlayerSpriteSetup` 在 Editor 离线等比采样、对齐脚底和上半身横向参考、拼接 Sheet；没有修改生成原稿、重绘头脸或靠分件扭曲补造脚步。帧间细小绘画差异仍需以实际循环观感判断。
- 集成：`Player_Idle.anim`、`Player_Move.anim` 与 `Player.controller` 保存 Sprite 引用；原 Keeper Prefab 原位绑定，旧静态玩家另存 `KeeperStatic.prefab`。普通怪、地图、HUD 与 Buff 资源不变。
- 参考边界：《弹壳特攻队》的截图和本地参考工程只用于理解少帧切换、朝向翻转等表现方法；没有复制其中的角色或怪物贴图。当前玩家也不依赖该工程或外部生成路径。

## 无脚悬浮怪物皮肤

2026-10-18，按用户「同玩家风格、依兵种设计、尽量没有脚」的要求，使用内置 imagegen 参考已认可的蓝绿提灯玩家，生成独立怪物素材。保持粗深色外轮廓、宽色块、暗面孔和亮眼；全部以破布尾端、烟尾或悬浮壳体收尾，不含腿或靴子，不从《弹壳特攻队》复制角色资源。

素材根为 `Assets/Art/CleanToonSample/Enemies`，所有文件保留生成原图及真正透明 Alpha：

| 源文件 | 内容（从左到右） |
|---|---|
| `enemy-core.png` | 灰紫红眼普通幽灵、窄身洋红快怪、宽肩灰甲高血怪 |
| `enemy-special.png` | 紫袍晶杖射手、绿色卵囊母体、小型绿色尾茧子体 |
| `enemy-guardians.png` | 蓝色铁甲盾卫、橙色引线火灵、紫袍金冠提灯 Boss |
| `guard-shield.png` | 独立蓝色金属盾，运行时随盾卫防御朝向旋转 |

`EmberEnemySkinSetup` 按透明列间隔导入完整轮廓，切片与持久引用保存到 `.meta` 和既有 Shade Prefab，不重绘源图、不在运行时切图。尺寸与导入参数见需求 §7。怪物没有新增行走帧，复用浮动、压缩、倾斜与技能提示；Boss 皮肤不叠加旧冠饰，盾卫不把功能盾烘进身体。完整工程需包含这批 PNG、`.meta`、Prefab、Editor 工具和表现源码，游戏运行不依赖生成工具。

## A 清爽卡通角色

2026-10-18，用户从 A / B / C 造型对比中选择 A。以对比图的 A 栏为风格参照，使用内置 imagegen 分别生成玩家与普通敌人的完整透明图，保留折叠布兜帽、简洁轮廓、宽衣褶和少量色阶：玩家为蓝绿斗篷、暖色眼睛、圆铜扣与铜提灯，普通怪为灰紫斗篷与红眼。未从第三方游戏截图提取素材。

- 原图：`Assets/Art/CleanToonSample/Textures/keeper-v1.png`、`shade-v1.png`，各为 1199×1312 RGBA，原始透明通道和留白完整保留，没有程序抠图、重绘或缩放 PNG。
- 装配：`Assets/Editor/EmberCleanToonSetup.cs` 配置 Unity 导入设置并创建缺失 Prefab；`Assets/Art/CleanToonSample/Prefabs` 保存玩家与普通怪模板。切换只更新既有美术集的角色引用，不改地图、UI 或 Buff。
- 动作：这两张旧静态图不是行走序列帧；旧玩家浮动方案保存在 KeeperStatic 中。当前玩家使用上文的两帧动作，怪物使用无脚皮肤与既有躯干动画。
- 交付：保留 CleanToonSample 全目录、`.meta`、Editor 工具与既有运行代码；游戏运行不依赖 imagegen 或外部图片路径。

## 原版角色精修部件

`Assets/Resources/Art/Characters/classic-character-parts.png` 由 `Assets/Editor/EmberCharacterArtBake.cs` 的二维轮廓、衣褶与明暗绘制配方生成，属于代码原生美术，不是新生成的手绘整张角色。四格分别为斗篷、兜帽、衣摆和灯壳；以灰度与透明通道保存，供玩家和各类敌人共用，再乘原有兵种色。

运行时取图在 `Assets/Scripts/Visuals/EmberCharacterArt.cs`，不重绘像素。修改配方后使用 `Emberlight/Art/Bake Classic Character Parts` 重烘；切片身份由 Unity Editor API 保留，原有四张图集不受影响。交付时保留 PNG、`.meta` 与生成器源码，不依赖外部绘图软件或图片生成服务。

## 低模 2.5D 独立小样

`Assets/Art/LowPolySample` 只保留作独立实验，当前未采用该外观。模型和材质由本工程 Editor 源码生成：`EmberLowPolyMeshBuilder` 构建几何与法线，`EmberLowPolySampleSetup` 保存材质、蒙皮骨架、Mesh、Prefab 和独立场景。它们是实际三维资源，运行时不依赖生成过程或外部建模软件，也没有使用新生成的二维角色贴图。

空间方向参考用户提供的 [B 站视频前段](https://www.bilibili.com/video/BV1rgY76NEKn/)：斜俯视、立体地台、低模树石和统一阴影。场景保留项目的暗夜提灯主题。没有从视频提取模型、角色图或贴图；作者视频中角色的底层实现尚未确认。

交付时保留 LowPolySample 全目录及 `.meta`、生成器源码和 `Assets/Scripts/Visuals/LowPoly` 的运行代码。角色目前使用程序骨骼动作，可从骨架参数和步态代码继续调整；当前资产不包含 Blender 源文件或独立 Animator 动作资源。
