using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;


namespace LGLauncher
{
  /// <summary>
  /// Avs入力プラグイン
  /// </summary>
  enum PluginType
  {
    Unknown,
    D2v,
    Lwi,
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
  /// <remarks>
  /// 各ファイルパス、バイナリパス、アプリ設定をここで処理する。
  /// ファイル確認も行う
  /// </remarks>
  internal static class PathList
  {

    //  [  Input file  ]
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


    //App
    public static string AppPath { get; private set; }
    public static string AppDir { get; private set; }
    public static string AppName { get; private set; }

    //  [  Work item  ]
    //    LSystemDir
    public static string LSystemDir { get; private set; }
    public static string LTopWorkDir { get; private set; }
    public static string LWorkDir { get; private set; }

    //    WorkPath                                           //example
    public static string WorkPath { get; private set; }　　  //  C:\EDCB\Write\Write_PF\LGLauncher\LWork\010101_ショップジ_0a1b308c1\ショップジ.p3
    public static string WorkName { get; private set; }      //  ショップジ.p3
    //    previous work path
    public static string WorkPath_prv1 { get; private set; } //  C:\EDCB\Write\Write_PF\LGLauncher\LWork\010101_ショップジ_0a1b308c1\ショップジ.p2
    public static string WorkName_prv1 { get; private set; } //  ショップジ.p2


    //PartNo
    private static bool AutoDetectPartNo;
    public static int PartNo { get; private set; }
    public static bool PartALL { get; private set; }
    public static bool IsLastPart { get; private set; }
    private static string SequenceName;                       //作業フォルダ名のMD5作成用


    //  [  LSystem  ]
    public static string SystemIdleMonitor { get; private set; }
    public static string avs2pipemod { get; private set; }
    public static string DGDecode_dll { get; private set; }
    public static string LSMASHSource_dll { get; private set; }

    //lgd logo
    public static string Channel { get; private set; }
    public static string Program { get; private set; }
    public static string LogoSelector { get; private set; }

    //Detector
    public static PluginType Avs_iPlugin { get; private set; }
    public static LogoDetector Detector { get; private set; }
    public static int Detector_MultipleRun { get; private set; }

    //LogoGuillo
    public static string LogoGuillo { get; private set; }

    //Join_Logo_Scp
    public static string avsinp_aui { get; private set; }
    public static string Chapter_exe { get; private set; }
    public static string LogoFrame { get; private set; }
    public static string Join_Logo_Scp { get; private set; }

    //  JL command
    public static string JL_Cmd_Recording { get; private set; }
    public static string JL_Cmd_Standard { get; private set; }

    //  [  Chapter  ]
    //edit chapter
    public static double Regard_NsecCM_AsMain { get; private set; }
    public static double Regard_NsecMain_AsCM { get; private set; }

    //enable output chap file
    public static bool Out_tvtp { get; private set; }
    public static bool Out_ogm { get; private set; }
    public static bool Out_frame { get; private set; }
    public static bool Out_rawframe { get; private set; }

    //output chap directory
    public static bool Out_tvtp_toTsDir { get; private set; }
    public static bool Out_misc_toTsDir { get; private set; }
    public static string DirPath_tvtp { get; private set; }
    public static string DirPath_misc { get; private set; }

    //  [  Delete Work item  ]
    public static int Mode_DeleteWorkItem { get; private set; }


    /// <summary>
    /// パス作成
    /// </summary>
    public static void MakePath(CommandLine cmdline, Setting_File setting)
    {
      // CommandLine
      Copy_FromCommandLine(cmdline);

      // input ts
      Make_InputPath(setting);

      // work dir
      Make_WorkDir_and_DetectPartNo();

      // system binary file
      Make_SystemFile(setting);

      // chapter and misc
      Make_Chap_and_Misc(setting);

      // check file existance
      Log_and_ErrorCheck();

    }



