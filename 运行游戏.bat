@echo off
echo ========================================
echo 启动 osu! Mania 播放器模式
echo ========================================
echo.
echo 提示：
echo 1. 游戏启动后会直接显示拖拽等待界面
echo 2. 拖拽 .mp3 文件到游戏窗口即可播放
echo 3. 查看控制台中的 [ManiaMode] 日志
echo.
echo ========================================
echo.

dotnet run --project osu.Desktop -c Release

pause

