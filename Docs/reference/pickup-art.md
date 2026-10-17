# 拾取物美术来源

三件套使用内置 imagegen 生成，按现有烬灯角色的手绘质感和米白描边设计，用户于 2026-10-15 确认第一版。图中从左到右为治疗药瓶、增伤火种护符、疾行风羽。

- 工程原图：`Assets/Resources/Art/Pickups/ember-pickups.png`，RGBA，1942 × 809 像素。
- 保留生成结果的原始像素和真实 alpha；仅用 Unity Sprite Editor 数据接口建立三格切片，没有重新描绘或修改图像像素。
- 三个 Sprite 的身份和切片保存在同目录 `.meta`，一并交付工程。
- 使用内置工具生成，未采用 CLI / API 回退。运行时和打包不依赖图片生成工具。

## Buff HUD 迟缓图标

`Assets/Resources/Art/Buffs/buff-slow.png` 使用内置 imagegen 根据用户选择的 A 方案示意图生成，为带向下箭头的紫色靴子。它仅用于 HUD，不新增世界掉落。透明 PNG 保留生成的原始像素；Unity 导入为单 Sprite，图标框、计时环和数字均由 Prefab 分别提供。火种和风羽直接复用上述持久切片。

迟缓图标生成提示词：

```text
Use case: illustration-story. Production game UI asset, transparent-background cutout. Use the purple boot and downward arrow icon in the provided UI mockup as the design reference. Generate ONLY ONE standalone slowdown debuff icon: a small squat purple leather boot in a simple three-quarter side view, toe pointing left, with a bold purple downward arrow directly beside the boot on the right. Same friendly hand-painted fantasy game aesthetic, strong dark ink outlines and lavender edge highlights. Must read clearly when reduced to 32 pixels. Simple large shapes, restrained detail, no noise, no tiny laces. Center the complete boot-and-arrow silhouette inside a SQUARE canvas with equal modest empty margins, occupy about 80 percent of both width and height. No circular frame, no ring, no UI plate, no text, no numbers, no shadows extending beyond the silhouette, no scenery. Genuinely transparent background. Purple boot and arrow only, no other item icons.
```

## 拾取物生成提示词

```text
Use case: stylized-concept. Asset type: ONE cohesive game pickup icon collection preview sheet, containing exactly three isolated 2D inventory/pickup illustrations arranged left to right in three equally sized cells in a single horizontal row. This is for a Unity top-down survival game named Emberlight Wanderer, featuring a little dark-blue hooded lantern wanderer. Art direction: charming hand-drawn storybook dungeon game items, clean mildly irregular ink silhouettes, thick warm ivory outer contour, restrained dark navy inner contour, simple chunky shapes, flat two-tone shading with a subtle painted texture. Designed to remain recognizable at 32-48 pixels on a dark navy game floor. No pixel art, no realistic rendering, no complex ornament. Left: HEALING, a squat round green-glass potion bottle with a large ivory plus symbol, broad vivid leaf-green liquid mass, simple tan cork, a small dark olive cloth tie. Middle: DAMAGE BOOST, a chunky bronze hexagonal hanging ember talisman, a large bright orange flame with yellow center set in the middle, small upper hanging loop, very simple robust bronze border, no sword. Right: MOVE SPEED, one large turquoise wind feather angled diagonally upward, an ivory central quill, a compact simple feather silhouette with three broad barbs, two short ivory wind strokes hugging its lower side. Match outline weight, apparent size, lighting from upper left, and level of detail across all three. Composition: each item is fully visible and centered within its own equal square cell, occupies about 65 percent of its cell, generous and uniform clear padding, no overlap between items. Each item must be readable by silhouette alone, and healing green must be distinctly different from speed turquoise. Deliver as a wide transparent PNG with genuine alpha transparency. No background color, no checkerboard drawn into the picture, no ground plane or cast shadow, no scene, no characters, no UI panel, no borders around cells, no labels or letters or words, no watermark, no detached sparkles, no broad glow halo.
```
