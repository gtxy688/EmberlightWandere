# M7 美术与表现

> 相关需求：`../requirements.md` §7（技术约束：中文字体雷区）
> 代码：`Assets/Scripts/Visuals/` + `Assets/Scripts/Core/EmberFonts.cs` + `Assets/Editor/EmberFontBake.cs`、`EmberPickupArtSetup.cs`、`EmberPlayerSpriteSetup.cs` 与资源导入器

## 职责边界

- **本模块负责**：程序化精灵生成与共享、图集取图与缓存、稀有度卡框与词条图标映射、粒子效果池、玩家角色外观与序列帧 / 分件动画、世界美术、以及**中文 TMP 字体资产的构建与预热**。
- **本模块不负责**：
  - 玩法数值与碰撞 → M1 / M3
  - 卡面排版与文案 → M6
  - 敌人剪影与敌人动画 → M5（`EmberEnemySilhouette` / `EmberEnemyVisual`）

**⚠️ 本模块历史上含全项目最高风险的雷区**：中文字体预烘焙。该问题已解决（见下文「中文字体」），但预烘资产**必须留住 `ValidateBaked` 这道运行时闸门**——闸门被删掉就等于把"资产坏了就整个 UI 挂掉"这个性质放回来。

## 依赖

| 被依赖模块 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|------------|------------------|------|--------------|
| M2 | `EmberRarity` 枚举（卡框按稀有度取图）、词条 id（图标映射）、`AllNames`（字形预热） | 取图与预热 | 未知 id 回落占位图 |
| M0 | **阶段委托**（`System.Func<EmberRunPhase>`），通过 `Initialize` 注入 | "是否播放"门控与暂停冻结 | 委托为空时粒子和玩家动画均不推进，不反查游戏实例 |
| Resources | `Resources/Art/**`、`Resources/Fonts/**`、`Resources/Art/Baked/**` | 图集与字体 | 逐级回退，最终用程序化占位 |

**边界（2026-09-23 起）**：`EmberKeeperAnimation` 与 `EmberEffects` 的阶段门控**通过 `Initialize` 注入委托**，
表现通过注入的阶段委托工作，不主动查找游戏入口或读取游戏实例。
因此 `Assets/Scripts/Visuals/` 下**零 `EmberGame` 类型引用**；这条边界现在由源码扫描可验证。

## 设计与数据流

```text
图集资源（切片权威在 .meta，Sprite Editor 编辑；导入器维护 Sprite / Multiple 设置）
   doodle-icons.png        4×6  词条图标      → EmberCardIcons
   doodle-card-frames.png  2×2  稀有度卡框    → EmberCardFrames
   doodle-ui.png           2×3  UI 面板/按钮  → EmberUiArt
   wanderer-sheet.png      4×3  角色与世界    → EmberWorldArt
       ↓ 每个取图入口都是三级链
   切好的子 Sprite（<atlas>_<index>，Sprite Editor 产出，导入器保留）
       ↓ 缺失或未重新导入
   Sprite.Create 运行时切图（rect 硬编码在 C# 里）
       ↓ 图集整体缺失
   程序化占位（EmberArt.Panel / Flame）

中文字体
   Resources/Fonts/NotoSansCJKsc-Regular SDF.asset（预烘静态，实际字形与纹理规格以资产及烘制工具为准）
       ↓ ValidateBaked 不通过或资产缺失
   CreateChineseDynamic()：otf → CreateFontAsset → 两段式字形预热

程序化兜底（图集缺失时逐级回退）
   EmberBakedArt  从 Resources/Art/Baked/ 惰性载入预烘焙 PNG
        ↓ 缺失
   EmberArt       运行时逐像素光栅化 128×128（Flame / Ring / Panel / Glow）
        ↓
   EmberVisuals   共享 64×64 Disc 与单色 SpriteRenderer

粒子
   EmberEffects（MonoBehaviour，容量见需求 §7）
      Trail / Impact / Nova；粒子模拟由 Playing 阶段门控
```

