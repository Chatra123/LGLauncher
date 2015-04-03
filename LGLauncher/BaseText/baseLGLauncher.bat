
@echo off
setlocal
::WorkDir
pushd "#WorkDir#"


::begin time
::@msec‚Í‚QŒ…‚µ‚©‚Æ‚ê‚È‚¢‚Ì‚Å‚O‚ğ‰Á‚¦‚Ä‚RŒ…‚É‚·‚é
set hour=%TIME:~0,2%
set min_=%TIME:~3,2%
set sec_=%TIME:~6,2%
set msec=%TIME:~9,2%
set begin=%hour%	%min_%	%sec_%	%msec%0


::LogoGuillo
set LOGOG_PATH="#LOGOG_PATH#"
set AVS2X_PATH="#AVS2X_PATH#"
set AVSPLG_PATH="#AVSPLG_PATH#"
set VIDEO_PATH="#VIDEO_PATH#"
set LOGO_PATH="#LOGO_PATH#"
set PRM_PATH="#PRM_PATH#"
set OUTPUT_PATH="#OUTPUT_PATH#"
( %LOGOG_PATH% -video %VIDEO_PATH% -lgd %LOGO_PATH% -avs2x %AVS2X_PATH% -avsPlg %AVSPLG_PATH% -prm %PRM_PATH% -out %OUTPUT_PATH% -outFmt keyF -noLog -noChap ) 


::end time
set hour=%TIME:~0,2%
set min_=%TIME:~3,2%
set sec_=%TIME:~6,2%
set msec=%TIME:~9,2%
set end=%hour%	%min_%	%sec_%	%msec%0
echo #PartNo#		%begin%		%end%>>"_#TsShortName#_ˆ—ŠÔ.sys.txt"


endlocal
::pause
exit

