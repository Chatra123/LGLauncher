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
  /// LogoGuilloを実行できるまで待機
  /// </summary>
  class WaitForSystemReady
  {
    private IMutexControl mutexControl;

    /// <summary>
    /// destructor
    /// </summary>
    ~WaitForSystemReady()
    {
      Release();
    }

    /// <summary>
    /// Mutex解放
    /// </summary>
    public void Release()
    {
      if (mutexControl != null)
      {
        mutexControl.Release();
        mutexControl = null;
      }
    }

    /// <summary>
    /// システム確認　＆　Semaphore取得
    /// </summary>
    public bool GetReady(IEnumerable<string> targetNames,
                          int multiRun = 1,                //Mutexは常に１
                          bool check_SysIdle = true)
    {
      if (multiRun <= 0) return false;

      //targetNamesから.exe除去
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
      /// targetのプロセス数が少ないか？
      ///   target単体、外部ランチャーとの衝突回避
      /// </summary>
      var TargetHasExited = new Func<int, bool>((max_prc) =>
     {
        //プロセス数確認  ".exe"はつけない
        int sum = 0;
       foreach (var target in targetNames)
       {
         var prclist = Process.GetProcessesByName(target);
         sum += prclist.Count();
       }
       return sum < max_prc;
     });


      /// <summary>
      /// システムがアイドル状態か？
      /// </summary>
      var SystemIsIdle = new Func<bool>(() =>
      {
        //SystemIdleMonitor.exeは起動の負荷が少し高い
        string path = PathList.SystemIdleMonitor;    //LGL
        //string path = "disable launch";            //V2P

        //ファイルが無ければ return true;
        if (File.Exists(path) == false) return true;

        var prc = new Process();
        prc.StartInfo.FileName = path;
        prc.StartInfo.Arguments = "";
        prc.StartInfo.CreateNoWindow = true;
        prc.StartInfo.UseShellExecute = false;
        prc.Start();
        prc.WaitForExit(2 * 60 * 1000);
        return prc.HasExited && prc.ExitCode == 0;
      });


      //Mutex取得        LGL
      //  LGLauncher同士での衝突回避
      //  Mutexが取得できないときは待機時間を追加
      //bool addtionalWait;
      //{
      //  const string MutexName = "LGL-41CDEAC6-6717";      //LGL
      //  mutexControl = new MutexControl();
      //  mutexControl.Initlize(MutexName);
      //  mutexControl.Get();
      //  addtionalWait = mutexControl.HasControl == false;
      //}
      //Semaphore取得    LGL V2P
      bool additionalWait;
      {
        const string MutexName = "LGL-41CDEAC6-6717";  //LGL
        //const string MutexName = "V2P-33A2FE1F-0891";    //V2P
        mutexControl = new SemaphoreControl();
        mutexControl.Initilize(MutexName, multiRun);
        mutexControl.Get();
        additionalWait = mutexControl.HasControl == false;
      }


      //
      //システムチェック
      //
      var rand = new Random(DateTime.Now.Millisecond + Process.GetCurrentProcess().Id);
      while (true)
      {
        //プロセス数
        while (TargetHasExited(multiRun) == false)
        {
          Thread.Sleep(1 * 60 * 1000);                               // 1 min
        }

        //ＣＰＵ使用率
        if (check_SysIdle && SystemIsIdle() == false)
        {
          Thread.Sleep(rand.Next(3 * 60 * 1000, 5 * 60 * 1000));     // 3  to  5 min
          continue;
        }

        //Mutexが取得できないときは待機時間を追加
        if (additionalWait)
        {
          Thread.Sleep(rand.Next(0 * 1000, 3 * 60 * 1000));          // 0  to  3 min
        }

        //プロセス数  再チェック
        if (TargetHasExited(multiRun) == false)
          continue;

        //チェックＯＫ
        return true;
      }

    }

  }

}
