using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;



namespace LGLauncher.Bat
{
  static class BatLauncher
  {
    /// <summary>
    /// Bat実行　タイムアウト無し
    /// </summary>
    public static void Launch(string batPath)
    {
      bool timeout_byWin;
      Launch(batPath, out timeout_byWin, -1);
    }

    /// <summary>
    /// Bat実行　タイムアウト検出
    /// </summary>
    public static void Launch(string batPath, out bool timeout_byWin, int timeout_ms)
    {
      if (File.Exists(batPath) == false)
        throw new LGLException("not exist detector bat");

      var startTime = DateTime.Now;
      Launch_core(batPath, timeout_ms);

      var elapse_ms = (DateTime.Now - startTime).TotalMilliseconds;
      if (timeout_ms + 5 * 1000 < elapse_ms)
      {
        //Windows Sleepが原因のタイムアウト
        Log.WriteLine("    timeout by windows sleep");
        timeout_byWin = true;
        return;
      }
      else
      {
        //正常終了
        timeout_byWin = false;
        return;
      }
    }


    /// <summary>
    /// Bat実行　　プロセス実行部
    /// </summary>
    private static void Launch_core(string batPath, int timeout_ms)
    {
      int exitCode;
      bool hasExisted;
      {
        var prc = new Process();
        prc.StartInfo.FileName = batPath;
        prc.StartInfo.CreateNoWindow = true;
        prc.StartInfo.UseShellExecute = false;
        prc.Start();
        prc.WaitForExit(timeout_ms);

        hasExisted = prc.HasExited;
        exitCode = hasExisted ? prc.ExitCode : -100;
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
      if (PathList.IsJLS)
      {
        throw new LGLException("★ ExitCode = " + exitCode + "");
      }
      else if (PathList.IsLG)
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
