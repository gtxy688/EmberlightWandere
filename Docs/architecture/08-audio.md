# M8 音频

> 相关需求：`../requirements.md` §7
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
| Resources | `Resources.Load("Audio/...")` | clip 加载 | **缺失静默跳过**，只警告一次 |
| M3/M4/M5/M6 | 调用方各自触发 `Play*` | 接线点 | — |

**无玩法依赖**：本模块不知道难度、波次或武器数值，只接收播放指令。生命反馈由组合根按结算结果调用；武器只报告 `EmberWeaponSound`，由 M3 适配，不创建音频单例。

## 设计与数据流

```text
EmberAudio（MonoBehaviour 单例，DontDestroyOnLoad）
   ├─ music  AudioSource（loop，spatialBlend = 0 强制 2D）
   └─ sfx    AudioSource（PlayOneShot，spatialBlend = 0）
        ├─ 惰性加载：Resources.Load("Audio/...")；首帧导入未完成时再次尝试
        ├─ 节流：各发射/命中音、火雨落地、环火接触与燃地伤害分开计时
        └─ PlayerPrefs：ember_vol_music / ember_vol_sfx（已保存设置优先）
```

`Ensure()` 时若场景无 `AudioListener` 则挂到 `Camera.main`。

## 关键机制

### 资源与听音前提

两个 AudioSource 均强制 2D；`Ensure` 补齐监听器，资源缺失时警告并跳过播放。音频导入保持 2D 与预载，新增素材后同时检查导入设置、资源路径和实际听音。

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

当前武器音效优先加载 `Assets/Resources/Audio/Sfx/Generated` 中的短 WAV；缺失时回退 `Sfx` 根目录的同名武器音，再回退通用发射或命中音。具体素材清单与来源见 `../reference/audio-credits.md`。现仓库保留成品 WAV，没有合成生成器，不提供重新合成步骤。

### 触发点与节流

间隔与初始音量统一见 [需求 §7「UI 与音频默认参数」](../requirements.md#ui-与音频默认参数)。下表只描述调用关系与计时器归属，计时均使用 `Time.unscaledTime`。

| 触发点 | 调用 | 节流参数项 |
|--------|------|------|
| 主菜单显示 | `PlayMusic(menu)` | — |
| 进入战斗 / 加载页 | `PlayMusic(combat)` / 营地 BGM | — |
| UI 按钮 / 选卡确认 / 卡面打开 | `PlayUiClick` / `PlayUiConfirm` / `PlayCardOpen` / `PlayCardPick` | — |
| 火球发射 | `PlayWeaponFireball()` | 常规发射与命中音；独立计时器 |
| 穿透火矢发射（**仅主箭**，分裂箭静音） | `PlayWeaponPierceArrow()` | 常规发射与命中音；独立计时器 |
| 回旋烬蝶发射 | `PlayWeaponBoomerang()` | 常规发射与命中音；独立计时器 |
| 环火接触（**仅实际伤害后**） | `PlayWeaponOrbitContact()` | 环火接触音；共用环火计时器 |
| 燃地（**仅实际伤害后**） | `PlayWeaponBurnGround()` | 燃地伤害音；重叠火区共用计时器 |
| 火雨**落地瞬间**（预警圈静音） | `PlayMeteorImpact()` | 火雨落地音；多个落点共用计时器 |
| 通用发射 / 命中 | `PlayFire` / `PlayHit` | 常规发射与命中音；各自独立计时器 |
| 受击 / 拾取血包 | `PlayHurt` / `PlayPickupHeal` | 无独立音频节流 |

**设计要点**（改动时保持）：

- **环火与燃地只在实际造成伤害后发声**，各自共用计时器，避免多个接触目标或重叠火区同时叠音。
- **穿透分裂箭静音**：`isSplit` 的分裂箭不发声，避免一次射击叠两声。
- **连续接触伤害不叠加通用命中音**：由 `DamageFrom` 的同一伤害实现完成可见性与伤害检查，**生命实际下降才触发**专属声音（`DealContactDamage` 返回是否真扣血）。
- 普通 `PlayOneShot` 音量由 `AudioSource` **一次性设置**，不要重复乘音量。

### 音量

- `MusicVolume` / `SfxVolume`（0–1）set 时立刻写 `AudioSource` 并写 `PlayerPrefs`。
- 未保存设置时使用需求文档中的初始音量；已有设置在重启后恢复。
- 音效滑条拖动时调用 `PlayUiClick` 预览，预览节流由 M6 的 `EmberSettingsPanel` 负责；`PlayUiClick` 本身没有独立计时器。
- **不做**混音器与独立静音开关（音量拖到 0 即可）。

## 对外契约

| 名称 | 类型 | 输入 | 输出 | 保证 |
|------|------|------|------|------|
| `EmberAudio` 单例 | `MonoBehaviour` | — | — | `DontDestroyOnLoad`；`Ensure()` 幂等 |
| `MusicVolume` / `SfxVolume` | 属性（0–1） | 值 | — | set 立即生效并持久化 |
| `PlayMusic(Music)` | 方法 | 曲目 | — | clip 缺失静默 |
| `PlaySfx(...)` / `PlayWeapon*` / `PlayHit` / `PlayHurt` / `PlayPickupHeal` | 方法 | — | — | 节流按上表执行；武器专属 clip 缺失时尝试通用回退，最终缺失则跳过 |
| `PlayUiClick` / `PlayUiConfirm` / `PlayCardOpen` / `PlayCardPick` | 方法 | — | — | — |
| Prefs 键 | 常量 | — | — | `ember_vol_music` / `ember_vol_sfx` |

## Unity 装配

- `EmberAudio` 是 `MonoBehaviour` 单例，`Ensure` 惰性创建，使用 `DontDestroyOnLoad`；不随单局或组合根销毁自动回收。
- 两个 `AudioSource` 由代码创建，**不依赖场景预置**。
- 缺 `AudioListener` 时自动挂到 `Camera.main`。
- 资源全部走 `Resources.Load`，**不进 Addressables**；因此音频资源变更只需重新导入，无需改场景。
- 新音频资源的导入设置必须为 `3D: 0` + `preloadAudioData: 1`。

## 影响与回归范围

- **直接影响模块**：M3/M4/M5/M6 的触发点、M6 的设置滑条。
- **必须复验的契约**：导入设置（2D）、缺失回退、节流间隔、`DamageFrom` 驱动的伤害后发声、音量持久化。
