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
  ///   LGLauncher実行中にts,d2v,lwi,srtファイルを移動できないようにロック
  /// </summary>
  static class ProhibitFileMove_LGL
  {
    private static List<FileStream> LockItems;

    /// <summary>
    /// 移動禁止
    /// </summary>
    public static void Lock()
    {
      LockItems = LockItems ?? new List<FileStream>();

      //ts
      try
      {
        var ts = new FileStream(PathList.TsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        LockItems.Add(ts);
      }
      catch { throw new LGLException("cant lock ts file"); }

      //d2v
      if (PathList.IsD2v)
        try
        {
          var d2v = new FileStream(PathList.D2vPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
          LockItems.Add(d2v);
        }
        catch { throw new LGLException("cant lock d2v file"); }

      //lwi
      if (PathList.IsLwi)
        try
        {
          var lwi = new FileStream(PathList.LwiPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
          LockItems.Add(lwi);

          if (File.Exists(PathList.LwiFooterPath))  //lwifooterファイルは無いときもある
          {
            var footer = new FileStream(PathList.LwiFooterPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            LockItems.Add(footer);
          }
        }
        catch { throw new LGLException("cant lock lwi file"); }

      //srt
      try
      {
        //テキストが書き込まれている場合のみロック
        if (File.Exists(PathList.SrtPath))
        {
          var size = new FileInfo(PathList.SrtPath).Length;
          if (3 < size)  // -gt 3byte bom
          {
            var srt = new FileStream(PathList.SrtPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            LockItems.Add(srt);
          }
        }
      }
      catch { /* do nothing */ }
    }


    /// <summary>
    /// 移動禁止を解除
    /// </summary>
    public static void Unlock()
    {
      foreach (var fstream in LockItems)
        fstream.Close();
      LockItems = new List<FileStream>();
    }

  }
}
