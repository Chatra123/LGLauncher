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
    static MainMethod_Module module = new MainMethod_Module();

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


      string cmdline_ToString = "";  //ログ用のコマンドライン情報
      int[] trimFrame = null;        //avsの有効フレーム範囲
      try
      {
        //初期設定
        bool initialized = module.Initialize(args, out cmdline_ToString);
        if (initialized == false) return;

        //トリムフレーム取得
        var maker = new AvsVpyMaker();
        trimFrame = maker.GetTrimFrame();
      }
      catch (LGLException e)
      {
        Log.WriteLine();
        Log.WriteLine(e.ToString());
        Log.WriteLine(cmdline_ToString);
        return;
      }


      while (true)
      {
        Log.WriteLine();

        //分割トリム作成
        int[] splitTrim;
        if (PathList.IsPart)
        {
          //適度に分割して初回のチャプター作成を早くする。
          int EndFrame_Max = trimFrame[1];
          bool isLastSplit;
          splitTrim = module.CreateSplitTrim(EndFrame_Max, out isLastSplit);
          PathList.Set_IsLastSplit(isLastSplit);
        }
        else//IsAll
        {
          splitTrim = trimFrame;
          PathList.Set_IsLastSplit(true);
        }


        //フレーム検出
        bool HasError = false;
        try
        {
          module.DetectFrame(splitTrim);
        }
        catch (LGLException e)
        {
          /*
          * エラー発生時の動作について
          * 　・作成済みのavs  *.p3.2000__3000.avs  を削除
          * 　・ダミーavs      *.p3.2000__2000.avs  を作成
          * 　　次回のLGLauncherでダミーavsのトリムレンジを読み込んでもらう。
          * 
          * チャプター出力について
          * 　　エラーが発生してもチャプター出力は行う。
          * 　　Detect Part No があるので *.p3.frame.cat.txtを作成しなくてはいけない。
          * 　　値は前回のチャプターと同じ値にする。
          * 　　IsLastPartなら logo_scp_posのlast_batch、ogm chapter出力を実行する必要がある。。
          */
          HasError = true;
          Log.WriteLine();
          Log.WriteLine(e.ToString());
          Log.WriteLine(cmdline_ToString);
          CleanWorkItem.Clean_OnError();
          AvsVpyCommon.CreateDummy_OnError();
        }


        //チャプター出力
        try
        {
          var concat = EditFrame.FrameEditor.ConcatFrame(splitTrim);
          EditFrame.FrameEditor.OutputChapter(concat, splitTrim);
        }
        catch (LGLException e)
        {
          HasError = true;
          Log.WriteLine();
          Log.WriteLine(e.ToString());
          Log.WriteLine(cmdline_ToString);
        }


        if (PathList.IsLastSplit || PathList.IsAll || HasError)
          break;
        else
          PathList.IncrementPartNo();   //PartNo++で continue
      }

      Log.Close();
      CleanWorkItem.Clean_Lastly();
    }


    #region MainMethod_Module
    class MainMethod_Module
    {
      /// <summary>
      /// Initialize
      /// </summary>
      public bool Initialize(string[] args, out string cmdline_ToString)
      {
        var cmdline = new Setting_CmdLine(args);
        cmdline_ToString = cmdline.ToString();

        string AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string AppDir = System.IO.Path.GetDirectoryName(AppPath);
        Directory.SetCurrentDirectory(AppDir);

        var setting = Setting_File.LoadFile();
        if (setting.Enable <= 0) return false;
        if (args.Count() == 0) return false;               //”引数無し”なら設定ファイル作成後に終了

        //パス作成
        PathList.MakePath(cmdline, setting);

        ProhibitFileMove_LGL.Lock();
        CleanWorkItem.Clean_Beforehand();

        if (PathList.Is1stPart || PathList.IsAll)
          Log.WriteLine(cmdline_ToString);

        return true;
      }


      /// <summary>
      /// 分割トリム作成
      /// </summary>
      /// <param name="endFrame_Max">作成可能な最大の終了フレーム</param>
      /// <param name="isLastSplit">分割トリムの最後か？</param>
      public int[] CreateSplitTrim(int endFrame_Max, out bool isLastSplit)
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
          //直前のトリム用フレーム数取得   previous
          //  trimFrame_prv[0] : previous begin frame
          //  trimFrame_prv[1] : previous end frame
          int[] trimFrame_prv = (2 <= PathList.PartNo)
                                    ? AvsVpyCommon.GetTrimFrame_previous()
                                    : null;
          beginFrame = (trimFrame_prv != null) ? trimFrame_prv[1] + 1 : 0;
        }

        //splitTrim作成
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
        {
          double len_min = 1.0 * (splitTrim[1] - splitTrim[0]) / 29.970 / 60;
          var text = new StringBuilder();
          text.AppendLine("  [ Split Trim ]");
          text.AppendLine("    PartNo        =  " + PathList.PartNo);
          text.AppendLine("    SplitTrim[0]  =  " + splitTrim[0]);
          text.AppendLine("             [1]  =  " + splitTrim[1]);
          text.AppendLine("    length        =  " + string.Format("{0:f1}  min", len_min));
          text.AppendLine("    EndFrame_Max  =  " + endFrame_Max);
          text.AppendLine("    isLastSplit   =  " + isLastSplit);
          Log.WriteLine(text.ToString());
        }

        return splitTrim;
      }


      /// <summary>
      /// フレーム検出
      /// </summary>
      public void DetectFrame(int[] trimFrame)
      {
        //avs
        string avsPath;
        {
          var maker = new AvsVpyMaker();
          avsPath = maker.MakeTrimScript(trimFrame);
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
          if (PathList.Detector == DetectorType.Join_Logo_Scp)
          {
            var logo = LogoSelector.GetLogo();
            var jl_cmd = PathList.JL_Cmd_OnRec;
            batPath = Bat_Join_Logo_Scp.Make_OnRec(avsPath,
                                                   logo[0], jl_cmd);
          }
          else if (PathList.Detector == DetectorType.LogoGuillo)
          {
            var logo_param = LogoSelector.GetLogo_and_Param();
            batPath = Bat_LogoGuillo.Make(avsPath, srtPath,
                                          logo_param[0], logo_param[1]);
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
          int timeout_ms;
          {
            //  ”avsの総時間”の３倍
            int len_frame = trimFrame[1] - trimFrame[0] + 1;
            double len_sec = 1.0 * len_frame / 29.970;
            timeout_ms = (int)(len_sec * 3) * 1000;
            timeout_ms = timeout_ms <= 30 * 1000 ? 90 * 1000 : timeout_ms;
          }

          //Bat実行
          LwiFile.Set_ifLwi();
          BatLauncher.Launch(batPath, timeout_ms);
        }
        finally
        {
          LwiFile.Back_ifLwi();

          //Mutex解放
          if (waitForReady != null)
            waitForReady.Release();
        }
      }
    } //class MainMethod_Module 
    #endregion


  }//class Program
}//namespace