using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace LGLauncher
{
  /// <summary>
  /// 入力プラグイン
  /// </summary>
  enum PluginType
  {
    Unknown,
    D2v,
    Lwi,
  }

  /// <summary>
  /// FrameServer
  /// </summary>
  enum AvsVpyType
  {
    Unknown,
    Avs,
    Vpy,
  }

  /// <summary>
  /// ロゴ検出器
  /// </summary>
  enum LogoDetector
  {
    Unknown,
    Join_Logo_Scp,
    LogoGuillo,
  }


  /// <summary>
  /// パス一覧　＆　アプリ設定
  /// </summary>
  static class PathList
  {
    //  [  Input file  ]
    //    Ts
    public static string TsPath { get; private set; }
    public static string TsDir { get { return Path.GetDirectoryName(TsPath); } }
    public static string TsName { get { return Path.GetFileName(TsPath); } }
    public static string TsNameWithoutExt { get { return Path.GetFileNameWithoutExtension(TsPath); } }
    public static string TsShortName { get; private set; }

    //    D2v
    public static string D2vPath { get; private set; }
    public static string D2vDir { get { return Path.GetDirectoryName(D2vPath); } }
    public static string D2vName { get { return Path.GetFileName(D2vPath); } }
    public static string D2vPathInLWork { get { return Path.Combine(LWorkDir, D2vNameInLWork); } }
    public static string D2vNameInLWork { get { return PathList.TsShortName + ".d2v"; } }


    //    Lwi
    public static string LwiPath { get; private set; }
    public static string LwiDir { get { return Path.GetDirectoryName(LwiPath); } }
    public static string LwiName { get { return Path.GetFileName(LwiPath); } }
    public static string LwiPathInLWork { get { return Path.Combine(LWorkDir, LwiNameInLWork); } }
    public static string LwiNameInLWork { get { return PathList.TsShortName + ".lwi"; } }

    //    LwiFooter
    public static string LwiFooterPath { get; private set; }
    public static string LwiFooterDir { get { return Path.GetDirectoryName(LwiFooterPath); } }
    public static string LwiFooterName { get { return Path.GetFileName(LwiFooterPath); } }

    //    Srt
    public static string SrtPath { get; private set; }
    public static string SrtDir { get { return Path.GetDirectoryName(SrtPath); } }
    public static string SrtName { get { return Path.GetFileName(SrtPath); } }

    //App
    public static string AppPath { get; private set; }
    public static string AppDir { get { return Path.GetDirectoryName(AppPath); } }
    public static string AppName { get { return Path.GetFileName(AppPath); } }


    //  [  Work value  ]
    //    LSystemDir
    public static string LSystemDir { get; private set; }
    public static string LTopWorkDir { get; private set; }
    public static string LWorkDir { get; private set; }


    //  WorkPath                example
    //    current  work path      C:\EDCB\Write\Write_PF\LGLauncher\LWork\010101_ショップジ_0a1b308c1\ショップジ.p3
    //                            ショップジ.p3
    //    previous work path      C:\EDCB\Write\Write_PF\LGLauncher\LWork\010101_ショップジ_0a1b308c1\ショップジ.p2
    //                            ショップジ.p2
    public static string WorkName { get { return (IsAll) ? TsShortName + ".all" : TsShortName + ".p" + PartNo; } }
    public static string WorkPath { get { return Path.Combine(LWorkDir, WorkName); } }
    public static string WorkName_prv { get { return TsShortName + ".p" + (PartNo - 1); } }
    public static string WorkPath_prv { get { return Path.Combine(LWorkDir, WorkName_prv); } }

    //PartNo
    public static int PartNo { get; private set; }
    public static bool IsPart { get { return 1 <= PartNo; } }
    public static bool Is1stPart { get { return PartNo == 1; } }
    public static bool IsAll { get { return PartNo == -1; } }
    private static bool AutoDetectPartNo;
    private static string SequenceName;                      //作業フォルダ名のMD5作成用

    //コマンドラインに -IsLast があるか？
    public static bool HasLastFlag_OnCmdLine { get; private set; }

    //最後の SplitTrimか？
    //  SplitTrim : avsから有効フレームを取得した後に、さらに分割したTrim()
    public static bool IsLastSplit { get; private set; }

    //IsLastSplit更新
    public static void Set_IsLastSplit(bool islast)
    {
      IsLastSplit = islast;
    }

    //最後の PartNoか？
    public static bool IsLastPart
    {
      get
      {
        if (IsPart)
          return HasLastFlag_OnCmdLine && IsLastSplit;
        else//IsAll
          return true;
      }
    }

    //PartNo++
    public static void IncrementPartNo()
    {
      PartNo++;
      if (IsAll) throw new Exception("cannot increment PartNo at 'IsAll' session");
    }


    //  [  LSystem  ]  binary path
    public static string SystemIdleMonitor { get; private set; }
    public static string avs2pipemod { get; private set; }

    //avs vpy
    public static AvsVpyType AvsVpy { get; private set; }
    public static string AvsVpyExt { get { return "." + AvsVpy.ToString().ToLower(); } }

    public static PluginType InputPlugin { get; private set; }
    public static string DGDecode_dll { get; private set; }
    public static string LSMASHSource_dll { get; private set; }
    public static string d2vsource_dll { get; private set; }
    public static string vslsmashsource_dll { get; private set; }

    //logo select
    public static string Channel { get; private set; }
    public static string Program { get; private set; }
    public static string LogoSelector { get; private set; }

    //Detector
    public static LogoDetector Detector { get; private set; }
    public static int Detector_MultipleRun { get; private set; }
    public static readonly string[] DetectorName =
      new string[] { "chapter_exe", "logoframe", "logoGuillo" };

    //LogoGuillo
    public static string LogoGuillo { get; private set; }

    //Join_Logo_Scp
    public static string avsinp_aui { get; private set; }
    public static string Chapter_exe { get; private set; }
    public static string LogoFrame { get; private set; }
    public static string Join_Logo_Scp { get; private set; }

    //  JL command
    public static string JL_Cmd_OnRec { get; private set; }
    public static string JL_Cmd_Standard { get; private set; }

    //  [  Chapter  ]
    //edit chap
    public static double Regard_NsecCM_AsMain { get; private set; }
    public static double Regard_NsecMain_AsCM { get; private set; }

    //enable output
    public static bool Out_Tvtp { get; private set; }
    public static bool Out_Tgm { get; private set; }
    public static bool Out_Frame { get; private set; }
    public static bool Out_RawFrame { get; private set; }

    //chap directory
    //public static bool Out_tvtp_toTsDir { get; private set; }
    //public static bool Out_misc_toTsDir { get; private set; }
    public static string DirPath_Tvtp { get; private set; }
    public static string DirPath_Misc { get; private set; }

    //  [  Delete Work item  ]
    public static int Mode_CleanWorkItem { get; private set; }


    /// <summary>
    /// パス作成
    /// </summary>
    public static void MakePath(Setting_CmdLine cmdline, Setting_File setting)
    {
      //command line
      Copy_FromCommandLine(cmdline);

      //ts path
      Make_InputPath(setting);

      //work dir
      Make_WorkDir_and_DetectPartNo();

      //system binary
      Make_SystemFile(setting);

      //chapter
      Make_Chap_and_Misc(setting);

      Log_and_ErrorCheck();
    }


    /// <summary>
    /// コマンドラインから設定をコピー
    /// </summary>
    private static void Copy_FromCommandLine(Setting_CmdLine cmdline)
    {
      //copy
      PartNo = cmdline.No;
      PartNo = (cmdline.IsAll) ? -1 : PartNo;
      HasLastFlag_OnCmdLine = cmdline.IsLast;
      SequenceName = cmdline.SequenceName ?? "";

      TsPath = cmdline.TsPath;
      D2vPath = cmdline.D2vPath;
      LwiPath = cmdline.LwiPath;
      SrtPath = cmdline.SrtPath;

      Channel = cmdline.Channel;
      Program = cmdline.Program;

      //post process
      AutoDetectPartNo = PartNo == 0;

      //エラーチェック
      //PartNo
      if (PartNo <= -2)
        throw new LGLException("PartNo is less than equal -2");

      //Ts
      if (File.Exists(TsPath) == false)
        throw new LGLException("ts does not exist");
      //D2v  Lwi
      //  コマンドラインで指定されているときのみチェック
      //  srtは削除されるている可能性があるのでチェックしない
      if (D2vPath != null && File.Exists(D2vPath) == false)
        throw new LGLException("d2v does not exist");
      if (LwiPath != null && File.Exists(LwiPath) == false)
        throw new LGLException("lwi does not exist");

      //Extension
      if (TsPath != null && Path.GetExtension(TsPath).ToLower() != ".ts")
        throw new LGLException("TsPath has invalid extension");

      if (D2vPath != null && Path.GetExtension(D2vPath).ToLower() != ".d2v")
        throw new LGLException("D2vPath has invalid extension");

      if (LwiPath != null && Path.GetExtension(LwiPath).ToLower() != ".lwi")
        throw new LGLException("LwiPath has invalid extension");

      if (SrtPath != null && Path.GetExtension(SrtPath).ToLower() != ".srt")
        throw new LGLException("SrtPath has invalid extension");
    }

    private static void Copy_FromCommandLine_obsolete(Setting_CmdLine cmdline)
    {
      PartNo = cmdline.No;

      HasLastFlag_OnCmdLine = cmdline.IsLast || cmdline.IsAll || cmdline.No == -1;
      SequenceName = cmdline.SequenceName ?? "";

      TsPath = cmdline.TsPath;
      D2vPath = cmdline.D2vPath;
      LwiPath = cmdline.LwiPath;
      SrtPath = cmdline.SrtPath;

      Channel = cmdline.Channel;
      Program = cmdline.Program;

      PartNo = IsAll ? -1 : PartNo;
      AutoDetectPartNo = PartNo == 0;

      //エラーチェック
      //PartNo
      if (PartNo <= -2)
        throw new LGLException("PartNo is less than equal -2");

      //Ts
      if (File.Exists(TsPath) == false)
        throw new LGLException("ts does not exist");
      //D2v  Lwi
      //  コマンドラインで指定されているときのみチェック
      //  srtは削除されるている可能性があるのでチェックしない
      if (D2vPath != null && File.Exists(D2vPath) == false)
        throw new LGLException("d2v does not exist");
      if (LwiPath != null && File.Exists(LwiPath) == false)
        throw new LGLException("lwi does not exist");

      //Extension
      if (TsPath != null && Path.GetExtension(TsPath).ToLower() != ".ts")
        throw new LGLException("TsPath is invalid extension");

      if (D2vPath != null && Path.GetExtension(D2vPath).ToLower() != ".d2v")
        throw new LGLException("D2vPath is invalid extension");

      if (LwiPath != null && Path.GetExtension(LwiPath).ToLower() != ".lwi")
        throw new LGLException("LwiPath is invalid extension");

      if (SrtPath != null && Path.GetExtension(SrtPath).ToLower() != ".srt")
        throw new LGLException("SrtPath is invalid extension");
    }

    /// <summary>
    /// 入力ファイルの設定
    /// </summary>
    private static void Make_InputPath(Setting_File setting)
    {
      //Input
      TsShortName = new Regex("[ $|()^　]").Replace(TsName, "_");      //batの特殊文字
      TsShortName = (5 < TsShortName.Length) ? TsShortName.Substring(0, 5) : TsShortName;

      D2vPath = D2vPath ?? TsPath + ".pp.d2v";
      LwiPath = LwiPath ?? TsPath + ".pp.lwi";
      LwiFooterPath = LwiPath + "footer";
      SrtPath = SrtPath ?? TsPath + ".srt";

      //PluginType  LogoDetector
      {
        string plugin = setting.InputPlugin.Trim().ToLower();
        string detector = setting.LogoDetector.Trim().ToLower();

        bool isD2v = plugin == "d2v".ToLower();
        bool isLwi = plugin == "lwi".ToLower();

        bool isLG = detector == "LG".ToLower()
          || detector == "LogoGuillo".ToLower();

        bool isJLS = detector == "JLS".ToLower()
          || detector == "Join_Logo_Scp".ToLower()
          || detector == "Join_Logo_Scpos".ToLower();

        InputPlugin = isD2v ? PluginType.D2v
          : isLwi ? PluginType.Lwi
          : PluginType.Unknown;

        Detector = isJLS ? LogoDetector.Join_Logo_Scp
          : isLG ? LogoDetector.LogoGuillo
          : LogoDetector.Unknown;
      }

      //Avs固定 
      {
        const AvsVpyType frameServerType = AvsVpyType.Avs;
        AvsVpy = frameServerType;

        if (AvsVpy == AvsVpyType.Vpy)
          throw new NotImplementedException();
      }
    }



    /// <summary>
    /// WorkDirの設定
    /// </summary>
    private static void Make_WorkDir_and_DetectPartNo()
    {
      //App
      AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

      //Dir作成
      LTopWorkDir = Path.Combine(AppDir, @"LWork");
      LSystemDir = Path.Combine(AppDir, @"LSystem");
      if (Directory.Exists(LTopWorkDir) == false)
        Directory.CreateDirectory(LTopWorkDir);
      if (Directory.Exists(LSystemDir) == false)
        Directory.CreateDirectory(LSystemDir);

      //
      //WorkDir
      //    衝突は考えない、重複チェックはしてない。
      string workDirName;
      {
        var tsinfo = new FileInfo(TsPath);
        string timecode_ID = tsinfo.CreationTime.ToString("ddHHmm");
        string timecode_MD5 = tsinfo.CreationTime.ToString("yyyyMMdd_dddd_HHmmss_fffffff");
        string MD5 = Hash.ComputeMD5(TsPath + timecode_MD5 + SequenceName).Substring(0, 10);
        workDirName = timecode_ID + "_" + TsShortName + "_" + MD5;
      }
      LWorkDir = Path.Combine(LTopWorkDir, workDirName);

      if (Directory.Exists(LWorkDir) == false)
        Directory.CreateDirectory(LWorkDir);
      //LWorkDirが作成されたので、ここからのログはLWorkDir内に書き出される。


      //PartNo検出
      if (AutoDetectPartNo)
        PartNo = new Func<int>(() =>
        {
          //TsShortName, LWorkDirの設定前だとこのFunc<int>()は処理できない。
          //通常のExceptionを投げる
          if (TsShortName == null) throw new Exception();
          if (LWorkDir == null) throw new Exception();

          // search *.p2.frame.cat.txt
          var files = Directory.GetFiles(LWorkDir,
                                         TsShortName + ".p*.frame.cat.txt");
          // not found previous part file. set PartNo 1.
          if (files.Count() == 0)
            return 1;

          //ファイル名  -->  数字抽出
          var strNums = files.Select(fullname =>
          {
            //"ON&OF.p1.frame.cat.txt"
            string name = Path.GetFileName(fullname);
            //  "ON&OF.p"
            int len_1st = (TsShortName + ".p").Length;
            //     "1"  = "ON&OF.p1.frame.cat.txt"  -  "ON&OF.p"  -  ".frame.cat.txt"
            int len_num = name.Length - len_1st - ".frame.cat.txt".Length;
            string num = name.Substring(len_1st, len_num);
            return num;
          });

          // string --> int
          var intNums = strNums.Select(strnum =>
          {
            try { return int.Parse(strnum); }
            catch { throw new LGLException("PartNo parse error"); }
          }).ToList();

          intNums.Sort();
          intNums.Reverse();
          return intNums[0] + 1;
        })();
    }



    class Hash
    {
      /// <summary>
      /// ＭＤ５作成
      /// </summary>
      public static string ComputeMD5(string srcText)
      {
        byte[] data = System.Text.Encoding.UTF8.GetBytes(srcText);                         //文字列をbyte型配列に変換する
        var md5 = System.Security.Cryptography.MD5.Create();
        byte[] bytes_md5 = md5.ComputeHash(data);                                          //ハッシュ値を計算する
        md5.Clear();
        string result = BitConverter.ToString(bytes_md5).ToLower().Replace("-", "");       //16進数の文字列に変換
        return result;
      }
    }


    /// <summary>
    /// Systemフォルダ内のファイルを取得、設定
    /// </summary>
    private static void Make_SystemFile(Setting_File setting)
    {
      //ファイルリストから対象のフルパス取得
      var SearchItem_OrEmpty = new Func<FileInfo[], string, string>(
        (filelist, target) =>
        {
          var finfo = filelist.Where(fi => fi.Name.ToLower() == target.ToLower())
                              .FirstOrDefault();
          if (finfo != null)
            return finfo.FullName;
          else
            return "";
        });

      var SearchItem = new Func<FileInfo[], string, string>(
        (filelist, target) =>
        {
          string path = SearchItem_OrEmpty(filelist, target);
          if (path != "")
            return path;
          else
            throw new LGLException("not found: " + target);
        });


      //LSystemDirのファイル一覧を取得し、各バイナリ検索
      var dirInfo = new DirectoryInfo(LSystemDir);
      var sysFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);

      SystemIdleMonitor = SearchItem_OrEmpty(sysFiles, "SystemIdleMonitor.exe");
      avs2pipemod = SearchItem(sysFiles, "avs2pipemod.exe");

      //InputPlugin
      if (PathList.AvsVpy == AvsVpyType.Avs)
      {
        if (InputPlugin == PluginType.D2v)
          DGDecode_dll = SearchItem(sysFiles, "DGDecode.dll");
        else if (InputPlugin == PluginType.Lwi)
          LSMASHSource_dll = SearchItem(sysFiles, "LSMASHSource.dll");
      }
      else if (PathList.AvsVpy == AvsVpyType.Vpy)
      {
        if (InputPlugin == PluginType.D2v)
          d2vsource_dll = SearchItem(sysFiles, "d2vsource.dll");
        else if (InputPlugin == PluginType.Lwi)
          vslsmashsource_dll = SearchItem(sysFiles, "vslsmashsource.dll");
      }

      //Detector
      if (Detector == LogoDetector.Join_Logo_Scp)
      {
        //Join_Logo_Scp
        avsinp_aui = SearchItem(sysFiles, "avsinp.aui");
        Chapter_exe = SearchItem(sysFiles, "Chapter_exe.exe");
        LogoFrame = SearchItem(sysFiles, "LogoFrame.exe");
        Join_Logo_Scp = SearchItem(sysFiles, "Join_Logo_Scp.exe");
        JL_Cmd_OnRec = SearchItem(sysFiles, "JL_標準_Rec.txt");
        JL_Cmd_Standard = SearchItem(sysFiles, "JL_標準.txt");
      }
      else if (Detector == LogoDetector.LogoGuillo)
      {
        //LogoGuillo
        LogoGuillo = SearchItem(sysFiles, "LogoGuillo.exe");

        //USE_AVS    LTopWorkDir内に作成
        var AVSPLG = Path.Combine(LTopWorkDir, "USE_AVS");
        try
        {
          if (File.Exists(AVSPLG) == false)
            File.Create(AVSPLG).Close();
        }
        catch (IOException)
        {
          //別プロセスとぶつかった
          System.Threading.Thread.Sleep(300);
          if (File.Exists(AVSPLG) == false)
            throw new LGLException("USE_AVS creating error ");
        }
      }

      //LogoSelector
      //  複数ある場合の優先順位は、
      //　    （高）  .exe .vbs .js  （低）
      var logoSelector_list = new string[] {
                                              SearchItem_OrEmpty(sysFiles, "LogoSelector.exe"),
                                              SearchItem_OrEmpty(sysFiles, "LogoSelector.vbs"),
                                              SearchItem_OrEmpty(sysFiles, "LogoSelector.js") 
                                           };
      LogoSelector = logoSelector_list.Where((LSpath) => File.Exists(LSpath)).FirstOrDefault();
      LogoSelector = LogoSelector ?? "";
    }


    /// <summary>
    /// チャプター出力の設定
    /// </summary>
    private static void Make_Chap_and_Misc(Setting_File setting)
    {
      //edit chapter
      Regard_NsecCM_AsMain = setting.Regard_NsecCM_AsMain;
      Regard_NsecMain_AsCM = setting.Regard_NsecMain_AsCM;
      Regard_NsecCM_AsMain = 0 < Regard_NsecCM_AsMain ? Regard_NsecCM_AsMain : 0;
      Regard_NsecMain_AsCM = 0 < Regard_NsecMain_AsCM ? Regard_NsecMain_AsCM : 0;

      //enable output
      Out_Tvtp = 0 < setting.Out_Tvtp;
      Out_Tgm = 0 < setting.Out_Ogm;
      Out_Frame = 0 < setting.Out_Frame;
      Out_RawFrame = 0 < setting.Out_RawFrame;

      //chapter directory
      //Out_tvtp_toTsDir = 0 < setting.Out_tvtp_toTsDir;
      //Out_misc_toTsDir = 0 < setting.Out_misc_toTsDir;
      DirPath_Tvtp = setting.DirPath_Tvtp;
      DirPath_Misc = setting.DirPath_Misc;

      //misc
      Detector_MultipleRun = setting.Detector_MultipleRun;
      Mode_CleanWorkItem = setting.CleanWorkItem;
    }


    /// <summary>
    /// エラーチェック
    /// </summary>
    private static void Log_and_ErrorCheck()
    {
      //log
      {
        Log.WriteLine("  No  = 【    " + PathList.PartNo + "    】");
        if (PathList.Is1stPart || PathList.IsAll)
        {
          Log.WriteLine("          " + PathList.TsPath);
          Log.WriteLine("       InputPlugin  :  " + InputPlugin.ToString());
          Log.WriteLine("       LogoDetector :  " + Detector.ToString());
        }
        if (HasLastFlag_OnCmdLine)
          Log.WriteLine("       HasLastFlag  :  " + HasLastFlag_OnCmdLine);
        Log.WriteLine();
      }

      //error check
      if (InputPlugin == PluginType.D2v)
      {
        if (File.Exists(D2vPath) == false)
          throw new LGLException(D2vName + " dose not exist");
      }

      if (InputPlugin == PluginType.Lwi)
      {
        if (File.Exists(LwiPath) == false)
          throw new LGLException(LwiName + " dose not exist");
      }

      if (InputPlugin == PluginType.Unknown)
        throw new LGLException("Unknown InputPlugin");

      if (Detector == LogoDetector.Unknown)
        throw new LGLException("Unknown LogoDetector");

      if (InputPlugin == PluginType.D2v
        && Detector == LogoDetector.Join_Logo_Scp)
        throw new LGLException("Cannot select d2v with Join_Logo_Scp");

    }






  }

}