    /// <summary>
    /// コマンドラインから設定をコピー
    /// </summary>
    private static void Copy_FromCommandLine(CommandLine cmdline)
    {
      AutoDetectPartNo = cmdline.AutoNo;
      PartNo = cmdline.No;
      PartALL = cmdline.No == -1;
      IsLastPart = cmdline.IsLast || cmdline.No == -1;

      TsPath = cmdline.TsPath;
      D2vPath = cmdline.D2vPath;
      LwiPath = cmdline.LwiPath;
      SrtPath = cmdline.SrtPath;

      Channel = cmdline.Channel;
      Program = cmdline.Program;

      SequenceName = cmdline.SequenceName;
      SequenceName = SequenceName ?? "";

      //エラーチェック
      //PartNo
      if (PartNo <= -2)
        throw new LGLException("PartNo is less than equal -2");
      if (AutoDetectPartNo == false && PartNo == 0)
        throw new LGLException("PartNo is equal 0");

      //ファイルチェック
      //Ts
      if (File.Exists(TsPath) == false)
        throw new LGLException("ts does not exist");

      //D2v  Lwi
      //  コマンドラインで指定されているときだけチェック
      //  srtは削除される可能性があるのでチェックしない
      if (D2vPath != null && File.Exists(D2vPath) == false)
        throw new LGLException("d2v does not exist");

      if (LwiPath != null && File.Exists(LwiPath) == false)
        throw new LGLException("lwi does not exist");

      //拡張子チェック
      if (TsPath != null && Path.GetExtension(TsPath).ToLower() != ".ts")
        throw new LGLException("TsPath is invalide extension");

      if (D2vPath != null && Path.GetExtension(D2vPath).ToLower() != ".d2v")
        throw new LGLException("D2vPath is invalide extension");

      if (LwiPath != null && Path.GetExtension(LwiPath).ToLower() != ".lwi")
        throw new LGLException("LwiPath is invalide extension");

      if (SrtPath != null && Path.GetExtension(SrtPath).ToLower() != ".srt")
        throw new LGLException("SrtPath is invalide extension");
    }



    /// <summary>
    /// 入力ファイルの設定
    /// </summary>
    private static void Make_InputPath(Setting_File setting)
    {
      //TsPath
      TsDir = Path.GetDirectoryName(TsPath);
      TsName = Path.GetFileName(TsPath);
      TsNameWithoutExt = Path.GetFileNameWithoutExtension(TsPath);
      TsShortName = new Regex("[ $|()^　]").Replace(TsName, "_");      //batの特殊文字　置換
      TsShortName = (5 < TsShortName.Length) ? TsShortName.Substring(0, 5) : TsShortName;


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
      SrtPath = SrtPath ?? Path.Combine(TsDir, TsName + ".srt");
      SrtDir = Path.GetDirectoryName(SrtPath);
      SrtName = Path.GetFileName(SrtPath);

      //PluginType  LogoDetector
      {
        string iplugin = setting.sAvs_iPlugin.Trim().ToLower();
        string detector = setting.sLogoDetector.Trim().ToLower();

        bool isD2v = iplugin == "d2v".ToLower();
        bool isLwi = iplugin == "lwi".ToLower();

        bool isLG = detector == "LG".ToLower()
          || detector == "LogoGuillo".ToLower();

        bool isJLS = detector == "JLS".ToLower()
          || detector == "Join_Logo_Scp".ToLower()
          || detector == "Join_Logo_Scpos".ToLower();

        if (isD2v) Avs_iPlugin = PluginType.D2v;
        else if (isLwi) Avs_iPlugin = PluginType.Lwi;
        else Avs_iPlugin = PluginType.Unknown;

        if (isJLS) Detector = LogoDetector.Join_Logo_Scp;
        else if (isLG) Detector = LogoDetector.LogoGuillo;
        else Detector = LogoDetector.Unknown;
      }

    }



