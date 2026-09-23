# EmberlightWandere 架构课 Resources

> 规则：只收高信任来源。项目自己的文档与代码是**本项目事实**的最高权威；外部资源用来补「一般性知识」。
> 排序原则——先能回答「这个项目为什么这样做」，再能回答「业界一般怎么做」。

## Knowledge

### 一、本项目的一手证据（最高信任）

- **代码本身** —— `Assets/Scripts/`。文档记录的是**意图**，代码是**现状**；两者不符时以代码为准，并把差异指出来。
  用于：任何「它到底怎么跑的」的问题。
- [模块总览](../architecture/00-overview.md) —— 模块边界、依赖图、跨模块契约、任务索引、已知技术债。
  用于：定位一个功能属于哪个模块、改动会波及谁。**本课程所有模块编号（M1–M9）只以这一份为准。**
- [需求文档](../requirements.md) —— 产品行为与**数值唯一权威**。
  用于：一切数值口径；架构文档不复制数值，冲突时以它为准。
- **Git 提交历史**（`git log`）—— 43 次提交，每条都在讲「当时为什么这么改」。
  用于：理解某段代码为什么长这样。例：`528a40f`（波次计数器失控与 Boss 被跳过）解释了 `EmberCombat.StartWave()` 里那道门的位置。

### 二、外部一手资料（Unity 官方）

- [Unity 手册 · Order of execution for event functions](https://docs.unity3d.com/Manual/ExecutionOrder.html)
  生命周期函数的权威顺序表。用于：判断一段代码该放 `Update` / `FixedUpdate` / `LateUpdate`，以及它们之间的先后关系。
- [Unity 手册 · Script execution order](https://docs.unity3d.com/Manual/script-execution-order.html)
  多个脚本之间的执行顺序与 Project Settings 里的排序设置。用于：怀疑「谁先跑」导致的时序 bug 时。
- [Unity Learn](https://learn.unity.com/)
  官方免费课程平台。用于：补 Unity 基础操作，不是架构读物。
- [Unity · Advanced programming and code architecture](https://unity.com/how-to/advanced-programming-and-code-architecture)
  官方关于脚本性能与代码组织的实践建议。用于：性能与结构的取舍判断。

### 三、外部一手资料（架构通识）

- [Game Programming Patterns · Robert Nystrom（全书免费在线）](https://www.gameprogrammingpatterns.com/)
  **本课程最对口的一本外部书**，因为它的目录几乎就是本项目的架构词汇表：
  - [Game Loop](https://www.gameprogrammingpatterns.com/game-loop.html) —— 对应本课的「一个心跳」
  - Update Method —— 对应全项目的 `Tick(dt)`
  - [State](https://www.gameprogrammingpatterns.com/state.html) —— 对应 `EmberGame.Mode` 与那些门
  - Service Locator —— 对应 `FindObjectOfType<EmberGame>()` 为什么是问题
  - Object Pool —— 对应 `EmberPool`
  用于：想知道「这种做法业界叫什么、有什么替代方案」时。每课会给到具体章节。

## Wisdom (Communities)

- [Unity Discussions（官方论坛）](https://discussions.unity.com/)
  官方社区，历史沉淀厚，架构与工程实践类问题能得到有经验者的回答。
  用于：把你在这个项目里形成的判断拿去和更大范围的人对撞，例如「上帝类该怎么拆」。
- [Official Unity Discord](https://discord.com/invite/unity)
  实时问答，适合卡住时快速求解。
  用于：具体 API 或工程配置的即时确认。
- [r/Unity3D](https://www.reddit.com/r/Unity3D/)
  项目展示与经验帖密度高，能看到别人怎么组织同类游戏。
  用于：横向对比——同类 2D 幸存者类项目怎么切模块。

> 用户尚未表示是否愿意参与社区。默认先不推送，需要时再问。

## Gaps

- **项目内没有自动化测试基础设施**（Assets 下 0 个 `.asmdef`），因此「我的理解对不对」无法靠跑测试验证，只能靠读代码 + 在编辑器里动手确认。本课程的所有练习都据此设计为手动可验证。
- **外部链接未经抓取核验**：本工作区的网络抓取被沙箱 DNS 策略拦下（`docs.unity3d.com`、`gameprogrammingpatterns.com` 均返回 non-public IP），上述外部资源只通过搜索结果标题与摘要确认存在，**未逐页读过全文**。第一次引用某一页的具体结论前，应先人工打开核对。
- 缺少一本「如何读陌生代码库」的系统性读物。目前「拼全局」三步法是本课程自研的，尚未与外部方法论对照过。
