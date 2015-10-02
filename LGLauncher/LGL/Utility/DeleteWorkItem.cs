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
    /// 分割処理の初回ならLWorkDir内のファイル削除
    /// </summary>
    public static void Clean_Beforehand()
    {
      //LWorkDir
      if (PathList.No == 1)
      {
        Delete_file(0.0, PathList.LWorkDir, "*.p?*.*");    //ワイルドカード指定可
      }
    }

    /// <summary>
    /// 終了処理でのファイル削除
    /// </summary>
    public static void Clean_Lastly()
    {
      //使い終わったファイルを削除？
      if (2 <= PathList.Mode_DeleteWorkItem)
      {
        //LWorkDir
        //  IsLast　→　全ての作業ファイル削除
        if (PathList.Mode_IsLast)
        {
          Delete_file(0.0, PathList.LWorkDir, "_" + PathList.TsShortName + "*");
          Delete_file(0.0, PathList.LWorkDir, PathList.TsShortName + "*");
        }
        //  通常　→　１つ前の作業ファイル削除
        else if (2 <= PathList.No)
          Delete_file(0.0, PathList.LWorkDir, PathList.WorkName_m1 + "*", "catframe.txt");
      }

      //古いファイル削除？
      if (1 <= PathList.Mode_DeleteWorkItem)
      {
        if (PathList.No == 1 || PathList.No == -1)
        {
          const double ndaysBefore = 2.0;
          //LTopWorkDir
          //サブフォルダ内も対象
          Delete_file(ndaysBefore, PathList.LTopWorkDir, "*.all.*");
          Delete_file(ndaysBefore, PathList.LTopWorkDir, "*.p?*.*");
          Delete_file(ndaysBefore, PathList.LTopWorkDir, "*.sys.*");
          Delete_emptydir(PathList.LTopWorkDir);

          //Windows Temp
          Delete_file(ndaysBefore, Path.GetTempPath(), "logoGuillo_*.avs");
          Delete_file(ndaysBefore, Path.GetTempPath(), "logoGuillo_*.txt");
          Delete_file(ndaysBefore, Path.GetTempPath(), "DGI_pf.tmp*");
        }
      }
    }

    /// <summary>
    /// 削除処理の実行部
    /// </summary>
    /// <param name="nDaysBefore">Ｎ日前のファイルを削除対象にする</param>
    /// <param name="directory">ファイルを探すフォルダ。　サブフォルダ内も対象</param>
    /// <param name="searchKey">ファイル名に含まれる文字。ワイルドカード可*</param>
    /// <param name="ignoreKey">除外するファイルに含まれる文字。ワイルドカード不可×</param>
    private static void Delete_file(double nDaysBefore, string directory, string searchKey, string ignoreKey = null)
    {
      if (Directory.Exists(directory) == false) return;
      Thread.Sleep(500);

      //ファイル取得
      var dirInfo = new DirectoryInfo(directory);
      var files = dirInfo.GetFiles(searchKey, SearchOption.AllDirectories);

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

      var dirInfo = new DirectoryInfo(parent_directory);
      var dirs = dirInfo.GetDirectories("*", SearchOption.AllDirectories);

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
