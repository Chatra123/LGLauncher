import os
import sys
import vapoursynth as vs
core = vs.get_core()


## work dir
script_dir = os.path.dirname(__file__)
os.chdir(script_dir)


## d2v
#d2v#  core.std.LoadPlugin(r'#d2vsource#')
#d2v#  d2vname = r'#D2vName#'
#d2v#  d2vname = d2vname.encode('cp932')
#d2v#  clip = core.d2v.Source(d2vname)


## lwi
#lwi#  core.std.LoadPlugin(r'#vslsmashsoruce#')
#lwi#  tspath = r'#TsPath#'
#lwi#  tspath = tspath.encode('cp932')
#lwi#  clip = core.lsmas.LWLibavSource(tspath)


## return
## unlike AviSynth Trim(), the end value of python slicing is not inclusive
#EnableTrim# clip = clip[#BeginFrame#:#EndFrame_plus1#]
clip = core.std.AssumeFPS(clip, fpsnum = 30000, fpsden = 1001)
clip.set_output()

