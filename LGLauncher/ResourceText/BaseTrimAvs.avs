
SetMemoryMax(16)
SetWorkingDir("#AvsWorkDir#")


LoadPlugin("#InputPlugin#")
#  d2v
#d2v#  DGDecode_MPEG2Source("#D2vName#")

#  Lwi
#lwi#  tspath = "#TsPath#"
#lwi#  #LogoGuillo#     LWLibavVideoSource(tspath, cache=true, stream_index=-1, threads=0, repeat=true)
#lwi#  #Join_Logo_Scp#  video = LWLibavVideoSource(tspath, cache=true, stream_index=-1, threads=0, repeat=true)
#lwi#  #Join_Logo_Scp#  audio = LWLibavAudioSource(tspath, stream_index=1, av_sync=true)
#lwi#  #Join_Logo_Scp#  AudioDub(video, audio) 



#EnableTrim#  Trim(#BeginFrame#, #EndFrame#)

AssumeFPS(30000, 1001, true) 


