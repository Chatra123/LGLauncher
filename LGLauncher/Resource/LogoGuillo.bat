@echo off
setlocal

::WorkDir
pushd %0\..

::begin time
::@msec‚Í‚QŒ…‚µ‚©‚Æ‚ê‚È‚¢‚Ì‚Å‚O‚ğ‰Á‚¦‚Ä‚RŒ…‚É‚·‚é
set hour=%TIME:~0,2%
set min_=%TIME:~3,2%
set sec_=%TIME:~6,2%
set msec=%TIME:~9,2%
set begin=%hour%	%min_%	%sec_%	%msec%0


::
::  [[  LogoGuillo  ]]
::
(
   "#LOGOG_PATH#"                                ^
                   -video     "#VIDEO_PATH#"     ^
                   -lgd       "#LOGO_PATH#"      ^
                   -avs2x     "#AVS2X_PATH#"     ^
                   -avsPlg    "#AVSPLG_PATH#"    ^
                   -prm       "#PRM_PATH#"       ^
                   -out       "#OUTPUT_PATH#"    ^
                   -outFmt    keyF               ^
                   -noLog                        ^
                   -noChap 
)


::end time
set hour=%TIME:~0,2%
set min_=%TIME:~3,2%
set sec_=%TIME:~6,2%
set msec=%TIME:~9,2%
set end=%hour%	%min_%	%sec_%	%msec%0
echo #PartNo#		%begin%		%end%>>"_#TsShortName#_ˆ—ŠÔ.sys.txt"


popd
endlocal
::  TIMEOUT /T 5 /NOBREAK
::  pause
::  exit /b



