﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;

namespace LGLauncher
{
  using OctNov.IO;

  class AvsMaker : AbstractAvsMaker
  {
    AvsMakerModule module = new AvsMakerModule();

    /// <summary>
    /// トリムフレーム数取得
    /// </summary>
    public override int[] GetTrimFrame()
    {
      //総フレーム数取得用スクリプト　作成、実行
      {
        string path = module.CreateInfo_avs();
        try
        {
          LwiFile.Set_ifLwi();
          module.RunInfo_avs(path);
        }
        finally
        {
          LwiFile.Back_ifLwi();
        }
      }

      //総フレーム数取得
      int totalframe = 0;
      {
        string path = Path.Combine(PathList.LWorkDir, PathList.WorkName + ".info.txt");
        var info = AvsVpyCommon.GetInfo_fromText(path);
        totalframe = (int)info[0];
      }

      //開始フレーム　　（直前の終了フレーム　＋　１　）
      int beginFrame;
      {
        //直前のトリム用フレーム数取得
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
    public override string MakeTrimScript(int[] trimFrame)
    {
      var text = module.CreateTrimText_avs(trimFrame);
      string path = AvsVpyCommon.OutScript(trimFrame, text, ".avs", TextEnc.Shift_JIS);
      return path;
    }

  }



  /// <summary>
  /// AvsMaker用 Module
  /// </summary>
  class AvsMakerModule
  {
    /// <summary>
    /// 総フレーム数取得用のAvs作成
    /// </summary>
    public string CreateInfo_avs()
    {
      //読
      var text = FileR.ReadFromResource("LGLauncher.Resource.GetInfo_avs.avs");

      //置換
      for (int i = 0; i < text.Count; i++)
      {
        var line = text[i];
        line = Regex.Replace(line, "#LWorkDir#", PathList.LWorkDir, RegexOptions.IgnoreCase);

        //Plugin
        if (PathList.IsD2v_DGDecode)
        {
          line = Regex.Replace(line, "#d2v#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#DGD#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#DGDecode#", PathList.DGDecode, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInLWork, RegexOptions.IgnoreCase);
        }
        else if (PathList.IsD2v_MPEG2DecPlus)
        {
          line = Regex.Replace(line, "#d2v#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#M2Dp#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#MPEG2DecPlus#", PathList.MPEG2DecPlus, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInLWork, RegexOptions.IgnoreCase);
        }
        else if (PathList.IsLwi)
        {
          line = Regex.Replace(line, "#lwi#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#LSMASHSource#", PathList.LSMASHSource, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#TsPath#", PathList.TsPath, RegexOptions.IgnoreCase);
        }

        line = Regex.Replace(line, "#InfoName#", PathList.WorkName + ".info.txt", RegexOptions.IgnoreCase);
        text[i] = line.Trim();
      }

      //書
      string infoPath = PathList.WorkPath + ".info.avs";
      File.WriteAllLines(infoPath, text, TextEnc.Shift_JIS);

      return infoPath;
    }
    /*
     * note
     *  - avs内のWriteFileStart()のファイル名が長いと*.info.txtのファイル名が途中で切れる。
     *  - ファイルパスの長さが255byteあたりでファイル名が切れる
     */



    /// <summary>
    /// InfoSciprt実行  avs
    /// </summary>
    public void RunInfo_avs(string avsPath)
    {
      var prc = new Process();
      {
        prc.StartInfo.FileName = PathList.avs2pipemod;
        prc.StartInfo.Arguments = " -info \"" + avsPath + "\"";
        prc.StartInfo.CreateNoWindow = true;
        prc.StartInfo.UseShellExecute = false;
      }

      try
      {
        prc.Start();
        prc.WaitForExit(20 * 1000);  //数秒かかるので短すぎるのはダメ
        if (prc.HasExited && prc.ExitCode == 0)
          return;  //正常終了
      }
      catch
      {
        throw new LGLException("  RunInfo_avs() runtime error");
      }
      new LGLException("  RunInfo_avs() timeout");
    }


    /// <summary>
    /// TrimAvs作成
    /// </summary>
    public List<string> CreateTrimText_avs(int[] trimFrame)
    {
      //読
      var text = FileR.ReadFromResource("LGLauncher.Resource.TrimAvs.avs");

      //置換
      for (int i = 0; i < text.Count; i++)
      {
        var line = text[i];
        line = Regex.Replace(line, "#LWorkDir#", PathList.LWorkDir, RegexOptions.IgnoreCase);

        //Plugin
        if (PathList.IsD2v_DGDecode)
        {
          line = Regex.Replace(line, "#d2v#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#DGD#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#DGDecode#", PathList.DGDecode, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInLWork, RegexOptions.IgnoreCase);
        }
        else if (PathList.IsD2v_MPEG2DecPlus)
        {
          line = Regex.Replace(line, "#d2v#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#M2Dp#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#MPEG2DecPlus#", PathList.MPEG2DecPlus, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInLWork, RegexOptions.IgnoreCase);
        }
        else if (PathList.IsLwi)
        {
          line = Regex.Replace(line, "#lwi#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#LSMASHSource#", PathList.LSMASHSource, RegexOptions.IgnoreCase);
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
          line = Regex.Replace(line, "#EndFrame#", "" + endFrame, RegexOptions.IgnoreCase);
        }
        text[i] = line.Trim();
      }

      return text;
    }


  }
}