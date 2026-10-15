# 音频素材来源

本页区分第三方素材、现行合成音效与旧版回退资源。第三方许可标注沿用随素材保留的来源记录，不把整套工程资源统一声明为 CC0。播放优先级见 [M8 音频](../architecture/08-audio.md)。

## Music
- menu_ambient.ogg — Heat Adventure by MintoDog — https://opengameart.org/content/heat-adventure — CC0
- combat_loop.ogg — Heat Battle by MintoDog — https://opengameart.org/content/heat-battle — CC0

## SFX
- ui_click / ui_confirm / card_pick — Kenney Interface Sounds — https://opengameart.org/content/interface-sounds — CC0
- card_open — Kenney 50 RPG sound effects — https://opengameart.org/content/50-rpg-sound-effects — CC0
- meteor_impact.ogg — Kenney Sci-Fi Sounds `lowFrequency_explosion_000` — https://opengameart.org/content/sci-fi-sounds — CC0
- fire / hit / hurt / pickup_heal — Kenney Sci-Fi Sounds — https://opengameart.org/content/sci-fi-sounds — CC0

## 当前武器音效（Generated，优先播放）

`Assets/Resources/Audio/Sfx/Generated` 保存以下六个短 WAV：

- `weapon_fireball.wav`：火球发射。
- `weapon_orbit_ignite.wav`：环火实际接触伤害。
- `weapon_burn_ground.wav`：燃地实际伤害。
- `weapon_pierce_arrow.wav`：穿透主箭发射。
- `weapon_boomerang.wav`：回旋烬蝶发射。
- `weapon_meteor.wav`：火雨落地。

项目原有说明将这组六个音效记为程序合成 v2；当前仓库可交付的是实际 WAV 与 `.meta`，没有保留生成器，不声明可从源码重新合成。这组文件不沿用下列 Kenney/MintoDog 的许可标注。

## 旧版武器音效（缺少 Generated 时回退）
Generated with TJGenerators (Sonilo SFX), 2026-09-14 — one dedicated clip per roster weapon.
原始生成历史位于 `Assets/TJGenerators/History/`；该目录随工程存在时仅作来源资料。此处不将生成服务产物标为 CC0。
- weapon_fireball.wav — 火球 cast/launch
- weapon_orbit_ignite.wav — 环火 ignite
- weapon_burn_ground.wav — 燃地 ground fire catching
- weapon_pierce_arrow.wav — 穿透火矢 bowstring release + flame trail
- weapon_boomerang.wav — 回旋烬蝶 ember butterfly swirl
- weapon_meteor.wav — 天降火雨 meteor impact

第三方重命名成品位于 `Assets/Resources/Audio`，工作区副本位于 `Assets/Audio`。`Assets/Audio/_download/` 为 Git 忽略目录，克隆与交付不依赖本机是否保留原始下载包。
