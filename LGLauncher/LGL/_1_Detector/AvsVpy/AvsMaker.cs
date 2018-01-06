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
  /// avsの作成、総フレーム数の取得
  /// </summary>
  class AvsMaker : AbstractAvsMaker
  {
    AvsScripter scripter = new AvsScripter();

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
          LwiFileMover.Set();
          scripter.RunInfo(path);
        }
        finally
        {
          LwiFileMover.Back();
        }
      }
      //総フレーム数取得
      int totalframe = 0;
      {
        string path = Path.Combine(PathList.LWorkDir, PathList.WorkName + ".info.txt");
        var info = AvsVpyCommon.GetInfo_fromText(path);
        totalframe = (int)info[0];
      }
      //開始フレーム　　（　直前の終了フレーム＋１　）
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
      string path = AvsVpyCommon.OutScript(trimFrame, text, PathList.AvsVpyExt, TextEnc.Shift_JIS);
      return path;
    }

  }



  #region AvsScripter

  /// <summary>
  /// ApyMaker用 スクリプト作成の実行部
  /// </summary>
  class AvsScripter
  {
    /*
     * note
     *  - avs内のWriteFileStart()のファイルパスが長いと*.info.txtのファイル名が途中で切れる。
     *  - ファイルパスの長さが255byteあたりでファイル名が切れる。
     */

    /// <summary>
    /// 総フレーム数取得用のavs作成  InfoSciprt
    /// </summary>
    public string CreateInfo()
    {
      //読
      var text = TextR.ReadFromResource("LGLauncher.Resource.GetInfo_avs.avs");

      for (int i = 0; i < text.Count; i++)
      {
        var line = text[i];
        line = Regex.Replace(line, "#LWorkDir#", PathList.LWorkDir);

        //Plugin
        if (PathList.IsD2v_DGDecode)
        {
          line = Regex.Replace(line, "#d2v#", "");
          line = Regex.Replace(line, "#DGD#", "");
          line = Regex.Replace(line, "#DGDecode#", PathList.DGDecode);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInLWork);
        }
        else if (PathList.IsD2v_MPEG2DecPlus)
        {
          line = Regex.Replace(line, "#d2v#", "");
          line = Regex.Replace(line, "#M2Dp#", "");
          line = Regex.Replace(line, "#MPEG2DecPlus#", PathList.MPEG2DecPlus);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInLWork);
        }
        else if (PathList.IsLwi)
        {
          line = Regex.Replace(line, "#lwi#", "");
          line = Regex.Replace(line, "#LSMASHSource#", PathList.LSMASHSource);
          line = Regex.Replace(line, "#TsPath#", PathList.TsPath);
        }

        line = Regex.Replace(line, "#InfoName#", PathList.WorkName + ".info.txt");
        text[i] = line.Trim();
      }

      //書
      string infoPath = PathList.WorkPath + ".info.avs";
      File.WriteAllLines(infoPath, text, TextEnc.Shift_JIS);
      return infoPath;
    }



    /// <summary>
    /// InfoSciprt実行  avs
    /// </summary>
    public void RunInfo(string avsPath)
    {
      var prc = new Process();
      prc.StartInfo.FileName = PathList.avs2pipemod;
      prc.StartInfo.Arguments = " -info \"" + avsPath + "\"";
      prc.StartInfo.CreateNoWindow = true;
      prc.StartInfo.UseShellExecute = false;
      try
      {
        prc.Start();
        prc.WaitForExit(30 * 1000);  //数秒かかるので短すぎるのはダメ
        if (prc.HasExited && prc.ExitCode == 0)
          return;
        else
          throw new LGLException("RunInfo() avs timeout");
      }
      catch
      {
        throw new LGLException("RunInfo() avs runtime error");
      }
    }


    /// <summary>
    /// avsスクリプトの文字列を作成
    /// </summary>
    public List<string> CreateText(int[] trimFrame)
    {
      //読
      var text = TextR.ReadFromResource("LGLauncher.Resource.TrimAvs.avs");

      for (int i = 0; i < text.Count; i++)
      {
        var line = text[i];
        line = Regex.Replace(line, "#LWorkDir#", PathList.LWorkDir);

        //Plugin
        if (PathList.IsD2v_DGDecode)
        {
          line = Regex.Replace(line, "#d2v#", "");
          line = Regex.Replace(line, "#DGD#", "");
          line = Regex.Replace(line, "#DGDecode#", PathList.DGDecode);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInLWork);
        }
        else if (PathList.IsD2v_MPEG2DecPlus)
        {
          line = Regex.Replace(line, "#d2v#", "");
          line = Regex.Replace(line, "#M2Dp#", "");
          line = Regex.Replace(line, "#MPEG2DecPlus#", PathList.MPEG2DecPlus);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInLWork);
        }
        else if (PathList.IsLwi)
        {
          line = Regex.Replace(line, "#lwi#", "");
          line = Regex.Replace(line, "#LSMASHSource#", PathList.LSMASHSource);
          line = Regex.Replace(line, "#TsPath#", PathList.TsPath);
        }

        //Detector
        if (PathList.IsJLS)
          line = Regex.Replace(line, "#JLS#", "");
        else if (PathList.IsLG)
          line = Regex.Replace(line, "#LG#", "");

        //Trim
        if (PathList.IsPart)
        {
          int beginFrame = trimFrame[0];
          int endFrame = trimFrame[1];
          line = Regex.Replace(line, "#EnableTrim#", "");
          line = Regex.Replace(line, "#BeginFrame#", "" + beginFrame);
          line = Regex.Replace(line, "#EndFrame#", "" + endFrame);
        }
        text[i] = line.Trim();
      }

      return text;
    }

    #endregion


  }
}