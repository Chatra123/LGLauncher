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



## get info
num_frames = clip.num_frames
num_frames = str(num_frames) + "\n"
lines = [num_frames, "end of text"]

## write
infoPath = r'#InfoName#'
f = open(infoPath, 'w')
f.writelines(lines)
f.close()


## Return
clip = clip[0:1]
clip.set_output()

