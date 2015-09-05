using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace LGLauncher
{
  internal class Program
  {
    private static void Main(string[] args)
    {
      ///*テスト用引数*/
      //var testArgs = new List<string>() { "-no", "1", "-last" , "-ts",
      //                                    @".\cap8s.ts",
      //                                    "-ch", "CBC", "-program", "program" };
      //args = testArgs.ToArray();

      //例外を捕捉する
      AppDomain.CurrentDomain.UnhandledException += ExceptionInfo.OnUnhandledException;

      //初期化
      var cmdline = new CommandLine(args);                 //引数解析
      try
      {
        var setting = Setting.LoadFile();
        if (setting.bEnable <= 0) return;

        PathList.Make(cmdline, setting);                   //パス作成

        Log.WriteLine("  No=【    " + PathList.No + "    】");
        if (PathList.No == -1 || PathList.No == 1)
          Log.WriteLine(PathList.TsPath);

        LockTheFile();

        DeleteWorkItem_Beforehand();
      }
      catch (LGLException e)                               //LGLExceptionのみ捕捉、その他はOnUnhandledExceptionで捕捉する。
      {
        Log.WriteLine();
        Log.WriteLine(cmdline.ToString());
        Log.WriteLine();
        Log.WriteException(e);
        Environment.Exit(1);                               //アプリ強制終了
      }

      //メイン処理
      int[] trimFrame = null;
      try
      {
        trimFrame = MainProcess();
      }
      catch (LGLException e)
      {
        //例外が発生してもアプリは終了させない。EditFrame.Concat()を実行する。
        Log.WriteLine();
        Log.WriteException(e);
      }

      //後処理
      try
      {
        //フレーム合成＆チャプターファイル作成
        EditFrame.Concat(trimFrame);
      }
      catch (LGLException e)
      {
        Log.WriteLine();
        Log.WriteLine(cmdline.ToString());
        Log.WriteLine();
        Log.WriteException(e);
        Environment.Exit(1);                               //アプリ強制終了
      }

      //ファイル削除
      Log.Close();
      DeleteWorkItem_Lastly();
    }

    #region メイン処理

    private static int[] MainProcess()
    {
      //avs
      var avsMaker = PathList.Mode_D2v
                          ? new AvsWithD2v() as AbstractAvsMaker
                          : new AvsWithLwi() as AbstractAvsMaker;
      avsMaker.Make();

      //srt
      var srtPath = TimeShiftSrt.Make(avsMaker.TrimFrame_m1);

      //bat
      var batPath = BatLogoGuillo.Make(avsMaker.AvsPath, srtPath);

      try
      {
        //同時起動数の制限
        bool isReady = WaitForReady();                               //セマフォ取得
        if (isReady == false) return null;

        //LogoGuillo実行
        if (PathList.Mode_D2v)
        {
          //d2v
          LaunchLogoGuillo(batPath);
        }
        else
        {
          //lwi
          AvsWithLwi.SetLwi();
          LaunchLogoGuillo(batPath);
          AvsWithLwi.BackLwi();
        }

        return avsMaker.TrimFrame;
      }
      finally
      {
        if (LGLSemaphore != null) LGLSemaphore.Release();            //セマフォ解放
      }
    }

    #endregion メイン処理

    #region ファイルの移動禁止

    private static FileStream lock_ts, lock_d2v, lock_lwi, lock_lwifooter, lock_srt;             //プロセス終了でロック解放

    /// <summary>
    /// ファイルを移動禁止にする。
    /// </summary>
    private static void LockTheFile()
    {
      //ts
      try
      {
        lock_ts = new FileStream(PathList.TsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      }
      catch { throw new LGLException(); }

      //d2v
      if (PathList.Mode_D2v == true)
        try
        {
          lock_d2v = new FileStream(PathList.D2vPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch { throw new LGLException(); }

      //lwi
      if (PathList.Mode_D2v == false)
        try
        {
          lock_lwi = new FileStream(PathList.LwiPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

          if (File.Exists(PathList.LwiFooterPath))  //lwifooterファイルが無い場合もある
            lock_lwifooter = new FileStream(PathList.LwiFooterPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch { throw new LGLException(); }

      //srt
      try
      {
        //srtファイルはすでに削除されている場合も。ある
        //また、テキストが書き込まれて無いとCaption2Ass_PCR_pfによって削除される可能性がある。
        if (File.Exists(PathList.SrtPath)) 
        {
          var filesize = new FileInfo(PathList.SrtPath).Length;

          if (3 < filesize)  // -gt 3byte bom
            lock_srt = new FileStream(PathList.SrtPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
      }
      catch { throw new LGLException(); }
    }

    #endregion ファイルの移動禁止

    #region LogoGuillo同時起動数の制限

    private static Semaphore LGLSemaphore = null;

    /// <summary>
    /// LogoGuillo同時起動数の制限
    /// </summary>
    private static bool WaitForReady()
    {
      //同時起動数
      int multiRun = PathList.LogoGuillo_MultipleRun;
      if (multiRun <= 0) return false;

      /// <summary>
      /// セマフォを取得
      ///    LGLauncher同士での衝突回避
      /// </summary>
      /// <returns>
      ///   return semaphore;　→　セマフォ取得成功
      ///   return null; 　　　→　        取得失敗
      /// </returns>
      var GetSemaphore = new Func<Semaphore>(() =>
      {
        var semaphore = new Semaphore(multiRun, multiRun, "LGL-A8245043-3476");
        var waitBegin = DateTime.Now;

        while (semaphore.WaitOne(60 * 1000) == false)
        {
          //タイムアウト？
          if (30 < (DateTime.Now - waitBegin).TotalMinutes)
          {
            //プロセスが強制終了されているとセマフォが解放されず取得できない。
            //一定時間でタイムアウトさせる。
            //全てのLGLauncherが終了するとセマフォがリセットされ再取得できるようになる。
            Log.WriteLine(DateTime.Now.ToString("G"));
            Log.WriteLine("timeout of semaphore release");
            semaphore = null;
            break;
          }
        }

        return semaphore;
      });

      /// <summary>
      /// LogoGuilloのプロセス数が規定値未満か？
      ///   LogoGuillo単体、外部ランチャーとの衝突回避
      /// </summary>
      var LogoGuilloHasExited = new Func<bool, bool>((extraWait) =>
      {
        int PID = Process.GetCurrentProcess().Id;
        var rand = new Random(PID + DateTime.Now.Millisecond);

        var prclist = Process.GetProcessesByName("LogoGuillo");      //プロセス数確認  ”.exe”はつけない
        if (prclist.Count() < multiRun)
        {
          Thread.Sleep(rand.Next(5 * 1000, 10 * 1000));
          if (extraWait)
            Thread.Sleep(rand.Next(0 * 1000, 30 * 1000));

          prclist = Process.GetProcessesByName("LogoGuillo");        //再確認
          if (prclist.Count() < multiRun) return true;
        }

        return false;
      });

      /// <summary>
      /// システムがアイドル状態か？
      /// </summary>
      var SystemIsIdle = new Func<bool>(() =>
      {
        //SystemIdleMonitor.exeは起動時の負荷が高い
        string monitor_path = Path.Combine(PathList.LSystemDir, "SystemIdleMonitor.exe");
        string monitor_arg = "";
        if (File.Exists(monitor_path) == false) return true;

        var prc = new Process();
        prc.StartInfo.FileName = monitor_path;
        prc.StartInfo.Arguments = monitor_arg;
        prc.StartInfo.CreateNoWindow = true;
        prc.StartInfo.UseShellExecute = false;
        prc.Start();
        prc.WaitForExit(5 * 60 * 1000);

        return prc.HasExited && prc.ExitCode == 0;
      });

      //
      //WaitForReady
      LGLSemaphore = GetSemaphore();                       //セマフォを取得

      //タイムアウトなし
      while (true)
      {
        bool extraWait = (LGLSemaphore == null);           //セマフォが取得できない場合は待機時間を長くする。

        while (LogoGuilloHasExited(extraWait) == false)    //LogoGuilloプロセス数をチェック
          Thread.Sleep(20 * 1000);

        if (SystemIsIdle() == false)                       //システム負荷が高い、５分待機
        {
          Thread.Sleep(5 * 60 * 1000);
          continue;
        }

        if (LogoGuilloHasExited(extraWait) == false)       //LogoGuilloプロセス数を再チェック
          continue;

        //システムチェックＯＫ
        break;
      }

      return true;
    }

    #endregion LogoGuillo同時起動数の制限

    #region LogoGuillo実行

    /// <summary>
    /// LogoGuillo実行
    /// </summary>
    /// <param name="batPath">実行するパッチパス</param>
    /// <returns></returns>
    private static void LaunchLogoGuillo(string batPath)
    {
      if (File.Exists(batPath) == false)
        throw new LGLException();

      var prc = new Process();
      prc.StartInfo.FileName = batPath;
      prc.StartInfo.CreateNoWindow = true;
      prc.StartInfo.UseShellExecute = false;
      prc.Start();
      prc.WaitForExit();

      //終了コード
      if (prc.ExitCode == 0)
      {
        //正常終了
        return;
      }
      else if (prc.ExitCode == -9)
      {
        //ロゴ未検出
        throw new LGLException("★LogoGuillo ExitCode = " + prc.ExitCode + " :  ロゴ未検出");
      }
      else if (prc.ExitCode == -1)
      {
        //何らかのエラー
        throw new LGLException("★LogoGuillo ExitCode = " + prc.ExitCode + " :  エラー");
      }
      else
      {
        //強制終了すると ExitCode = 1
        throw new LGLException("★LogoGuillo ExitCode = " + prc.ExitCode + " :  Unknown code");
      }
    }

    //logoGuillo_v210_r1  readme_v210.txt
    // ◎終了コード
    // 0：正常終了
    //-9：ロゴ未検出
    //-1：何らかのエラー

    #endregion LogoGuillo実行

    #region 作業ファイル削除

    /// <summary>
    /// 分割処理の初回ならLWorkDir内のファイル削除
    /// </summary>
    private static void DeleteWorkItem_Beforehand()
    {
      //LWorkDir
      if (PathList.No == 1)
      {
        Delete_file(0.0, PathList.LWorkDir, "*.p?*.*");    //ワイルドカード指定可
      }
    }

    /// <summary>
    /// 終了処理でのファイル削除
    /// </summary>
    private static void DeleteWorkItem_Lastly()
    {
      //使い終わったファイルを削除？
      if (2 <= PathList.Mode_DeleteWorkItem)
      {
        //LWorkDir
        //  IsLast　→　全ての作業ファイル削除
        if (PathList.Mode_IsLast)
        {
          Delete_file(0.0, PathList.LWorkDir, "_" + PathList.TsShortName + "*");
          Delete_file(0.0, PathList.LWorkDir, PathList.TsShortName + "*");
        }
        //  通常　→　１つ前の作業ファイル削除
        else if (2 <= PathList.No)
          Delete_file(0.0, PathList.LWorkDir, PathList.WorkName_m1 + "*", "catframe.txt");
      }

      //古いファイル削除？
      if (1 <= PathList.Mode_DeleteWorkItem)
      {
        if (PathList.No == 1 || PathList.No == -1)
        {
          const double ndaysBefore = 2.0;
          //LTopWorkDir
          //サブフォルダ内も対象
          Delete_file(ndaysBefore, PathList.LTopWorkDir, "*.all.*");
          Delete_file(ndaysBefore, PathList.LTopWorkDir, "*.p?*.*");
          Delete_file(ndaysBefore, PathList.LTopWorkDir, "*.sys.*");
          Delete_emptydir(PathList.LTopWorkDir);

          //Windows Temp
          Delete_file(ndaysBefore, Path.GetTempPath(), "logoGuillo_*.avs");
          Delete_file(ndaysBefore, Path.GetTempPath(), "logoGuillo_*.txt");
          Delete_file(ndaysBefore, Path.GetTempPath(), "DGI_pf.tmp*");
        }
      }
    }

    /// <summary>
    /// 削除処理の実行部
    /// </summary>
    /// <param name="nDaysBefore">Ｎ日前のファイルを削除対象にする</param>
    /// <param name="directory">ファイルを探すフォルダ。　サブフォルダ内も対象</param>
    /// <param name="searchKey">ファイル名に含まれる文字。ワイルドカード可*</param>
    /// <param name="ignoreKey">除外するファイルに含まれる文字。ワイルドカード不可×</param>
    private static void Delete_file(double nDaysBefore, string directory, string searchKey, string ignoreKey = null)
    {
      if (Directory.Exists(directory) == false) return;
      Thread.Sleep(500);

      //ファイル取得
      var dirInfo = new DirectoryInfo(directory);
      var files = dirInfo.GetFiles(searchKey, SearchOption.AllDirectories);

      foreach (var onefile in files)
      {
        if (onefile.Exists == false) continue;
        if (ignoreKey != null && 0 <= onefile.Name.IndexOf(ignoreKey)) continue;

        //nDaysBeforeより前のファイル？
        bool over_creation = nDaysBefore < (DateTime.Now - onefile.CreationTime).TotalDays;
        bool over_lastwrite = nDaysBefore < (DateTime.Now - onefile.LastWriteTime).TotalDays;
        if (over_creation && over_lastwrite)
        {
          try { onefile.Delete(); }
          catch { /*ファイル使用中*/ }
        }
      }
    }

    /// <summary>
    /// 空フォルダ削除
    /// </summary>
    /// <param name="parent_directory">親フォルダを指定。空のサブフォルダが削除対象、親フォルダ自身は削除されない。</param>
    private static void Delete_emptydir(string parent_directory)
    {
      if (Directory.Exists(parent_directory) == false) return;

      var dirInfo = new DirectoryInfo(parent_directory);
      var dirs = dirInfo.GetDirectories("*", SearchOption.AllDirectories);

      foreach (var onedir in dirs)
      {
        if (onedir.Exists == false) continue;

        //空フォルダ？
        var files = onedir.GetFiles();
        if (files.Count() == 0)
        {
          try { onedir.Delete(); }
          catch { /*フォルダ使用中*/ }
        }
      }
    }

    #endregion 作業ファイル削除
  }//class

  #region コマンドライン

  /// <summary>
  /// コマンドライン
  /// </summary>
  internal class CommandLine
  {
    public int No { get; private set; }
    public string TsPath { get; private set; }
    public string D2vPath { get; private set; }
    public string LwiPath { get; private set; }
    public string SrtPath { get; private set; }
    public string Channel { get; private set; }
    public string Program { get; private set; }
    public bool IsLast { get; private set; }

    public CommandLine(string[] args)
    {
      Parse(args);
    }

    /// <summary>
    /// コマンドライン解析
    /// </summary>
    /// <param name="args">解析するコマンドライン</param>
    private void Parse(string[] args)
    {
      for (int i = 0; i < args.Count(); i++)
      {
        string key, sValue;
        bool canParse;
        int iValue;

        key = args[i].ToLower();
        sValue = (i + 1 < args.Count()) ? args[i + 1] : "";
        canParse = int.TryParse(sValue, out iValue);

        //  - / をはずす
        if (key.IndexOf("-") == 0 || key.IndexOf("/") == 0)
          key = key.Substring(1, key.Length - 1);
        else
          continue;

        //小文字で比較
        switch (key)
        {
          case "no":
            if (canParse)
              this.No = iValue;
            break;

          case "ts":
            this.TsPath = sValue;
            break;

          case "d2v":
            this.D2vPath = sValue;
            break;

          case "lwi":
            this.LwiPath = sValue;
            break;

          case "srt":
            this.SrtPath = sValue;
            break;

          case "ch":
          case "channel":
            this.Channel = sValue;
            break;

          case "program":
            this.Program = sValue;
            break;

          case "last":
            this.IsLast = true;
            break;

          default:
            break;
        }//switch
      }//for
    }//func

    /// <summary>
    /// コマンドライン一覧を出力する。
    /// </summary>
    /// <returns></returns>
    public new string ToString()
    {
      var sb = new StringBuilder();
      sb.AppendLine("    No      = " + No);
      sb.AppendLine("    TsPath  = " + TsPath);
      sb.AppendLine("    D2vPath = " + D2vPath);
      sb.AppendLine("    LwiPath = " + LwiPath);
      sb.AppendLine("    SrtPath = " + SrtPath);
      sb.AppendLine("    Channel = " + Channel);
      sb.AppendLine("    Program = " + Program);
      sb.AppendLine("    IsLast  = " + IsLast);
      return sb.ToString();
    }
  }//class

  #endregion コマンドライン
}//namespace