@echo off
setlocal

::WorkDir
pushd "%~dp0"

::begin time
::@msec‚Í‚QŒ…‚µ‚©‚Æ‚ê‚È‚¢‚Ì‚Å‚O‚ğ‰Á‚¦‚Ä‚RŒ…‚É‚·‚é
set hour=%TIME:~0,2%
set min_=%TIME:~3,2%
set sec_=%TIME:~6,2%
set msec=%TIME:~9,2%
set begin=%hour%	%min_%	%sec_%	%msec%0


::
::  [[ chapter_exe    logoframe    join_logo_scp ]]
::
         
"#chapter_exe#"                             ^
                  -v  "#AvsPath#"           ^
                  -o  "#SCPosPath#"         ^
                  -m  100  -s 15

if not %ERRORLEVEL% == 0 (
    exit %ERRORLEVEL%
)
echo ERRORLEVEL = %ERRORLEVEL%
echo ----------------------------------------------------------------------
echo .
::  pause
::  exit /b




"#logoframePath#"                           ^
                         "#AvsPath#"        ^
                  -logo  "#LogoPath#"       ^
                  -oa    "#LogoFrameText#"

if not %ERRORLEVEL% == 0 (
    exit %ERRORLEVEL%
)
echo ERRORLEVEL = %ERRORLEVEL%
echo ----------------------------------------------------------------------
echo .
::  pause
::  exit /b




"#join_logo_scp#"                                ^
                    -inlogo  "#LogoFrameText#"   ^
                    -inscp   "#SCPosPath#"       ^
                    -incmd   "#JL_CmdPath#"      ^
                    -o       "#JLS_ResultPath#"

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
echo #PartNo#		%begin%		%end%>>"_#TsShortName#_ˆ—ŠÔ.sys.txt"


popd
endlocal
::  timeout /t 5 /nobreak
::  pause
::  exit /b