    /// <summary>
    /// WorkDirの設定
    /// </summary>
    private static void Make_WorkDir_and_DetectPartNo()
    {
      AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
      AppDir = Path.GetDirectoryName(AppPath);

      //LTopWorkDir、LSystemDir作成
      LTopWorkDir = Path.Combine(AppDir, @"LWork\");
      LSystemDir = Path.Combine(AppDir, @"LSystem\");

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
      timecode_MD5 = tsinfo.CreationTime.ToString("yyyyMMdd_dddd_HHmmss_fffffff");       //ＭＤ５用　　　作成日
      MD5 = Hash.ComputeMD5(TsPath + timecode_MD5 + SequenceName).Substring(0, 10);

      workDirName = timecode_ID + "_" + TsShortName + "_" + MD5;
      LWorkDir = Path.Combine(LTopWorkDir, workDirName);

      if (Directory.Exists(LWorkDir) == false)
        Directory.CreateDirectory(PathList.LWorkDir);
      //LWorkDirが作成できたので、ここからのログはLWorkDir内に書き出される。


      //PartNo検出
      if (AutoDetectPartNo)
        PartNo = new Func<int>(() =>
        {
          //通常のExceptionを投げる
          //TsShortName, LWorkDirが設定される前だとこの関数は処理できない。
          if (PathList.TsShortName == null) throw new Exception();  //TsShortNameが未設定
          if (PathList.LWorkDir == null) throw new Exception();     //LWorkDirが未設定

          //LWorkフォルダ内の  *.p2.frame.cat.txt  を探す
          Log.WriteLine("    AutoDetect PartNo");
          int no = 0;
          for (int i = 100; 1 <= i; i--)
          {
            var catpath = Path.Combine(PathList.LWorkDir,
                                       PathList.TsShortName + ".p" + i + ".frame.cat.txt");
            // found  *.p2.frame.cat.txt
            if (File.Exists(catpath))
            {
              //set no = 2 + 1 
              no = i + 1;
              Log.WriteLine("             found :  " + PathList.TsShortName + ".p" + i + ".frame.cat.txt");
              break;
            }
          }

          if (no == 0)
          {
            no = 1;
            Log.WriteLine("         not found :  " + PathList.TsShortName + ".p1.frame.cat.txt");
            Log.WriteLine("                   :  set no = 1");
          }

          return no;
        })();


      //WorkPath
      WorkName = (1 <= PartNo) ? TsShortName + ".p" + PartNo : TsShortName + ".all";
      WorkPath = Path.Combine(LWorkDir, WorkName);
      WorkName_prv1 = TsShortName + ".p" + (PartNo - 1);
      WorkPath_prv1 = Path.Combine(LWorkDir, WorkName_prv1);
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
    /// Systemフォルダ関連の設定
    /// </summary>
    private static void Make_SystemFile(Setting_File setting)
    {
      //フルパス取得
      //ファイルリストから対象ファイルを取得
      var SearchItem_orEmpty = new Func<FileInfo[], string, string>(
        (filelist, target) =>
        {
          var fileinfo = filelist.Where(fi => fi.Name.ToLower() == target.ToLower())
                                 .FirstOrDefault();
          if (fileinfo != null)
            return fileinfo.FullName;
          else
            return "";
        });
      var SearchItem = new Func<FileInfo[], string, string>(
        (filelist, target) =>
        {
          string fullpath = SearchItem_orEmpty(filelist, target);

          if (fullpath != "")
            return fullpath;
          else
            throw new LGLException("not found: " + target);
        });


      //LSystemDirのファイル一覧から検索、取得
      var dirInfo = new DirectoryInfo(LSystemDir);
      var systemFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);


      SystemIdleMonitor = SearchItem_orEmpty(systemFiles, "SystemIdleMonitor.exe");
      avs2pipemod = SearchItem(systemFiles, "avs2pipemod.exe");


      //Plugin
      if (Avs_iPlugin == PluginType.D2v)
      {
        DGDecode_dll = SearchItem(systemFiles, "DGDecode.dll");
      }
      else if (Avs_iPlugin == PluginType.Lwi)
      {
        LSMASHSource_dll = SearchItem(systemFiles, "LSMASHSource.dll");
      }


      Detector_MultipleRun = setting.iDetector_MultipleRun;

      //Detector
      if (Detector == LogoDetector.Join_Logo_Scp)
      {
        //Join_Logo_Scp
        avsinp_aui = SearchItem(systemFiles, "avsinp.aui");
        Chapter_exe = SearchItem(systemFiles, "Chapter_exe.exe");
        LogoFrame = SearchItem(systemFiles, "LogoFrame.exe");
        Join_Logo_Scp = SearchItem(systemFiles, "Join_Logo_Scp.exe");

        JL_Cmd_Recording = SearchItem(systemFiles, "JL_標準_Recording.txt");
        JL_Cmd_Standard = SearchItem(systemFiles, "JL_標準.txt");
      }
      else if (Detector == LogoDetector.LogoGuillo)
      {
        //LogoGuillo
        LogoGuillo = SearchItem(systemFiles, "LogoGuillo.exe");

        //USE_AVS    LTopWorkDir内に作成
        var AVSPLG = Path.Combine(LTopWorkDir, "USE_AVS");
        if (File.Exists(AVSPLG) == false)
          File.Create(AVSPLG).Close();
      }

      //LogoSelector
      //  SystemDirにあるLogoSelectorを取得。複数ある場合の優先順位は、
      //　                                     （高）  .exe .vbs .js  （低）
      var logoSelector_list = new string[] {
                                              SearchItem_orEmpty(systemFiles, "LogoSelector.exe"),
                                              SearchItem_orEmpty(systemFiles, "LogoSelector.vbs"),
                                              SearchItem_orEmpty(systemFiles, "LogoSelector.js") };

      LogoSelector = logoSelector_list.Where((LSpath) => File.Exists(LSpath)).FirstOrDefault();
      LogoSelector = LogoSelector ?? "";
    }


    /// <summary>
    /// チャプター出力の設定
    /// </summary>
    private static void Make_Chap_and_Misc(Setting_File setting)
    {
      //edit chapter
      Regard_NsecCM_AsMain = setting.dRegard_NsecCM_AsMain;
      Regard_NsecMain_AsCM = setting.dRegard_NsecMain_AsCM;
      Regard_NsecCM_AsMain = 0 < Regard_NsecCM_AsMain ? Regard_NsecCM_AsMain : 0;
      Regard_NsecMain_AsCM = 0 < Regard_NsecMain_AsCM ? Regard_NsecMain_AsCM : 0;

      //output chapter
      Out_tvtp = 0 < setting.bOut_tvtp;
      Out_ogm = 0 < setting.bOut_ogm;
      Out_frame = 0 < setting.bOut_frame;
      Out_rawframe = 0 < setting.bOut_rawframe;

      //chapter directory
      Out_tvtp_toTsDir = 0 < setting.bOut_tvtp_toTsDir;
      Out_misc_toTsDir = 0 < setting.bOut_misc_toTsDir;
      DirPath_tvtp = setting.sDirPath_tvtp;
      DirPath_misc = setting.sDirPath_misc;

      //misc
      //  delete work item
      Mode_DeleteWorkItem = setting.iDeleteWorkItem;
    }




    /// <summary>
    /// エラーチェック
    /// </summary>
    private static void Log_and_ErrorCheck()
    {
      //log
      Log.WriteLine("  No  = 【    " + PathList.PartNo + "    】");
      Log.WriteLine("            IsLast :  " + IsLastPart);
      if (PathList.PartALL || PathList.PartNo == 1)
      {
        Log.WriteLine("        " + PathList.TsPath);
        Log.WriteLine("    AvsInputPlugin :  " + Avs_iPlugin.ToString());
        Log.WriteLine("      LogoDetector :  " + Detector.ToString());
        Log.WriteLine();
      }

      // error check
      if (Avs_iPlugin == PluginType.D2v)
      {
        if (File.Exists(D2vPath) == false)
          throw new LGLException(D2vName + " dose not exist");
      }

      if (Avs_iPlugin == PluginType.Lwi)
      {
        if (File.Exists(LwiPath) == false)
          throw new LGLException(LwiName + " dose not exist");
      }

      if (Avs_iPlugin == PluginType.Unknown)
        throw new LGLException("Unknown  AvsInputPlugin");

      if (Detector == LogoDetector.Unknown)
        throw new LGLException("Unknown  LogoDetector");

      if (Avs_iPlugin == PluginType.D2v
        && Detector == LogoDetector.Join_Logo_Scp)
        throw new LGLException("Cannot select d2v with Join_Logo_Scp");

    }






  }

}