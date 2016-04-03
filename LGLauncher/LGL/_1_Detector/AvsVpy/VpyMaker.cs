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

  class VpyMaker : AbstractAvsMaker
  {
    VpyMakerModule module = new VpyMakerModule();


    /// <summary>
    /// トリム用フレーム数取得
    /// </summary>
    public override int[] GetTrimFrame()
    {
      //フレーム数取得用スクリプト　作成、実行
      {
        string infoPath = module.CreateInfo_vpy();
        var action = new Action(() => { module.RunInfo_vpy(infoPath); });
        AvsVpyCommon.RunInfo(action);
      }

      //総フレーム数取得
      int totalframe;
      {
        string infoText = Path.Combine(PathList.LWorkDir, PathList.WorkName + ".info.txt");
        var info = AvsVpyCommon.GetInfoText(infoText);
        totalframe = (int)info[0];
      }

      //前回のトリム用フレーム数取得   previous
      int[] trimFrame_prv = (2 <= PathList.PartNo)
                                ? AvsVpyCommon.GetTrimFrame_previous()
                                : null;

      //トリム用フレーム計算
      int[] trimFrame = AvsVpyCommon.CalcTrimFrame(totalframe, trimFrame_prv);      //Trim付きスクリプト作成
      return trimFrame;
    }

    /// <summary>
    /// Trim付きスクリプト作成
    /// </summary>
    public override string MakeTrimScript(int[] trimFrame)
    {
      var text = module.CreateTrim_vpy(trimFrame);
      string path = AvsVpyCommon.OutScript(trimFrame, text, ".vpy", TextEnc.UTF8_nobom);
      return path;
    }
  }//  class VpyMaker



  /// <summary>
  /// VpyMaker用 Module
  /// </summary>
  class VpyMakerModule
  {
    /// <summary>
    /// フレーム数取得用のVpy作成
    /// </summary>
    public string CreateInfo_vpy()
    {
      //読
      var text = FileR.ReadFromResource("LGLauncher.Resource.GetInfo_vpy.vpy");

      //置換
      for (int i = 0; i < text.Count; i++)
      {
        var line = text[i];
        line = Regex.Replace(line, "#LWorkDir#", PathList.LWorkDir, RegexOptions.IgnoreCase);

        //Plugin
        if (PathList.InputPlugin == PluginType.D2v)
        {
          line = Regex.Replace(line, "#d2v#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#d2vsource#", PathList.d2vsource_dll, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInWork, RegexOptions.IgnoreCase);
        }
        else if (PathList.InputPlugin == PluginType.Lwi)
        {
          line = Regex.Replace(line, "#lwi#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#vslsmashsoruce#", PathList.vslsmashsource_dll, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#TsPath#", PathList.TsPath, RegexOptions.IgnoreCase);
        }

        line = Regex.Replace(line, "#InfoName#", PathList.WorkName + ".info.txt", RegexOptions.IgnoreCase);
        text[i] = line.Trim(); // pythonは unexpected indentになるので必ずTrim()
      }

      //書
      string infoPath = PathList.WorkPath + ".info.vpy";
      File.WriteAllLines(infoPath, text, TextEnc.UTF8_bom);
      return infoPath;
    }



    /// <summary>
    /// InfoSciprt実行  vpy
    /// </summary>
    public void RunInfo_vpy(string vpyPath)
    {
      var prc = new Process();
      {
        var psi = new ProcessStartInfo();
        psi.FileName = "python";
        psi.Arguments = " \"" + vpyPath + "\"";
        psi.CreateNoWindow = true;
        psi.UseShellExecute = false;
        prc.StartInfo = psi;
      }

      try
      {
        prc.Start();
        prc.WaitForExit(6 * 1000);
        if (prc.HasExited && prc.ExitCode == 0)
          return;  //正常終了
      }
      catch
      {
        throw new LGLException("python runtime error");
      }

      Log.WriteLine("RunInfo process error");
      Log.WriteLine("  prc.ExitCode  =  " + prc.ExitCode);
    }



    /// <summary>
    /// TrimVpy作成
    /// </summary>
    public List<string> CreateTrim_vpy(int[] trimFrame)
    {
      int beginFrame = trimFrame[0];
      int endFrame = trimFrame[1];

      //読
      var text = FileR.ReadFromResource("LGLauncher.Resource.TrimVpy.vpy");

      //置換
      for (int i = 0; i < text.Count; i++)
      {
        var line = text[i];
        //Plugin
        if (PathList.InputPlugin == PluginType.D2v)
        {
          line = Regex.Replace(line, "#d2v#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#d2vsource#", PathList.d2vsource_dll, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#D2vName#", PathList.D2vNameInWork, RegexOptions.IgnoreCase);
        }
        else if (PathList.InputPlugin == PluginType.Lwi)
        {
          line = Regex.Replace(line, "#lwi#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#vslsmashsoruce#", PathList.vslsmashsource_dll, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#TsPath#", PathList.TsPath, RegexOptions.IgnoreCase);
        }

        //Detector
        //if (PathList.Detector == LogoDetector.Join_Logo_Scp)
        //  line = Regex.Replace(line, "#Join_Logo_Scp#", "", RegexOptions.IgnoreCase);
        //else if (PathList.Detector == LogoDetector.LogoGuillo)
        //  line = Regex.Replace(line, "#LogoGuillo#", "", RegexOptions.IgnoreCase);

        //Trim
        if (PathList.IsPart)
        {
          line = Regex.Replace(line, "#EnableTrim#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#BeginFrame#", "" + beginFrame, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#EndFrame_plus1#", "" + (endFrame + 1), RegexOptions.IgnoreCase);
        }
        text[i] = line.Trim(); // pythonは unexpected indentになるので必ずTrim()
      }

      return text;
    }


  }//  class VpyMakerModule
}



