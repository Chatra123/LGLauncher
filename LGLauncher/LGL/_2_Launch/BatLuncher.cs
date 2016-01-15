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
      ////デバッグ用
      //// 1/x の確立で例外を発生させる
      //if (PathList.IsLastPart == false)
      //  if (DateTime.Now.Second % 4 == 0)
      //  {
      //    throw new LGLException("fake error:  Launch_Detector1");
      //  }

      //if (PathList.PartALL == false)
      //  if (PathList.IsLastPart && 0 < timeout_ms)
      //  {
      //    throw new LGLException("fake error:  Launch_Detector2");
      //  }


      try
      {
        Launch_Detector(batPath, timeout_ms);

        //成功
        return;
      }
      catch (LGLException e)
      {
        //失敗
        //エラーが発生したら、ログに書き込んでリトライ
        Log.WriteLine();
        Log.WriteLine(e.Message);
        Log.WriteLine("Retry detector");
      }

      //リトライ
      //ここで発生する LGLExceptionは呼び出し元でキャッチ
      //エラーとして処理する。
      Launch_Detector(batPath, timeout_ms);

    }



    /// <summary>
    /// LogoDetector実行
    /// </summary>
    private static void Launch_Detector(string batPath, int timeout_ms)
    {
      if (File.Exists(batPath) == false)
        throw new LGLException("not exist detector bat");

      var prc = new Process();
      prc.StartInfo.FileName = batPath;
      prc.StartInfo.CreateNoWindow = true;
      prc.StartInfo.UseShellExecute = false;
      prc.Start();
      prc.WaitForExit(timeout_ms);


      if (prc.HasExited == false)
      {
        throw new LGLException("bat timeout");
      }

      //正常終了
      if (prc.ExitCode == 0)
      {
        return;
      }

      //異常終了
      if (PathList.Detector == LogoDetector.Join_Logo_Scp)
      {
        //Join_Logo_Scp
        throw new LGLException("★ ExitCode = " + prc.ExitCode + "");
      }
      else if (PathList.Detector == LogoDetector.LogoGuillo)
      {
        //LogoGuillo
        if (prc.ExitCode == -9)
        {
          //ロゴ未検出
          Log.WriteLine("★ ExitCode = " + prc.ExitCode + " :  ロゴ未検出");
        }
        else if (prc.ExitCode == -1)
        {
          //何らかのエラー
          throw new LGLException("★ ExitCode = " + prc.ExitCode + " :  エラー");
        }
        else
        {
          //強制終了すると ExitCode = 1
          throw new LGLException("★ ExitCode = " + prc.ExitCode + " :  Unknown code");
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