5 个程序化图元（火焰/圆环/面板/辉光/圆盘）在构建期预生成，运行时加载 PNG，减少首次使用时的光栅化工作，并支持直接编辑图片。烘焙结束会解码 PNG 与现场光栅化逐像素比对（`maxDelta` 必须为 0），防止两处实现产生美术漂移。

## 关键机制

### 拾取物专用图集

`Assets/Resources/Art/Pickups/ember-pickups.png` 保存用户确认的三件套透明原图，`EmberPickupArt` 用 `Resources.LoadAll<Sprite>` 按名称缓存，不依赖子资源返回顺序。切片名为 `pickup-heal`、`pickup-power`、`pickup-haste`，对应药瓶、火种护符和风羽。缺失素材时回落已有图元，工程检查会发现缺失的持久切片。

`EmberPickupArtSetup.Configure` 通过 Unity 2022 自带的 Sprite Editor 数据接口切片，校验 provider 与名字/Id 数据支持，按原图宽度整数分成三格，居中 pivot，末格承接余数像素。重复运行保留已有 spriteID；`.meta` 是切片权威，不在运行时切图。导入保留原尺寸与透明通道、关闭 mipmap，使用 Bilinear 与非压缩设置，Android 同步覆写。没有改动其他图集的导入规则。

可在关闭本工程的编辑器后重新配置：

```powershell
& "<Unity 安装目录>/Editor/Unity.exe" -batchmode -quit -projectPath "<项目>" -executeMethod Emberlight.Editor.EmberPickupArtSetup.Configure -logFile "<日志绝对路径>"
```

来源与生成提示词见 [拾取物美术说明](../reference/pickup-art.md)。

Buff HUD 复用持久的火种、风羽 Sprite；迟缓使用 `Assets/Resources/Art/Buffs/buff-slow.png`，为独立的紫色靴子与向下箭头透明图标，不作为世界掉落。`EmberBuffHudSetup` 导入单 Sprite 并限制导入分辨率，源 PNG 保留原像素。环使用既有烘焙圆环纹理的持久 Sprite，填充由 uGUI 的 Radial360 控制，不把数字或圆环烘入图标。来源见 [拾取物美术说明](../reference/pickup-art.md)。

### ⚠️ 中文字体（历史雷区，已解决）

`EmberFonts.CreateChinese()` **优先使用预烘静态图集**，加载不到或校验不通过时回落到运行时动态图集：

现有 Buff 名称从 `EmberBuffCatalog`、效果和提示用词从 `EmberCopy.BuffGlyphs` 加入 `EmberFonts.AllGlyphs`，烘制与运行时预热使用同一字符来源。新增可见效果名称也需纳入字形来源并重烘；`Update Buff HUD` 同步刷新两个 UI Prefab 的字体引用。

1. `Resources.Load<TMP_FontAsset>("Fonts/NotoSansCJKsc-Regular SDF")`
2. `EmberFonts.ValidateBaked` 逐项校验（图集纹理存在且非空、`characterTable` 非空、`material` 存在、每个 `characterTable` 条目都有对应 glyph、每个 glyph 的 `atlasIndex` 都落在 `atlasTextures` 范围内）
3. 通过 → 直接用；任一不通过 → `Debug.LogWarning` + 回落 `CreateChineseDynamic()`

`EmberFontBake` 写入时保证纹理、材质与字体对象的序列化顺序；禁用多图集，并在写盘前通过 `ApplySingleAtlas` 保证索引一致。`VerifyReloaded` 从磁盘重新加载后执行真实渲染检查。资产字段完整不代表画面正确，技术背景见 [开发历史](../archive/development-history.md)。

烘制会逐步降低采样点，直到完整字符集装入单张图集。

`FitsOneAtlas` 单独用是不够的：关掉多图集后，装不下的字**根本不会进 `glyphTable`**，所以"没有字形指向 atlas 0 以外"依然成立而字数已经缺了。因此烘制还要求 `CoversEveryCharacter` 通过（逐字符 `HasCharacter`），两者**都**满足才接受该采样点。

实现取舍：

