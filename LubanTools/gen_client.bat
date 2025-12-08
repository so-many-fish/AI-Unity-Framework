set WORKSPACE=.

set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll
set CONF_ROOT=%WORKSPACE%\DesignerConfigs

dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-bin ^
    -d bin  ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%WORKSPACE%\..\Assets\Scripts\LubanGenCode ^
    -x outputDataDir=%WORKSPACE%\..\Assets\ConfigAssets\LubanConfig

pause    