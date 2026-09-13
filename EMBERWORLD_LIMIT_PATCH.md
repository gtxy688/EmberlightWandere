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
