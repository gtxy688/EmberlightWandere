# UI reuse check

Copy the current Assets/Scripts, Assets/Resources and Assets/TextMesh Pro into a disposable Unity 2022.3.62f3 project with uGUI and TextMeshPro packages. Copy RuntimeUiChecks.cs into its Assets/Editor folder. Do not add it to the production build.

Run Unity with `-batchmode -projectPath <disposable-project> -executeMethod RuntimeUiChecks.Run -logFile <log>`. Do not pass -quit: the script enters Play Mode and exits after checks. Read result.txt and inspect hud.png. This does not replace a full gameplay run or Android profiling.
