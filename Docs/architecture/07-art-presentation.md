# M7 美术与表现

> 相关需求：`../requirements.md` §7（技术约束：中文字体雷区）
> 验收文档：`07-art-presentation-test.md`
> 代码：`Assets/Scripts/Visuals/`（9 个文件）+ `Assets/Scripts/Core/EmberFonts.cs` + `Assets/Editor/EmberFontBake.cs` + 4 个资源导入器

## 职责边界

- **本模块负责**：程序化精灵生成与共享、图集取图与缓存、稀有度卡框与词条图标映射、粒子效果池、玩家角色外观与浮动动画、世界美术、以及**中文 TMP 字体资产的构建与预热**。
- **本模块不负责**：
  - 玩法数值与碰撞 → M1 / M3
  - 卡面排版与文案 → M6
  - 敌人剪影与敌人动画 → M5（`EmberEnemySilhouette` / `EmberEnemyVisual`）

**⚠️ 本模块历史上含全项目最高风险的雷区**：中文字体预烘焙。该问题已解决（见下文「中文字体」），但预烘资产**必须留住 `ValidateBaked` 这道运行时闸门**——闸门被删掉就等于把"资产坏了就整个 UI 挂掉"这个性质放回来。

## 依赖

| 被依赖模块 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|------------|------------------|------|--------------|
| M2 | `EmberRarity` 枚举（卡框按稀有度取图）、词条 id（图标映射）、`AllNames`（字形预热） | 取图与预热 | 未知 id 回落占位图 |
| M3 | `EmberGame.State`（是否播放动画的门控）、`Elapsed` | 动画相位与暂停冻结 | 找不到游戏实例则动画照常播放 |
| Resources | `Resources/Art/**`、`Resources/Fonts/**`、`Resources/Art/Baked/**` | 图集与字体 | 逐级回退，最终用程序化占位 |

**反向依赖警告**：`EmberKeeperAnimation` 通过 `Object.FindObjectOfType<EmberGame>()` **反查**游戏实例，`EmberEffects` 也读取 `EmberGame.State`。这是 Visuals → Core 的反向依赖（服务定位器），是本项目已知的边界问题，见 `00-overview.md` §已知边界问题。

## 设计与数据流

```text
图集资源（导入时经 AssetPostprocessor 切片）
   doodle-icons.png        4×6  词条图标      → EmberCardIcons
   doodle-card-frames.png  2×2  稀有度卡框    → EmberCardFrames
   doodle-ui.png           2×3  UI 面板/按钮  → EmberUiArt
   wanderer-sheet.png      4×3  角色与世界    → EmberWorldArt
       ↓ 每个取图入口都是三级链
   切好的子 Sprite（<atlas>_<index>，Sprite Editor / 导入器产出）
       ↓ 缺失或未重新导入
   Sprite.Create 运行时切图（rect 硬编码在 C# 里）
       ↓ 图集整体缺失
   程序化占位（EmberArt.Panel / Flame）

中文字体
   Resources/Fonts/NotoSansCJKsc-Regular SDF.asset（预烘，491 字形 / 2048×2048）
       ↓ ValidateBaked 不通过或资产缺失
   CreateChineseDynamic()：otf → CreateFontAsset → 两段式字形预热

程序化兜底（图集缺失时逐级回退）
   EmberBakedArt  从 Resources/Art/Baked/ 惰性载入预烘焙 PNG
        ↓ 缺失
   EmberArt       运行时逐像素光栅化 128×128（Flame / Ring / Panel / Glow）
        ↓
   EmberVisuals   共享 64×64 Disc 与单色 SpriteRenderer

粒子
   EmberEffects（MonoBehaviour，池上限 120）
      Trail / Impact / Nova；仅在 Mode.Playing 发射
```

**烘焙收益说明**：5 个程序化图元（火焰/圆环/面板/辉光/圆盘）已改为构建期预生成，实测收益很小（合计个位数毫秒，一局一次）。保留它的理由是**形状可以当 PNG 改**、以及消掉首次使用的卡顿，**不是启动时间**。烘焙结束会解码 PNG 与现场光栅化逐像素比对（`maxDelta` 必须为 0），防止改了一处忘了另一处导致美术漂移。

## 关键机制

### ⚠️ 中文字体（历史雷区，已解决）

`EmberFonts.CreateChinese()` **优先使用预烘静态图集**，加载不到或校验不通过时回落到运行时动态图集：

