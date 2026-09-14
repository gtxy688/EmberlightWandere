# 角色与场景简笔画素材

内置 image_gen 生成图集，保存于 Assets/Resources/Art/World/wanderer-sheet.png。4 列 3 行：前两行是角色正背面各 4 帧；第三行依次开场徽记、医疗挎包、烬蝶、火矢。未替换系统安装图标，仅主菜单和加载页面徽记。

角色按实际移动距离播放步行帧，停止时待机，向上移动切背面，左右移动镜像；只移动视觉子节点，保留角色坐标与碰撞规则。此版为正背面加镜像的简化动作，不是完整八方向动画。

火球使用圆形火弹，环火是脉动火种；穿透使用长箭贴图，烬蝶有振翅与轻微摆动；燃地已有地面火焰、火雨已有预警后爆发保持原有行为。对象池重新租用火种时恢复子特效，避免烬蝶和箭的隐藏子对象影响之后的火球。

血包替换之前医疗爱心，保留掉落、吸附、回血、寿命与闪烁规则。Sprite 使用项目图集，透明边缘导入，不依赖生成目录。

## 原始生成提示词

Production 2D game sprite sheet, clean simple hand-drawn cartoon doodles, bold cream outlines, flat muted blue orange green colors, no glow or shading. Exactly 4 columns by 3 rows equal square cells, square-ish landscape canvas 1536x1152. TRUE TRANSPARENT background everywhere outside sprites, no background tiles, no shadows, no text. All sprites safely inside central75% of cells. Row1: four animation frames of SAME cute blue hooded tiny wanderer carrying orange lantern, front three-quarter view, face dark with two cream eyes, cream outlines, identical proportions and aligned feet baseline: idle, walk left foot forward, passing pose, right foot forward. Row2: same character back view idle, back walk left foot, back passing, back right foot, same size and baseline. Row3: left cell a game emblem of blue hood surrounding orange flame lantern; second cell a sage green healing satchel with cream medical plus and orange clasp; third cell an orange fire butterfly with broad simple wings; fourth cell a slim cream arrow with orange flame tip pointing UP. Ultra simple flat doodle style, readable at tiny game size. No realistic textures, no gradients, no decorative frames.
