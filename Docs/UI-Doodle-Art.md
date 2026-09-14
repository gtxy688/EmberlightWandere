# 简笔画 UI 素材

资源：`Assets/Resources/Art/UI/doodle-ui.png`。内置 image_gen 生成并编辑，原图不做程序像素处理。

六件素材：主按钮、次按钮、面板框、标题框、火焰分隔线、提灯装饰。菜单按钮、菜单提灯与选卡页提灯已接入；面板框、标题框与分隔线保留可复用接口。原卡片图标、稀有度、布局及游戏文字保留。中文由 TMP 绘制。

EmberUiArt 按实际归一化矩形建立 Sprite 缓存；按钮使用九宫格伸缩。导入禁用 mipmap、保持原比例与双线性过滤。未覆盖其他 AI 的音效和设置功能。

## 生成提示词

Create a usable 2D mobile game UI ASSET ATLAS in the same simple doodle style as cream-outline fireball, lantern, stopwatch icons. Canvas 1536x1152, EXACT 2 columns x 3 rows, 6 equal 768x384 rectangular cells. Each asset centered, 5% empty margin inside its own cell, never crossing cells. Background uniform solid deep navy #101c28 throughout. Flat fills and simple slightly wobbly rounded cream pen lines, small warm orange accents, very minimal charming cartoon. NO text, NO letters, NO numbers, NO gradients, NO glow, NO realistic materials, NO elaborate ornaments, NO shadows, NO interface screenshot. Cell row1col1: wide rounded rectangular primary button, thick warm orange outline and thin cream inner outline, navy empty center, tiny orange triangular arrow at far LEFT with large blank center for game text. Row1col2: secondary rounded rectangular button, cream simple outline, navy empty interior, tiny cream dot at far left, plenty of blank space. Row2col1: minimalist rounded rectangular panel frame, cream double hand drawn outline, dark navy fill, entirely blank center, tiny orange stitch marks only at corners. Row2col2: title plaque, simple rounded navy capsule framed cream, tiny orange flame at each far end, very large empty center for a heading. Row3col1: horizontal divider, two thin cream lines separated by a tiny central orange flame, ample navy whitespace. Row3col2: small centered charming doodle lantern with orange flame, 2 simple cream curved leafy sprigs on either side, a few tiny orange spark dots, no frame. Keep all outlines clean, flat, legible at phone resolution, matching simple cartoon roguelite game.

## 最终编辑要求

Preserve all six assets and exact layout. Replace background and panel interiors with opaque flat navy #101c28. Remove transparency, glow, gradients, shadows, bevels, textures and grid lines. Use only simple solid cream outlines and orange accents; preserve empty centers for game text.

## 面板透明边缘修复
已离线移除 doodle-ui 图集四种封闭面板描边外的深色底块，保留内部底色、描边、尺寸及切片坐标。启用 alphaIsTransparency 防止边缘采样色晕，无运行时裁剪或纹理处理。
