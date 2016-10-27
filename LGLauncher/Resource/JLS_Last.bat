@echo off
setlocal

::WorkDir
pushd "%~dp0"



"#join_logo_scp#"                                ^
                    -inlogo  "#LogoFrameText#"   ^
                    -inscp   "#SCPosPath#"       ^
                    -incmd   "#JL_CmdPath#"      ^
                    -o       "#JLS_ResultPath#"

echo ERRORLEVEL = %ERRORLEVEL%


popd
endlocal
::  timeout /t 5 /nobreak
::  pause
::  exit /b



