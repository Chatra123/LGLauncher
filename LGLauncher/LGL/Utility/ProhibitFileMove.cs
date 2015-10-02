using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace LGLauncher
{
  /// <summary>
  /// ファイルの移動禁止
  /// </summary>
  static class ProhibitFileMove
  {
    private static FileStream lock_ts, lock_d2v, lock_lwi, lock_lwifooter, lock_srt;             //プロセス終了でロック解放

    /// <summary>
    /// ファイルを移動禁止にする。
    /// </summary>
    public static void Lock()
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
        //srtファイルはすでに削除されている場合がある。
        //また、テキストが書き込まれてないとCaption2Ass_PCR_pfによって削除される可能性がある。
        if (File.Exists(PathList.SrtPath))
        {
          var filesize = new FileInfo(PathList.SrtPath).Length;

          if (3 < filesize)  // -gt 3byte bom
            lock_srt = new FileStream(PathList.SrtPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
      }
      catch { throw new LGLException(); }
    }
  }

}
