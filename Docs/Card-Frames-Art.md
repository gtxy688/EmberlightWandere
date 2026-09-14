# 简笔画选卡框

内置 image_gen 生成四档选卡贴图，保存于 `Assets/Resources/Art/UI/doodle-card-frames.png`。2×2 图集依次为青铜、白银、黄金、钻石。暖铜斜线、银色月牙、金色星芒、青色菱形提供颜色与形状双重区分。

已接入 EmberUpgradePanel；九宫格伸缩保护圆角，图标与中文独立叠放，内容缩进避让边框。按钮反馈作用于卡框，原有选择回调、音效和词条规则不变。

## 生成提示词

Production UI sprite atlas for a minimal doodle mobile roguelite. Exactly 2 columns by 2 rows of FOUR horizontal card frames, canvas 1536x1024. Each cell 768x512. Each card rectangle occupies x 5%-95%, y 10%-90% within its cell. ALL background and card interiors perfectly flat opaque dark navy #101c28. Style simple hand-drawn cartoon, flat solid strokes, rounded slightly irregular outlines. NO glow NO gradients NO shadows NO textures NO realistic metal NO ornate fantasy. Each card has generous COMPLETELY EMPTY navy center for separate game icons and Chinese text. Thin double outline and small simple corner marks only. Top-left bronze: muted copper orange outline with two short diagonal corner stitches. Top-right silver: pale cool gray cream outline with tiny crescent corner marks. Bottom-left gold: warm golden yellow outline with tiny simple four-ray spark corners. Bottom-right diamond: light cyan outline with small outlined diamond corner shapes. Same exact frame size and aligned edges across all four. Border strokes around 8 pixels, corners rounded about40px. Decoration confined to outer8% of card, 84% central area absolutely empty. No symbols in centers, no text, no numbers, no grid lines. Match minimal cream-outline doodle fireball and lantern game icons.
