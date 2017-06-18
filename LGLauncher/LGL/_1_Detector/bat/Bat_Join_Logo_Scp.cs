using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LGLauncher
{
  using OctNov.IO;

  static class Bat_Join_Logo_Scp
  {
    /// <summary>
    /// Join_Logo_Scp起動用バッチ作成
    /// </summary>
    public static string Make_OnRec(string avsPath, string logoPath, string jl_CmdPath)
    {
      if (File.Exists(avsPath) == false)
        throw new LGLException("avs does not exist");
      if (File.Exists(logoPath) == false)
        throw new LGLException("logoPath does not exist");
      if (File.Exists(jl_CmdPath) == false)
        throw new LGLException("jl_cmdPath does not exist");

      //読
      var batText = new List<string>();
      batText = FileR.ReadFromResource("LGLauncher.Resource.JLS_OnRec.bat");

      string chapter_exePath = PathList.Chapter_exe;
      string scPosPath = PathList.WorkName + ".jls.scpos.txt";
      string logoframePath = PathList.LogoFrame;
      string logoFrameText = PathList.WorkName + ".jls.logoframe.txt";
      string join_logo_scpPath = PathList.Join_Logo_Scp;
      string jls_resultPath = PathList.WorkName + ".jls.result.txt";

      for (int i = 0; i < batText.Count; i++)
      {
        var line = batText[i];
        //Part
        line = Regex.Replace(line, "#PartNo#", "" + PathList.PartNo);
        line = Regex.Replace(line, "#TsShortName#", PathList.TsShortName);
        //chapter_exe
        line = Regex.Replace(line, "#chapter_exe#", chapter_exePath);
        line = Regex.Replace(line, "#AvsPath#", avsPath);
        line = Regex.Replace(line, "#SCPosPath#", scPosPath);
        //logoframe
        line = Regex.Replace(line, "#logoframePath#", logoframePath);
        line = Regex.Replace(line, "#LogoPath#", logoPath);
        line = Regex.Replace(line, "#LogoFrameText#", logoFrameText);
        //join_logo_scp
        line = Regex.Replace(line, "#join_logo_scp#", join_logo_scpPath);
        line = Regex.Replace(line, "#JL_CmdPath#", jl_CmdPath);
        line = Regex.Replace(line, "#JLS_ResultPath#", jls_resultPath);
        batText[i] = line;
      }

      //書
      string outBatPath = PathList.WorkPath + ".JLS.bat";
      File.WriteAllLines(outBatPath, batText, TextEnc.Shift_JIS);
      return outBatPath;
    }



    /// <summary>
    /// Join_Logo_Scp起動用バッチ作成  　最終 concat
    /// </summary>
    public static string Make_AtLast(string jl_CmdPath)
    {
      if (File.Exists(jl_CmdPath) == false)
        throw new LGLException("jl_CmdPath does not exist");

      //読
      var batText = new List<string>();
      batText = FileR.ReadFromResource("LGLauncher.Resource.JLS_Last.bat");

      string scpos_catPath =   PathList.TsShortName + ".jls.scpos.cat.txt";
      string logoFrame_catPath = PathList.TsShortName + ".jls.logoframe.cat.txt";
      string join_logo_scpPath = PathList.Join_Logo_Scp;
      string jls_resultPath = PathList.TsShortName + ".jls.last.result.txt";

      for (int i = 0; i < batText.Count; i++)
      {
        var line = batText[i];
        line = Regex.Replace(line, "#SCPosPath#", scpos_catPath);
        line = Regex.Replace(line, "#LogoFrameText#", logoFrame_catPath);
        line = Regex.Replace(line, "#join_logo_scp#", join_logo_scpPath);
        line = Regex.Replace(line, "#JL_CmdPath#", jl_CmdPath);
        line = Regex.Replace(line, "#JLS_ResultPath#", jls_resultPath);
        batText[i] = line;
      }

      //書
      string outBatPath = Path.Combine(PathList.LWorkDir,
                                       PathList.TsShortName + ".JLS.last.bat");
      File.WriteAllLines(outBatPath, batText, TextEnc.Shift_JIS);
      return outBatPath;
    }

  }
}