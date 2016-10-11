@echo off
setlocal

::WorkDir
pushd %0\..

::begin time
::　msecは２桁しかとれないので０を加えて３桁にする
set hour=%TIME:~0,2%
set min_=%TIME:~3,2%
set sec_=%TIME:~6,2%
set msec=%TIME:~9,2%
set begin=%hour%	%min_%	%sec_%	%msec%0


::
::  [[ chapter_exe    logoframe    join_logo_scp ]]
::
(                 
  "#chapter_exe#"                            ^
                   -v  "#AvsPath#"           ^
                   -o  "#SCPosPath#"         ^
                   -m  100  -s 15
)
if not %ERRORLEVEL% == 0 (
    exit %ERRORLEVEL%
)
echo ERRORLEVEL = %ERRORLEVEL%
echo ----------------------------------------------------------------------
echo .
::  pause
::  exit /b



(
  "#logoframeExe#"                           ^
                          "#AvsPath#"        ^
                   -logo  "#LogoPath#"       ^
                   -oa    "#LogoFrameText#"
)
if not %ERRORLEVEL% == 0 (
    exit %ERRORLEVEL%
)
echo ERRORLEVEL = %ERRORLEVEL%
echo ----------------------------------------------------------------------
echo .
::  pause
::  exit /b



(
  "#join_logo_scp#"                               ^
                     -inlogo  "#LogoFrameText#"   ^
                     -inscp   "#SCPosPath#"       ^
                     -incmd   "#JL_CmdPath#"      ^
                     -o       "#JLS_ResultPath#"
)
echo ERRORLEVEL = %ERRORLEVEL%
echo ----------------------------------------------------------------------
echo .
::  pause
::  exit /b



::end time
set hour=%TIME:~0,2%
set min_=%TIME:~3,2%
set sec_=%TIME:~6,2%
set msec=%TIME:~9,2%
set end=%hour%	%min_%	%sec_%	%msec%0
echo #PartNo#		%begin%		%end%>>"_#TsShortName#_処理時間.sys.txt"


popd
endlocal
::  TIMEOUT /T 5 /NOBREAK
::  pause
::  exit /b



