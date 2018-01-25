using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

#region region_title
#endregion

namespace LGLauncher
{
  internal class Program
  {
    static Program_Core core = new Program_Core();

    private static void Main(string[] args)
    {
      //args = new string[] {
      //  @".\cap8s.ts",
      //};


      //例外を捕捉する
      AppDomain.CurrentDomain.UnhandledException += OctNov.Excp.ExceptionInfo.OnUnhandledException;


      int[] trimRange = null;        //avsの有効フレーム範囲
      try
      {
        bool initialized = core.Initialize(args);
        if (initialized == false) return;
        //有効フレーム範囲取得
        AvsVpyMaker.Init();
        trimRange = AvsVpyMaker.GetTrimRange();
      }
      catch (LGLException e)
      {
        Log.WriteLine();
        Log.WriteLine(e.ToString());
        return;
      }

      while (true)
      {
        //分割トリム作成
        //有効フレーム範囲を適度に分割して初回のチャプター作成を早くする。
        int[] splitTrim;
        if (PathList.DisableSplit)
        {
          splitTrim = trimRange;
          PathList.Update_IsLastSplit(true);
        }
        else
        {
          int endFrame_Max = trimRange[1];
          bool isLastSplit;
          splitTrim = core.MakeSplitTrim(endFrame_Max, out isLastSplit);
          PathList.Update_IsLastSplit(isLastSplit);
        }

        /* LogoGuillo bat実行 */
        bool HasError = false;
        try
        {
          string batpath = core.MakeDetectorBat(splitTrim);
          core.RunDetectorBat(splitTrim, batpath);
        }
        catch (LGLException e)
        {
          /*
          * ◇エラー発生時の動作について
          * 　・作成済みのavs  *.p3.2001__3000.avs  を削除
          * 　・ダミーavs      *.p3.2000__2000.avs  を作成
          * 　　次回のLGLauncherでダミーavsの 2000, 2000を読み込んでもらう。
          * 
          * ◇チャプター出力について
          * 　　エラーが発生してもチャプター出力は行う。
          * 　　Detect_PartNo()があるので *.p3.frame.cat.txtは作成しなくてはならない。
          * 　　値は前回のチャプターと同じ値。
          * 　　IsLastPartならば join_logo_scpのlast_batch、chapter出力を実行する必要がある。
          */
          HasError = true;
          Log.WriteLine();
          Log.WriteLine(e.ToString());
          CleanWorkItem.Clean_OnError();
          AvsVpyCommon.CreateDummy_OnError();
        }

        /* チャプター出力 */
        try
        {
          var concat = Frame.EditFrame.Concat(splitTrim);
          Frame.EditFrame.OutputChapter(concat, splitTrim);
        }
        catch (LGLException e)
        {
          HasError = true;
          Log.WriteLine();
          Log.WriteLine(e.ToString());
        }

        if (PathList.IsLastSplit || HasError)
          break;
        else
          PathList.IncrementPartNo();  /* PartNo++ */
      }


      Log.WriteLine("  exit");
      Log.Close();
      CleanWorkItem.Clean_Lastly();
    }



    #region Program_Core
    class Program_Core
    {
      /// <summary>
      /// Initialize
      /// </summary>
      public bool Initialize(string[] args)
      {
        var cmdline = new Setting_CmdLine(args);
        var setting = Setting_File.LoadFile();
        if (setting == null)
        {
          Log.WriteLine("fail to read xml");
          return false;
        }
        if (setting.Enable <= 0) return false;
        if (args.Count() == 0) return false;     /* ”引数０”なら設定ファイル作成後に終了 */

        /* パス作成 */
        PathList.Init(cmdline, setting);
        ProhibitFileMove_LGL.Lock();
        CleanWorkItem.Clean_Beforehand();

        if (PathList.Is1stPart)
          Log.WriteLine(cmdline.Result());
        return true;
      }