1. `Resources.Load<TMP_FontAsset>("Fonts/NotoSansCJKsc-Regular SDF")`
2. `EmberFonts.ValidateBaked` 逐项校验（图集纹理存在且非空、`characterTable` 非空、`material` 存在、每个 `characterTable` 条目的 `glyphIndex` 都能在 `glyphLookupTable` 里查到）
3. 通过 → 直接用；任一不通过 → `Debug.LogWarning` + 回落 `CreateChineseDynamic()`

**历史：预烘曾失败三次，都让整个 UI 挂掉、游戏进不去，而资产字段校验全绿。**三次的根因已逐条定位并修正：

| 尝试 | 症状 | 根因与修正 |
|------|------|------------|
| 一 | `m_AtlasTextures` 数组从未序列化 → `UnassignedReferenceException`（`GetFallbackMaterial`） | 旧工具先 `CreateAsset(font)` 再 `AddObjectToAsset(atlasTexture)`；`CreateAsset` 序列化的是**当时**的对象图，图集像素还没落盘。修正：**先写图集纹理与材质子资产，最后写字体资产**（`EmberFontBake.Write`） |
| 二 | 资产有 1 个图集槽，但字形指向 atlas index 1 → `IndexOutOfRangeException` | 全程只数 `characterTable.Count`，**从未真正绘制过任何字形**。修正：`VerifyReloaded` 从磁盘重新加载后**真渲染一遍** |
| 三 | `m_AtlasTextures: [{fileID: …}, {fileID: 0}]` + `m_AtlasTextureIndex: 1` → `NullReferenceException`（`GetFallbackMaterial` 读 `atlasTextures[1].name`） | **TMP 开了第二张图集**：2048² 装不下 491 个 90px/9padding 的 CJK 字形，`enableMultiAtlasSupport` 默认 `true`，于是数组里多了一个无纹理的槽，而字体指向它。修正：烘制传 `enableMultiAtlasSupport: false`，并**自动降采样点**直到全部装进一张；`ApplySingleAtlas` 在写盘前强制数组只含一张，有字形指向更高索引就**拒绝写盘** |

**采样点必须是 72px，不是 90px。** 单图集容量实测：

| 采样点 | 烘入字形 | 丢失 |
|--------|----------|------|
| 90 px | 384 / 491 | **丢 107 个** |
| 80 px | 472 / 491 | 丢 19 个 |
| **72 px** | **491 / 491** | **无** |

`FitsOneAtlas` 单独用是不够的：关掉多图集后，装不下的字**根本不会进 `glyphTable`**，所以"没有字形指向 atlas 0 以外"依然成立而字数已经缺了。因此烘制还要求 `CoversEveryCharacter` 通过（逐字符 `HasCharacter`），两者**都**满足才接受该采样点。

现行实现的实测结果：

```text
[EmberFontBake] 90 px -> 384 chars, ... fits=True,  complete=False, dropped 107
[EmberFontBake] 80 px -> 472 chars, ... fits=True,  complete=False, dropped 19
[EmberFontBake] 72 px -> 491 chars, 491 glyphs, 1 atlas texture(s), 0 past atlas 0, fits=True, complete=True
[EmberFontBake] baked 491 glyphs of 665 requested (493 distinct) at 72 px into ONE 2048x2048 atlas
[EmberFontBake] VERIFY OK: reloaded 491 glyphs / 1 atlas texture(s) / max atlasIndex 0; drew 63/64 sampled characters
[EmberFonts] pre-baked atlas validated: 1 atlas texture(s), 491 characters, 491 glyphs, max atlasIndex 0
[EmberFontCheck] PASS: the pre-baked atlas is in use (no runtime bake, drawable on frame 0)
```

> `493 distinct` − 2（`\r`、`\n` 控制字符，TMP 不为其生成字形）= **491**，与烘入数完全一致，无遗漏。

结果与取舍：

- **首屏中文字第 0 帧即可绘制**，加载页不必再 `SetActive(false)` 藏文字。
- 运行时不再 `CreateFontAsset`、不再 `TryAddCharacters`，`PrewarmInBackground` 对静态图集直接 `yield break`。
- **SDF 采样点由 90px 降到 72px**，字形轮廓精度略有下降；显示字号为 11–40px，余量仍充足，但**观感需人工验收**。
- **包体仍带完整 Noto CJK SC（约 16.4 MB）**：静态图集的 `m_SourceFontFile` 仍引用该 otf，裁剪需把字体移出 Resources 并确认引用关系，属未做的优化。
- 新增/修改文案若引入图集里没有的字，**该字会缺失**（静态图集不能追加字形）。改文案后必须重跑 `Emberlight/Bake Chinese font atlas`。
- 重跑命令（批处理）：
  `Unity.exe -batchmode -quit -projectPath <proj> -executeMethod Emberlight.Editor.EmberFontBake.Bake`
  只读自检：`-executeMethod Emberlight.Editor.EmberFontBake.CheckRuntimeLoad`
