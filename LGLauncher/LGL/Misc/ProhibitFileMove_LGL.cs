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
  class ProhibitFileMove_LGL
  {
    private static List<FileStream> lock_items;

    /// <summary>
    /// ファイルの移動禁止
    /// </summary>
    public static void Lock()
    {
      lock_items = lock_items ?? new List<FileStream>();

      //ts
      try
      {
        var ts = new FileStream(PathList.TsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        lock_items.Add(ts);
      }
      catch { throw new LGLException("cant lock ts file"); }

      //d2v
      if (PathList.InputPlugin == PluginType.D2v)
        try
        {
          var d2v = new FileStream(PathList.D2vPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
          lock_items.Add(d2v);
        }
        catch { throw new LGLException("cant lock d2v file"); }

      //lwi
      if (PathList.InputPlugin == PluginType.Lwi)
        try
        {
          var lwi = new FileStream(PathList.LwiPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
          lock_items.Add(lwi);

          if (File.Exists(PathList.LwiFooterPath))  //lwifooterファイルは無いときもある
          {
            var footer = new FileStream(PathList.LwiFooterPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            lock_items.Add(footer);
          }
        }
        catch { throw new LGLException("cant lock lwi file"); }

      //srt
      try
      {
        //srtはテキストが書き込まれて無いとCaption2Ass_PCR_pfによって削除される可能性がある。
        if (File.Exists(PathList.SrtPath))
        {
          var filesize = new FileInfo(PathList.SrtPath).Length;
          if (3 < filesize)  // -gt 3byte bom
          {
            var srt = new FileStream(PathList.SrtPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            lock_items.Add(srt);
          }
        }
      }
      catch { /* do nothing */ }
    }


    /// <summary>
    /// ファイルの移動禁止を解除
    /// </summary>
    public static void Unlock()
    {
      foreach (var item in lock_items)
        item.Close();
    }

  }
}
