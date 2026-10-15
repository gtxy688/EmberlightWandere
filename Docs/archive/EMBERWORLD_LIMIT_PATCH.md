> ⚠️ **已完成，仅作历史留存（2026-10-14 归档）**
>
> 本文件描述的改动**已经落地**：`EmberWorld.Limit` 现为可变 `public static float Limit = 22f;`
> （`Assets/Scripts/Combat/EmberWorld.cs:6`（`Limit`）），`LevelConfig.ApplyWorld()` 会把 `MapLimit` 写进去。
> 因此**不要再按本文件操作**。
>
> 另：文中引用的 `write_difficulty_waves_v2.py` **在本仓库中不存在**，无从复现它所说的自动改写。
>
> 保留本文件只为说明「竞技场边界为什么是可变静态字段」这一段历史。
# EmberWorld.Limit patch (required)

Find `EmberWorld.cs` (likely `Assets/Scripts/Combat/EmberWorld.cs`) and change:

```csharp
public const float Limit = 22f;
// or: public static readonly float Limit = 22f;
```

to:

```csharp
public static float Limit = 22f;
```

`LevelConfig.ApplyWorld()` / `BeginRun` assign `EmberWorld.Limit = MapLimit` (default 22).
Arena ±22 value unchanged; only mutability changes.
`write_difficulty_waves_v2.py` patches this automatically when run on Windows.
