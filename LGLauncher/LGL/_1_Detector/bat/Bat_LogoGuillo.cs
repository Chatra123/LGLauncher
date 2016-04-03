using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LGLauncher
{
  using OctNov.IO;

  static class Bat_LogoGuillo
  {
    /// <summary>
    /// LogoGuillo起動用バッチ作成
    /// </summary>
    public static string Make(string avsPath, string srtPath, string logoPath, string paramPath)
    {
      if (File.Exists(avsPath) == false)
        throw new LGLException("format avs does not exist"); 
      if (File.Exists(logoPath) == false)
        throw new LGLException("logoPath does not exist");
      if (File.Exists(paramPath) == false)
        throw new LGLException("paramPath does not exist");


      //srtをavsと同じ名前にリネーム
      if (File.Exists(srtPath))
      {
        string avsWithoutExt = Path.GetFileNameWithoutExtension(avsPath);
        string newSrtPath = Path.Combine(PathList.LWorkDir, avsWithoutExt + ".srt");
        try
        {
          if (File.Exists(newSrtPath))
            File.Delete(newSrtPath);
          File.Move(srtPath, newSrtPath);
        }
        catch { /*do nothing*/}
      }

      //読
      var batText = new List<string>();
      batText = FileR.ReadFromResource("LGLauncher.Resource.LogoGuillo.bat");

      //#LOGOG_PATH#
      string LOGOG_PATH = PathList.LogoGuillo;
      //#AVS2X_PATH#
      string AVS2X_PATH = PathList.avs2pipemod;
      //#AVSPLG_PATH#
      string AVSPLG_PATH = @"..\..\LWork\USE_AVS";
      //#VIDEO_PATH#
      string VIDEO_PATH = avsPath;     //相対パスだとLogoGuilloの作業フォルダから検索される。フルパスで指定
      //#LOGO_PATH#
      string LOGO_PATH = logoPath;
      //#PRM_PATH#
      string PRM_PATH = paramPath;
      //#OUTPUT_PATH#
      string OUTPUT_PATH = PathList.WorkName + ".frame.txt";

      //bat
      for (int i = 0; i < batText.Count; i++)
      {
        var line = batText[i];
        //Part
        line = Regex.Replace(line, "#PartNo#", "" + PathList.PartNo, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#TsShortName#", PathList.TsShortName, RegexOptions.IgnoreCase);
        //LogoGuillo
        line = Regex.Replace(line, "#LOGOG_PATH#", LOGOG_PATH, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#AVS2X_PATH#", AVS2X_PATH, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#AVSPLG_PATH#", AVSPLG_PATH, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#VIDEO_PATH#", VIDEO_PATH, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#LOGO_PATH#", LOGO_PATH, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#PRM_PATH#", PRM_PATH, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#OUTPUT_PATH#", OUTPUT_PATH, RegexOptions.IgnoreCase);
        batText[i] = line;
      }

      //書
      string outBatPath = PathList.WorkPath + ".LG.bat";
      File.WriteAllLines(outBatPath, batText, TextEnc.Shift_JIS);
      return outBatPath;
    }

  }
}