# Weapon-Sfx-v1 · 六把武器专属音效（2026-09-14）

> jack：查看当前 6 把武器的音效，生成更贴合的音效

## 背景
原 Audio-v1 只有 3 个「通用」战斗音：`fire` / `hit` / `meteor_impact`（Kenney Sci-Fi Sounds，科幻味）。
火球、烬蝶、穿透火矢三把武器共用同一个 `fire`，环火与燃地**完全没有音效**，听感上无法区分武器。

## 名册（6 把，`RunProgress.RosterIds`）
| 武器 | Id | 原音效 | 问题 |
|------|----|--------|------|
| 火球 | `WeaponBasic` 13 | `fire` | 与其他武器共用 |
| 环火 | `WeaponOrbit` 14 | **无** | 只能听到移动时的环境音 |
| 燃地 | `WeaponTrail` 15 | **无** | 放置燃烧地面完全静音 |
| 穿透火矢 | `WeaponPierce` 10 | `fire` | 与其他武器共用，缺少蓄势/箭矢感 |
| 回旋烬蝶 | `WeaponBoom` 11 | `fire` | 与其他武器共用 |
| 天降火雨 | `WeaponMeteor` 12 | `meteor_impact` | 音源是科幻爆炸，非奇幻陨石 |

## 方案：一武器一音效
用 TJGenerators（Sonilo SFX）生成 6 个**单一音源、单次事件**的奇幻火焰音，24-bit/44.1kHz 立体声 WAV，
落到 `Assets/Resources/Audio/Sfx/`，命名 `weapon_*.wav`（`Resources.Load("Audio/Sfx/...")` 约定不变）。

| 武器 | 资源 | 时长 | 生成 prompt 要点 |
|------|------|------|------------------|
| 火球 | `weapon_fireball.wav` | 2.01s | 温暖奇幻火球施放，轻柔气声 + 火焰噼啪 + 低频余烬 |
| 环火 | `weapon_orbit_ignite.wav` | 2.01s | 稳定的火焰点燃，近距离小火把，柔和噼啪 |
| 燃地 | `weapon_burn_ground.wav` | 2.94s | 火焰沿干土/草地蔓延，篝火式爆燃，低沉温暖 |
| 穿透火矢 | `weapon_pierce_arrow.wav` | 2.01s | 火焰箭射出，绷紧弓弦释放 + 高速火焰尾迹|
| 回旋烬蝶 | `weapon_boomerang.wav` | 2.01s | 余烬蝴蝶旋转飞过，柔和火焰旋流 + 轻魔法闪光 |
| 天降火雨 | `weapon_meteor.wav` | 2.94s | 陨石砸地，深沉低频撞击 + 火焰爆发 + 温暖余震 |

全部 prompt 均带 `no voice` / `no music` 且禁止金属与科幻电子音，保证与既有 BGM（Heat Adventure / Heat Battle）同风格。

## 接线
`EmberAudio` 新增 6 个 `[SerializeField] AudioClip` + 6 个播放方法，全部**带通用 clip 回退**（资源缺失时退回 `fire` / `meteor_impact`），并加限频防叠音：

| 触发点 | 调用 | 限频 |
|--------|------|------|
| `EmberBasicShotWeapon.Fire` | `PlayWeaponFireball()` | 0.05s |
| `EmberOrbitWeapon`（球数变化时，即首次拥有/+1 球） | `PlayWeaponOrbitIgnite()` | 0.25s |
| `EmberTrailWeapon.SpawnPatch`（含火海 ring 每 0.35s 落点） | `PlayWeaponBurnGround()` | **1.5s** |
| `EmberPierceWeapon.Fire`（仅主箭，非分裂箭） | `PlayWeaponPierceArrow()` | 0.05s |
| `EmberBoomerangWeapon.Fire` | `PlayWeaponBoomerang()` | 0.05s |
| `EmberMeteorWeapon`（预警落地瞬间） | `PlayMeteorImpact()` → 优先 `weapon_meteor` | 0.02s |

设计要点：
- **环火只在球数变化时发声**，不是每帧；否则会持续噪音。
- **燃地限频 1.5s 而音效 2.94s**：patch 每 ~0.45s 掉落一次，若按 patch 播放会严重叠音成糊。
- **穿透分裂箭静音**：`isSplit` 的分裂箭不发声，避免一次射击叠加两声。
- 陨石预警圈仍然静音，只有**落地瞬间**发声（沿用原设计）。

## 导入设置
新 WAV 默认导入为 `3D: 1` + `preloadAudioData: 0`，与 `Audio-Fix-Settings-v1.md` 的约定冲突，
已统一改为 `3D: 0`（2D）+ `preloadAudioData: 1`，与既有 SFX 一致。

## 验收（已在 Play Mode 实测）
1. 6 个 clip 均能 `Resources.Load` 到，时长/声道/采样率正确（2.01/2.01/2.94/2.01/2.01/2.94s，44.1kHz 立体声）。
2. 单独探针：每个 `Play*` 方法都能让 `sfx` AudioSource 进入 `isPlaying`。
3. **隔离测试**（把其余所有 clip 置空，只留目标 clip）：6 把武器各自的 Tick 仍能发声 →
   证明武器确实走的是**自己那条** clip，而不是碰巧回退到通用音。
4. 每把武器真实 Tick 均能产出伤害/弹体（火球 132、穿透 150、烬蝶 108、火雨 90），说明接线点确实被执行。
5. 燃地逐帧调用 3s → 只播 1 次（限频生效，无叠音）。
6. 把 `fire` 之外置空后 `PlayWeaponFireball()` 仍能出声 → 通用回退路径有效。

## 边界 / 未做
- 未做 3D 空间音：仍走 `EmberAudio` 的 2D SFX 总线（沿用 Audio-v1 架构）。
- 未加音量随机化/pitch 抖动，多次连发时音色完全一致。
- 未改 BGM、命中(`hit`)、受击(`hurt`)、UI 音。
