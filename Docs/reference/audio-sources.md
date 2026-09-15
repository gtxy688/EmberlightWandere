# 音频素材备选（CC0 / 可商用）

> 2026-09-13 · 打 Android 前可先落地最小集：BGM×2 + SFX 核心包

## 音乐（优先 CC0）
| 用途 | 素材 | 许可 | 链接 |
|------|------|------|------|
| 战斗/紧张 | The Fire Dragon（含 loop） | CC0 | https://opengameart.org/content/the-fire-dragon |
| 探索/行进 | Heat Adventure（可 loop） | CC0 | https://opengameart.org/content/heat-adventure |
| 氛围/营地 | Atmospheric Adventures Freebie | 可商用、可不署名 | https://rosentwig.itch.io/atmospheric-adventures-freebie |
| 火光氛围垫底 | Fire Place | CC0 | https://freemusicarchive.org/music/holiznacc0/winter-lofi/fire-place/ |

建议：主菜单用氛围，局内用 Heat Adventure / Fire Dragon 低音量循环，Boss 切紧张轨。

## 音效（Kenney CC0，优先）
| 用途 | 包 | 链接 |
|------|-----|------|
| UI 点击/确认 | 51 UI SFX | https://opengameart.org/content/51-ui-sound-effects-buttons-switches-and-clicks |
| 界面通用 | Interface Sounds | https://opengameart.org/content/interface-sounds |
| 打击/魔法/拾取 | 50 RPG sound effects | https://opengameart.org/content/50-rpg-sound-effects |
| 额外 RPG | 80 CC0 RPG SFX | https://cc0-sounds.exi.software/collection/80-cc0-rpg-sfx/ |

## 最小接入清单（程序）
1. `EmberAudio`：Music / SFX 两个 AudioSource，Master 音量  
2. SFX：开火、命中、受击、击杀、拾取血包、选卡、刷新、通关/失败  
3. Music：菜单 / 战斗 loop；清波选卡可 duck 音量  
4. 设置页：音乐/音效开关（可后做）  
5. `Docs/reference/audio-credits.md` 留作者链接（虽 CC0 建议留档）