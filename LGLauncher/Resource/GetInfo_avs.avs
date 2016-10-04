SetMemoryMax(16)

## work dir
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
#lwi#  LWLibavVideoSource(tspath, repeat=true)



## get info
frameCount = String(FrameCount()) 

## write
infoPath   = "#InfoName#"
WriteFileStart(last, infoPath, frameCount, false)
WriteFileStart(last, infoPath, """ "end of text" """,  true)


Trim(0,-1)