- 加载页视图先保持隐藏，经 `CreateChinese` 校验或动态回落后赋字体，再显示；静态图集可直接绘制已烘字形。
- 静态图集通过闸门时不再创建字体或追加字形，`PrewarmInBackground` 直接返回；动态回落仍会创建字体与预热。
- 烘制采样点自适应单图集容量，实际值以生成资产为准；清晰度以游戏内画面判断。
- 包体保留 Noto CJK SC 源字体以供重烘和动态回落；裁剪前须同时确认这两条资源路径。
- 新增/修改文案若引入图集里没有的字，**该字会缺失**（静态图集不能追加字形）。改文案后必须按下面的批处理命令重跑烘制。
- 重跑命令（批处理）：
  `Unity.exe -batchmode -quit -projectPath <proj> -executeMethod Emberlight.Editor.EmberFontBake.Bake`
  只读自检：`-executeMethod Emberlight.Editor.EmberFontBake.CheckRuntimeLoad`
- 单独烘制会重新创建字体与材质子资源，其 fileID 可能改变。随后须执行 `Emberlight/UI/Connect UIRoot Runtime`（批处理入口 `Emberlight.Editor.EmberUiPrefabSetup.Connect`），重新保存 UIRoot 与 IntroRoot 的字体引用；不能只检查字体资产自身。`Update Buff HUD` 已包含重烘和两个 Prefab 的同步装配，可一并完成。
- ⚠️ 批处理必须**在沙箱外**运行，且**项目不能被编辑器打开**（否则 `HandleProjectAlreadyOpenInAnotherInstance` 直接 crash）。Unity 授权客户端与编辑器通过命名管道通信，沙箱拦截会导致 `exit 199`（`IPC channel to LicensingClient doesn't exist`），此时烘制根本不会执行。

### 字形预热

**静态图集路径下不再需要预热**（字形已在图集里，`PrewarmInBackground` 立即返回）。

回落路径（动态图集）仍保留两段式预热：

- `FirstScreenGlyphSet` 同步加入，覆盖首屏标题与提示所需字形。
- `AllGlyphs` 中尚未加入的字符通过协程分批补充。

`CardGlyphSet()` 收集 `EmberCopy.UpgradeNames`、`UpgradeDetails`、`EmberRarityUtil.AllNames` 与 `ExtraCardText`。`MenuGlyphSet()` 收集 `EmberCopy` 的菜单、难度、槽位、武器库与结算常量。

修改文案后同步检查字形覆盖；使用静态图集时重新烘制，使用动态回落时确保预热字符集完整。

### 字形覆盖（静态图集的真正约束）

静态图集**不能追加字形**，所以"图集里有没有这个字"完全取决于烘制时请求了哪些字符。请求集是 `EmberFonts.AllGlyphs = GlyphSet + CardGlyphSet() + MenuGlyphSet()`。

字符来源分为三类：`EmberCopy` / Catalog 的共享文案、方法体内的字面量，以及 TMP 内部需要的特殊字符。`ExtraVisibleGlyphs` 与 `ExtraCardText` 补齐内联来源；下划线和省略号需要显式收集，不能只扫描游戏文案。

**判断"是否还有漏字"的唯一可靠手段是 `Tools/atlas_glyph_check.py`**，它不读 C# 常量，而是：

1. 解析 `Library/ScriptAssemblies/Emberlight.Runtime.dll` 的 `#US`（user strings）元数据堆，取出**每一个字符串字面量**——包括方法体内的；
2. 解析 `.asset` 里的 `m_Unicode` 条目，得到图集实际持有的字符；
3. 求差集，非空则退出码 1 并打印**可直接粘贴的 C# 转义字面量**。

```bash
python Tools/atlas_glyph_check.py
```

**改任何文案后的流程**：先跑这个脚本 → 有缺口就把它打印的字面量补进 `EmberFonts` 相应的 glyph set → 关掉 Unity → 重跑烘制 → 再跑一次脚本确认 `MISSING (0)`。

> 之所以不能只靠 `GlyphSet` 一处收口：`AllGlyphs` 里的字符来源分散在 6 个数组和十余个常量中，而"某句文案是否已被某个来源覆盖"无法从代码结构判断——只能从编译产物验证。

