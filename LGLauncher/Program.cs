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
      //                                    "-ch", "A", "-program", "program"
      //                                    "-sequencename", "pfA233740427248"
      //                                   };
      //args = testArgs.ToArray();

      //var testArgs = new List<string>() { "-autono", "-last" , "-ts",
      //                                    @"E:\TS_PFDebug\ラーメン大好き小泉さん2016新春SP【女子高生がラーメンを食べまくるドラマ】  東海テレビ011  01月04日23時30分.ts",
      //                                    "-ch", "東海テレビ", "-program", "program",
      //                                    "-sequencename", "pfA233740427248"
      //};
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

          string AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
          string AppDir = System.IO.Path.GetDirectoryName(AppPath);
          Directory.SetCurrentDirectory(AppDir);

          //設定ファイル
          var setting = Setting_File.LoadFile();
          if (setting.bEnable <= 0) return;
          if (args.Count() == 0) return;                   //”引数無し”なら設定ファイル作成後に終了

          //パス作成
          PathList.MakePath(cmdline, setting);

          ProhibitFileMove.Lock();
          DeleteWorkItem.Clean_Beforehand();
        }

        //フレーム検出
        DetectFrame();

      }
      catch (LGLException e)
      {
        Log.WriteLine();
        Log.WriteLine(CmdLine_ToString);
        Log.WriteLine(e.ToString());

        /*
         * エラー発生時の動作について
         * 　・作成済みのavs *.p3.lwi_2000__3000.avsを削除
         * 　・ダミーavs     *.p3.lwi_2000__2000.avsを作成
         * 　　次回のLGLauncherでダミーavsのトリムレンジを読み込んでもらう。
         * 
         * チャプター出力について
         * 　　エラーが発生してもチャプター出力は行う。
         * 　　detect part No があるので *.p3.frame.cat.txtを作成しなくてはいけない。
         * 　　値は前回のチャプターと同じ値になる。
         * 　　IsLastPartなら logo_scp_posの　last_batch、 mp4用チャプター出力等が実行される。
         */
        DeleteWorkItem.CleanAvs_OnError();
        MakeAvsCommon.CreateDummyAvs_OnError();
      }



      //チャプター出力
      try
      {
        int[] trimFrame = MakeAvsCommon.GetTrimFrame_fromAvsName(PathList.WorkName + ".*_*__*.avs");
        if (trimFrame == null)
          throw new LGLException("Could'nt specify trim range.");

        //フレーム合成
        var concat = EditFrame.FrameEditor.ConcatFrame(trimFrame);

        //チャプター作成、出力
        EditFrame.FrameEditor.OutChap(concat, trimFrame);
      }
      catch (LGLException e)
      {
        Log.WriteLine();
        Log.WriteLine(CmdLine_ToString);
        Log.WriteLine(e.ToString());
      }


      DeleteWorkItem.Clean_Lastly();
      Log.Close();　　                                     //ログは残すので削除処理の後でclose

    }




    /// <summary>
    /// Avs作成　から　LogoDetector起動　まで
    /// </summary>
    /// <returns>Avsの開始、終了フレーム数</returns>
    private static void DetectFrame()
    {
      //avs
      string avsPath;
      int beginFrame;
      double avsTime_sec;
      {
        var avsMaker = PathList.Avs_iPlugin == PluginType.D2v
                            ? new AvsWithD2v() as AbstractAvsMaker
                            : new AvsWithLwi() as AbstractAvsMaker;
        avsMaker.Make();

        avsPath = avsMaker.AvsPath;
        beginFrame = avsMaker.TrimFrame[0];
        avsTime_sec = 1.0 * (avsMaker.TrimFrame[1] - avsMaker.TrimFrame[0]) / 29.970;
      }


      //srt
      string srtPath;
      {
        //開始時間をずらす
        double shiftSec = 1.0 * beginFrame / 29.970;
        srtPath = TimeShiftSrt.Make(shiftSec);
      }

      //bat
      string batPath;
      {
        if (PathList.Detector == LogoDetector.Join_Logo_Scp)
        {
          //Join_Logo_Scp
          var logoPath = LogoSelector.GetLogo();
          var jl_cmdPath = PathList.JL_Cmd_Recording;
          batPath = Bat_Join_Logo_Scp.Make_InRecording(avsPath,
                                                       logoPath[0], jl_cmdPath);
        }
        else
        {
          //logoGuillo
          var logo_Param = LogoSelector.GetLogo_and_Param();
          batPath = Bat_LogoGuillo.Make(avsPath, srtPath,
                                        logo_Param[0], logo_Param[1]);
        }
      }

      try
      {
        //セマフォ取得
        bool isReady = WaitForSystemIdle.GetReady(
                                                  PathList.Detector_MultipleRun,
                                                  new List<string> { "chapter_exe", "logoframe", "logoGuillo" },
                                                  true
                                                 );
        if (isReady == false) return;


        // logoframeが終了しないことがあったので timeoutを設定
        // ”avsの総時間”の２倍の時間で処理できなければ中断
        int timeout_ms = (int)(avsTime_sec * 2) * 1000;
        timeout_ms = timeout_ms <= 60 * 1000 ? 60 * 1000 : timeout_ms;

        //Detector Bat実行
        if (PathList.Avs_iPlugin == PluginType.D2v)
        {
          //d2v
          BatLuncher.Launch(batPath, timeout_ms);
        }
        else
        {
          //lwi
          AvsWithLwi.SetLwi();
          BatLuncher.Launch(batPath, timeout_ms);
        }
      }
      finally
      {
        if (PathList.Avs_iPlugin == PluginType.Lwi)
          AvsWithLwi.BackLwi();

        //セマフォ解放
        WaitForSystemIdle.ReleaseSemaphore();
      }

    }






  }//class

}//namespace