- ⚠️ 批处理必须**在沙箱外**运行，且**项目不能被编辑器打开**（否则 `HandleProjectAlreadyOpenInAnotherInstance` 直接 crash）。Unity 授权客户端与编辑器通过命名管道通信，沙箱拦截会导致 `exit 199`（`IPC channel to LicensingClient doesn't exist`），此时烘制根本不会执行。

### 字形预热

**静态图集路径下不再需要预热**（字形已在图集里，`PrewarmInBackground` 立即返回）。

回落路径（动态图集）仍保留两段式预热：

- `FirstScreenGlyphSet` 同步加入 —— 加载页文案+主菜单标题的约 98 个不同字形；已验证与 `EmberIntro` 会绘制的字符集完全一致。
- `GlyphSet + CardGlyphSet()` 的余量按 48 字/帧用协程分片加入。

`CardGlyphSet()` 从 `EmberGame.UpgradeNames` / `UpgradeDetails`、`EmberGodsSelectPanel.RosterNames` / `RosterDescs`、`EmberRarityUtil.AllNames` 及 `ExtraCardText` **推导**得出，不手写，避免改文案后失同步。

**改这三处文案必须同步确认预热覆盖**，否则回落路径下首次显示会触发运行时字形烘焙卡顿。

### 字形覆盖（静态图集的真正约束）

静态图集**不能追加字形**，所以"图集里有没有这个字"完全取决于烘制时请求了哪些字符。请求集是 `EmberFonts.AllGlyphs = GlyphSet + CardGlyphSet() + MenuGlyphSet()`。

**这曾经两次漏字，都表现为游戏里的空心方框：**

| 漏的是什么 | 为什么漏 | 现状 |
|------------|----------|------|
| `休闲适合轻松构筑标准体验完整挑战困难面对更多精英` | 难度页文案是 `EmberGame.ChooseLength` **方法体内的内联字面量**，反射与数组遍历都够不到 | 提为 `EmberGame.DifficultyTitle` / `DifficultyBody()` / `DifficultyChoices`，由 `MenuGlyphSet` 收集 |
| `—乐交声弃引律景涌渐潮烛确背虫请遇遭铁队阵静音` | 设置页、敌人图鉴、选卡与结算的文案同样残留在方法体内 | `EmberFonts.ExtraVisibleGlyphs` 显式列出，附来源注释 |
| `_`（U+005F） | **TMP 的内部要求，不是游戏文案**：`TMP_Text.GetSpecialCharacters` 每次赋字体都去字体里找 U+005F（下划线）与 U+2026（省略号），缺一个就每个 `TextMeshProUGUI` 报一条 `The character used for Underline is not available`。U+005F 是 ASCII，看着"显然存在"，而**没有任何文案含它**，所以手写白名单与字面量扫描都抓不到 | `ExtraVisibleGlyphs` 首两项 `\u005f\u2026` |

> 这三类**都不会崩、也不会缺功能**（游戏完全没用 `<u>` `<s>` `<sup>` `<sub>` 富文本标记），但方框是可见缺陷、警告是 Console 噪声，都按缺陷处理。

**判断"是否还有漏字"的唯一可靠手段是 `Tools/atlas_glyph_check.py`**，它不读 C# 常量，而是：

1. 解析 `Library/ScriptAssemblies/Assembly-CSharp.dll` 的 `#US`（user strings）元数据堆，取出**每一个字符串字面量**——包括方法体内的；
2. 解析 `.asset` 里的 `m_Unicode` 条目，得到图集实际持有的字符；
3. 求差集，非空则退出码 1 并打印**可直接粘贴的 C# 转义字面量**。

```bash
python Tools/atlas_glyph_check.py
# assembly literals : 420
# distinct visible  : 489
# atlas holds       : 528
# MISSING (0):
# coverage complete        <- 退出码 0
```

**改任何文案后的流程**：先跑这个脚本 → 有缺口就把它打印的字面量补进 `EmberFonts` 相应的 glyph set → 关掉 Unity → 重跑烘制 → 再跑一次脚本确认 `MISSING (0)`。

> 之所以不能只靠 `GlyphSet` 一处收口：`AllGlyphs` 里的字符来源分散在 6 个数组和十余个常量中，而"某句文案是否已被某个来源覆盖"无法从代码结构判断——只能从编译产物验证。

### 图集与导入设置