### 图集与导入设置

- 图集为**离线生成**（内置 image_gen），原图**不做程序像素处理**。
- 运行时优先加载 `.meta` 切片的 Sprite，缺失时才按格创建并缓存，**不读取像素**；关闭 mipmap、保留原比例、双线性过滤。
- 边缘修复：已离线移除四档卡框与 UI 面板外的深色底色，保留内部底色与手绘描边；导入启用 `alphaIsTransparency` 防止边缘采样色晕，**无运行时裁剪或纹理处理**。
- Android 平台覆写为 **Uncompressed**（默认 ETC2 会让描边起块状伪影），写在各自 `.meta` 的 Android 平台块。
- 4 个 `AssetPostprocessor` 导入器维护 Sprite / Multiple 导入设置；切片定义保存在图集 `.meta` 中，通过 Sprite Editor 编辑。

### 粒子

- `EmberEffects` 复用粒子对象，容量见需求 §7；达到活动上限时跳过发射，复用池无缓存时创建粒子对象。
- 粒子模拟仅在 `EmberRunPhase.Playing` 推进，暂停时冻结。
- 新局会清理旧特效世界。

容量检查在 `Assets/Scripts/Visuals/EmberEffects.cs` 的 `Emit`；`Update` 按注入阶段控制模拟，`Emit` 本身不校验阶段。

### 玩家角色

- 当前 `EmberArtSet.KeeperPrefab` 仍绑定 `Assets/Art/CleanToonSample/Prefabs/Keeper.prefab`，玩家以用户提供的提灯小法师为参考使用 Idle / Move 两帧序列。`Keeper artwork` 原有 SpriteRenderer 同节点绑定 Animator，两段 Clip 只写 `m_Sprite`，通过 `IsMoving` 零时长切换；不改变移动根或叠加整图浮动。帧内包含交替短靴、衣摆和轻微提灯变化。
- `EmberKeeperAnimation` 读取移动模块已产生的实际位移，只在 Playing 阶段手动推进 Animator；平时 Animator 的 speed 保持冻结值，避免暂停、选卡时自动播放。水平方向按原图朝向设置 `flipX`，上下与斜向共用 Move，X 接近零时保留朝向。初始化重置为 Idle；速度阈值与帧率由需求 §7 统一记录。
- 角色引用留空时，组合根仍可创建原版玩家。`BuildOriginal` 创建兜帽、细眼和提灯部件，`AnimateOriginal` 驱动浮动、斗篷呼吸、脸部偏移与独立提灯摆动。原版视图与敌人原有路径通过 `EmberCharacterArt` 缓存灰度部件，缺图回落圆形图元；这个入口不参与 A 玩家整体贴图的绘制。
- 手绘整体 `Keeper.prefab` 和分件 `KeeperWalk.prefab` 只保留作对比。显式绑定这类模板时，同一驱动读取序列化的躯干、柔光与可选 `EmberKeeperRig`；不在运行时重新创建模板部件。
- 分件小样观察移动根的实际位移，在 Playing 阶段驱动 `EmberKeeperRig.Step`，按位移采样 `KeeperWalk.anim`。停止时平滑回站姿，左右镜像一个侧前造型；没有膝盖蒙皮或独立四方向素材，当前未接入正式入口。整体手绘路径仅浮动、倾斜和呼吸缩放。
- `Use Original Keeper` 指第一版手绘整体贴图；恢复主分支外观应使用 `Use Main Branch Characters`，该入口同时清空玩家和敌人模板，并打开正式主场景，保留环境、UI 与 Buff 展示。
- **游戏暂停时停止动画**（读注入的阶段委托，非 Playing 即冻结）。
- 只移动视觉子节点，保留角色坐标与碰撞规则。

`Emberlight/Art/Bake Classic Character Parts` 调用 `EmberCharacterArtBake.Bake`：由代码原生的二维贝塞尔轮廓、衣褶和灰度明暗生成新 PNG，用边缘距离作抗锯齿，再经 Sprite Editor 数据接口保存切片。重复烘制保留切片身份；修改画法后重烘即可，运行时不逐像素绘制，不新增角色材质。源资产为 `Assets/Resources/Art/Characters/classic-character-parts.png` 及 `.meta`；没有改动既有四张图集。

