> # ⛔ 已归档 · 已被取代 · 请勿实现
> 取代链：v1 → v2 → **现行 v3**。
>
> 现行文档：`../requirements.md` §3、`../architecture/03-combat-waves.md`
> ⚠️ 本文的「6 波 / Wave6 Boss / Boss HP 2200 / 5 张卡」全部过期；现行局长为 **25 / 50 波**。

---

> 已被 Difficulty-Waves-v3.md 取代：现行局长为 25/50 波，难度独立选择。以下为历史规则。

# 难度与节奏 · 波次制（v2）

> 终稿：xiaoPlanner · v2 取代 v1（清波强制三选一；移除 XP）
> 衔接：`EmberCombat` / `EmberGame` / `RunProgress` / `EmberRarity` / `LevelConfig` / HUD
> **v1 已作废**：见 `Difficulty-Waves-v1.md` 顶部标记。

## 目标
- 升级入口只来自 **WaveClear**（Wave1–5 各一次，最多 5 张卡）；Boss 前不叠 XP 等级。
- 余烬（Ember）只加 **Score**，不驱动升级。
- 清波后立刻出卡，**无** 1.5s 自动休息。

## 规则

### 1. 清波 → 强制三选一
- Clear = 本波配额已刷完（`waveSpawned >= quota`）**且** 场上敌人 == 0。
- 立刻 `Mode.Upgrade`；`OfferChoices(3)`；`Choose` 后 `AdvanceAfterUpgrade()` → 下一波。
- Wave6 = Boss（清 Wave5 选卡后进入）；无 Elapsed>=480。
- 保留 `PreviewBoss`。

### 2. 移除 XP 进度
- 删除 `Level` / `Experience` / `Required` / `AddExperience` 及 XP 条驱动的 Pending。
- 升级链路唯一：`WaveClear → OfferChoices(3) → Choose → NextWave`。
- `Pending` 可临时 = 1 供 Offer/Choose 门控。
- `EmberGame.Update` **不再** `if (Pending > 0) OfferUpgrade()`。

### 3. 余烬 Score
- 掉落/吸附手感保留；`AddScore(value)`。
- Id5 PickupBonus：拾取半径和/或分数效率（非战斗）。

### 4. 波次表（同 v1 数值）
| Wave | 配额 | 说明 |
|------|------|------|
| 1 | 12 | 仅 kind0 |
| 2 | 18 | |
| 3 | 24 | |
| 4 | 30 | |
| 5 | 36 | |
| 6 | Boss HP 2200 | |

杂兵 HP：普通 `16+Wave*6`；tank(kind2) `40+Wave*10`。

### 5. 稀有度
- Magnitude **0.20 / 0.40 / 0.65 / 0.90**
- CountBonus **1 / 2 / 2 / 3**
- `Roll(random, waveCleared)`：W1–2 `70/25/5/0`；W3–4 `55/30/12/3`；W5 `50/28/15/7`

### 6. LevelConfig
- 解耦：MapLimit、配额、刷怪间隔、BossHp（无 WaveRestSeconds）。

### 7. HUD
- 去掉 XP 条；显示「第 N 波」+ 波次剩余/进度；可选 Score。中文用 `\u` 转义。

## 关键接线
- `EmberCombat`：Wave1–5 clear → `game.OnWaveCleared(wave)`；不自动进波。
- `AdvanceAfterUpgrade()` → `StartWave(wave+1)`（6 刷 Boss）。
- `EmberGame.OnWaveCleared`：`Pending=1`，`OfferUpgrade` + `Choices(random, clearedWave)`。
- `SelectUpgrade`：`Choose` → `combat.AdvanceAfterUpgrade()` → `SetPlaying`。

## 验收
1. Wave1–5 每次清波必出三选一；选后才下一波；最多 5 张卡后 Boss。
2. 拾取余烬不加 Pending / 不出卡。
3. 无 XP 条；HUD 有波次进度与可选 Score。
4. Wave1–2 不出钻石；Magnitude / CountBonus / 配额 / BossHp 断言通过。
5. `PreviewBoss` 仍可用；无 480 秒触发。

## 实现备注
- 已落地：Clear = spawned>=quota && enemies==0 → 立即 Upgrade；无 1.5s rest。
- XP API 已删；余烬 `AddScore`；Id5 半径 + 轻度分数效率。
- `LevelConfig` 配额 12/18/24/30/36、BossHp 2200、MapLimit 22；无 WaveRestSeconds。
- Validate：Pending=1 门控、Score、配额、BossHp、反射确认无 Level/Experience/Required/AddExperience。
- HUD：波次条替代 XP 条；结算含余烬 Score。

## 实现备注（无死亡掉落）
- **死亡掉落已删除**：击杀直接 AddScore（小怪 1 / Boss 30），不再生成小余烬拾取物；PickupBonus 停用。
