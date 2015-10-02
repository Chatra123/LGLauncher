using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace LGLauncher
{
  static class SystemChecker
  {
    private static Semaphore LGLSemaphore;

    /// <summary>
    /// LogoGuillo同時起動数の制限
    /// </summary>
    public static bool GetReady()
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
            Log.WriteLine("timeout of wait for semaphore");
            semaphore = null;
            break;
          }
        }

        return semaphore;
      });

      /// <summary>
      /// LogoGuilloのプロセス数がmultiRun未満か？
      ///   LogoGuillo単体、外部ランチャーとの衝突回避
      /// </summary>
      var LogoGuilloHasExited = new Func<bool, bool>((extraWait) =>
      {
        int PID = Process.GetCurrentProcess().Id;
        var rand = new Random(PID + DateTime.Now.Millisecond);

        var prclist = Process.GetProcessesByName("LogoGuillo");      //プロセス数確認  ".exe"はつけない
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
        //SystemIdleMonitor.exeは起動時の負荷が少し高い
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
      //システムチェック　準備ができるまで待機
      //

      //セマフォが取得できない場合は待機時間を長くする。
      LGLSemaphore = GetSemaphore();
      bool addWait = (LGLSemaphore == null);

      //タイムアウトなし
      while (true)
      {
        //プロセス数チェック
        while (LogoGuilloHasExited(addWait) == false)
          Thread.Sleep(20 * 1000);

        //ＣＰＵ使用率チェック
        if (SystemIsIdle() == false)
        {
          Thread.Sleep(5 * 60 * 1000);
          continue;
        }

        //プロセス数を再チェック
        if (LogoGuilloHasExited(addWait) == false)
          continue;

        //システムチェックＯＫ
        return true;
      }

    }//func


    //セマフォ解放
    public static void ReleaseSemaphore()
    {
      if (LGLSemaphore != null)
        LGLSemaphore.Release();
    }
  }

}
