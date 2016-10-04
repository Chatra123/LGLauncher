SetMemoryMax(16)
SetWorkingDir("#LWorkDir#")


## d2v
##   DGDecode
#d2v# #DGD#  LoadPlugin("#DGDecode#")
#d2v# #DGD#  DGDecode_MPEG2Source("#D2vName#")

##   MPEG2DecPlus
#d2v# #M2Dp#  LoadPlugin("#MPEG2DecPlus#")
#d2v# #M2Dp#  MPEG2Source("#D2vName#")

## Lwi
#lwi#  LoadPlugin("#LSMASHSource#")
#lwi#  tspath = "#TsPath#"
#lwi#  #LG#   LWLibavVideoSource(tspath, repeat=true)
#lwi#  #JLS#  video = LWLibavVideoSource(tspath, repeat=true)
#lwi#  #JLS#  audio = LWLibavAudioSource(tspath)
#lwi#  #JLS#  AudioDub(video, audio) 


#EnableTrim#  Trim(#BeginFrame#, #EndFrame#)
AssumeFPS(30000, 1001, true) 


