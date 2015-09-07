using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace LGLauncher
{
  /// <summary>
  /// 設定ファイル
  /// </summary>
  [Serializable]
  public class Setting
  {
    public int bEnable = 1;
    public int bPrefer_d2v = 1;
    public int iPriority = -1;
    public int iLogoGuillo_MultipleRun = 1;
    public int bUseTSDir_asChapDir = 1;
    public string sChapDir_Path = @"   C:\chap_dir   ";
    public string sFrameDir_Path = @"   C:\frame_dir   ";
    public int iDeleteWorkItem = 1;

    //設定ファイル名
    private static readonly string
            AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location,
            AppDir = Path.GetDirectoryName(AppPath),
            AppName = Path.GetFileNameWithoutExtension(AppPath),
            DefXmlName = AppName + ".xml";

    /// <summary>
    /// ファイルから読込む
    /// </summary>
    /// <returns></returns>
    public static Setting LoadFile(string xmlpath = null)
    {
      //カレントディレクトリ設定
      Directory.SetCurrentDirectory(AppDir);

      if (xmlpath == null)
      {
        //デフォルト名を使用
        if (File.Exists(Setting.DefXmlName) == false)
        {
          var defSetting = new Setting();
          XmlRW.Save(Setting.DefXmlName, defSetting);      //デフォルト設定保存
        }
        xmlpath = Setting.DefXmlName;
      }

      var file = XmlRW.Load<Setting>(xmlpath);             //xml読込

      //文字列がスペースのみだと読込み時にstring.Emptyになり、xmlに<sChapDir_Path />と書き込まれる。
      //スペースを加えて<sChapDir_Path>        </sChapDir_Path>になるようにする。
      file.sChapDir_Path = (string.IsNullOrWhiteSpace(file.sChapDir_Path))
                              ? new String(' ', 8) : file.sChapDir_Path;
      file.sFrameDir_Path = (string.IsNullOrWhiteSpace(file.sFrameDir_Path))
                              ? new String(' ', 8) : file.sFrameDir_Path;

      XmlRW.Save(xmlpath, file);                 //古いバージョンのファイルなら新たに追加された項目がxmlに加わる。

      //優先度
      if (file.iPriority == 0)
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;
      else if (file.iPriority == 1)
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
      else if (file.iPriority == 2)
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;

      return file;
    }
  }

  #region PathList

  /// <summary>
  /// パス一覧　＆　アプリ設定
  /// </summary>
  internal static class PathList
  {
    //Input file
    //    Ts
    public static string TsPath { get; private set; }

    public static string TsDir { get; private set; }
    public static string TsName { get; private set; }
    public static string TsNameWithoutExt { get; private set; }
    public static string TsShortName { get; private set; }

    //    D2v
    public static string D2vPath { get; private set; }

    public static string D2vDir { get; private set; }
    public static string D2vName { get; private set; }

    //    Lwi
    public static string LwiPath { get; private set; }

    public static string LwiDir { get; private set; }
    public static string LwiName { get; private set; }

    //    LwiFooter
    public static string LwiFooterPath { get; private set; }

    public static string LwiFooterDir { get; private set; }
    public static string LwiFooterName { get; private set; }

    //    Srt
    public static string SrtPath { get; private set; }

    public static string SrtDir { get; private set; }
    public static string SrtName { get; private set; }

    //Work item
    //    AppPath
    public static string AppPath { get; private set; }

    public static string AppDir { get; private set; }
    public static string AppName { get; private set; }

    //    LSystemDir
    public static string LSystemDir { get; private set; }

    public static string LTopWorkDir { get; private set; }
    public static string LWorkDir { get; private set; }

    //    WorkPath
    public static string WorkPath { get; private set; }

    public static string WorkName { get; private set; }
    public static string WorkPath_m1 { get; private set; }
    public static string WorkName_m1 { get; private set; }

    //LogoGuillo
    public static int LogoGuillo_MultipleRun { get; private set; }

    public static string LogoGuillo { get; private set; }
    public static string AVS2X { get; private set; }
    public static string AVSPLG { get; private set; }
    public static string LogoSelector { get; private set; }

    //App setting
    public static int No { get; private set; }

    public static bool Mode_D2v { get; private set; }
    public static bool Mode_IsLast { get; private set; }

    public static string Channel { get; private set; }
    public static string Program { get; private set; }

    public static string ChapDir { get; private set; }
    public static string FrameDir { get; private set; }
    public static int Mode_DeleteWorkItem { get; private set; }

    /// <summary>
    /// パス作成
    /// </summary>
    public static void Make(CommandLine cmdline, Setting setting)
    {
      CopyFromCommandLine(cmdline);

      Make_InputPath(setting);
      Make_WorkDir(setting);

      Make_LogoGuillo(setting);
      Make_MiscPath(setting);

      //ファイルチェック
      //パス作成直後でなく、WorkDir作成後に行う。
      if (Mode_D2v == true && File.Exists(PathList.D2vPath) == false)
        throw new LGLException("D2vPath not exist");
      if (Mode_D2v == false && File.Exists(PathList.LwiPath) == false)
        throw new LGLException("LwiPath not exist");
    }

    /// <summary>
    /// コマンドラインからの設定コピー
    /// </summary>
    private static void CopyFromCommandLine(CommandLine cmdline)
    {
      No = cmdline.No;
      TsPath = cmdline.TsPath;
      D2vPath = cmdline.D2vPath;
      LwiPath = cmdline.LwiPath;
      SrtPath = cmdline.SrtPath;
      Channel = cmdline.Channel;
      Program = cmdline.Program;
      Mode_IsLast = cmdline.IsLast;

      //エラーチェック
      if (No == 0) throw new LGLException();
      else if (No < -1) throw new LGLException();
      //ファイル  Ts
      if (File.Exists(TsPath) == false) throw new LGLException();
      //コマンドラインから値が設定されているときのみ  D2v  Lwi
      //  srtは削除される可能性があるのでチェックしない
      if (D2vPath != null && File.Exists(D2vPath) == false) throw new LGLException();
      if (LwiPath != null && File.Exists(LwiPath) == false) throw new LGLException();
      //拡張子
      if (TsPath != null && Path.GetExtension(TsPath).ToLower() != ".ts") throw new LGLException();
      if (D2vPath != null && Path.GetExtension(D2vPath).ToLower() != ".d2v") throw new LGLException();
      if (LwiPath != null && Path.GetExtension(LwiPath).ToLower() != ".lwi") throw new LGLException();
      if (SrtPath != null && Path.GetExtension(SrtPath).ToLower() != ".srt") throw new LGLException();
    }

    /// <summary>
    /// 入力ファイルの設定
    /// </summary>
    private static void Make_InputPath(Setting setting)
    {
      //TsPath
      TsDir = Path.GetDirectoryName(TsPath);
      TsName = Path.GetFileName(TsPath);
      TsNameWithoutExt = Path.GetFileNameWithoutExtension(TsPath);
      TsShortName = (5 < TsName.Length) ? TsName.Substring(0, 5) : TsName;

      //D2vPath
      D2vPath = D2vPath ?? TsPath + ".pp.d2v";
      D2vDir = Path.GetDirectoryName(D2vPath);
      D2vName = Path.GetFileName(D2vPath);

      //LwiPath
      LwiPath = LwiPath ?? TsPath + ".pp.lwi";
      LwiDir = Path.GetDirectoryName(LwiPath);
      LwiName = Path.GetFileName(LwiPath);

      //LwiFooter
      LwiFooterPath = LwiPath + "footer";
      LwiFooterDir = Path.GetDirectoryName(LwiFooterPath);
      LwiFooterName = Path.GetFileName(LwiFooterPath);

      //SrtPath
      SrtPath = SrtPath ?? Path.Combine(TsDir, TsNameWithoutExt + ".srt");
      SrtDir = Path.GetDirectoryName(SrtPath);
      SrtName = Path.GetFileName(SrtPath);

      //Mode_D2v
      Mode_D2v = (0 < setting.bPrefer_d2v);
    }

    /// <summary>
    /// WorkDirの設定
    /// </summary>
    private static void Make_WorkDir(Setting setting)
    {
      AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
      AppDir = Path.GetDirectoryName(AppPath);

      //LTopWorkDir、LSystemDir作成
      LTopWorkDir = Path.Combine(AppDir, "LWork");
      LSystemDir = Path.Combine(AppDir, "LSystem");

      if (Directory.Exists(LTopWorkDir) == false) 
        Directory.CreateDirectory(LTopWorkDir);

      if (Directory.Exists(LSystemDir) == false)
        Directory.CreateDirectory(LSystemDir);

      //
      //WorkDir
      //    .\LWork\TsName\TsName.p1.info.txtが２５５文字を超えることがあったので短縮名を使う。
      //    衝突は考えない、重複チェックはしてない。
      string workDirName, timecode_ID, timecode_MD5, MD5;
      var tsinfo = new FileInfo(TsPath);
      timecode_ID = tsinfo.CreationTime.ToString("ddHHmm");                              //フォルダ名用　作成日
      timecode_MD5 = tsinfo.CreationTime.ToString("yyyyMMdd_ddddHHmmssfffffff");         //ＭＤ５用　　　作成日
      MD5 = ComputeMD5(TsPath + timecode_MD5).Substring(0, 10);                          //ＭＤ５　    　ファイルパス＋作成日

      workDirName = timecode_ID + "_" + TsShortName + "_" + MD5;
      LWorkDir = Path.Combine(LTopWorkDir, workDirName);
      if (Directory.Exists(LWorkDir) == false) 
        Directory.CreateDirectory(PathList.LWorkDir);

      //WorkPath
      WorkName = (1 <= No) ? TsShortName + ".p" + No : TsShortName + ".all";
      WorkPath = Path.Combine(LWorkDir, WorkName);
      WorkName_m1 = TsShortName + ".p" + (No - 1);
      WorkPath_m1 = Path.Combine(LWorkDir, WorkName_m1);
    }

    /// <summary>
    /// LogoGuillo関連の設定
    /// </summary>
    private static void Make_LogoGuillo(Setting setting)
    {
      LogoGuillo_MultipleRun = setting.iLogoGuillo_MultipleRun;

      //LogoGuillo
      LogoGuillo = Path.Combine(LSystemDir, "logoGuillo.exe");
      if (File.Exists(LogoGuillo) == false)
        throw new LGLException("LogoGuillo does not exist");

      //avs2pipemod
      var avs2pipemod = Path.Combine(LSystemDir, "avs2pipemod.exe");
      AVS2X = avs2pipemod;
      if (File.Exists(AVS2X) == false)
        throw new LGLException("avs2pipemod does not exist");


      //USE_AVS    LTopWorkDirに作成
      AVSPLG = Path.Combine(LTopWorkDir, "USE_AVS");
      if (File.Exists(AVSPLG) == false) 
        File.Create(AVSPLG).Close();

      //LogoSelector
      //  SystemDirにあるLogoSelectorを取得。複数ある場合の優先順位は、
      //　                                     （高）  .exe .vbs .js  （低）
      var lsPathList = new string[]{
                                      Path.Combine(LSystemDir, "LogoSelector.exe"),
                                      Path.Combine(LSystemDir, "LogoSelector.vbs"),
                                      Path.Combine(LSystemDir, "LogoSelector.js"),
                                   };
      //  ファイルの有無でフィルター、最初の要素を取得
      LogoSelector = lsPathList.Where((lspath) => File.Exists(lspath)).FirstOrDefault();
    }

    /// <summary>
    /// その他の設定
    /// </summary>
    private static void Make_MiscPath(Setting setting)
    {
      //output external directory
      if (0 < setting.bUseTSDir_asChapDir)
        ChapDir = TsDir;
      else if (Directory.Exists(setting.sChapDir_Path))
        ChapDir = setting.sChapDir_Path;

      FrameDir = setting.sFrameDir_Path;

      Mode_IsLast |= (No == -1);            //引数で指定されてる or No == -1

      //delete work item
      Mode_DeleteWorkItem = setting.iDeleteWorkItem;
    }

    /// <summary>
    /// ＭＤ５作成
    /// </summary>
    /// <param name="srcText">ＭＤ５の元になるテキスト</param>
    /// <returns>ＭＤ５文字列</returns>
    private static string ComputeMD5(string srcText)
    {
      byte[] data = System.Text.Encoding.UTF8.GetBytes(srcText);                         //文字列をbyte型配列に変換する
      var md5 = System.Security.Cryptography.MD5.Create();
      byte[] bytes_md5 = md5.ComputeHash(data);                                          //ハッシュ値を計算する
      md5.Clear();
      string result = BitConverter.ToString(bytes_md5).ToLower().Replace("-", "");       //16進数の文字列に変換
      return result;
    }
  }

  #endregion PathList
}