      /// <summary>
      /// 分割トリム作成
      /// </summary>
      /// <param name="endFrame_Max">作成可能な終了フレーム</param>
      /// <param name="isLastSplit">endFrame_Maxに到達したか？</param>
      public int[] MakeSplitTrim(int endFrame_Max, out bool isLastSplit)
      {
        /*
         *  適度に分割して初回のチャプター作成を早くする。
         *  
         *  length = 35min なら、 35m            に分割
         *  length = 45min なら、 30m, 15m       に分割
         *  length = 95min なら、 30m, 30m, 35m  に分割
         */
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
        int[] splitTrim;
        {
          const int len_30min = (int)(30.0 * 60.0 * 29.970);
          const int len_40min = (int)(40.0 * 60.0 * 29.970);
          int len = endFrame_Max - beginFrame + 1;
          if (len_40min < len)
          {
            splitTrim = new int[] { beginFrame, beginFrame + len_30min };
            isLastSplit = false;
          }
          else
          {
            splitTrim = new int[] { beginFrame, endFrame_Max };
            isLastSplit = true;
          }
        }
        //Log
        double len_min = 1.0 * (splitTrim[1] - splitTrim[0]) / 29.970 / 60;
        var text = new StringBuilder();
        text.AppendLine("  [ Split Trim ]");
        text.AppendLine("    PartNo        =  " + PathList.PartNo);
        text.AppendLine("    SplitTrim[0]  =  " + splitTrim[0]);
        text.AppendLine("             [1]  =  " + splitTrim[1]);
        text.AppendLine("    length        =  " + string.Format("{0:f1}  min", len_min));
        text.AppendLine("    EndFrame_Max  =  " + endFrame_Max);
        text.AppendLine("    IsLastSplit   =  " + isLastSplit);
        Log.WriteLine(text.ToString());

        return splitTrim;
      }


      /// <summary>
      /// LogoGuillo実行用のbat作成
      /// </summary>
      public string MakeDetectorBat(int[] trimRange)
      {
        //avs
        string avsPath;
        {
          avsPath = AvsVpyMaker.MakeScript(trimRange);
        }
        //srt
        string srtPath;
        {
          int beginFrame = trimRange[0];
          double shiftSec = 1.0 * beginFrame / 29.970;
          srtPath = new SrtFile().Format(shiftSec);
        }
        //bat
        string batPath = "";
        {
          if (PathList.IsJLS)
          {
            var logo = Bat.LogoSelector.LogoPath;
            var jl_cmd = PathList.JL_Cmd_OnRec;
            batPath = Bat.Bat_JLS.Make_OnRec(avsPath, logo, jl_cmd);
          }
          else if (PathList.IsLG)
          {
            var logo = Bat.LogoSelector.LogoPath;
            var param = Bat.LogoSelector.ParamPath;
            batPath = Bat.Bat_LG.Make(avsPath, srtPath, logo, param);
          }
        }
        return batPath;
      }


      /// <summary>
      /// LogoGuillo実行
      /// </summary>
      public void RunDetectorBat(int[] trimRange, string batPath)
      {
        //retry
        //  Windows sleep でタイムアウトしたらリトライする。
        for (int retry = 0; retry <= 2; retry++)
        {
          WaitForSystemReady waitForReady = null;
          try
          {
            //Semaphore取得
            waitForReady = new WaitForSystemReady();
            bool isReady = waitForReady.GetReady(PathList.DetectorName, PathList.Detector_MultipleRun);
            if (isReady == false) return;

            int timeout_ms;
            {
              //logoframeが終了しないことがあったのでタイムアウトを設定
              //  ”avsの総時間”の３倍
              int len_frame = trimRange[1] - trimRange[0] + 1;
              double len_sec = 1.0 * len_frame / 29.970;
              timeout_ms = (int)(len_sec * 3) * 1000;
              timeout_ms = timeout_ms <= 30 * 1000 ? 90 * 1000 : timeout_ms;
            }

            //実行
            LwiFileMover.Set();
            bool need_retry;
            Bat.BatLauncher.Launch(batPath, out need_retry, timeout_ms);
            if (need_retry)
              continue;
            else
              break;
          }
          finally
          {
            LwiFileMover.Back();
            if (waitForReady != null)
              waitForReady.Release();
          }
        }
      }
    } //class Program_Core 
    #endregion

  }//class Program
}//namespace