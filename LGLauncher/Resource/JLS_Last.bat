@echo off
setlocal

::WorkDir
pushd %0\..


(
  "#join_logo_scp#"                               ^
                     -inlogo  "#LogoFrameText#"   ^
                     -inscp   "#SCPosPath#"       ^
                     -incmd   "#JL_CmdPath#"      ^
                     -o       "#JLS_ResultPath#"
)
echo ERRORLEVEL = %ERRORLEVEL%



endlocal
::  TIMEOUT /T 5 /NOBREAK
::  pause
::  exit /b



