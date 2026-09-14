# Android 打包前准备（非美术）

> 美术可并行；下列是逻辑/工程侧收口。

## A. 必须先有（否则真机必炸）
1. **音频最小集**：见 `Audio-Sources.md`，至少 BGM + 核心 SFX  
2. **真机操作**：虚拟摇杆已有；验：多点触控、选卡点按、暂停/返回键（Escape→Android Back）  
3. **竖屏锁定**：Player Settings → Portrait；UI 按 540×960 已对齐  
4. **性能测试**：中端机 30 分钟：卡顿、发热、内存；对照 `Perf-Android-v0` 对象池  
5. **存档路径**：`persistentDataPath` 已用；验杀进程重进、权限（存储一般不需危险权限）  
6. **公司名/包名/版本**：`com.xxx.emberlight`、Version/Bundle Version Code  
7. **IL2CPP + ARM64**：正式包建议 IL2CPP，至少 ARM64（Google Play 要求）  

## B. 强烈建议
8. 首次启动：权限/隐私（若无联网可极简）  
9. 低配档：粒子/同屏敌人上限可调（已有一部分）  
10. 失败恢复：字体加载失败、存档损坏（已有部分）  
11. 一局流程回归清单：开局选 N→选1武器→5 波选卡→Boss→结算  
12. 关闭开发菜单进玩家包（试玩 Boss 仅 Editor）  
13. 日志：正式包关 Debug.Log 刷屏 / Development Build  

## C. 打包检查表（Unity）
- Build Target: Android  
- Scripting Backend: IL2CPP  
- Target Architectures: ARM64（+ ARMv7 若要老机）  
- Min API：建议 24+  
- Orientation: Portrait  
- Internet：若不用可关  
- Keystore：正式签名（别用 debug 上架）  
- 先打 **Development APK** 真机，再打 Release  

## D. 可后置
- 完整局外（已暂停）  
- Enemy-Variety  
- Google Play 上架文案/截图（要美术）  
- 广告/IAP/联网账号  

## 建议顺序
音频接入 → 真机手感/性能 → 包名签名 → Dev APK → 修崩溃 → Release APK