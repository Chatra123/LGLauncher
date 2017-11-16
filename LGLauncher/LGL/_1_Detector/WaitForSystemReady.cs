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
    public bool GetReady(IEnumerable<string> targetNames,
                          int multiRun = 1,
                          bool check_SysIdle = true)
    {
      if (multiRun <= 0) return false;
      //.exe除去
      targetNames = targetNames.Select(
                      (prcname) =>
                      {
                        prcname = prcname.Trim();
                        bool hasExe = (Path.GetExtension(prcname).ToLower() == ".exe");
                        prcname = (hasExe) ? Path.GetFileNameWithoutExtension(prcname) : prcname;
                        return prcname;
                      })
                     .ToList();

      /// <summary>
      /// プロセス数？
      ///   LG単体、外部ランチャーとの衝突回避
      /// </summary>
      var TargetHasExited = new Func<int, bool>((max_prc) =>
      {
        //プロセス数確認  ".exe"はつけない
        int sum = 0;
        foreach (var target in targetNames)
        {
          var prc = Process.GetProcessesByName(target);
          sum += prc.Count();
        }
        return sum < max_prc;
      });


      /// <summary>
      /// ＣＰＵ使用率？
      /// </summary>
      var SystemIsIdle = new Func<bool>(() =>
      {
        //SystemIdleMonitor.exeは起動の負荷が少し高い
        string path = PathList.SystemIdleMonitor;    //LGL
        //string path = "disable launch";            //V2P

        //ファイルが無ければアイドル状態とみなす。
        if (File.Exists(path) == false) return true;
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
        const string name = "LGL-41CDEAC6-6717";      //LGL
        //const string name = "V2P-33A2FE1F-0891";      //V2P
        semaphore = new Semaphore(multiRun, multiRun, name);
        if (semaphore.WaitOne(TimeSpan.FromMinutes(timeout_min)))
        {
          hasSemaphore = true;
        }
        else
        {
          //プロセスが強制終了されているとセマフォが解放されず取得できない。
          //全ての待機プロセスが終了するとセマフォがリセットされ再取得できるようになる。
          Log.WriteLine("  timeout of waiting semaphore");      //LGL
          hasSemaphore = false;
        }
      }


      //システムチェック
      var rand = new Random(DateTime.Now.Millisecond + Process.GetCurrentProcess().Id);
      while (true)
      {
        //プロセス数
        while (TargetHasExited(multiRun) == false)
        {
          Thread.Sleep(1 * 60 * 1000);
        }
        //ＣＰＵ使用率
        if (check_SysIdle && SystemIsIdle() == false)
        {
          Thread.Sleep(rand.Next(3 * 60 * 1000, 5 * 60 * 1000));
          continue;
        }
        if (hasSemaphore == false)
        {
          Thread.Sleep(rand.Next(0 * 60 * 1000, 3 * 60 * 1000));
        }
        //プロセス数  再チェック
        if (TargetHasExited(multiRun) == false)
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
