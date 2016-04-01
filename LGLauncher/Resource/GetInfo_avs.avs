
SetMemoryMax(16)

## work dir
SetWorkingDir("#LWorkDir#")


## d2v
#d2v#  LoadPlugin("#DGDecode#")
#d2v#  DGDecode_MPEG2Source("#D2vName#")


## Lwi
#lwi#  LoadPlugin("#LSMASHSource#")
#lwi#  tspath = "#TsPath#"
#lwi#  LWLibavVideoSource(tspath, cache=true, stream_index=-1, threads=0, repeat=true)


## get info
frameCount = String(FrameCount()) 

## write
infoPath   = "#InfoName#"
WriteFileStart(last, infoPath, frameCount, false)
WriteFileStart(last, infoPath, """ "end of text" """,  true)


Trim(0,-1)