`Emberlight/Art/Use Clean Toon Characters` 调用 `EmberCleanToonSetup.Build`：导入旧单 Sprite，首次从既有中性模板创建新 Prefab，再更新当前 `ArtSet.asset` 的角色引用。重复执行保留已有玩家动画与怪物皮肤映射，庭院、UI 和 Buff 资产不改写。当前玩家序列和全部兵种皮肤分别由下述工具维护；导入与外观数值统一见需求 §7，来源见 [美术样板说明](../reference/hand-painted-art.md)。

`Emberlight/Art/Build Enemy Skins` 调用 `EmberEnemySkinSetup.Build`：从 `Assets/Art/CleanToonSample/Enemies` 导入三张横排皮肤图和独立盾图，通过真正透明的列间隔找到完整角色范围，避免不等宽原稿被机械均分裁断；Sprite Editor 数据接口保存切片和命名身份，不重绘源 PNG。原位补齐 Shade Prefab 的 kind / Sprite / height 与盾引用，重复执行保留已有映射和高度。全部敌人与 Boss 使用无脚悬浮皮肤，复用 M5 的浮动与攻击提示；缺少皮肤的旧模板保持回落路径。

同目录的 `EnemySkin.shader` 与持久 `EnemySkin.mat` 保留 Sprite 的透明度、乘色与 flipX，额外支持彩图受击混白。Editor 只在原默认材质时接入，已有支持该属性的材质保留；不兼容的自定义材质报错，不静默覆盖。运行时复用共享材质，经每个视图缓存的 MaterialPropertyBlock 更新强度，不实例化每怪材质。

`Emberlight/Art/Build Player Sprite Animations` 调用 `EmberPlayerSpriteSetup.Build`：从工程内 `Player/Source` 的生成原稿离线导出两组独立 PNG 与 Sheet；Move 只取两次相反的落脚姿势。归一过程只等比采样、定位和拼图，不通过整体变换伪造动作。Sheet 由 Sprite Editor 数据接口保存切片及命名身份，单帧 PNG 供外部使用。首次装配按旧玩家有效 alpha 范围保留可见高度和脚底位置，原位更新 Keeper Prefab，先另存 KeeperStatic 供对比；不修改 ArtSet 或正式场景。重复执行保留切片身份、已有 PNG、Clip、Controller 和 Prefab 外观布局。运行帧引用 Sheet PNG，单帧是导出副本，修改单帧不会自动同步 Sheet；日常调整运行画面编辑 Sheet，节奏编辑 Clip，阈值与默认朝向编辑 Prefab。

`EmberKeeperWalkSetup.Build` 用 Sprite Editor 数据接口从九格原图建立命名切片，按有效 alpha 定义每格的 Sprite 范围，保留原 PNG；已有切片保持身份。首次创建 Prefab 与动画，重复运行不覆盖人工修改。拆件大小和关节位置编辑 Prefab，摆幅与关键帧编辑 AnimationClip；禁用自动播放的 Animation 组件供编辑器识别动画资产，运行时由关节视图手动采样，避免暂停时自动推进。位置曲线显式保存固定轴，防止 Unity 合并为 Vector3 后把肩部、髋部装配位置清零；只有显式选择 Reset Keeper Walk Curves 才重新生成默认曲线。

`EmberKeeperWalkExport.Export` 在独立 PreviewScene 中固定相机渲染动画，导出工程内 `Animations/KeeperWalkFrames` 的透明 PNG；它们是可交付素材，当前运行时仍直接使用关节与曲线。视频和八姿势预览保存在忽略目录 `Library/TestRun`，不作为运行依赖。导出不切换或保存当前场景。

### 世界美术

#### 低模 2.5D 独立小样

