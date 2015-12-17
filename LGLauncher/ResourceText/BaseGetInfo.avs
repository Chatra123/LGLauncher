
SetMemoryMax(16)
SetWorkingDir("#AvsWorkDir#")


LoadPlugin("#InputPlugin#")
#  d2v
#d2v#  DGDecode_MPEG2Source("#D2vName#")


#  Lwi
#lwi#  tspath = "#TsPath#"
#lwi#  LWLibavVideoSource(tspath, cache=true, stream_index=-1, threads=0, repeat=true)

AssumeFPS(30000, 1001, true) 


filename   = "#InfoName#"
frameCount = String(FrameCount()) 
frameRate  = String(FrameRate()) 
totalTime  = String(float(FrameCount()) / FrameRate()) 

WriteFileStart(last, filename, frameCount, false)
WriteFileStart(last, filename, frameRate,  true)
WriteFileStart(last, filename, totalTime,  true)
WriteFileStart(last, filename, """ "end of text" """,  true)

