# M8 音频

> 相关需求：`../requirements.md` §7
> 验收文档：`08-audio-test.md`
> 代码：`Assets/Scripts/Audio/EmberAudio.cs`
> 素材致谢：`../reference/audio-credits.md`、素材备选 `../reference/audio-sources.md`

## 职责边界

- **本模块负责**：BGM/SFX 双总线、音量设置与持久化、全部播放触发点、播放节流防叠音、资源惰性加载与**缺失静默降级**。
- **本模块不负责**：
  - 玩法数值 → M1
  - 3D 空间音、混音器、静音开关 → 明确不做
  - 音量滑条 UI → M6（本模块只暴露 `MusicVolume` / `SfxVolume`）

## 依赖

| 被依赖模块 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|------------|------------------|------|--------------|
| Resources | `Resources.Load("Audio/Music|Sfx/...")` | clip 加载 | **缺失静默跳过**，只警告一次 |
| M3/M4/M5/M6 | 调用方各自触发 `Play*` | 接线点 | — |

**无玩法依赖**：本模块不知道难度、波次或武器数值，只接收"播放某个音"的指令。

## 设计与数据流

```text
EmberAudio（MonoBehaviour 单例，DontDestroyOnLoad）
   ├─ music  AudioSource（loop，spatialBlend = 0 强制 2D）
   └─ sfx    AudioSource（PlayOneShot，spatialBlend = 0）
        ├─ 惰性加载：Resources.Load("Audio/...")；首帧导入未完成时再次尝试
        ├─ 节流：fire/hit 0.05s、meteor 0.02s、orbit 0.25s、burn 0.7~1.5s
        └─ PlayerPrefs：ember_vol_music / ember_vol_sfx（默认 0.7 / 1.0）
```

`Ensure()` 时若场景无 `AudioListener` 则挂到 `Camera.main`。

## 关键机制

### 历史缺陷与修复（不要再退回）

初版有四个导致"完全没声音"的原因，均已修复：

| 原因 | 修复 |
|------|------|
| Resources 下 ogg `.meta` 导入为 `3D: 1` + `preloadAudioData: 0` | 批量改为 `3D: 0`（2D）+ `preloadAudioData: 1` |
| 代码未强制 2D | `music` / `sfx` 的 `spatialBlend` 显式设 0 |
| 场景无 `AudioListener` | `Ensure()` 时挂到 `Camera.main` |
| `clip == null` 时静默 return，无日志 | 加载失败时 `Debug.LogWarning` 一次列出缺失名 |

> ⚠️ 新增 WAV 的**默认导入设置是 `3D: 1` + `preloadAudioData: 0`**，与本模块约定冲突。每次新增音频资源后都要检查导入设置，否则表现为"资源在但听不到"。

### 资源路径约定

```text
Assets/Resources/Audio/
  Music/  menu_ambient.*  combat_loop.*
  Sfx/    ui_click  ui_confirm  card_open  card_pick
          fire  hit  hurt  pickup_heal  meteor_impact
          weapon_fireball  weapon_orbit_ignite  weapon_burn_ground
          weapon_pierce_arrow  weapon_boomerang  weapon_meteor
```

工作区源文件可放在 `Assets/Audio/`；成品需拷到 `Assets/Resources/Audio/`。

武器专属音效有两代实现：

- **v1（AI 生成，TJGenerators / Sonilo SFX）**：24-bit/44.1kHz 立体声，时长 2.01–2.94s。原始 prompt 与接线表保留在归档记录中。
- **v2（原创程序合成）**：44.1kHz / 16-bit / **单声道**，0.19–0.72s，存于 `Assets/Resources/Audio/Sfx/Generated`，生成器随说明保存可重现。**v2 为现行**——短素材更适合高频触发，且不依赖长音效跳头播放。

两者都保留通用 clip 回退（资源缺失时退回 `fire` / `meteor_impact`）。

### 触发点与节流