`Assets/Art/LowPolySample/LowPolyCourtyard.unity` 只保留作独立美术实验，当前不继续向正式游戏迁移。它采用三维地形、真实灯光阴影与 `SkinnedMeshRenderer` 角色，不通过当前二维 `EmberArtSet` 接入正式战斗，也不加入正式 Build Settings。移动与相机默认值统一见需求 §7。

- `EmberLowPolyMeshBuilder` / `EmberLowPolySampleSetup` 仅在 Editor 创建平面法线的几何、单骨权重、Bind Pose、材质和可编辑 Prefab。运行不生成 Mesh、材质或层级。墙柱、岩石、树、火盆和蜡烛为独立 Prefab，庭院实例可直接调整位置和缩放。
- `EmberLowPolyGait.Foot` 给出支撑/摆动轨迹：支撑脚局部后移抵消身体直行位移，摆动段用相接切线与平滑抬落，避免触地切换时速度突跳。`Knee` 用两段骨的长度求前弯膝盖位置；骨长与目标可达范围由当前骨架配置保证。
- `EmberLowPolyKeeper.Initialize` 缓存站姿；`Step(velocity, dt)` 驱动真实骨架、落脚、转身、上身与提灯。腿有独立大腿、小腿和脚，脚踝在摆动后回到地面；没有 root motion、角色贴图翻转或动画事件中的玩法逻辑。当前是程序骨骼动画，没有 Animator Controller 或美术师制作的动作片段。
- `EmberLowPolyPreview` 只负责独立样板：序列化 Keeper / Camera，复用 `EmberInput.KeyboardAxis`，将屏幕方向映射到水平面，执行边界钳制和相机跟随，再把实际速度交给骨架。`Paused` 阻止移动和动作推进。初始化时临时提高预览质量，销毁时恢复原值，不保存项目质量资产。

菜单 `Emberlight/Art/Build Low Poly Prototype` 创建缺失资产，保留已有模型、材质、Prefab 和场景；`Open Low Poly Courtyard` 在正常保存提示后打开小样。批处理入口为 `Emberlight.Editor.EmberLowPolySampleSetup.Build`，仅保存独立样板场景，不保存正式主场景。

`EmberLowPolySampleTests` 检查持久蒙皮资源、骨长、支撑脚位置、触地速度连续性与构建场景隔离；`EmberLowPolyCaptureTests` 在真实 PlayMode 录制整体和角色近景，并检查转向、抬腿、停止、暂停及质量恢复。预览 PNG / MP4 位于 `Library/TestRun`，交付源资产与工具位于 Assets，来源见 [美术样板说明](../reference/hand-painted-art.md)。

#### 手绘场景样板

- `Assets/Art/HandPaintedSample/ArtSet.asset` 保存必需的庭院与可选的玩家、敌人模板。当前两个角色模板仍在 CleanToonSample 目录，玩家绑定两帧 Animator，敌人模板覆盖全部 kind 的无脚皮肤；庭院、UI 和 Buff 展示保留。角色模板留空仍恢复原版，非空时检查对应表现组件，缺少庭院仍报配置错误。
- `Courtyard.prefab` 保存 Tiled 石地、灯柱、碎墙、蜡烛与原有边界装饰。环境不增加碰撞和寻路规则；地面铺到逻辑活动范围之外，避免相机到边界时露空。地图边界和全套菜单没有批量换风格。
- 角色、敌人和装饰 PNG 为独立素材，HUD 图片不包含文字、数值和图标。来源与生成约束见 [手绘样板素材](../reference/hand-painted-art.md)。原有 `wanderer-sheet` 仍提供徽记与弹体等共享素材。
- 粒子与对象池重新租用时恢复子特效，避免隐藏子对象影响后续表现。

`Emberlight/Art/Build Hand Painted Sample` 通过 Unity Sprite Editor 数据接口配置切片、导入设置和持久 Sprite，创建缺失的样板 Prefab 并绑定庭院。重复执行保留角色模板的当前选择（包括留空）、切片身份、已存在的 Prefab 和手动 HUD 布局；修改日常排版或环境位置直接编辑 Prefab。`BuildProject` 是批处理入口，会显式打开并保存主场景；普通 Build 菜单只对当前已打开的主场景标记改动，不切换其他场景。

