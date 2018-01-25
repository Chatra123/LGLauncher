using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;


namespace LGLauncher
{
  using cleaner = LGLauncher.FileCleaner;

  /// <summary>
  /// 作業ファイル削除
  /// </summary>
  static class CleanWorkItem
  {
    /// <summary>
    /// LWorkDir内のファイル削除
    ///   以前の処理でファイルが残っていたら削除
    /// </summary>
    public static void Clean_Beforehand()
    {
      //LWorkDir
      if (PathList.Is1stPart)
      {
        cleaner.OldFile(0.0, PathList.LWorkDir, "*.p?*.*");
        cleaner.OldFile(0.0, PathList.LWorkDir, "*.frame.cat.txt");
        cleaner.OldFile(0.0, PathList.LWorkDir, "*.jls.*");
        cleaner.OldFile(0.0, PathList.LWorkDir, "*.d2v");
        cleaner.OldFile(0.0, PathList.LWorkDir, "*.lwi");
      }
    }


    /// <summary>
    /// 終了処理でのファイル削除
    /// </summary>
    public static void Clean_Lastly()
    {
      // Mode = 2    使用済みのファイル削除
      if (2 <= PathList.Mode_CleanWorkItem)
        if (PathList.IsLastPart)
        {
          //LWorkDir
          cleaner.OldFile(0.0, PathList.LWorkDir, "*" + PathList.TsShortName + "*");
        }
      // Mode = 2, 1    古いファイル削除
      if (1 <= PathList.Mode_CleanWorkItem)
        if (PathList.IsLastPart)
        {
          const double nDaysBefore = 2.0;
          //LTopWorkDir                    サブフォルダ内も対象
          cleaner.OldFile(nDaysBefore, PathList.LTopWorkDir, "*.p?*.*");
          cleaner.OldFile(nDaysBefore, PathList.LTopWorkDir, "*.all.*");
          cleaner.OldFile(nDaysBefore, PathList.LTopWorkDir, "*.frame.cat.txt");
          cleaner.OldFile(nDaysBefore, PathList.LTopWorkDir, "*.jls.*");
          cleaner.OldFile(nDaysBefore, PathList.LTopWorkDir, "*.sys.*");
          cleaner.OldFile(nDaysBefore, PathList.LTopWorkDir, "*.d2v");
          cleaner.OldFile(nDaysBefore, PathList.LTopWorkDir, "*.lwi");
          cleaner.EmptyDir(PathList.LTopWorkDir);
        }
    }


    /// <summary>
    /// 例外発生時に作成済みのavsファイル削除
    /// </summary>
    public static void Clean_OnError()
    {
      // *.p3.2000__3000.avs 削除
      cleaner.OldFile(0.0, PathList.LWorkDir, PathList.WorkName + ".*__*.avs");
      cleaner.OldFile(0.0, PathList.LWorkDir, PathList.WorkName + ".*__*.vpy");
    }
  }



  /// <summary>
  /// ファイル削除
  /// </summary>
  static class FileCleaner
  {
    /// <summary>
    /// 古いファイル削除
    /// </summary>
    /// <param name="nDaysBefore">Ｎ日前のファイルを削除対象にする。</param>
    /// <param name="directory">ファイルを探すフォルダ。　子フォルダ内も対象</param>
    /// <param name="pattern">ファイル名に含まれる文字。ワイルドカード可 * </param>
    /// <param name="ignore">除外するファイルに含まれる文字。ワイルドカード不可 × </param>
    public static void OldFile(double nDaysBefore, string directory,
                        string pattern, string ignore = null)
    {
      if (Directory.Exists(directory) == false) return;

      //ファイル取得
      var files = new FileInfo[] { };
      try
      {
        var di = new DirectoryInfo(directory);
        files = di.GetFiles(pattern, SearchOption.AllDirectories);//ignore case
      }
      catch (UnauthorizedAccessException)
      {
        /*
         * アクセス権限の無いファイルが含まれているフォルダに
         * files = di.GetFiles();
         * を実行すると System.UnauthorizedAccessExceptionが発生する。
         * 
         * Java  jre-8u73-windows-i586.exeを実行してインストール用のウィンドウを表示させると、
         * Windows Tempフォルダにjds262768703.tmpがReadOnlyで作成される。
         * Windows Tempを処理すると発生するかもしれない。
         */
        return;
      }

      foreach (var fi in files)
      {
        if (fi.Exists == false) continue;
        if (ignore != null && 0 <= fi.Name.IndexOf(ignore)) continue;

        //古いファイル？
        bool over_creation = nDaysBefore < (DateTime.Now - fi.CreationTime).TotalDays;
        bool over_lastwrite = nDaysBefore < (DateTime.Now - fi.LastWriteTime).TotalDays;
        if (over_creation && over_lastwrite)
        {
          try { fi.Delete(); }
          catch { /*ファイル使用中*/ }
        }
      }
    }


    /// <summary>
    /// 空フォルダ削除
    /// </summary>
    /// <param name="parent_dir">親フォルダを指定。空の子フォルダが削除対象、親フォルダ自身は削除されない。</param>
    public static void EmptyDir(string parent_dir)
    {
      if (Directory.Exists(parent_dir) == false) return;

      var dirs = new DirectoryInfo[] { };
      try
      {
        var di = new DirectoryInfo(parent_dir);
        dirs = di.GetDirectories("*", SearchOption.AllDirectories);
      }
      catch (UnauthorizedAccessException)
      {
        return;
      }

      foreach (var di in dirs)
      {
        if (di.Exists == false) continue;
        //空フォルダ？
        var files = di.GetFiles();
        if (files.Count() == 0)
        {
          try { di.Delete(); }
          catch { /*フォルダ使用中*/ }
        }
      }
    }
  }


}
