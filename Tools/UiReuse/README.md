# UI 复用检查工具

这是可选的开发工具，在一次性工程副本中检查 UI 控件复用、回调与取消选择，并输出截图。正常开发优先使用根目录 [README](../../README.md) 中的测试命令。

1. 将当前工程的 **Assets、Packages、ProjectSettings** 完整复制到一次性目录，保留全部 `.meta`。不能只复制 Scripts/Resources：脚本会加载 `Assets/UI/UIRoot.prefab`，还依赖其 Generated 图片和序列化控件引用。
2. 把本目录 `RuntimeUiChecks.cs` 复制到副本的 `Assets/Editor`，仅在副本中导入运行；无需把它加入正式工程的 Editor 或 Runtime 程序集。
3. 用 Unity 2022.3.62f3 打开副本，等待依赖与资源导入完成，保持空场景后关闭，再执行以下命令。工具自行实例化 UIRoot，避免同时启动主场景中的游戏入口。

```powershell
& "<Unity 安装目录>/Editor/Unity.exe" -batchmode -projectPath "<一次性工程目录>" -executeMethod RuntimeUiChecks.Run -logFile "<日志路径>"
```

保留图形模式，不加 `-nographics` 或 `-quit`：脚本进入 Play Mode 后自行退出。`result.txt` 与 `hud.png` 使用相对路径，调用前将终端工作目录切到副本目录，并检查日志、进程退出码与输出；截图仍需人工查看。本工具不替代完整游戏流程和性能测量，不在项目文档中维护执行历史。
