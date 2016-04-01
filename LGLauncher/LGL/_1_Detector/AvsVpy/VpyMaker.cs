using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;


#pragma warning disable 0162           //警告0162：到達できないコード
namespace LGLauncher
{
  using OctNov.IO;

  internal class VpyMaker : AbstractAvsMaker
  {

    public override string AvsPath { get; protected set; }            //作成したVpyのパス
    public override int[] TrimFrame { get; protected set; }           //トリム用フレーム数

    /// <summary>
    /// Trim付きVpy作成
    /// </summary>
    public override void Make()
    {
      /* ☆ NotImplementedException */
      throw new NotImplementedException();

      //フレーム数取得用スクリプト
      {
        string infoPath = CreateInfo_vpy();

        var action = new Action(() => { RunInfo_vpy(infoPath); });
        CommonAvsVpy.RunInfo(action);
      }


      //総フレーム数取得
      int totalframe;
      {
        string infoText = Path.Combine(PathList.LWorkDir, PathList.WorkName + ".info.txt");
        var info = CommonAvsVpy.GetInfoText(infoText);
        totalframe = (int)info[0];
      }

      //前回のトリム用フレーム数取得   previous
      int[] trimFrame_prv = (2 <= PathList.PartNo)
                                ? CommonAvsVpy.GetTrimFrame_previous()
                                : null;

      //トリム用フレーム計算
      this.TrimFrame = CommonAvsVpy.CalcTrimFrame(totalframe, trimFrame_prv);

      //vpy作成
      var vpyText = CreateTrim_vpy(this.TrimFrame);
      this.AvsPath = CommonAvsVpy.OutScript(this.TrimFrame, vpyText, ".vpy", TextEnc.UTF8_nobom);

    }


    /// <summary>
    /// フレーム数取得用のVpy作成
    /// </summary>
    private string CreateInfo_vpy()
    {
      //読
      var vpyText = FileR.ReadFromResource("LGLauncher.Resource.GetInfo_vpy.vpy");

      //置換
      for (int i = 0; i < vpyText.Count; i++)
      {
        var line = vpyText[i];
        line = Regex.Replace(line, "#LWorkDir#", PathList.LWorkDir, RegexOptions.IgnoreCase);

        //Plugin
        if (PathList.InputPlugin == PluginType.D2v)
        {
          line = Regex.Replace(line, "#d2v#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#d2vsource#", PathList.d2vsource_dll, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#D2vName#", PathList.WorkName + ".d2v", RegexOptions.IgnoreCase);
        }
        else
        {
          line = Regex.Replace(line, "#lwi#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#vslsmashsoruce#", PathList.vslsmashsource_dll, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#TsPath#", PathList.TsPath, RegexOptions.IgnoreCase);
        }

        line = Regex.Replace(line, "#InfoName#", PathList.WorkName + ".info.txt", RegexOptions.IgnoreCase);
        vpyText[i] = line.Trim(); // pythonは unexpected indentになるので必ずTrim()
      }

      //書
      string infoPath = PathList.WorkPath + ".info.vpy";
      File.WriteAllLines(infoPath, vpyText, TextEnc.UTF8_bom);
      return infoPath;
    }



    /// <summary>
    /// InfoSciprt実行  vpy
    /// </summary>
    private void RunInfo_vpy(string vpyPath)
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
    private List<string> CreateTrim_vpy(int[] trimFrame)
    {
      int beginFrame = trimFrame[0];
      int endFrame = trimFrame[1];

      //読
      var vpyText = FileR.ReadFromResource("LGLauncher.Resource.TrimVpy.vpy");

      //置換
      for (int i = 0; i < vpyText.Count; i++)
      {
        var line = vpyText[i];
        //Plugin
        if (PathList.InputPlugin == PluginType.D2v)
        {
          line = Regex.Replace(line, "#d2v#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#d2vsource#", PathList.d2vsource_dll, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#D2vName#", PathList.WorkName + ".d2v", RegexOptions.IgnoreCase);
        }
        else
        {
          line = Regex.Replace(line, "#lwi#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#vslsmashsoruce#", PathList.vslsmashsource_dll, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#TsPath#", PathList.TsPath, RegexOptions.IgnoreCase);
        }

        /* ☆ NotImplementedException */
        throw new NotImplementedException();
        //Detector
        //if (PathList.Detector == LogoDetector.Join_Logo_Scp)
        //  line = Regex.Replace(line, "#Join_Logo_Scp#", "", RegexOptions.IgnoreCase);
        //else
        //  line = Regex.Replace(line, "#LogoGuillo#", "", RegexOptions.IgnoreCase);

        //Trim
        if (PathList.IsPart)
        {
          line = Regex.Replace(line, "#EnableTrim#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#BeginFrame#", "" + beginFrame, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#EndFrame_plus1#", "" + (endFrame + 1), RegexOptions.IgnoreCase);
        }
        vpyText[i] = line.Trim(); // pythonは unexpected indentになるので必ずTrim()
      }

      return vpyText;
    }



  }
}



#pragma warning restore 0162          //警告0162：到達できないコード