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
  static class BatLuncher
  {
    /// <summary>
    /// LogoDetector実行
    /// </summary>
    public static void Launch(string batPath, int timeout_ms = -1)
    {
      //LogoGuilloでエラーが発生しても一度だけリトライする
      for (int retry = 1; retry <= 2; retry++)
      {
        try
        {
          var startTime = DateTime.Now;
          Launch_Detector(batPath, timeout_ms);

          //Windows Sleepが原因のタイムアウト
          //  timeout_msから１０秒以上経過している。
          var elapse_ms = (DateTime.Now - startTime).TotalMilliseconds;
          if (timeout_ms + 10 * 1000 < elapse_ms)
          {
            Log.WriteLine("bat timeout by windows sleep");
            retry--;
            continue;
          }

          break;//正常終了
        }
        catch (LGLException e)
        {
          if (retry == 2)
          {
            throw e;
          }
          else
          {
            Log.WriteLine();
            Log.WriteLine(e.Message);
            Log.WriteLine("Retry BatLuncher");
            continue;
          }
        }
      }
    }


    /// <summary>
    /// LogoDetector実行
    /// </summary>
    private static void Launch_Detector(string batPath, int timeout_ms)
    {
      if (File.Exists(batPath) == false)
        throw new LGLException("not exist detector bat");

      int exitCode = 0;
      bool hasExisted = false;
      {
        var prc = new Process();
        prc.StartInfo.FileName = batPath;
        prc.StartInfo.CreateNoWindow = true;
        prc.StartInfo.UseShellExecute = false;
        prc.Start();
        prc.WaitForExit(timeout_ms);

        hasExisted = prc.HasExited;
        exitCode = prc.ExitCode;
        if (hasExisted == false) prc.Kill();
        prc.Close();
      }

      //正常終了
      if (exitCode == 0)
      {
        return;
      }

      //タイムアウト
      if (hasExisted == false)
      {
        Log.WriteLine("bat timeout");
        return;
      }

      //異常終了
      if (PathList.Detector == LogoDetector.Join_Logo_Scp)
      {
        throw new LGLException("★ ExitCode = " + exitCode + "");
      }
      else if (PathList.Detector == LogoDetector.LogoGuillo)
      {
        if (exitCode == -9)
        {
          Log.WriteLine("★ ExitCode = " + exitCode + " :  ロゴ未検出");
          return;
        }
        else if (exitCode == -1)
        {
          throw new LGLException("★ ExitCode = " + exitCode + " :  エラー");
        }
        else
        {
          //強制終了させると ExitCode = 1
          throw new LGLException("★ ExitCode = " + exitCode + " :  Unknown code");
        }
      }
    }
    //logoGuillo_v210_r1  readme_v210.txt
    // ◎終了コード
    // 0：正常終了
    //-9：ロゴ未検出
    //-1：何らかのエラー





  }

}
