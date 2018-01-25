using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;


namespace LGLauncher
{
  /// <summary>
  /// vpyの作成、総フレーム数の取得
  /// </summary>
  class VpyMaker : AbstractAvsMaker
  {
    VpyScripter scripter = new VpyScripter();

    /// <summary>
    /// トリム範囲取得
    /// </summary>
    public override int[] GetTrimRange()
    {
      //総フレーム数取得用スクリプトの作成、実行
      {
        string path = scripter.CreateInfo();
        try
        {
          LwiFileMover.Set();
          scripter.RunInfo(path);
        }
        finally
        {
          LwiFileMover.Back();
        }
      }
      //総フレーム数取得
      int totalframe;
      {
        string path = Path.Combine(PathList.LWorkDir, PathList.WorkName + ".info.txt");
        var info = AvsVpyCommon.GetInfo_fromText(path);
        totalframe = (int)info[0];
      }
      //開始フレーム　　（　直前の終了フレーム＋１　）
      int beginFrame;
      {
        //  trimRange_prv[0] : previous begin frame
        //  trimRange_prv[1] : previous end   frame
        int[] trimRange_prv = (2 <= PathList.PartNo)
                                  ? AvsVpyCommon.GetTrimRange_previous()
                                  : null;
        beginFrame = (trimRange_prv != null) ? trimRange_prv[1] + 1 : 0;
      }
      int[] trimRange = new int[] { beginFrame, totalframe - 1 };
      return trimRange;
    }


    /// <summary>
    /// Trim付きスクリプト作成
    /// </summary>
    public override string MakeScript(int[] trimRange)
    {
      var text = scripter.CreateText(trimRange);
      string path = AvsVpyCommon.OutputScript(trimRange, text, PathList.AvsVpyExt, TextEnc.UTF8_nobom);
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
      var text = TextR.ReadFromResource("LGLauncher.Resource.GetInfo_vpy.vpy");

      for (int i = 0; i < text.Count; i++)
      {
        var line = text[i];
        line = Regex.Replace(line, "#LWorkDir#", PathList.LWorkDir);

        //Plugin
        if (PathList.IsD2v)
        {
          line = Regex.Replace(line, "#d2v#", "");
          line = Regex.Replace(line, "#d2vsource#", PathList.d2vsource);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInLWork);
        }
        else if (PathList.IsLwi)
        {
          line = Regex.Replace(line, "#lwi#", "");
          line = Regex.Replace(line, "#vslsmashsoruce#", PathList.vslsmashsoruce);
          line = Regex.Replace(line, "#TsPath#", PathList.TsPath);
        }

        line = Regex.Replace(line, "#InfoName#", PathList.WorkName + ".info.txt");
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
        else
          throw new LGLException("RunInfo() vpy timeout");
      }
      catch
      {
        //not found python 
        throw new LGLException("RunInfo() vpy runtime error [python]");
      }
    }


    /// <summary>
    /// vpyスクリプトの文字列を作成
    /// </summary>
    public List<string> CreateText(int[] trimRange)
    {
      //読
      var text = TextR.ReadFromResource("LGLauncher.Resource.TrimVpy.vpy");

      for (int i = 0; i < text.Count; i++)
      {
        var line = text[i];
        //Plugin
        if (PathList.IsD2v)
        {
          line = Regex.Replace(line, "#d2v#", "");
          line = Regex.Replace(line, "#d2vsource#", PathList.d2vsource);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInLWork);
        }
        else if (PathList.IsLwi)
        {
          line = Regex.Replace(line, "#lwi#", "");
          line = Regex.Replace(line, "#vslsmashsoruce#", PathList.vslsmashsoruce);
          line = Regex.Replace(line, "#TsPath#", PathList.TsPath);
        }
        //Detector
        if (PathList.IsJLS)
          line = Regex.Replace(line, "#JLS#", "");
        else if (PathList.IsLG)
          line = Regex.Replace(line, "#LG#", "");
        //Trim
        int beginFrame = trimRange[0];
        int endFrame = trimRange[1];
        line = Regex.Replace(line, "#EnableTrim#", "");
        line = Regex.Replace(line, "#BeginFrame#", "" + beginFrame);
        line = Regex.Replace(line, "#EndFrame_plus1#", "" + (endFrame + 1));

        text[i] = line.Trim();  // pythonは unexpected indentになるので必ずTrim()
      }

      return text;
    }

    #endregion


  }
}



