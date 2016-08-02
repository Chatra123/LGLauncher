using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace LGLauncher
{
  /// <summary>
  /// DebugMode
  /// </summary>
  static class Debug
  {
    // format d2v, lwiをデバッグ用に別名で保存するか？
    public const bool CopyIndex = false;  // true  false
  }

  /// <summary>
  /// 入力プラグイン
  /// </summary>
  enum Plugin
  {
    Unknown,
    D2v,
    Lwi,
  }

  /// <summary>
  /// FrameServer
  /// </summary>
  enum AvsVpy
  {
    Unknown,
    Avs,
    Vpy,
  }

  /// <summary>
  /// 検出器
  /// </summary>
  enum Detector
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
    //                  name      ショップジ.p3
    //    previous work path      C:\EDCB\Write\Write_PF\LGLauncher\LWork\010101_ショップジ_0a1b308c1\ショップジ.p2
    //                  name      ショップジ.p2
    public static string WorkPath { get { return Path.Combine(LWorkDir, WorkName); } }
    public static string WorkName { get { return (IsAll) ? TsShortName + ".all" : TsShortName + ".p" + PartNo; } }
    public static string WorkPath_prv { get { return Path.Combine(LWorkDir, WorkName_prv); } }
    public static string WorkName_prv { get { return TsShortName + ".p" + (PartNo - 1); } }

    //PartNo
    //   1 <= No  IsPart
    //  No  =  0  uninitialized value and detect No 
    //  No  = -1  IsAll
    //  -2 <= No  throw Exception
    public static int PartNo { get; private set; }
    public static bool Is1stPart { get { return PartNo == 1; } }
    public static bool IsPart { get { return 1 <= PartNo; } }
    public static bool IsAll { get; private set; }
    private static string SequenceName;                      //作業フォルダ名のMD5に使用

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
      if (IsAll) throw new Exception(" cannot increment PartNo at 'IsAll' ");
    }


    //  [  LSystem binary path ]  
    public static string SystemIdleMonitor { get; private set; }
    public static string avs2pipemod { get; private set; }

    //avs vpy
    public static AvsVpy AvsVpy { get; private set; }
    public static string AvsVpyExt { get { return "." + AvsVpy.ToString().ToLower(); } }

    public static Plugin InputPlugin { get; private set; }
    public static string DGDecode_dll { get; private set; }
    public static string LSMASHSource_dll { get; private set; }
    public static string d2vsource_dll { get; private set; }
    public static string vslsmashsource_dll { get; private set; }

    //select logo
    public static string Channel { get; private set; }
    public static string Program { get; private set; }
    public static string LogoSelector { get; private set; }

    //Detector
    public static Detector Detector { get; private set; }
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
    //edit chapter
    public static double Regard_NsecCM_AsMain { get; private set; }
    public static double Regard_NsecMain_AsCM { get; private set; }

    //chapter output mode
    public static int Output_RawFrame { get; private set; }
    public static int Output_Frame { get; private set; }
    public static int Output_Tvtp { get; private set; }
    public static int Output_Ogm { get; private set; }

    //chapter directory
    public static string DirPath_Tvtp { get; private set; }
    public static string DirPath_Misc { get; private set; }

    //  [  Clean Work Item  ]
    public static int Mode_CleanWorkItem { get; private set; }


    /// <summary>
    /// 初期化、パス作成
    /// </summary>
    public static void Initialize(Setting_CmdLine cmdline, Setting_File setting)
    {
      Copy_fromCommandLine(cmdline);

      Make_InputPath(setting);

      Make_WorkDir();

      Detect_PartNo();

      Get_BinaryFile(setting);

      Set_Chap_and_Misc(setting);

      Log_and_ErrorCheck();
    }


    /// <summary>
    /// コマンドラインから設定をコピー
    /// </summary>
    private static void Copy_fromCommandLine(Setting_CmdLine cmdline)
    {
      //copy
      IsAll = cmdline.IsAll;
      HasLastFlag_OnCmdLine = cmdline.IsLast;
      SequenceName = cmdline.SequenceName ?? "";

      TsPath = cmdline.TsPath;
      D2vPath = cmdline.D2vPath;
      LwiPath = cmdline.LwiPath;
      SrtPath = cmdline.SrtPath;

      Channel = cmdline.Channel ?? "";
      Program = cmdline.Program ?? "";

      //ファイルチェック
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

      //PluginType  DetectorType
      {
        string plugin = setting.InputPlugin.Trim().ToLower();
        string detector = setting.Detector.Trim().ToLower();

        bool isD2v = plugin == "d2v".ToLower();
        bool isLwi = plugin == "lwi".ToLower();

        bool isLG = detector == "LG".ToLower()
          || detector == "LogoGuillo".ToLower();

        bool isJLS = detector == "JLS".ToLower()
          || detector == "JoinLogoScp".ToLower()
          || detector == "JoinLogoScpos".ToLower()
          || detector == "Join_Logo_Scp".ToLower()
          || detector == "Join_Logo_Scpos".ToLower();

        InputPlugin = isD2v ? Plugin.D2v
          : isLwi ? Plugin.Lwi
          : Plugin.Unknown;

        Detector = isJLS ? Detector.Join_Logo_Scp
          : isLG ? Detector.LogoGuillo
          : Detector.Unknown;
      }

      //AvsVpyType
      {
        //Avs固定 
        const AvsVpy frameServerType = AvsVpy.Avs;
        AvsVpy = frameServerType;

        if (AvsVpy == AvsVpy.Vpy)
          throw new NotImplementedException();
      }
    }



    /// <summary>
    /// WorkDirの設定
    /// </summary>
    private static void Make_WorkDir()
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
      //  衝突は考えない、重複チェックはしてない。
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
    /// PartNo検出
    /// </summary>
    private static void Detect_PartNo()
    {
      //LWorkDir, TsShortNameの設定前だと処理できない。
      if (LWorkDir == null) throw new Exception();
      if (TsShortName == null) throw new Exception();

      PartNo = IsAll ? -1 : Detect_PartNo_fromFileName();

      if (PartNo <= -2 || PartNo == 0)
        throw new LGLException("Invalid PartNo.  PartNo = " + PartNo);
    }
    /// <summary>
    /// PartNo検出  ファイル名から値を取得
    /// </summary>
    private static int Detect_PartNo_fromFileName()
    {
      //  search *.p2.frame.cat.txt
      var files = Directory.GetFiles(LWorkDir,
                                     TsShortName + ".p*.frame.cat.txt");
      // not found file
      if (files.Count() == 0)
        return 1;

      //ファイル名  -->  PartNo抽出
      var strNums = files.Select(fullname =>
      {
        //"movie.p1.frame.cat.txt"
        string name = Path.GetFileName(fullname);
        //  "movie.p"
        int len_title = (TsShortName + ".p").Length;
        //    "1"  =  "movie.p1.frame.cat.txt"  -  "movie.p"  -  ".frame.cat.txt"
        int len_no = name.Length - len_title - ".frame.cat.txt".Length;
        string no = name.Substring(len_title, len_no);
        return no;
      });

      // string  -->  int
      var intNums = strNums.Select(num =>
      {
        try { return int.Parse(num); }
        catch { throw new LGLException("PartNo parse error"); }
      }).ToList();

      intNums.Sort();
      intNums.Reverse();
      return intNums[0] + 1;
    }



    /// <summary>
    /// LSystemフォルダ内の各バイナリを取得
    /// </summary>
    private static void Get_BinaryFile(Setting_File setting)
    {
      //ファイル一覧から対象のフルパス取得
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


      //ファイル一覧を取得
      var dirInfo = new DirectoryInfo(LSystemDir);
      var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);

      SystemIdleMonitor = SearchItem_OrEmpty(files, "SystemIdleMonitor.exe");
      avs2pipemod = SearchItem(files, "avs2pipemod.exe");

      //InputPlugin
      if (PathList.AvsVpy == AvsVpy.Avs)
      {
        if (InputPlugin == Plugin.D2v)
          DGDecode_dll = SearchItem(files, "DGDecode.dll");
        else if (InputPlugin == Plugin.Lwi)
          LSMASHSource_dll = SearchItem(files, "LSMASHSource.dll");
      }
      else if (PathList.AvsVpy == AvsVpy.Vpy)
      {
        if (InputPlugin == Plugin.D2v)
          d2vsource_dll = SearchItem(files, "d2vsource.dll");
        else if (InputPlugin == Plugin.Lwi)
          vslsmashsource_dll = SearchItem(files, "vslsmashsource.dll");
      }

      //Detector
      if (Detector == Detector.Join_Logo_Scp)
      {
        //Join_Logo_Scp
        avsinp_aui = SearchItem(files, "avsinp.aui");
        Chapter_exe = SearchItem(files, "Chapter_exe.exe");
        LogoFrame = SearchItem(files, "LogoFrame.exe");
        Join_Logo_Scp = SearchItem(files, "Join_Logo_Scp.exe");
        JL_Cmd_OnRec = SearchItem(files, "JL_標準_Rec.txt");
        JL_Cmd_Standard = SearchItem(files, "JL_標準.txt");
      }
      else if (Detector == Detector.LogoGuillo)
      {
        //LogoGuillo
        LogoGuillo = SearchItem(files, "LogoGuillo.exe");

        //USE_AVS    LTopWorkDir内に作成
        var AVSPLG = Path.Combine(LTopWorkDir, "USE_AVS");
        try
        {
          if (File.Exists(AVSPLG) == false)
            File.Create(AVSPLG).Close();
        }
        catch (IOException)
        {
          //多重起動で別プロセスとぶつかった
          System.Threading.Thread.Sleep(300);
          if (File.Exists(AVSPLG) == false)
            throw new LGLException("USE_AVS creating error");
        }
      }

      //LogoSelector
      //  複数ある場合の優先順位は、
      //　    （高）  .exe .vbs .js  （低）
      //  16/06/23    .vbs .jsは使用していないので削除してもいい
      var logoSelector_list = new string[] {
                                              SearchItem_OrEmpty(files, "LogoSelector.exe"),
                                              SearchItem_OrEmpty(files, "LogoSelector.vbs"),
                                              SearchItem_OrEmpty(files, "LogoSelector.js") 
                                           };
      LogoSelector = logoSelector_list.Where((LSpath) => File.Exists(LSpath)).FirstOrDefault();
      LogoSelector = LogoSelector ?? "";
    }


    /// <summary>
    /// チャプター出力の設定
    /// </summary>
    private static void Set_Chap_and_Misc(Setting_File setting)
    {
      //edit chapter
      Regard_NsecCM_AsMain = setting.Regard_NsecCM_AsMain;
      Regard_NsecMain_AsCM = setting.Regard_NsecMain_AsCM;
      Regard_NsecCM_AsMain = 0 < Regard_NsecCM_AsMain ? Regard_NsecCM_AsMain : 0;
      Regard_NsecMain_AsCM = 0 < Regard_NsecMain_AsCM ? Regard_NsecMain_AsCM : 0;

      //enable output
      Output_Tvtp = setting.Output_Tvtp;
      Output_Ogm = setting.Output_Ogm;
      Output_Frame = setting.Output_Frame;
      Output_RawFrame = setting.Output_RawFrame;

      //chapter directory
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
          Log.WriteLine("       Detector     :  " + Detector.ToString());
        }
        if (HasLastFlag_OnCmdLine)
          Log.WriteLine("       HasLastFlag  :  " + HasLastFlag_OnCmdLine);
        Log.WriteLine();
      }

      //check
      if (InputPlugin == Plugin.D2v
        && File.Exists(D2vPath) == false)
        throw new LGLException("d2v dose not exist: " + D2vName);

      if (InputPlugin == Plugin.Lwi
        && File.Exists(LwiPath) == false)
        throw new LGLException("lwi dose not exist: " + LwiName);

      if (InputPlugin == Plugin.Unknown)
        throw new LGLException("Unknown InputPlugin");

      if (AvsVpy == AvsVpy.Unknown)
        throw new LGLException("Unknown AvsVpyType");

      if (Detector == Detector.Unknown)
        throw new LGLException("Unknown LogoDetector");

      if (InputPlugin == Plugin.D2v
        && Detector == Detector.Join_Logo_Scp)
        throw new LGLException("Cannot select d2v with JLS");

    }






  }

}