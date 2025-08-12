@echo off
:: 使用完整日期格式 (周四 2025/08/07)
SET week=%DATE:~11,2%
SET year=%DATE:~0,4%
SET month=%DATE:~5,2%
SET day=%DATE:~8,2%

:: 执行Git命令
git add .
git commit -m "%year%/%month%/%day%"
git push origin master

:: 添加确认提示
echo.
echo Commit Finished! Date: %year%-%month%-%day%-%week%
pause
