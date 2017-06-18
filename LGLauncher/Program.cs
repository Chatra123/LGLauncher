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
      ///*テスト用*/
      //var testArgs = new List<string>() { 
      //                                    "-part", 
      //                                    "-ts",
      //                                    @".\cap8s.ts",
      //                                    "-ch", "A", 
      //                                  };
      //args = testArgs.ToArray();


      //例外を捕捉する
      AppDomain.CurrentDomain.UnhandledException += OctNov.Excp.ExceptionInfo.OnUnhandledException;



      int[] trimFrame = null;        //avsの有効フレーム範囲
      try
      {
        //初期設定
        bool initialized = core.Initialize(args);
        if (initialized == false) return;

        //有効フレーム範囲取得
        var maker = new AvsVpyMaker();
        trimFrame = maker.GetTrimFrame();
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
        if (PathList.IsPart)
        {
          int EndFrame_Max = trimFrame[1];
          bool isLastSplit;
          splitTrim = core.MakeSplitTrim(EndFrame_Max, out isLastSplit);
          PathList.Update_IsLastSplit(isLastSplit);
        }
        else//IsAll
        {
          splitTrim = trimFrame;
          PathList.Update_IsLastSplit(true);
        }


        //LogoGuillo実行
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
          * 　・作成済みのavs  *.p3.2000__3000.avs  を削除
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


        //チャプター出力
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


        if (PathList.IsLastSplit || PathList.IsAll || HasError)
          break;
        else
          PathList.IncrementPartNo();  //PartNo++
      }


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
        if (args.Count() == 0) return false;               //”引数０”なら設定ファイル作成後に終了

        //パス作成
        PathList.Initialize(cmdline, setting);
        ProhibitFileMove_LGL.Lock();
        CleanWorkItem.Clean_Beforehand();

        if (PathList.Is1stPart || PathList.IsAll)
          Log.WriteLine(cmdline.ToString());
        return true;
      }


      /// <summary>
      /// 分割トリム作成
      /// </summary>
      /// <param name="endFrame_Max">作成可能な終了フレーム</param>
      /// <param name="isLastSplit">分割トリムの最後か？</param>
      public int[] MakeSplitTrim(int endFrame_Max, out bool isLastSplit)
      {
        /*
         *  適度に分割して初回のチャプター作成を早くする。
         *  
         *  length = 35min なら、 35m            に分割
         *  length = 45min なら、 30m, 15m       に分割
         *  length = 95min なら、 30m, 30m, 35m  に分割
         */
        //開始フレーム　　（　直前の終了フレーム　＋　１　）
        int beginFrame;
        {
          //  trimFrame_prv[0] : previous begin frame
          //  trimFrame_prv[1] : previous end   frame
          int[] trimFrame_prv = (2 <= PathList.PartNo)
                                    ? AvsVpyCommon.GetTrimFrame_previous()
                                    : null;
          beginFrame = (trimFrame_prv != null) ? trimFrame_prv[1] + 1 : 0;
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
      public string MakeDetectorBat(int[] trimFrame)
      {
        //avs
        string avsPath;
        {
          var maker = new AvsVpyMaker();
          avsPath = maker.MakeScript(trimFrame);
        }
        //srt
        string srtPath;
        {
          int beginFrame = trimFrame[0];
          double shiftSec = 1.0 * beginFrame / 29.970;
          srtPath = TimeShiftSrt.Make(shiftSec);
        }
        //bat
        string batPath = "";
        {
          if (PathList.IsJLS)
          {
            var logo = LogoSelector.GetLogo();
            var jl_cmd = PathList.JL_Cmd_OnRec;
            batPath = Bat_Join_Logo_Scp.Make_OnRec(avsPath,
                                                   logo[0], jl_cmd);
          }
          else if (PathList.IsLG)
          {
            var logo_param = LogoSelector.GetLogo_and_Param();
            batPath = Bat_LogoGuillo.Make(avsPath, srtPath,
                                          logo_param[0], logo_param[1]);
          }
        }
        return batPath;
      }


      /// <summary>
      /// LogoGuillo実行
      /// </summary>
      public void RunDetectorBat(int[] trimFrame, string batPath)
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
              int len_frame = trimFrame[1] - trimFrame[0] + 1;
              double len_sec = 1.0 * len_frame / 29.970;
              timeout_ms = (int)(len_sec * 3) * 1000;
              timeout_ms = timeout_ms <= 30 * 1000 ? 90 * 1000 : timeout_ms;
            }

            //実行
            LwiFile.Set();
            bool need_retry;
            BatLauncher.Launch(batPath, out need_retry, timeout_ms);
            if (need_retry)
              continue;
            else
              break;
          }
          finally
          {
            LwiFile.Back();
            //Semaphore解放
            if (waitForReady != null)
              waitForReady.Release();
          }
        }
      }


    } //class Program_Core 
    #endregion


  }//class Program
}//namespace