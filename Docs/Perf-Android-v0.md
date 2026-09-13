# Perf-Android-v0（早期）

目标：竖屏手机不卡。先不打包 APK。

## 已落地
- `EmberPool`：子弹(`fire`) / 敌人(`shade`/`boss`) / 燃地(`burn`) / 陨石预警(`warn`) 复用
- `EmberEffects` 粒子池保留；同屏粒子上限 120；死亡碎屑 5；Nova 射线 12
- 燃地同屏上限 40
- 敌人上限沿用 `enemies.Count < 150`
- `FindRandomVisibleEnemy` 复用 scratch list，避免每帧 `new List`

## 后续
- 血条/Boss 预警也可入池
- 真机 Profiler 后再调池容量与粒子预算
