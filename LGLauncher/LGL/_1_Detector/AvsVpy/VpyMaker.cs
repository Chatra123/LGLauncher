using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;


namespace LGLauncher
{
  using OctNov.IO;

  /// <summary>
  /// vpyの作成、総フレーム数の取得
  /// </summary>
  class VpyMaker : AbstractAvsMaker
  {
    VpyScripter scripter = new VpyScripter();

    /// <summary>
    /// トリムフレーム取得
    /// </summary>
    public override int[] GetTrimFrame()
    {
      //総フレーム数取得用スクリプトの作成、実行
      {
        string path = scripter.CreateInfo();
        try
        {
          LwiFile.Set();
          scripter.RunInfo(path);
        }
        finally
        {
          LwiFile.Back();
        }
      }

      //総フレーム数取得
      int totalframe;
      {
        string path = Path.Combine(PathList.LWorkDir, PathList.WorkName + ".info.txt");
        var info = AvsVpyCommon.GetInfo_fromText(path);
        totalframe = (int)info[0];
      }

      //開始フレーム　　（　直前の終了フレーム　＋　１　）
      int beginFrame;
      {
        //  trimFrame_prv[0] : previous begin frame
        //  trimFrame_prv[1] : previous end   frame
        int[] trimFrame_prv = (2 <= PathList.PartNo)
                                  ? AvsVpyCommon.GetTrimFrame_previous()
                                  : null;
        beginFrame = (trimFrame_prv != null) ? trimFrame_prv[1] + 1 : 0;
      }

      int[] trimFrame = new int[] { beginFrame, totalframe - 1 };
      return trimFrame;
    }


    /// <summary>
    /// Trim付きスクリプト作成
    /// </summary>
    public override string MakeScript(int[] trimFrame)
    {
      var text = scripter.CreateText(trimFrame);
      string path = AvsVpyCommon.OutScript(trimFrame, text, ".vpy", TextEnc.UTF8_nobom);
      return path;
    }

  }



  #region VpyScripter

  /// <summary>
  /// VpyMaker用 スクリプト作成の実行部
  /// </summary>
  class VpyScripter
  {
    /// <summary>
    /// 総フレーム数取得用のvpy作成  InfoSciprt
    /// </summary>
    public string CreateInfo()
    {
      //読
      var text = FileR.ReadFromResource("LGLauncher.Resource.GetInfo_vpy.vpy");

      for (int i = 0; i < text.Count; i++)
      {
        var line = text[i];
        line = Regex.Replace(line, "#LWorkDir#", PathList.LWorkDir, RegexOptions.IgnoreCase);

        //Plugin
        if (PathList.IsD2v)
        {
          line = Regex.Replace(line, "#d2v#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#d2vsource#", PathList.d2vsource, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInLWork, RegexOptions.IgnoreCase);
        }
        else if (PathList.IsLwi)
        {
          line = Regex.Replace(line, "#lwi#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#vslsmashsoruce#", PathList.vslsmashsoruce, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#TsPath#", PathList.TsPath, RegexOptions.IgnoreCase);
        }

        line = Regex.Replace(line, "#InfoName#", PathList.WorkName + ".info.txt", RegexOptions.IgnoreCase);
        text[i] = line.Trim();  // pythonは unexpected indentになるので必ずTrim()
      }

      //書
      string infoPath = PathList.WorkPath + ".info.vpy";
      File.WriteAllLines(infoPath, text, TextEnc.UTF8_bom);

      return infoPath;
    }



    /// <summary>
    /// InfoSciprt実行  vpy
    /// </summary>
    public void RunInfo(string vpyPath)
    {
      var prc = new Process();
      prc.StartInfo.FileName = "python";
      prc.StartInfo.Arguments = " \"" + vpyPath + "\"";
      prc.StartInfo.CreateNoWindow = true;
      prc.StartInfo.UseShellExecute = false;

      try
      {
        prc.Start();
        prc.WaitForExit(30 * 1000);  //数秒かかるので短すぎるのはダメ
        if (prc.HasExited && prc.ExitCode == 0)
          return;
      }
      catch
      {
        //not found python 
        throw new LGLException("  RunInfo() runtime error");
      }
      throw new LGLException("  RunInfo() timeout");
    }



    /// <summary>
    /// vpyスクリプトの文字列を作成
    /// </summary>
    public List<string> CreateText(int[] trimFrame)
    {
      //読
      var text = FileR.ReadFromResource("LGLauncher.Resource.TrimVpy.vpy");

      for (int i = 0; i < text.Count; i++)
      {
        var line = text[i];
        //Plugin
        if (PathList.IsD2v)
        {
          line = Regex.Replace(line, "#d2v#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#d2vsource#", PathList.d2vsource, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInLWork, RegexOptions.IgnoreCase);
        }
        else if (PathList.IsLwi)
        {
          line = Regex.Replace(line, "#lwi#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#vslsmashsoruce#", PathList.vslsmashsoruce, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#TsPath#", PathList.TsPath, RegexOptions.IgnoreCase);
        }

        //Detector
        if (PathList.IsJLS)
          line = Regex.Replace(line, "#JLS#", "", RegexOptions.IgnoreCase);
        else if (PathList.IsLG)
          line = Regex.Replace(line, "#LG#", "", RegexOptions.IgnoreCase);

        //Trim
        if (PathList.IsPart)
        {
          int beginFrame = trimFrame[0];
          int endFrame = trimFrame[1];
          line = Regex.Replace(line, "#EnableTrim#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#BeginFrame#", "" + beginFrame, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#EndFrame_plus1#", "" + (endFrame + 1), RegexOptions.IgnoreCase);
        }
        text[i] = line.Trim();  // pythonは unexpected indentになるので必ずTrim()
      }

      return text;
    }

    #endregion


  }
}



