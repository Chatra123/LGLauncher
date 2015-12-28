using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;


namespace LGLauncher
{
  using OctNov.Excp;

  internal class Program
  {

    private static void Main(string[] args)
    {
      ///*テスト用引数*/
      //var testArgs = new List<string>() { "-no", "1", "-last" , "-ts",
      //                                    @".\cap8s.ts",
      //                                    "-ch", "A", "-program", "program" };
      //args = testArgs.ToArray();


      //例外を捕捉する
      AppDomain.CurrentDomain.UnhandledException += ExceptionInfo.OnUnhandledException;


      string CmdLine_ToString = "";　　　　　　　　　　　　//例外発生時のログ用
      try
      {
        //初期化
        {
          var cmdline = new CommandLine(args);
          CmdLine_ToString = cmdline.ToString();

          //カレントディレクトリ設定
          string AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
          string AppDir = System.IO.Path.GetDirectoryName(AppPath);
          Directory.SetCurrentDirectory(AppDir);

          var setting = Setting_File.LoadFile();
          if (setting.bEnable <= 0) return;
          if (args.Count() == 0) return;                   //引数無しは設定ファイル作成後に終了

          PathList.MakePath(cmdline, setting);

          ProhibitFileMove.Lock();
          DeleteWorkItem.Clean_Beforehand();
        }


        //フレーム検出
        //   trimFrame:  avsの開始、終了フレーム数
        int[] trimFrame = DetectFrame();


        //出力
        {
          //フレーム合成
          var concat = EditFrame.Tweaker.ConcatFrame(trimFrame);

          //チャプターファイル作成、出力
          EditFrame.Tweaker.OutChap(concat, trimFrame);
        }

      }
      catch (LGLException e)
      {
        DeleteWorkItem.Clean_OnError();
        Log.WriteLine();
        Log.WriteLine(CmdLine_ToString);
        Log.WriteLine();
        Log.WriteLine(e.ToString());
        Log.Close();
        Environment.ExitCode = 1;
        return;                                            //強制終了
      }


      DeleteWorkItem.Clean_Lastly();
      Log.Close();　　                                     //ログは残すので削除処理の後でclose

    }




    /// <summary>
    /// Avs作成　から　LogoDetector起動　まで
    /// </summary>
    /// <returns>Avsの開始、終了フレーム数</returns>
    private static int[] DetectFrame()
    {
      //avs
      var avsMaker = PathList.Avs_iPlugin == PluginType.D2v
                          ? new AvsWithD2v() as AbstractAvsMaker
                          : new AvsWithLwi() as AbstractAvsMaker;
      avsMaker.Make();

      //srt
      var srtPath = TimeShiftSrt.Make(avsMaker.TrimFrame_m1);

      //bat
      string batPath;
      {
        if (PathList.Detector == LogoDetector.Join_Logo_Scp)
        {
          //Join_Logo_Scp
          var logoPath = LogoSelector.GetLogo();
          var jl_cmdPath = PathList.JL_Cmd_Recording;
          batPath = Bat_Join_Logo_Scp.Make_InRecording(avsMaker.AvsPath,
                                                       logoPath[0], jl_cmdPath);
        }
        else
        {
          //logoGuillo
          var logoAndParam = LogoSelector.GetLogo_and_Param();
          batPath = Bat_LogoGuillo.Make(avsMaker.AvsPath, srtPath,
                                        logoAndParam[0], logoAndParam[1]);
        }
      }

      try
      {
        //セマフォ取得
        bool isReady = WaitForSystemIdle.GetReady(PathList.Detector_MultipleRun,
                                              new List<string> { "chapter_exe", "logoframe", "logoGuillo" },
                                              true
                                              );
        if (isReady == false) return null;

        //Detector Bat実行
        if (PathList.Avs_iPlugin == PluginType.D2v)
        {
          //d2v
          BatLuncher.Launch(batPath);
        }
        else
        {
          //lwi
          AvsWithLwi.SetLwi();
          BatLuncher.Launch(batPath);
          AvsWithLwi.BackLwi();
        }

        return avsMaker.TrimFrame;
      }
      finally
      {
        //セマフォ解放
        WaitForSystemIdle.ReleaseSemaphore();
      }

    }






  }//class

}//namespace