`EmberArtSampleSetup.UseMainBranchCharacters` 通过 Editor API 清空两个角色模板并保存美术集，再打开正式主场景；不会调用 HUD 配置或更改 Buff 控件。`EmberArtSampleCapture` 在真实 PlayMode 中开局、移动、施加 Buff 并录制普通和密集战况；`EmberBossSwarmSmokeTests` 另布置兵种对照图、阶段与最终 Boss 群战，检查退场和结算。图片与视频输出到 `Library/TestRun`，不作为运行依赖。

导入工具保留源 PNG；手绘 HUD 仅按有效 alpha 定义切片范围和九宫格边框，不裁改图片。Unity 按导入上限决定实际纹理分辨率。石地使用可重复纹理，其余使用 Clamp，均关闭 mipmap。贴图上限与外观默认值见需求 §7。

### UI Prefab 图片持久化

`EmberUiPrefabRepair` 是 Editor 工具。菜单 `Emberlight/UI/Repair UIRoot Images` 修复并保存 `Assets/UI/UIRoot.prefab`；`Repair Selected Images` 修复选中对象，场景对象支持 Undo，需保存场景或 Apply 才持久化实例改动。

工具沿用现有取图入口，将临时 Sprite 克隆为 `Assets/UI/Generated/Sprites/*.asset`，引用原 PNG，保留裁剪、pivot、PPU 和九宫格边框。仅修复已知 UI 节点，保留已有持久资源、布局、颜色和显隐；透明遮罩与点击区域允许空 Sprite。原纹理缺失时停止并提示先烘制程序化素材，不保存临时 Texture，也不改图集导入器或切片 `.meta`。

影响范围：Editor 与 UIRoot 的图片引用。此修复工具不接管运行时数据刷新或按钮回调，也不改变字体闸门。运行入口由 M6 的 EmberMenuUi.Bind 接入同一 Prefab；连接工具创建的补充控件也经图片持久化处理。

IntroRoot 同样使用持久图片：背景、徽标与进度条沿用原取图入口，保存到 Prefab 后停止 Play 仍可预览。EmberIntro 激活视图前仍调用 CreateChinese/ValidateBaked，再为已保存的 TMP 控件赋已校验字体；未绕过静态图集闸门。文案值保持原样，字形覆盖检查决定是否需要重烘。

## 对外契约

| 名称 | 类型 | 输入 | 输出 | 保证 |
|------|------|------|------|------|
| `EmberArt.Flame` / `Ring` / `Panel` / `Glow` / `Fire` | 静态属性/方法 | — | `Sprite` | 优先烘焙 PNG，回退逐像素光栅化 |
| `EmberBakedArt` | 静态类 | — | `Texture2D` | 惰性载入，缺失返回 null |
| `EmberCardFrames` | 静态类 | `EmberRarity` | `Sprite` | 切图 → 手工切分 → `EmberArt.Panel` 三级回退 |
| `EmberCardIcons` | 静态类 | 词条 id | `Sprite` | 24 格图集映射并缓存；`Healing` = cell 23 |
| `EmberUiArt.Piece` / 取图 | 静态 | `Piece` 枚举 | `Sprite` | 6 个归一化 rect 回退 |
| `EmberWorldArt` | 静态类 | 格索引 | `Sprite` | 12 格世界美术；含 `Projectile` 重定向 |
| `EmberVisuals.Disc` / `Shape` / `Character` | 静态 | — | `Sprite` / `SpriteRenderer` | `Character` 目前**仅敌人路径**使用 |
| `EmberEffects` | `MonoBehaviour` | 位置/类型 | — | 容量见需求 §7；仅 Playing 推进模拟，发射由调用方控制 |
| `EmberFonts.CreateChinese()` | 静态 | — | TMP 字体资产 | 预烘静态图集优先，`ValidateBaked` 不通过则回落动态图集；动态回落的源字体缺失或创建失败时仍可返回 null；加载页有对应兜底分支 |
| `EmberKeeperAnimation` | `MonoBehaviour` | 阶段委托、可选模板视图与 Sprite Animator 引用 | 原版、分件或两态序列帧动画 | 无模板时创建原版外观；新路径按实际位移切换两态并记忆水平朝向；暂停与选卡冻结，不改移动根，不反查游戏入口 |
| `EmberCharacterArt` | 静态类 | 命名部件 | 共享持久 Sprite | 惰性加载灰度图集，缺图回落 Disc，不包含角色数值或动画推进 |
| `EmberKeeperRig` | `MonoBehaviour` | `Initialize()`、`Step(velocity, dt)` | 视觉关节姿态 | 调用方只在 Playing 推进；初始化缓存站姿，实际位移驱动循环，零速度回站姿；不写移动根 |
| `EmberLowPolyGait` | 静态类 | 周期/步幅、髋与脚目标/骨长 | 脚局部偏移、膝盖位置 | 纯几何计算，不读场景或输入 |
| `EmberLowPolyKeeper` | `MonoBehaviour` | `Initialize()`、`Step(velocity, dt)` | 三维骨架姿态与角色朝向 | 位移由外部驱动，姿态按实际速度推进；零速度回站姿 |
| `EmberLowPolyPreview` | `MonoBehaviour` | Keeper / Camera 引用、`Step(input, dt)`、`Paused` | 独立小样位移与相机 | 暂停冻结动作与移动；销毁时恢复进入样板前的质量设置 |
| `EmberArtSet` | `ScriptableObject` | 序列化 Prefab 引用 | 只读外观资源 | 庭院必需；角色模板可空，非空时检查表现组件；不存玩法数值 |

