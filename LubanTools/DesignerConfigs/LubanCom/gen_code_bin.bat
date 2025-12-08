set WORKSPACE=..

set OUTPUT_CODE_PATH=%WORKSPACE%\..\Assets\Deer\Scripts\HotFix\HotFixCommon\LubanConfig
set OUTPUT_DATA_PATH=%WORKSPACE%\GenerateDatas\LubanConfig\Datas
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll
set CONF_ROOT=%WORKSPACE%\DesignerConfigs

dotnet %LUBAN_DLL% ^
    -t gf_client ^
    -c cs-bin ^
    -d bin  ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%OUTPUT_CODE_PATH% ^
    -x outputDataDir=%OUTPUT_DATA_PATH%

pause
