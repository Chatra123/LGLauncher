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
        string infoPath = module.CreateInfo_avs();
        try
        {
          LwiFile.Set_ifLwi();
          module.RunInfo_avs(infoPath);
        }
        finally
        {
          LwiFile.Back_ifLwi();
        }
      }

      //総フレーム数取得
      int totalframe = 0;
      {
        string infoText = Path.Combine(PathList.LWorkDir, PathList.WorkName + ".info.txt");
        var info = AvsVpyCommon.GetInfo_fromText(infoText);
        totalframe = (int)info[0];
      }

      //開始フレーム　　（＝　直前の終了フレーム　＋　１）
      //  if  Is1stPart || IsAll  then  beginFrame = 0
      int beginFrame;
      {
        //直前のトリム用フレーム数取得   previous
        int[] trimFrame_prv = (2 <= PathList.PartNo)
                                  ? AvsVpyCommon.GetTrimFrame_previous()
                                  : null;
        int endFrame_prv = (trimFrame_prv != null) ? trimFrame_prv[1] : -1;
        beginFrame = endFrame_prv + 1;
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
        if (PathList.InputPlugin == PluginType.D2v)
        {
          line = Regex.Replace(line, "#d2v#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#DGDecode#", PathList.DGDecode_dll, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInLWork, RegexOptions.IgnoreCase);
        }
        else if (PathList.InputPlugin == PluginType.Lwi)
        {
          line = Regex.Replace(line, "#lwi#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#LSMASHSource#", PathList.LSMASHSource_dll, RegexOptions.IgnoreCase);
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
      for (int retry = 1; retry <= 2; retry++)
      {
        var prc = new Process();
        {
          var psi = new ProcessStartInfo();
          psi.FileName = PathList.avs2pipemod;
          psi.Arguments = " -info \"" + avsPath + "\"";
          psi.CreateNoWindow = true;
          psi.UseShellExecute = false;
          prc.StartInfo = psi;
        }

        prc.Start();
        prc.WaitForExit(6 * 1000);
        if (prc.HasExited && prc.ExitCode == 0)
          return;  //正常終了

        Thread.Sleep(6 * 1000);
      }

      Log.WriteLine("RunInfo process error");
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
        if (PathList.InputPlugin == PluginType.D2v)
        {
          line = Regex.Replace(line, "#d2v#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#DGDecode#", PathList.DGDecode_dll, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInLWork, RegexOptions.IgnoreCase);
        }
        else if (PathList.InputPlugin == PluginType.Lwi)
        {
          line = Regex.Replace(line, "#lwi#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#LSMASHSource#", PathList.LSMASHSource_dll, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#TsPath#", PathList.TsPath, RegexOptions.IgnoreCase);
        }

        //Detector
        if (PathList.Detector == LogoDetector.Join_Logo_Scp)
          line = Regex.Replace(line, "#Join_Logo_Scp#", "", RegexOptions.IgnoreCase);
        else if (PathList.Detector == LogoDetector.LogoGuillo)
          line = Regex.Replace(line, "#LogoGuillo#", "", RegexOptions.IgnoreCase);

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