## Unity 装配

- 资源根：`Assets/Resources/Art/{UI,Cards,World,Baked}`、`Assets/Resources/Fonts`；当前玩家帧、Sheet、两段 Clip、Controller 与生成原稿在 `Assets/Art/CleanToonSample/Player`，怪物皮肤与盾图在同级 Enemies，角色模板在 Prefabs。第一版手绘样板、拆件图、AnimationClip 和透明行走帧在 `Assets/Art/HandPaintedSample`。
- 4 个 `AssetPostprocessor`（`EmberCardFrameImporter` / `EmberCardIconImporter` / `EmberUiArtImporter` / `EmberWorldArtImporter`）在导入时维持 Sprite / Multiple 设置。**切片通过 Sprite Editor 编辑并保存在 `.meta`；导入器不负责重建切片。**
- Android 压缩覆写位于 `.meta` 平台块（`overridden: 1` + `textureCompression: 0`），涉及三张 doodle 图集。
- `EmberKeeperAnimation` 与 `EmberEffects` 为 `MonoBehaviour`，`EmberArtSet` 为资源资产，其余取图服务为静态类。阶段门控由组合根通过 `Initialize` 注入委托，没有任何 `FindObjectOfType` 反查。

## 影响与回归范围

- **直接影响模块**：M6（UI 精灵）、M5（共享剪影服务）、M4（弹体外观）、M3（粒子反馈）。
- **必须复验的契约**：预烘字体的 `ValidateBaked` 闸门（删掉它 = 资产坏了整个 UI 挂掉）、主 UI 与加载页对已校验字体的绑定、粒子上限、图集三级回退链、`AssetPostprocessor` 切片设置、字形预热覆盖（回落路径）。
- **玩家序列帧契约**：持久 Sprite / Clip / Controller 引用、重新装配身份稳定、帧内真实交替落脚、固定脚底和显示大小、Idle / Move 切换、水平翻转与上下共用、暂停/选卡冻结，以及原有分件与原版回退。主观步态、两帧切换观感与背景可读性需在游戏中判断。
- **怪物皮肤契约**：透明切片完整、全部 kind 映射有效、重复导入身份稳定、池复用恢复 Sprite 与翻转、功能盾和预警仍可读；Boss 无重复冠饰，保留原招式。美术不接管碰撞和数值规则。
- 彩图材质还需运行 `Assets/Tests/PlayMode/EmberEnemyRenderingTests.cs` 的实际像素检查，确认原贴图、透明边缘、闪白与翻转，而不能只断言属性存在或材质引用有效。
