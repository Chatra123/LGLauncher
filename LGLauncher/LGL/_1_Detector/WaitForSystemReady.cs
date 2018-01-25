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
  /// <summary>
  /// LogoGuilloが実行可能になるまで待機
  /// </summary>
  class WaitForSystemReady
  {
    bool hasSemaphore;
    Semaphore semaphore;

    /// <summary>
    /// システム確認　＆　Semaphore取得
    /// </summary>
    public bool GetReady(IEnumerable<string> prcNames, int multiRun = 1)
    {
      if (multiRun <= 0) return false;
      //.exe除去
      prcNames = prcNames.Select(
        (name) => { return Path.GetFileNameWithoutExtension(name.Trim()); }).ToList();

      /// <summary>
      /// プロセス数？
      ///   別のLogoGuilloとの衝突回避
      /// </summary>
      var ProcesstHasExited = new Func<bool>(() =>
     {
       //プロセス数確認  ".exe"はつけない
       int sum = 0;
       foreach (var name in prcNames)
       {
         var prc = Process.GetProcessesByName(name);
         sum += prc.Count();
       }
       return sum < multiRun;
     });


      /// <summary>
      /// ＣＰＵ使用率？
      /// </summary>
      var SystemIsIdle = new Func<bool>(() =>
     {
       //ファイルが無ければアイドル状態とみなす。
       string path = PathList.SystemIdleMonitor;
       if (File.Exists(path) == false) return true;
       //SystemIdleMonitor.exeは起動の負荷が少し高い
       var prc = new Process();
       prc.StartInfo.FileName = path;
       prc.StartInfo.Arguments = "";
       prc.StartInfo.CreateNoWindow = true;
       prc.StartInfo.UseShellExecute = false;
       prc.Start();
       prc.WaitForExit(3 * 60 * 1000);
       return prc.HasExited && prc.ExitCode == 0;
     });


      //Semaphore取得
      //  LGLauncher同士での衝突回避
      //  取得できなければ待機時間を追加
      hasSemaphore = false;
      {
        const int timeout_min = 120;
        const string name = "LGL-41CDEAC6-6717";
        semaphore = new Semaphore(multiRun, multiRun, name);
        if (semaphore.WaitOne(TimeSpan.FromMinutes(timeout_min)))
        {
          hasSemaphore = true;
        }
        else
        {
          //LGLauncherが強制終了されてるとセマフォが解放されず取得できない。
          //全ての待機プロセスが終了するとセマフォがリセットされ再取得できるようになる。
          Log.WriteLine("  timeout of waiting semaphore");
          hasSemaphore = false;
        }
      }


      //システムチェック
      var rand = new Random(DateTime.Now.Millisecond + Process.GetCurrentProcess().Id);
      while (true)
      {
        //プロセス数
        while (ProcesstHasExited() == false)
        {
          Thread.Sleep(1 * 60 * 1000);
        }
        //ＣＰＵ使用率
        if (SystemIsIdle() == false)
        {
          Thread.Sleep(rand.Next(3 * 60 * 1000, 7 * 60 * 1000));
          continue;
        }
        if (hasSemaphore == false)
        {
          Thread.Sleep(rand.Next(0 * 60 * 1000, 3 * 60 * 1000));
        }
        //プロセス数  再チェック
        if (ProcesstHasExited() == false)
          continue;
        //チェックＯＫ
        return true;
      }
    }//func


    /// <summary>
    /// Semaphore解放
    /// </summary>
    public void Release()
    {
      if (hasSemaphore)
      {
        semaphore.Release();
        hasSemaphore = false;
      }
    }
    ~WaitForSystemReady()
    {
      Release();
    }


  }//class
}//namespace
