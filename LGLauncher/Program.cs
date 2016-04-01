using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace LGLauncher
{
  internal class Program
  {
    private static void Main(string[] args)
    {
      ///*テスト用引数*/
      //var testArgs = new List<string>() { 
      //                                    "-last", 
      //                                    "-ts",
      //                                    @".\cap8s.ts",
      //                                    "-ch", "A", 
      //                                    "-SequenceName", "pfA233740427248"
      //                                  };
      //args = testArgs.ToArray();



      //例外を捕捉する
      AppDomain.CurrentDomain.UnhandledException += OctNov.Excp.ExceptionInfo.OnUnhandledException;


      string CmdLine_ToString = "";　　　　　　　　　　　　//例外発生時のログ用
      try
      {
        //初期化
        {
          var cmdline = new Setting_CmdLine(args);
          CmdLine_ToString = cmdline.ToString();

          string AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
          string AppDir = System.IO.Path.GetDirectoryName(AppPath);
          Directory.SetCurrentDirectory(AppDir);

          var setting = Setting_File.LoadFile();
          if (setting.Enable <= 0) return;
          if (args.Count() == 0) return;                   //”引数無し”なら設定ファイル作成後に終了

          //パス作成
          PathList.MakePath(cmdline, setting);

          ProhibitFileMove_LGL.Lock();
          DeleteWorkItem.Clean_Beforehand();

          if (PathList.Is1stPart || PathList.IsAll)
          {
            Log.WriteLine(CmdLine_ToString);
            Log.WriteLine();
          }
        }

        //フレーム検出
        DetectFrame();

      }
      catch (LGLException e)
      {
        Log.WriteLine();
        Log.WriteLine(e.ToString());
        Log.WriteLine(CmdLine_ToString);
        /*
         * エラー発生時の動作について
         * 　・作成済みのavs *.p3.lwi_2000__3000.avsを削除
         * 　・ダミーavs     *.p3.lwi_2000__2000.avsを作成
         * 　　次回のLGLauncherでダミーavsのトリムレンジを読み込んでもらう。
         * 
         * チャプター出力について
         * 　　エラーが発生してもチャプター出力は行う。
         * 　　detect part No があるので *.p3.frame.cat.txtを作成しなくてはいけない。
         * 　　値は前回のチャプターと同じ値にする。
         * 　　IsLastPartなら logo_scp_posのlast_batch、ogm chapter出力を実行する。
         */
        DeleteWorkItem.CleanAvs_OnError();
        CommonAvsVpy.CreateDummy_OnError();
      }


      //チャプター出力
      try
      {
        int[] trimFrame = CommonAvsVpy.GetTrimFrame();
        if (trimFrame == null)
          throw new LGLException("Could'nt specify trim range.");

        //フレーム合成
        var concat = EditFrame.FrameEditor.Edit_ConcatFrame(trimFrame);

        //チャプター作成、出力
        EditFrame.FrameEditor.Edit_OutChapter(concat, trimFrame);
      }
      catch (LGLException e)
      {
        Log.WriteLine();
        Log.WriteLine(CmdLine_ToString);
        Log.WriteLine(e.ToString());
      }


      DeleteWorkItem.Clean_Lastly();
      Log.Close();　　                 //ログは残すので削除処理の後でclose
    }


    /// <summary>
    /// ”Avs作成”から”LogoDetector起動”まで
    /// </summary>
    private static void DetectFrame()
    {
      //avs
      string avsPath;
      int beginFrame;
      double avsTime_sec;
      {
        var maker = new AvsVpyMaker();
        maker.Make();

        avsPath = maker.AvsVpyPath;
        beginFrame = maker.TrimFrame[0];
        avsTime_sec = 1.0 * (maker.TrimFrame[1] - maker.TrimFrame[0]) / 29.970;
      }


      //srt
      string srtPath;
      {
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
          var jl_cmdPath = PathList.JL_Cmd_OnRec;
          batPath = Bat_Join_Logo_Scp.Make_OnRec(avsPath,
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


      WaitForSystemReady waitForReady = null;
      try
      {
        //Mutex取得
        {
          waitForReady = new WaitForSystemReady();
          bool isReady = waitForReady.GetReady(PathList.DetectorName, PathList.Detector_MultipleRun);
          if (isReady == false) return;
        }

        //timeout
        //  ”avsの総時間”の３倍
        int timeout_ms;
        {
          timeout_ms = (int)(avsTime_sec * 3) * 1000;
          timeout_ms = timeout_ms <= 30 * 1000 ? 90 * 1000 : timeout_ms;
        }

        //Bat実行
        LwiFile.Set_ifLwi();
        BatLuncher.Launch(batPath, timeout_ms);

      }
      finally
      {
        LwiFile.Back_ifLwi();

        //Mutex解放
        if (waitForReady != null) 
          waitForReady.Release();
      }

    }






  }//class

}//namespace