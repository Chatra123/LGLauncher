
SetMemoryMax(16)
SetWorkingDir("#AvsWorkDir#")


#d2v#  LoadPlugin("..\..\LSystem\DGDecode.dll")
#d2v#  DGDecode_MPEG2Source("#D2vName#")

#lwi#  LoadPlugin("..\..\LSystem\LSMASHSource.dll")
#lwi#  tspath = "#TsPath#"
#lwi#  LWLibavVideoSource(tspath, cache=true, stream_index=-1, threads=0, repeat=true)

AssumeFPS(30000, 1001, true) 



#EnableTrim#  Trim(#BeginFrame#, #EndFrame#)

