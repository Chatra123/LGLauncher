using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LGLauncher
{
  using OctNov.IO;

  internal static class Bat_Join_Logo_Scp
  {
    /// <summary>
    /// Join_Logo_Scp起動用バッチ作成
    /// </summary>
    public static string Make_OnRec(string avsPath, string logoPath, string jl_CmdPath)
    {
      if (File.Exists(avsPath) == false)
        throw new LGLException("format avs does not exist");
      if (File.Exists(logoPath) == false)
        throw new LGLException("logoPath does not exist");
      if (File.Exists(jl_CmdPath) == false)
        throw new LGLException("jl_cmdPath does not exist");

      //読
      var batText = new List<string>();
      batText = FileR.ReadFromResource("LGLauncher.Resource.JLS_OnRec.bat");

      //chapter_exe
      string chapter_exePath = PathList.Chapter_exe;
      string scPosPath = PathList.WorkName + ".jls.scpos.txt";
      //logoframe
      string logoframeExe = PathList.LogoFrame;
      string logoFrameText = PathList.WorkName + ".jls.logoframe.txt";
      //join_logo_scp
      string join_logo_scpPath = PathList.Join_Logo_Scp;
      string jls_resultPath = PathList.WorkName + ".jls.result.txt";

      //置換
      for (int i = 0; i < batText.Count; i++)
      {
        var line = batText[i];
        //Part
        line = Regex.Replace(line, "#PartNo#", "" + PathList.PartNo, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#TsShortName#", PathList.TsShortName, RegexOptions.IgnoreCase);
        //chapter_exe
        line = Regex.Replace(line, "#chapter_exe#", chapter_exePath, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#AvsPath#", avsPath, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#SCPosPath#", scPosPath, RegexOptions.IgnoreCase);
        //logoframe
        line = Regex.Replace(line, "#logoframeExe#", logoframeExe, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#LogoPath#", logoPath, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#LogoFrameText#", logoFrameText, RegexOptions.IgnoreCase);
        //join_logo_scp
        line = Regex.Replace(line, "#join_logo_scp#", join_logo_scpPath, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#JL_CmdPath#", jl_CmdPath, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#JLS_ResultPath#", jls_resultPath, RegexOptions.IgnoreCase);
        batText[i] = line;
      }

      //書
      string outBatPath = PathList.WorkPath + ".JLS.bat";
      File.WriteAllLines(outBatPath, batText, TextEnc.Shift_JIS);
      return outBatPath;
    }



    /// <summary>
    /// Join_Logo_Scp起動用バッチ作成  　最終
    /// </summary>
    public static string Make_AtLast(string jl_CmdPath)
    {
      if (File.Exists(jl_CmdPath) == false)
        throw new LGLException("jl_CmdPath does not exist");

      //読
      var batText = new List<string>();
      batText = FileR.ReadFromResource("LGLauncher.Resource.JLS_Last.bat");

      //scpos
      string scpos_catPath =   PathList.TsShortName + ".jls.scpos.cat.txt";
      //logoframe
      string logoFrame_catPath = PathList.TsShortName + ".jls.logoframe.cat.txt";
      //join_logo_scp
      string join_logo_scpPath = PathList.Join_Logo_Scp;
      string jls_resultPath = PathList.TsShortName + ".jls.lastcat.result.txt";

      //置換
      for (int i = 0; i < batText.Count; i++)
      {
        var line = batText[i];
        //scpos
        line = Regex.Replace(line, "#SCPosPath#", scpos_catPath, RegexOptions.IgnoreCase);
        //logoframe
        line = Regex.Replace(line, "#LogoFrameText#", logoFrame_catPath, RegexOptions.IgnoreCase);
        //join_logo_scp
        line = Regex.Replace(line, "#join_logo_scp#", join_logo_scpPath, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#JL_CmdPath#", jl_CmdPath, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#JLS_ResultPath#", jls_resultPath, RegexOptions.IgnoreCase);
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