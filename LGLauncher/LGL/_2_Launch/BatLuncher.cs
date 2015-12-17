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
    public static void Launch(string batPath)
    {
      if (File.Exists(batPath) == false)
        throw new LGLException();

      var prc = new Process();
      prc.StartInfo.FileName = batPath;
      prc.StartInfo.CreateNoWindow = true;
      prc.StartInfo.UseShellExecute = false;
      prc.Start();
      prc.WaitForExit();

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
          throw new LGLException("★ ExitCode = " + prc.ExitCode + " :  ロゴ未検出");
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