| 触发点 | 调用 | 节流 |
|--------|------|------|
| 主菜单显示 | `PlayMusic(menu)` | — |
| 进入战斗 / 加载页 | `PlayMusic(combat)` / 营地 BGM | — |
| UI 按钮 / 选卡确认 / 卡面打开 | `PlayUiClick` / `PlayUiConfirm` / `PlayCardOpen` / `PlayCardPick` | — |
| 火球发射 | `PlayWeaponFireball()` | 0.05s |
| 穿透火矢发射（**仅主箭**，分裂箭静音） | `PlayWeaponPierceArrow()` | 0.05s |
| 回旋烬蝶发射 | `PlayWeaponBoomerang()` | 0.05s |
| 环火（**球数变化时**，不是每帧） | `PlayWeaponOrbitIgnite()` | 0.25s |
| 燃地铺火（**仅实际伤害后**） | `PlayWeaponBurnGround()` | 0.7s（v2）/ 1.5s（v1） |
| 火雨**落地瞬间**（预警圈静音） | `PlayMeteorImpact()` | 0.02s |
| 命中 / 受击 / 拾取血包 | `PlayHit` / `PlayHurt` / `PlayPickupHeal` | 0.05s |

**设计要点**（改动时保持）：

- **环火只在球数变化时发声**，否则持续噪音。
- **燃地限频必须短于音效时长**：patch 每约 0.45s 掉落一次，按 patch 播放会严重叠音成糊。
- **穿透分裂箭静音**：`isSplit` 的分裂箭不发声，避免一次射击叠两声。
- **连续接触伤害不叠加通用命中音**：由 `DamageFrom` 的同一伤害实现完成可见性与伤害检查，**生命实际下降才触发**专属声音（`DealContactDamage` 返回是否真扣血）。
- 普通 `PlayOneShot` 音量由 `AudioSource` **一次性设置**，不要重复乘音量。

### 音量

- `MusicVolume` / `SfxVolume`（0–1）set 时立刻写 `AudioSource` 并写 `PlayerPrefs`。
- 默认 0.7 / 1.0；杀进程重进保持。
- 音效滑条拖动时 `PlayUiClick` 预览（限频）。
- **不做**混音器与独立静音开关（音量拖到 0 即可）。

## 对外契约

| 名称 | 类型 | 输入 | 输出 | 保证 |
|------|------|------|------|------|
| `EmberAudio` 单例 | `MonoBehaviour` | — | — | `DontDestroyOnLoad`；`Ensure()` 幂等 |
| `MusicVolume` / `SfxVolume` | 属性（0–1） | 值 | — | set 立即生效并持久化 |
| `PlayMusic(Music)` | 方法 | 曲目 | — | clip 缺失静默 |
| `PlaySfx(...)` / `PlayWeapon*` / `PlayHit` / `PlayHurt` / `PlayPickupHeal` | 方法 | — | — | 内置节流；缺失回退通用 clip |
| `PlayUiClick` / `PlayUiConfirm` / `PlayCardOpen` / `PlayCardPick` | 方法 | — | — | — |
| Prefs 键 | 常量 | — | — | `ember_vol_music` / `ember_vol_sfx` |

## Unity 装配

- `EmberAudio` 是 `MonoBehaviour` 单例，`DontDestroyOnLoad`，随 `EmberGame` 生命周期。
- 两个 `AudioSource` 由代码创建，**不依赖场景预置**。
- 缺 `AudioListener` 时自动挂到 `Camera.main`。
- 资源全部走 `Resources.Load`，**不进 Addressables**；因此音频资源变更只需重新导入，无需改场景。
- 新音频资源的导入设置必须为 `3D: 0` + `preloadAudioData: 1`。

## 影响与回归范围

- **直接影响模块**：M3/M4/M5/M6 的触发点、M6 的设置滑条。
- **必须复验的契约**：导入设置（2D）、缺失回退、节流间隔、`DamageFrom` 驱动的伤害后发声、音量持久化。
- **对应验收项**：`08-audio-test.md`。

## 待裁定事项

| 问题 | 候选方案 | 影响 | 需要谁裁定 |
|------|----------|------|------------|
| v1 长素材是否清理 | 删除 / 保留作回退 | 当前作为缺失回退保留，占用包体 | jack |
| 是否加 pitch 随机化 | 加轻微随机 / 维持一致 | 多次连发音色偏机械 | jack |
| 是否补 3D 空间音 | 做 / 不做 | 明确列为不做；改动面较大 | jack |
