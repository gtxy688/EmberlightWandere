> # ⛔ 已归档 · 已被取代 · 请勿实现
> 取代链：v1 → v2 → **现行 v3**。
>
> 现行文档：`../requirements.md` §3、`../architecture/03-combat-waves.md`
> ⚠️ 本文的「Wave6 Boss」「Boss HP 2200」「`Elapsed >= 480`」全部过期；
> 现行局长为 **25 / 50 波**，Boss 由波次驱动。
> 本文提到的 `Docs/Rarity-and-TMP.md` 已并入 `../architecture/02-run-progression-test.md`（且其中幸运权重表本身也已过期）。

---

> **OBSOLETE (v1)** — superseded by `Docs/Difficulty-Waves-v2.md`. Do not implement from this doc.

# 难度与节奏 · 波次制（v1）

> 终稿：xiaoPlanner · 对齐 jack 反馈（过易 / Boss 太慢 / 倾向波次）
> 衔接：`EmberCombat` / `EmberBoss` / `RunProgress` / `EmberRarity` / 升级池

## 目标
- 前期不能「一发黄金」打穿整局；Boss 战要有压力。
- Boss 出现改为波次推进，目标熟练局约 **3.5–5 分钟** 见 Boss（不再锁 480 秒）。
- 先闭环 PC 试玩；安卓实机优先级最后。

## 规则

### 1. 改波次：是
- 局内主推进 = **Wave**，`Elapsed` 只做 HUD/结算展示，**不再**作为 Boss 触发条件。
- 波次表（可调）：

| Wave | 类型 | 配额 | 说明 |
|------|------|------|------|
| 1 | 清杂 | 12 | 仅 kind0 |
| 2 | 清杂 | 18 | 混种起步 |
| 3 | 加压 | 24 | |
| 4 | 加压 | 30 | 开始出现高血种 |
| 5 | 精英压 | 36 | 高密度，为 Boss 铺垫 |
| 6 | Boss | 1 Boss | 出场；可选少量 add（先不做） |

- 清波条件：本波配额刷完且场上敌人 = 0 → 休息 **1.5s** → 下一波。
- 刷怪间隔：按波次表，不再用 `0.9 - Elapsed*0.0018`。
- 杂兵血量改跟 **Wave** 走（替换 `Elapsed * k`），公式初值：
  - 普通：`16 + Wave * 6`
  - 高血种：`40 + Wave * 10`

### 2. Boss 出现条件
- **主条件**：进入 Wave 6（即 Wave 5 清空后）。
- **删除**：`Elapsed >= 480` 触发生成。
- **保留**：`Emberlight/试玩 Boss（运行时）` → `PreviewBoss`（强制 Wave6 等价生成，便于手感）。
- Boss 血量初值：**2200**（原 1800；因见 Boss 更早、叠词条更少，略抬一点保压力）。先不做按已选稀有度动态上浮——等一局试玩再定。

### 3. 稀有度 / 黄金强度怎么压
- **CountBonus** 维持 **1/2/2/3**（已落地）。
- **幅度下调**（百分比类 Magnitude）：

| 稀有度 | 旧 | 新 |
|--------|----|----|
| 青铜 | 30% | **20%** |
| 白银 | 60% | **40%** |
| 黄金 | 90% | **65%** |
| 钻石 | 120% | **90%** |

- **波次权重门控**（Roll 时按当前 Wave，不是固定 50/28/15/7）：

| Wave | 铜/银/金/钻 |
|------|-------------|
| 1–2 | 70 / 25 / 5 / **0** |
| 3–4 | 55 / 30 / 12 / 3 |
| 5+ | 50 / 28 / 15 / 7 |

- 生命上限仍 `100 * mag`（新表 → +20/+40/+65/+90）；治愈维持 20/30/40/50。
- 拾取范围仍 `mag * 2`（随幅度一起变软）。

### 4. 与现有系统衔接
- `RunProgress.Choose` / 升级池 Id 0–9：**不变**；只改 `EmberRarityUtil.Magnitude` + `Roll(wave)`。
- `EmberCombat`：加 `Wave` / `WaveKillsRemaining`（或 spawned/quota）；Boss 分支改挂 Wave6；杂兵缩放改 Wave。
- `EmberBoss` AI / 场地 ±22：**先不动**。
- HUD：显示 `第 N 波`；计时可保留作次要信息。
- `Docs/Rarity-and-TMP.md`：幅度与权重以本篇为准（波次门控优先）。

## 边界
- 不做无尽波；本版到 Wave6 Boss 胜负即一局闭环（Boss 死 = 胜）。
- 不做安卓适配、不做联网。
- 不在本版改升级三选一数量、不改词条池内容。
- Boss 动态血量 / 召唤小怪：本版不做，记为候选项。

## 验收
1. 新开一局：Wave1→…→Wave5 清空后必出 Boss；480 秒前也会出；不再出现「等到 8 分钟才刷」。
2. Wave1–2 升级卡：**不出钻石**；黄金明显少见。
3. 单张黄金不能让 Wave3 前清图无伤感；Boss 战至少需要走位/输出窗口（非 3 秒融化）。
4. `PreviewBoss` 仍可强制刷 Boss。
5. `Emberlight/Validate progression`：Magnitude 新档 + CountBonus 1/2/2/3 断言更新并通过。
6. 熟练局见 Boss 时间落在约 3.5–5 分钟（可 ±1 分钟，按配额再调）。

## 落地分工（建议）
- **xiaoPlanner**：本规格（已写）
- **xiaoCoder**：`EmberRarity` 幅度/按波权重；Validate 断言
- **xiaoDesigner 或 xiaoCoder**：`EmberCombat` 波次状态机 + 去 480 触发 + 杂兵/Boss 血量
- **xiaoUI**：HUD「第 N 波」
- **xiaoLevel**：按新波次表试玩节奏笔记；配额微调建议回抛策划

## 实现备注
- 行为已对齐本规格：Wave1–5 清杂配额（12/18/24/30/36）清空后休息 1.5s 进入下一波；Wave6 刷 Boss。
- 已删除 `Elapsed >= 480` Boss 触发；Boss HP = **2200**；`PreviewBoss` 仍为 Wave6 等价强制生成。
- 新增 `Assets/Scripts/Core/LevelConfig.cs`（MapLimit 22、休息、BossHP、配额、按波刷怪间隔）；`EmberWorld.Limit` 改为可变 static，开局由配置写入。
- 杂兵 HP 跟 Wave：普通 `16+Wave*6`，tank(kind2) `40+Wave*10`；Wave1 仅 kind0。
- 稀有度幅度 0.20/0.40/0.65/0.90；`Roll(random, wave)` 波次权重；HUD 显示「第 N 波」。
