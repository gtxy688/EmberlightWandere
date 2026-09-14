# Audio-Fix + Settings-v1

> 2026-09-14 · jack：音效/音乐未生效；设置页要音乐/音效音量

## 原因排查（策划侧）
1. **导入为 3D 音**：Resources 下 ogg `.meta` 均为 `3D: 1` + `preloadAudioData: 0` → 已批量改为 `3D: 0`、`preloadAudioData: 1`（需 Unity 刷新）
2. **代码未强制 2D**：`EmberAudio` 的 Music/SFX `AudioSource` 未设 `spatialBlend = 0`，未保证存在 `AudioListener`
3. **静默失败**：`clip == null` 时直接 return，Load 失败时玩家听不到、也无日志
4. 接线本身在：菜单 BGM、战斗 BGM、UI/选卡/开火/命中/受伤/回血（部分武器未接开火声，非全无声原因）

## 程序修复（必须）
### A. `EmberAudio` 救活播放
- Bootstrap 时：`music.spatialBlend = 0; sfx.spatialBlend = 0;`（强制 2D）
- `music.ignoreListenerPause = true`（丢焦点暂停时 BGM 可按产品决定；建议 SFX 跟游戏，BGM 仍播或一并停——**默认两者都正常播，不要因 Pause 无声**）
- `Ensure()` 时若场景无 `AudioListener`，挂到 `Camera.main`
- `TryLoadFromResources` 后若仍有 null，`Debug.LogWarning` 一次列出缺失名（正式包可用条件编译）
- `PlayMusic`/`PlaySfx`：若 clip 空则再 `TryLoadFromResources` 一次（防首帧导入未完成）
- 公开属性：`MusicVolume` / `SfxVolume`（0–1），set 时立刻写到 AudioSource，并 `PlayerPrefs`：`ember_vol_music` / `ember_vol_sfx`（默认 0.7 / 1.0）
- `Awake/Bootstrap` 读 Prefs

### B. 设置页（暂停 overlay 扩成简易设置）
当前暂停只有「继续战斗」。改为：

**标题**「设置」或保留「暂息灯塔」  
**内容**：
- 滑条「音乐」0–100% → `EmberAudio.MusicVolume`
- 滑条「音效」0–100% → `EmberAudio.SfxVolume`；拖动时 `PlayUiClick` 预览（限频）
- 按钮「继续战斗」

主菜单可加次要按钮「设置」，打开同一套 UI（可抽 `EmberSettingsPanel` 或复用 Overlay 扩展）。

**布局（竖屏 540×960）**
- 音乐滑条：锚点约 y 0.48–0.56
- 音效滑条：约 y 0.36–0.44
- 左侧标签 TMP，右侧 Slider（Unity UI）
- 继续按钮保持原位置偏下

### C. 验收
1. 进主菜单即有 BGM；点按钮有点击声
2. 开局战斗切 combat loop；开火/命中可闻
3. 暂停调音乐/音效，即时生效；杀进程重进音量保持
4. Console 无大量缺失 clip 警告；有则修路径

### D. 非本次
- 环绕/燃地武器补 PlayFire（可选）
- 混音器 / 静音开关（音量拖到 0 即可）