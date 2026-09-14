# 选卡简笔画图标

按 jack 最新反馈保留原有 UI 布局、卡框及稀有度颜色，仅替换图标，并在开局武器库复用对应图标。未接入之前的华丽插画和装饰卡框。

资源：`Assets/Resources/Art/Cards/doodle-icons.png`。4 列 × 6 行，23 个现行词条独立造型；攻速使用第 6 行第 3 列的秒表与速度线图标，最后一格回血图标仍预留。中文文字仍由 TMP 绘制。图标本色不被稀有度染色。

运行时按格创建并缓存 Sprite，不读取像素；关闭 mipmap、保留原比例、双线性过滤。资源在项目内，可随 Android 构建，不依赖生成目录。

生成方式：内置 image_gen；最终采用第二次去除背景光晕的版本。

## 初次生成提示词

Create a production game UI icon sprite sheet, exactly 4 columns by 6 rows, 24 equal square cells, overall portrait 1024x1536. STYLE: extremely simple cute hand-drawn doodles, bold rounded cream outlines, flat solid fills, only 2-3 muted colors per icon, charming slightly irregular pen strokes. Clear silhouettes readable at 64 pixels. NOT ornate, NOT painterly, no realism, no textures, no gradients, no glow, no detailed backgrounds, no card frames, no text, no numbers. Entire background uniform solid deep navy #101c28. Icons centered in each exact grid cell with 18% safe margin, no grid lines. Warm orange flames and pale cream lines, occasional cyan and green accents. Each cell must be a DIFFERENT recognizable pictogram. Row1 left-right: single round fireball with short tail; small lantern encircled by three tiny flames; two bootprints with tiny flames; long arrow with flame arrowhead. Row2: simple orange butterfly with curved return arrow; three falling fire droplets; chunky cyan shield; green four-leaf clover. Row3: short sword; lantern with outward rays; large flame over two small burning patches; sun with simple flame rays. Row4: ring of little flames surrounding empty center (fire sea); forked three-prong fire arrow; two butterflies; large falling comet with impact star. Row5: two fireballs side by side; lantern encircled by five tiny flames; long winding path of fire; arrow through two small shields. Row6: butterfly with two curved return arrows; three tiny meteor impact stars; simple stopwatch with speed marks; small heart with medical plus. Keep same minimal doodle style and stroke thickness across all 24 icons; flat cartoon mobile roguelite, calm clean icons not elaborate fantasy illustrations.

## 最终编辑提示词

Edit this exact 4x6 icon sheet. Preserve all 24 icon subjects, exact positions, size, flat colors and cream outlines. REMOVE ALL background haze, glow, gradients, brown watercolor and lighting. Replace entire background with perfectly uniform solid dark navy RGB(16,28,40) #101c28. All empty space exactly the same flat navy. Icons must be crisp FLAT minimal doodles with zero glow or shadow, solid orange yellow cyan green fills. Do not add anything. Keep exact 4 columns 6 rows layout and portrait 1024x1536.

## 2026-09-14 攻速图标接入

按 Hotfix-Weapon-AtkAs-v1，StatAs 为单武器攻速。复用已生成的秒表简笔画图标（cell 22），保留现有武器名标题与数值说明。仅改图标映射，不改攻速规则或数值。