- 图集为**离线生成**（内置 image_gen），原图**不做程序像素处理**。
- 运行时按格创建并缓存 `Sprite`，**不读取像素**；关闭 mipmap、保留原比例、双线性过滤。
- 边缘修复：已离线移除四档卡框与 UI 面板外的深色底色，保留内部底色与手绘描边；导入启用 `alphaIsTransparency` 防止边缘采样色晕，**无运行时裁剪或纹理处理**。
- Android 平台覆写为 **Uncompressed**（默认 ETC2 会让描边起块状伪影），写在各自 `.meta` 的 Android 平台块。
- 4 个 `AssetPostprocessor` 导入器负责切片，保证重新导入资源时仍有正确设置。

### 粒子

- `EmberEffects` 池化，**上限 120**（移动端预算）；池空时直接丢弃，不报错。
- 仅在 `Mode.Playing` 发射 → 暂停时战斗特效冻结。
- 新局会清理旧特效世界。

> ⚠️ 历史文档曾写"粒子上限 180"，**实际常量为 120**（`EmberEffects.cs:15`）。

### 玩家角色

- 圆润斗篷造型，斗篷遮住脚；兜帽层次、黑脸亮眼、分层提灯。
- **不再播放图集走路帧**：连续浮动 + 移动倾斜 + 衣摆呼吸 + 提灯摇晃。
- **游戏暂停时停止动画**（读 `EmberGame.State`）。
- 只移动视觉子节点，保留角色坐标与碰撞规则。

### 世界美术

- `wanderer-sheet` 4 列 3 行：前两行为角色正/背面各 4 帧，第三行为开局徽记、医疗挎包、烬蝶、火箭。
- 「正背面 + 镜像」的简化动作，**不是完整八方动画**。
- 粒子与对象池重新租用时恢复子特效，避免隐藏子对象影响后续表现。

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
| `EmberEffects` | `MonoBehaviour` | 位置/类型 | — | 池上限 120；仅 `Playing` 发射 |
| `EmberFonts.CreateChinese()` | 静态 | — | TMP 字体资产 | 预烘静态图集优先，`ValidateBaked` 不通过则回落动态图集；两条路径都保证返回可用字体 |
| `EmberFonts.ApplyFont(Transform, TMP_FontAsset)` | 静态 | 根节点 + 字体 | — | 把字体刷到子树内所有 `TextMeshProUGUI`；`TMP_Settings.defaultFontAsset` 在 TMP 3.0.7 是只读属性，无法用默认字体做注入，编辑器手搭的层级将来靠这个接 |
| `EmberKeeperAnimation` | `MonoBehaviour` | — | — | 暂停时停动画；⚠️ 用 `FindObjectOfType` 反查 |

## Unity 装配

- 资源根：`Assets/Resources/Art/{UI,Cards,World,Baked}`、`Assets/Resources/Fonts`。
- 4 个 `AssetPostprocessor`（`EmberCardFrameImporter` / `EmberCardIconImporter` / `EmberUiArtImporter` / `EmberWorldArtImporter`）在导入时设置切片；**不要手改 `.meta` 的切片，改导入器**。
- Android 压缩覆写位于 `.meta` 平台块（`overridden: 1` + `textureCompression: 0`），涉及三张 doodle 图集。
- `EmberEffects` 与 `EmberKeeperAnimation` 是场景 `MonoBehaviour`；其余为静态类。
- `EmberKeeperAnimation` 用 `FindObjectOfType` 反查游戏实例——**场景中必须有且只有一个 `EmberGame`**。

## 影响与回归范围

- **直接影响模块**：M6（UI 精灵）、M5（共享剪影服务）、M4（弹体外观）、M3（粒子反馈）。
- **必须复验的契约**：预烘字体的 `ValidateBaked` 闸门（删掉它 = 资产坏了整个 UI 挂掉）、`EmberFonts.ApplyFont`、粒子上限、图集三级回退链、`AssetPostprocessor` 切片设置、字形预热覆盖（回落路径）。
- **对应验收项**：`07-art-presentation-test.md`。

## 待裁定事项

| 问题 | 候选方案 | 影响 | 需要谁裁定 |
|------|----------|------|------------|
| 包体 16.4 MB 的 Noto CJK 是否裁剪 | 子集化字体 / 维持完整 | 可省十几 MB，但需保证全部文案字形覆盖 | jack + xiaoCoder |
| `FindObjectOfType<EmberGame>()` 是否改为注入 | 构造注入 / 事件契约 / 维持 | 影响可测性；当前为服务定位器 | xiaoCoder |
| 粒子上限 120 是否随真机实测调整 | Profiler 后调 / 维持 | 影响大波次表现与帧率 | xiaoCoder + jack |
| 是否补完整八方角色动画 | 补图集 / 维持正背面+镜像 | 当前为简化动作，观感取舍 | jack |
