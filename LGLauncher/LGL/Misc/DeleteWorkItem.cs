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
  /// 作業ファイル削除
  /// </summary>
  static class DeleteWorkItem
  {
    /// <summary>
    /// 処理の初回ならLWorkDir内のファイル削除
    /// </summary>
    public static void Clean_Beforehand()
    {
      //LWorkDir
      if (PathList.Is1stPart)
      {
        Delete_file(0.0, PathList.LWorkDir, "*.p?*.*");                                //ワイルドカード指定可
        Delete_file(0.0, PathList.LWorkDir, PathList.TsShortName + ".frame.cat.txt");  //前回までの合成フレーム
      }
      else if (PathList.IsAll)
      {
        Delete_file(0.0, PathList.LWorkDir, "*.all.*");
        Delete_file(0.0, PathList.LWorkDir, PathList.TsShortName + ".frame.cat.txt");
      }
    }


    /// <summary>
    /// 例外発生時に作成済みのavsファイル削除
    /// </summary>
    public static void Clean_OnError()
    {
      // *.p3.2000__3000.avs 削除
      Delete_file(0.0, PathList.LWorkDir, PathList.WorkName + ".*__*.avs");
      Delete_file(0.0, PathList.LWorkDir, PathList.WorkName + ".*__*.vpy");
    }


    /// <summary>
    /// 終了処理でのファイル削除
    /// </summary>
    public static void Clean_Lastly()
    {
      //使い終わったファイルを削除
      if (3 <= PathList.Mode_DeleteWorkItem)
      {
        //LWorkDir
        //  IsLast 　　　　　→　　全ての作業ファイル削除
        if (PathList.IsLastPart)
        {
          Delete_file(0.0, PathList.LWorkDir, PathList.TsShortName + "*");
          Delete_file(0.0, PathList.LWorkDir, "_" + PathList.TsShortName + "*.sys.*", ".log");
        }
        //  2 <= PartNo  　　→　　１つ前の作業ファイル削除
        else if (2 <= PathList.PartNo)
        {
          //今回作成したTsName.p3.20000__30000.avsは、
          //次回のLGLauncherが使用するので削除しない。
          Delete_file(0.0, PathList.LWorkDir, PathList.WorkName_prv + "*");
        }
      }

      //ファイルサイズが大きい d2v, lwi, srt削除
      if (2 <= PathList.Mode_DeleteWorkItem)
      {
        Delete_file(0.0, PathList.LWorkDir, PathList.TsShortName + "*.d2v");
        Delete_file(0.0, PathList.LWorkDir, PathList.TsShortName + "*.lwi");
        Delete_file(0.0, PathList.LWorkDir, PathList.TsShortName + "*.srt");
      }

      //古いファイル削除？
      if (1 <= PathList.Mode_DeleteWorkItem)
      {
        if (PathList.Is1stPart || PathList.IsAll)
        {
          const double nDaysBefore = 2.0;
          //LTopWorkDir         サブフォルダ内も対象
          Delete_file(nDaysBefore, PathList.LTopWorkDir, "*.frame.cat.txt*");
          Delete_file(nDaysBefore, PathList.LTopWorkDir, "*.all.*");
          Delete_file(nDaysBefore, PathList.LTopWorkDir, "*.p?*.*");
          Delete_file(nDaysBefore, PathList.LTopWorkDir, "*.sys.*");
          Delete_emptydir(PathList.LTopWorkDir);
          //Windows Temp
          Delete_file(nDaysBefore, Path.GetTempPath(), "logoGuillo_*.avs");
          Delete_file(nDaysBefore, Path.GetTempPath(), "logoGuillo_*.txt");
          Delete_file(nDaysBefore, Path.GetTempPath(), "DGI_pf_tmp_*_*");
        }
      }
    }


    /// <summary>
    /// 削除処理の実行部
    /// </summary>
    /// <param name="nDaysBefore">Ｎ日前のファイルを削除対象にする</param>
    /// <param name="directory">ファイルを探すフォルダ。　サブフォルダ内も対象</param>
    /// <param name="searchKey">ファイル名に含まれる文字。ワイルドカード可 * </param>
    /// <param name="ignoreKey">除外するファイルに含まれる文字。ワイルドカード不可 × </param>
    private static void Delete_file(double nDaysBefore, string directory,
                                    string searchKey, string ignoreKey = null)
    {
      if (Directory.Exists(directory) == false) return;
      Thread.Sleep(500);

      //ファイル取得
      var files = new FileInfo[] { };
      try
      {
        var dirInfo = new DirectoryInfo(directory);
        files = dirInfo.GetFiles(searchKey, SearchOption.AllDirectories);
      }
      catch (System.UnauthorizedAccessException)
      {
        /* Java  jre-8u73-windows-i586.exeを実行してインストール用のウィンドウを表示させると、
         * Tempフォルダにjds262768703.tmpがReadOnlyで作成される。
         * 
         * アクセス権限の無いファイルが含まれているフォルダに
         * files = dirInfo.GetFiles();
         * を実行すると System.UnauthorizedAccessExceptionが発生する。
         */
        return;
      }

      foreach (var onefile in files)
      {
        if (onefile.Exists == false) continue;
        if (ignoreKey != null && 0 <= onefile.Name.IndexOf(ignoreKey)) continue;

        //nDaysBeforeより前のファイル？
        bool over_creation = nDaysBefore < (DateTime.Now - onefile.CreationTime).TotalDays;
        bool over_lastwrite = nDaysBefore < (DateTime.Now - onefile.LastWriteTime).TotalDays;
        if (over_creation && over_lastwrite)
        {
          try { onefile.Delete(); }
          catch { /*ファイル使用中*/ }
        }
      }
    }


    /// <summary>
    /// 空フォルダ削除
    /// </summary>
    /// <param name="parent_directory">親フォルダを指定。空のサブフォルダが削除対象、親フォルダ自身は削除されない。</param>
    private static void Delete_emptydir(string parent_directory)
    {
      if (Directory.Exists(parent_directory) == false) return;

      var dirs = new DirectoryInfo[] { };
      try
      {
        var dirInfo = new DirectoryInfo(parent_directory);
        dirs = dirInfo.GetDirectories("*", SearchOption.AllDirectories);
      }
      catch (System.UnauthorizedAccessException)
      {
        return;
      }

      foreach (var onedir in dirs)
      {
        if (onedir.Exists == false) continue;

        //空フォルダ？
        var files = onedir.GetFiles();
        if (files.Count() == 0)
        {
          try { onedir.Delete(); }
          catch { /*フォルダ使用中*/ }
        }
      }
    }




  }
}
