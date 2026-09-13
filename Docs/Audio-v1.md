# Audio-v1 · Emberlight 最小音频

> 2026-09-13 · 可商用素材见 `Audio-Sources.md`

## 目标
局内/菜单有声；不挡玩法。先最小集，不做混音器花活。

## 规则
- 单例 `EmberAudio`（DontDestroyOnLoad 可选；跟 `EmberGame` 同生命周期也行）
- 两个 `AudioSource`：`music`（loop）/ `sfx`（PlayOneShot）
- 音量：`MusicVolume` `SfxVolume` 默认 0.7 / 1.0；PlayerPrefs 可后做
- 切场景/回菜单时停战斗 BGM，播菜单 BGM
- 选卡弹窗：Music 可 duck 到 0.4（可选，P2）

## 资源路径（约定）
```
Assets/Audio/
  Music/
    menu_ambient.*
    combat_loop.*
  Sfx/
    ui_click.*
    ui_confirm.*
    fire.*
    hit.*
    hurt.*
    pickup_heal.*
    card_open.*
    card_pick.*
```

## 触发点（接线）
| 事件 | 调用 |
|------|------|
| 主菜单显示 | `PlayMusic(menu)` |
| 开局进战斗 | `PlayMusic(combat)` |
| UI 按钮 | `PlaySfx(ui_click)` |
| 选卡确认 | `PlaySfx(ui_confirm)` / `card_pick` |
| 开火（任意武器 Tick 发射） | `PlaySfx(fire)` 限频 ≥0.05s |
| 命中敌人 | `PlaySfx(hit)` 限频 |
| 玩家受击 | `PlaySfx(hurt)` |
| 拾取血包 | `PlaySfx(pickup_heal)` |
| 选卡面板打开 | `PlaySfx(card_open)` |

## 边界
- 缺失 clip 不报错，静默跳过
- 同帧多 hit 用限频，避免爆音
- 暂不接 Boss 专用轨、3D 空间音

## 验收
1. 菜单有 BGM，进战斗切轨且 loop
2. 开火/受击/选卡/血包可听见
3. 缺资源不崩
4. 真机音量正常，切后台系统静音可后验

## 程序落地（Audio-v1）
- `Assets/Scripts/Audio/EmberAudio.cs`：单例 Ensure；Music/SFX；缺 clip 静默；fire/hit 限频 0.05s
- 自动加载：`Resources.Load("Audio/...")` → 请把成品也拷到 `Assets/Resources/Audio/Music|Sfx/`（文件名同文档）
- 工作区仍可用 `Assets/Audio/` 放源文件
- 已接线：菜单 BGM、进战斗 combat、UI 点击、选卡开/确认、开火、命中、受击、拾取血包
