@echo off
setlocal
pushd "%~dp0"


::
::  [[  join_logo_scp  ]]
::

"#join_logo_scp#"                                  ^
                    -inscp   "#chapter_exe_Result#" ^
                    -inlogo  "#LogoFrame_Result#"   ^
                    -incmd   "#JL_CmdPath#"        ^
                    -o       "#JLS_Result#"

echo ERRORLEVEL = %ERRORLEVEL%


popd
endlocal
::  timeout /t 5 /nobreak
::  pause
::  exit /b



