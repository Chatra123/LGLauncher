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


      //LGL_Main.Main_NormalTrim(args);

      LGLMain.Main_SplitTrim(args);

    }
  }


  static class LGLMain
  {
    static LGLMainModule module = new LGLMainModule();

    /// <summary>
    /// 実行　分割トリム無し
    /// </summary>
    public static void Main_NormalTrim(string[] args)
    {
      //初期設定
      string cmdline_ToString = "";
      try
      {
        bool initialized = module.Initialize(args, out cmdline_ToString);
        if (initialized == false) return;
      }
      catch (LGLException e)
      {
        Log.WriteLine();
        Log.WriteLine(e.ToString());
        Log.WriteLine(cmdline_ToString);
        return;
      }


      //トリムフレーム取得
      int[] trimFrame = null;
      try
      {
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


      //フレーム検出
      try
      {
        module.DetectFrame(trimFrame);
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
        * 　　IsLastPartなら logo_scp_posのlast_batch、ogm chapter出力を実行する。
        */
        Log.WriteLine();
        Log.WriteLine(e.ToString());
        Log.WriteLine(cmdline_ToString);
        DeleteWorkItem.Clean_OnError();
        AvsVpyCommon.CreateDummy_OnError();
      }

      //チャプター出力
      try
      {
        var concat = EditFrame.FrameEditor.Edit_ConcatFrame(trimFrame);
        EditFrame.FrameEditor.Edit_Chapter(concat, trimFrame);
      }
      catch (LGLException e)
      {
        Log.WriteLine();
        Log.WriteLine(e.ToString());
        Log.WriteLine(cmdline_ToString);
        return;
      }

      DeleteWorkItem.Clean_Lastly();
      Log.Close();　　                 //ログは残すのでDeleteWorkItemの後でclose
    }




    /// <summary>
    /// 実行　分割トリム有り
    /// </summary>
    public static void Main_SplitTrim(string[] args)
    {
      //初期設定
      string cmdline_ToString = "";
      try
      {
        bool initialized = module.Initialize(args, out cmdline_ToString);
        if (initialized == false) return;
      }
      catch (LGLException e)
      {
        Log.WriteLine();
        Log.WriteLine(e.ToString());
        Log.WriteLine(cmdline_ToString);
        return;
      }

      //
      //トリムフレーム取得
      //
      int[] trimFrame = null;
      try
      {
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

      //
      //Detector実行、チャプター作成
      //
      while (true)
      {
        //分割トリム作成
        int[] splitTrim;
        if (PathList.Enable_SplitTrim)
        {
          //適度に分割して初回のチャプター作成を早くする。
          int EndFrame_Max = trimFrame[1];
          bool isLastSplit;
          splitTrim = module.CreateSplitTrim(EndFrame_Max, out isLastSplit);
          PathList.Set_IsLastSplit(isLastSplit);
        }
        else
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
          * 　　IsLastPartなら logo_scp_posのlast_batch、ogm chapter出力を実行する。
          */
          HasError = true;
          Log.WriteLine();
          Log.WriteLine(e.ToString());
          Log.WriteLine(cmdline_ToString);
          DeleteWorkItem.Clean_OnError();
          AvsVpyCommon.CreateDummy_OnError();
        }


        //チャプター出力
        try
        {
          var concat = EditFrame.FrameEditor.Edit_ConcatFrame(splitTrim);
          EditFrame.FrameEditor.Edit_Chapter(concat, splitTrim);
        }
        catch (LGLException e)
        {
          HasError = true;
          Log.WriteLine();
          Log.WriteLine(e.ToString());
          Log.WriteLine(cmdline_ToString);
        }

        if (PathList.IsLastSplit || HasError)
          break;
        else
          PathList.IncreamentPartNo();  //PartNoを増やして続行
      }

      DeleteWorkItem.Clean_Lastly();
      Log.Close();　　                 //ログは残すのでDeleteWorkItemの後でclose
    }

  }//class LGLMain



  /// <summary>
  /// LGLMain用 Module
  /// </summary>
  class LGLMainModule
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
      DeleteWorkItem.Clean_Beforehand();

      if (PathList.Is1stPart || PathList.IsAll)
      {
        Log.WriteLine(cmdline_ToString);
        Log.WriteLine();
      }
      return true;
    }


    /// <summary>
    /// 分割トリム作成
    /// </summary>
    /// <param name="endFrame_Max">作成できる最大の終了フレーム</param>
    /// <param name="isLastSplit">分割トリムの最後か？</param>
    public int[] CreateSplitTrim(int endFrame_Max, out bool isLastSplit)
    {
      /*
       *  適度に分割して初回のチャプター作成を早くする。
       *  
       *  TrimFrame length =  35min なら、 35m                に分割
       *  TrimFrame length =  45min なら、 30m, 15m           に分割
       *  TrimFrame length =  75min なら、 30m, 30m, 15m      に分割
       *  TrimFrame length = 105min なら、 30m, 30m, 30m, 15m に分割
       */
      int[] splitTrim;
      {
        //const int framelen_30min = (int)(2.0 * 60.0 * 29.970);
        //const int framelen_40min = (int)(4.0 * 60.0 * 29.970);
        const int framelen_30min = (int)(30.0 * 60.0 * 29.970);
        const int framelen_40min = (int)(40.0 * 60.0 * 29.970);

        //開始フレーム　（直前の終了フレーム＋１）
        int beginFrame;
        {
          //直前のトリム用フレーム数取得   previous
          int[] trimFrame_prv = (2 <= PathList.PartNo)
                                    ? AvsVpyCommon.GetTrimFrame_previous()
                                    : null;
          int endFrame_prv = (trimFrame_prv != null) ? trimFrame_prv[1] : -1;
          beginFrame = endFrame_prv + 1;
        }

        //splitTrim作成
        int framelen_splitTrim = endFrame_Max - beginFrame;
        if (framelen_40min < framelen_splitTrim)
        {
          splitTrim = new int[] { beginFrame, beginFrame + framelen_30min };
          isLastSplit = false;
        }
        else
        {
          splitTrim = new int[] { beginFrame, endFrame_Max };
          isLastSplit = true;
        }

        //Log
        {
          double len = 1.0 * (splitTrim[1] - splitTrim[0]) / 29.970 / 60;
          var log = new StringBuilder();
          log.AppendLine("  [ Split Trim ]");
          log.AppendLine("    PartNo        =  " + PathList.PartNo);
          log.AppendLine("    SplitTrim[0]  =  " + splitTrim[0]);
          log.AppendLine("             [1]  =  " + splitTrim[1]);
          log.AppendLine("    length        =  " + string.Format("{0:f1}  min", len));
          log.AppendLine("    EndFrame_Max  =  " + endFrame_Max);
          log.AppendLine("    isLastSplit   =  " + isLastSplit);
          Log.WriteLine(log.ToString());
        }
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
        double shiftSec = 1.0 * trimFrame[0] / 29.970;
        srtPath = TimeShiftSrt.Make(shiftSec);
      }

      //bat
      string batPath = "";
      {
        if (PathList.Detector == LogoDetector.Join_Logo_Scp)
        {
          var logo = LogoSelector.GetLogo();
          var jl_cmdPath = PathList.JL_Cmd_OnRec;
          batPath = Bat_Join_Logo_Scp.Make_OnRec(avsPath,
                                                 logo[0], jl_cmdPath);
        }
        else if (PathList.Detector == LogoDetector.LogoGuillo)
        {
          var logo_param = LogoSelector.GetLogo_and_Param();
          batPath = Bat_LogoGuillo.Make(avsPath, srtPath,
                                        logo_param[0], logo_param[1]);
        }
      }

      // return : create trim script only 
      //return;


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
          double avsTime_sec = 1.0 * (trimFrame[1] - trimFrame[0]) / 29.970;
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
  } //class LGLMainModule 




}//namespace