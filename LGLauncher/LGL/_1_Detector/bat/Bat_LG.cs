using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;


namespace LGLauncher.Bat
{
  using OctNov.IO;

  static class Bat_LG
  {
    /// <summary>
    /// LogoGuillo用バッチ作成
    /// </summary>
    public static string Make(string avsPath, string srtPath, string logoPath, string paramPath)
    {
      if (File.Exists(avsPath) == false)
        throw new LGLException("avs does not exist"); 
      if (File.Exists(logoPath) == false)
        throw new LGLException("logoPath does not exist");
      if (File.Exists(paramPath) == false)
        throw new LGLException("paramPath does not exist");


      //srtをavsと同じ名前にリネーム
      if (File.Exists(srtPath))
      {
        string avsName = Path.GetFileNameWithoutExtension(avsPath);
        string newSrt = Path.Combine(PathList.LWorkDir, avsName + ".srt");
        try
        {
          if (File.Exists(newSrt))
            File.Delete(newSrt);
          File.Move(srtPath, newSrt);
        }
        catch { /* do nothing */ }
      }

      //読
      var batText = new List<string>();
      batText = TextR.ReadFromResource("LGLauncher.Resource.LogoGuillo.bat");

      //#LOGOG_PATH#
      string LOGOG_PATH = PathList.LogoGuillo;
      string AVS2X_PATH = PathList.avs2pipemod;
      string AVSPLG_PATH = @"..\..\LWork\USE_AVS";
      string VIDEO_PATH = avsPath;     //相対パスだとLogoGuilloの作業フォルダから検索される。フルパスで指定
      string LOGO_PATH = logoPath;
      string PRM_PATH = paramPath;
      string OUTPUT_PATH = PathList.WorkName + ".frame.txt";

      for (int i = 0; i < batText.Count; i++)
      {
        var line = batText[i];
        //Part
        line = Regex.Replace(line, "#PartNo#", "" + PathList.PartNo);
        line = Regex.Replace(line, "#TsShortName#", PathList.TsShortName);
        //LogoGuillo
        line = Regex.Replace(line, "#LOGOG_PATH#", LOGOG_PATH);
        line = Regex.Replace(line, "#AVS2X_PATH#", AVS2X_PATH);
        line = Regex.Replace(line, "#AVSPLG_PATH#", AVSPLG_PATH);
        line = Regex.Replace(line, "#VIDEO_PATH#", VIDEO_PATH);
        line = Regex.Replace(line, "#LOGO_PATH#", LOGO_PATH);
        line = Regex.Replace(line, "#PRM_PATH#", PRM_PATH);
        line = Regex.Replace(line, "#OUTPUT_PATH#", OUTPUT_PATH);
        batText[i] = line;
      }

      //書
      string batPath = PathList.WorkPath + ".LG.bat";
      File.WriteAllLines(batPath, batText, TextEnc.Shift_JIS);
      return batPath;
    }

  }
}