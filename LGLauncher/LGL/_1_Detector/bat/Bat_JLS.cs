using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;


namespace LGLauncher.Bat
{
  static class Bat_JLS
  {
    /// <summary>
    /// Join_Logo_Scp用バッチ作成
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
      batText = TextR.ReadFromResource("LGLauncher.Resource.JLS_OnRec.bat");

      string chapter_exePath = PathList.Chapter_exe;
      string chapter_exeResult = PathList.WorkName + ".jls.scpos.txt";
      string logoframe = PathList.LogoFrame;
      string logoFrameResult = PathList.WorkName + ".jls.logoframe.txt";
      string join_logo_scpPath = PathList.Join_Logo_Scp;
      string jls_Result = PathList.WorkName + ".jls.txt";

      for (int i = 0; i < batText.Count; i++)
      {
        var line = batText[i];
        //Part
        line = Regex.Replace(line, "#PartNo#", "" + PathList.PartNo);
        line = Regex.Replace(line, "#TsShortName#", PathList.TsShortName);
        //chapter_exe
        line = Regex.Replace(line, "#chapter_exe#", chapter_exePath);
        line = Regex.Replace(line, "#AvsPath#", avsPath);
        line = Regex.Replace(line, "#chapter_exeResult#", chapter_exeResult);
        //logoframe
        line = Regex.Replace(line, "#logoframe#", logoframe);
        line = Regex.Replace(line, "#LogoPath#", logoPath);
        line = Regex.Replace(line, "#LogoFrameResult#", logoFrameResult);
        //join_logo_scp
        line = Regex.Replace(line, "#join_logo_scp#", join_logo_scpPath);
        line = Regex.Replace(line, "#JL_CmdPath#", jl_CmdPath);
        line = Regex.Replace(line, "#JLS_Result#", jls_Result);
        batText[i] = line;
      }

      //書
      string batPath = PathList.WorkPath + ".JLS.bat";
      File.WriteAllLines(batPath, batText, TextEnc.Shift_JIS);
      return batPath;
    }



    /// <summary>
    /// Join_Logo_Scp用バッチ作成  　最終 concat
    /// </summary>
    public static string Make_AtLast(string jl_CmdPath)
    {
      if (File.Exists(jl_CmdPath) == false)
        throw new LGLException("jl_CmdPath does not exist");

      //読
      var batText = new List<string>();
      batText = TextR.ReadFromResource("LGLauncher.Resource.JLS_Last.bat");

      string scpos_catPath = PathList.TsShortName + ".jls.scpos.cat.txt";
      string logoFrame_catPath = PathList.TsShortName + ".jls.logoframe.cat.txt";
      string join_logo_scpPath = PathList.Join_Logo_Scp;
      string jls_resultPath = PathList.TsShortName + ".jls.last.txt";

      for (int i = 0; i < batText.Count; i++)
      {
        var line = batText[i];
        line = Regex.Replace(line, "#chapter_exe_Result#", scpos_catPath);
        line = Regex.Replace(line, "#LogoFrame_Result#", logoFrame_catPath);
        line = Regex.Replace(line, "#join_logo_scp#", join_logo_scpPath);
        line = Regex.Replace(line, "#JL_CmdPath#", jl_CmdPath);
        line = Regex.Replace(line, "#JLS_Result#", jls_resultPath);
        batText[i] = line;
      }

      //書
      string batPath = Path.Combine(PathList.LWorkDir,
                                       PathList.TsShortName + ".JLS.last.bat");
      File.WriteAllLines(batPath, batText, TextEnc.Shift_JIS);
      return batPath;
    }

  }
}