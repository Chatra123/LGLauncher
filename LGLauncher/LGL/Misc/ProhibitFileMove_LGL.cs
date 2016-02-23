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
  static class ProhibitFileMove_LGL
  {
    private static FileStream lock_ts, lock_d2v, lock_lwi, lock_lwifooter, lock_srt;     //プロセス終了でロック解放

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
      catch { throw new LGLException("cant lock ts file"); }

      //d2v
      if (PathList.Avs_iPlugin == PluginType.D2v)
        try
        {
          lock_d2v = new FileStream(PathList.D2vPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch { throw new LGLException("cant lock d2v file"); }

      //lwi
      if (PathList.Avs_iPlugin == PluginType.Lwi)
        try
        {
          lock_lwi = new FileStream(PathList.LwiPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

          if (File.Exists(PathList.LwiFooterPath))  //lwifooterファイルが無いときもある
            lock_lwifooter = new FileStream(PathList.LwiFooterPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch { throw new LGLException("cant lock lwi file"); }

      //srt
      try
      {
        //srtファイルはすでに削除されている可能性がある。
        //　テキストが書き込まれて無いとCaption2Ass_PCR_pfによって削除されるのでbomサイズで判断
        if (File.Exists(PathList.SrtPath))
        {
          var filesize = new FileInfo(PathList.SrtPath).Length;

          if (3 < filesize)  // -gt 3byte bom
            lock_srt = new FileStream(PathList.SrtPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
      }
      catch { /* do nothing */ } //エラーがでても無視
    }
  }

}
