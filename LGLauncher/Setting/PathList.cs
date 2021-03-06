﻿using System;
using System.Collections.Generic;
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
  [Flags]
  enum Plugin
  {
    None = 0x01,
    D2v = 0x02,
    D2v_DGDecode = 0x04,
    D2v_MPEG2DecPlus = 0x08,
    Lwi = 0x10,
  }

  /// <summary>
  /// FrameServer
  /// </summary>
  enum AvsVpy
  {
    /*
     vpyを利用することはないが、作成したので残しておく。
    */
    None,
    Avs,
    Vpy,
  }

  /// <summary>
  /// 検出器
  /// </summary>
  [Flags]
  enum Detector
  {
    None = 0x01,
    Join_Logo_Scp = 0x02,
    LogoGuillo = 0x04,
    AutoSelect = 0x08,
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
    public static string D2vNameInLWork { get { return TsShortName + ".d2v"; } }

    //    Lwi
    public static string LwiPath { get; private set; }
    public static string LwiDir { get { return Path.GetDirectoryName(LwiPath); } }
    public static string LwiName { get { return Path.GetFileName(LwiPath); } }
    public static string LwiPathInLWork { get { return Path.Combine(LWorkDir, LwiNameInLWork); } }
    public static string LwiNameInLWork { get { return TsShortName + ".lwi"; } }

    //    LwiFooter
    public static string LwiFooterPath { get; private set; }
    public static string LwiFooterDir { get { return Path.GetDirectoryName(LwiFooterPath); } }
    public static string LwiFooterName { get { return Path.GetFileName(LwiFooterPath); } }

    //    Srt
    public static string SrtPath { get; private set; }
    public static string SrtDir { get { return Path.GetDirectoryName(SrtPath); } }
    public static string SrtName { get { return Path.GetFileName(SrtPath); } }

    //App
    public static string AppPath { get { return System.Reflection.Assembly.GetExecutingAssembly().Location; } }
    public static string AppDir { get { return Path.GetDirectoryName(AppPath); } }
    public static string AppName { get { return Path.GetFileName(AppPath); } }

    //  [  Work dir  ]
    //    LSystemDir     C:\EDCB\Write\Write_PF\LGLauncher\LSystem
    //    LTopWorkDir    C:\EDCB\Write\Write_PF\LGLauncher\LWork
    //    LWorkDir       C:\EDCB\Write\Write_PF\LGLauncher\LWork\010101_ショップジ_0a1b308c1
    public static string LSystemDir { get; private set; }
    public static string LTopWorkDir { get; private set; }
    public static string LWorkDir { get; private set; }
    //WorkPath
    //             work path    C:\EDCB\Write\Write_PF\LGLauncher\LWork\010101_ショップジ_0a1b308c1\ショップジ.p3
    //                  name    ショップジ.p3
    //    previous work path    C:\EDCB\Write\Write_PF\LGLauncher\LWork\010101_ショップジ_0a1b308c1\ショップジ.p2
    //                  name    ショップジ.p2
    public static string WorkPath { get { return Path.Combine(LWorkDir, WorkName); } }
    public static string WorkName { get { return TsShortName + ".p" + PartNo; } }
    public static string WorkPath_prv { get { return Path.Combine(LWorkDir, WorkName_prv); } }
    public static string WorkName_prv { get { return TsShortName + ".p" + (PartNo - 1); } }

    /*
     * IsAll
     *     - 録画後のＴＳ用
     *     - １回のLGLauncherでＴＳ全体を処理する。
     *     - avsは30分ごとにTrim()。avsごとにPartNo++
     *     
     * IsPart
     *     - 録画中のＴＳ用
     *     - コマンドラインに -part, -lastがある
     *     - 数回のLGLauncherでＴＳ全体を処理する。
     *     - avsは30分ごとにTrim()。avsごとにPartNo++
     *     
     * PartNo
     *     - 分割されたavsの連番 
     */
    public static int PartNo { get; private set; }
    public static bool Is1stPart { get { return PartNo == 1 || IsAll; } }
    public static bool IsPart { get; private set; }
    public static bool IsAll { get { return IsPart == false; } }


    /*
     * IsLastSplit      最後の SplitTrimか？           (各プロセスごとにIsLastSplitがある)
     * IsLastProcess    最後の LGLauncherプロセスか？  ( コマンドラインに -last          )
     * IsLastPart       最後の PartNoか？              ( IsLastProcess & IsLastSplit     )
     */
    // SplitTrim : フレーム数を取得してTrim()したavsをさらに分割したavs
    public static bool DisableSplit { get; private set; }
    public static bool IsLastSplit { get; private set; }
    public static void Update_IsLastSplit(bool islast) { IsLastSplit = islast; }
    private static bool IsLast_on_cmdline;
    public static bool IsLastProcess { get { return IsAll || IsLast_on_cmdline; } }
    public static bool IsLastPart { get { return IsLastProcess && IsLastSplit; } }
    //PartNo++
    public static void IncrementPartNo() { PartNo++; }


    //  [  LSystem binary path  ]  
    public static string SystemIdleMonitor { get; private set; }
    public static string avs2pipemod { get; private set; }

    //AvsVpy
    private static AvsVpy AvsVpy;
    public static bool IsAvs { get { return AvsVpy.HasFlag(AvsVpy.Avs); } }
    public static bool IsVpy { get { return AvsVpy.HasFlag(AvsVpy.Vpy); } }
    public static string AvsVpyExt { get { return "." + AvsVpy.ToString().ToLower(); } }

    //InputPlugin
    private static Plugin InputPlugin;
    public static bool IsD2v { get { return InputPlugin.HasFlag(Plugin.D2v); } }
    public static bool IsD2v_DGDecode { get { return InputPlugin.HasFlag(Plugin.D2v_DGDecode); } }
    public static bool IsD2v_MPEG2DecPlus { get { return InputPlugin.HasFlag(Plugin.D2v_MPEG2DecPlus); } }
    public static bool IsLwi { get { return InputPlugin.HasFlag(Plugin.Lwi); } }
    public static string DGDecode { get; private set; }
    public static string MPEG2DecPlus { get; private set; }
    public static string LSMASHSource { get; private set; }
    public static string d2vsource { get; private set; }
    public static string vslsmashsoruce { get; private set; }

    //Select logo
    public static string Channel { get; private set; }
    public static string Program { get; private set; }
    public static string LogoSelector { get; private set; }

    //Detector
    private static Detector Detector;
    public static bool IsJLS { get { return Detector.HasFlag(Detector.Join_Logo_Scp); } }
    public static bool IsLG { get { return Detector.HasFlag(Detector.LogoGuillo); } }
    private static bool DetectorIsAutoSelect { get { return Detector.HasFlag(Detector.AutoSelect); } }
    public static int Detector_MultipleRun { get; private set; }
    public static readonly string[] DetectorName =
      new string[] { "Chapter_exe", "LogoFrame", "LogoGuillo" };
    //  LogoGuillo
    public static string LogoGuillo { get; private set; }
    //  Join_Logo_Scp
    public static string avsinp_aui { get; private set; }
    public static string Chapter_exe { get; private set; }
    public static string LogoFrame { get; private set; }
    public static string Join_Logo_Scp { get; private set; }
    //  JL command
    public static string JL_Cmd_OnRec { get; private set; }
    public static string JL_Cmd_Standard { get; private set; }

    //  [  Chapter  ]
    public static double Regard_NsecCM_AsMain { get; private set; }
    public static double Regard_NsecMain_AsCM { get; private set; }
    public static int Output_RawFrame { get; private set; }
    public static int Output_Frame { get; private set; }
    public static int Output_Tvtp { get; private set; }
    public static int Output_Jls { get; private set; }
    public static string ChapDir_Tvtp { get; private set; }
    public static string ChapDir_Misc { get; private set; }

    //  [  Clean Work Item  ]
    public static int Mode_CleanWorkItem { get; private set; }


    /// <summary>
    /// 初期化、パス作成
    /// </summary>
    public static void Init(Setting_CmdLine cmdline, Setting_File setting)
    {
      Copy_fromCommandLine(cmdline);

      Make_InputPath(setting);

      Make_WorkDir();

      Detect_PartNo();

      Get_BinaryPath();

      Set_Chap_and_Misc(setting);

      Log_and_ErrorCheck();
    }


    /// <summary>
    /// コマンドラインから設定をコピー
    /// </summary>
    private static void Copy_fromCommandLine(Setting_CmdLine cmdline)
    {
      IsPart = cmdline.IsPart || cmdline.IsLast;
      IsLast_on_cmdline = cmdline.IsLast;
      TsPath = cmdline.TsPath;
      D2vPath = cmdline.D2vPath;
      LwiPath = cmdline.LwiPath;
      SrtPath = cmdline.SrtPath;
      Channel = cmdline.Channel ?? "";
      Program = cmdline.Program ?? "";
      DisableSplit = cmdline.DisableSplit;

      if (File.Exists(TsPath) == false)
        throw new LGLException("ts does not exist");
      //  コマンドラインで指定されているときのみチェック
      //  srtは削除されるている可能性があるのでチェックしない
      if (D2vPath != null && File.Exists(D2vPath) == false)
        throw new LGLException("d2v does not exist");
      if (LwiPath != null && File.Exists(LwiPath) == false)
        throw new LGLException("lwi does not exist");

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
      TsShortName = new Regex("[ $|()^　]").Replace(TsName, "_");      //batの特殊文字
      TsShortName = (5 < TsShortName.Length) ? TsShortName.Substring(0, 5) : TsShortName;

      //作成中の *.ts.pp.lwi？
      D2vPath = D2vPath ?? TsPath + ".pp.d2v";
      LwiPath = LwiPath ?? TsPath + ".pp.lwi";
      LwiFooterPath = LwiPath + "footer";
      SrtPath = SrtPath ?? Path.Combine(TsDir, TsNameWithoutExt + ".srt");

      //作成済みの *.ts.lwi？
      if (IsAll)
      {
        if (File.Exists(D2vPath) == false && File.Exists(TsPath + ".d2v"))
          D2vPath = TsPath + ".d2v";
        if (File.Exists(LwiPath) == false && File.Exists(TsPath + ".lwi"))
          LwiPath = TsPath + ".lwi";
      }

      //Plugin  Detector  AvsVpy
      string plugin = setting.InputPlugin.Trim().ToLower();
      string detector = setting.Detector.Trim().ToLower();
      bool isD2v = plugin == "d2v".ToLower();
      bool isLwi = plugin == "lwi".ToLower();
      bool isJLS = detector == "JLS".ToLower();
      bool isLG = detector == "LG".ToLower();
      bool autoSelect = detector == "Auto".ToLower();
      InputPlugin = isD2v ? Plugin.D2v
                  : isLwi ? Plugin.Lwi
                  : Plugin.None;
      Detector = isJLS ? Detector.Join_Logo_Scp
                 : isLG ? Detector.LogoGuillo
                 : autoSelect ? Detector.AutoSelect
                 : Detector.None;
      //Avs固定 
      const AvsVpy avs = AvsVpy.Avs;
      AvsVpy = avs;
      if (AvsVpy == AvsVpy.Vpy)
        throw new NotImplementedException();
    }



    /// <summary>
    /// WorkDirの設定
    /// </summary>
    private static void Make_WorkDir()
    {
      //LSystem
      LTopWorkDir = Path.Combine(AppDir, @"LWork");
      LSystemDir = Path.Combine(AppDir, @"LSystem");
      if (Directory.Exists(LTopWorkDir) == false)
        Directory.CreateDirectory(LTopWorkDir);
      if (Directory.Exists(LSystemDir) == false)
        Directory.CreateDirectory(LSystemDir);

      //WorkDir
      //  衝突は考えない。
      string dirName;
      {
        var fi = new FileInfo(TsPath);
        string time = fi.CreationTime.ToString("ddHHmm");
        string time_full = fi.CreationTime.ToString("yyyyMMdd_dddd_HHmmss_fffffff");
        string MD5 = Hash.ComputeMD5(TsPath + time_full).Substring(0, 10);
        dirName = time + "_" + TsShortName + "_" + MD5;
      }
      LWorkDir = Path.Combine(LTopWorkDir, dirName);
      if (Directory.Exists(LWorkDir) == false)
        Directory.CreateDirectory(LWorkDir);
    }

    /// <summary>
    /// ＭＤ５作成
    /// </summary>
    class Hash
    {
      public static string ComputeMD5(string srcText)
      {
        byte[] data = Encoding.UTF8.GetBytes(srcText);     //文字列をbyte型配列に変換する
        var md5 = System.Security.Cryptography.MD5.Create();
        byte[] bytes_md5 = md5.ComputeHash(data);          //ハッシュ値を計算する
        md5.Clear();
        string result;
        result = BitConverter.ToString(bytes_md5);         //16進数の文字列に変換
        result = result.ToLower().Replace("-", "");
        return result;
      }
    }


    /// <summary>
    /// PartNo検出
    /// </summary>
    private static void Detect_PartNo()
    {
      //LWorkDir,TsShortName設定前だと処理できない。
      if (LWorkDir == null) throw new Exception();
      if (TsShortName == null) throw new Exception();

      PartNo = IsAll ? 1 : Detect_PartNo_fromFileName();
      if (PartNo <= 0)
        throw new LGLException("Invalid PartNo.  PartNo = " + PartNo);
    }
    /// <summary>
    /// PartNo検出、作業ファイル名から取得
    /// </summary>
    private static int Detect_PartNo_fromFileName()
    {
      //  search *.p2.frame.cat.txt
      var files = Directory.GetFiles(LWorkDir, TsShortName + ".p*.frame.cat.txt");
      if (files.Count() == 0)
        return 1;//not found

      //ファイルパス  -->  PartNo抽出
      var strNums = files.Select(fullname =>
      {
        //"movie.p1.frame.cat.txt"
        string name = Path.GetFileName(fullname);
        //"movie.p"
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
      return intNums.Last() + 1;
    }



    /// <summary>
    /// フォルダ内から目的のファイルパスを取得
    /// </summary>
    class FileSearch
    {
      FileInfo[] files;

      public FileSearch(string dir)
      {
        var di = new DirectoryInfo(dir);
        files = di.GetFiles("*", SearchOption.AllDirectories);
      }
      /// <summary>
      /// フルパス  or  null
      /// </summary>
      public string Get_orNull(string filename)
      {
        var fileInfo = files.Where(fi => fi.Name.ToLower() == filename.ToLower())
          .FirstOrDefault();
        return fileInfo != null ? fileInfo.FullName : null;
      }
      /// <summary>
      /// フルパス  or  LGLException
      /// </summary>
      public string Get(string filename)
      {
        string fullpath = Get_orNull(filename);
        if (fullpath != null)
          return fullpath;
        else
          throw new LGLException("not found: " + filename);
      }
    }


    /// <summary>
    /// LSystemフォルダ内の各バイナリを取得
    /// </summary>
    private static void Get_BinaryPath()
    {
      FileSearch searcher = new FileSearch(LSystemDir);

      LogoSelector = searcher.Get("LogoSelector.exe");
      SystemIdleMonitor = searcher.Get_orNull("SystemIdleMonitor.exe");
      avs2pipemod = searcher.Get("avs2pipemod.exe");

      //InputPlugin
      if (IsAvs)
      {
        if (IsD2v)
        {
          MPEG2DecPlus = searcher.Get_orNull("MPEG2DecPlus.dll");
          DGDecode = searcher.Get_orNull("DGDecode.dll");
          if (MPEG2DecPlus != null)
            InputPlugin |= Plugin.D2v_MPEG2DecPlus;  //MPEG2DecPlus.dll優先 
          else if (DGDecode != null)
            InputPlugin |= Plugin.D2v_DGDecode;
          if (MPEG2DecPlus == null && DGDecode == null)
            throw new LGLException("not found d2v plugin");
        }
        else if (IsLwi)
          LSMASHSource = searcher.Get("LSMASHSource.dll");
      }
      else if (IsVpy)
      {
        if (IsD2v)
          d2vsource = searcher.Get("d2vsource.dll");
        else if (IsLwi)
          vslsmashsoruce = searcher.Get("vslsmashsource.dll");
      }

      //Detector
      if (IsJLS || DetectorIsAutoSelect)
      {
        avsinp_aui = searcher.Get("avsinp.aui");
        Chapter_exe = searcher.Get("Chapter_exe.exe");
        LogoFrame = searcher.Get("LogoFrame.exe");
        Join_Logo_Scp = searcher.Get("Join_Logo_Scp.exe");
        JL_Cmd_OnRec = searcher.Get("JL_標準_Rec.txt");
        JL_Cmd_Standard = searcher.Get("JL_標準.txt");
      }
      if (IsLG || DetectorIsAutoSelect)
      {
        LogoGuillo = searcher.Get("LogoGuillo.exe");
      }

      Bat.LogoSelector.Run(LogoSelector, Channel, Program, TsPath);
      if (DetectorIsAutoSelect)
      {
        if (Bat.LogoSelector.HasLogo_Param)
          Detector |= Detector.LogoGuillo;
        else if (Bat.LogoSelector.HasLogo)
          Detector |= Detector.Join_Logo_Scp;
      }

      //USE_AVSをLTopWorkDir内に作成
      var USE_AVS = Path.Combine(LTopWorkDir, "USE_AVS");
      try
      {
        if (File.Exists(USE_AVS) == false)
          File.Create(USE_AVS).Close();
      }
      catch (IOException)
      {
        //多重起動で別プロセスとぶつかった
        System.Threading.Thread.Sleep(500);
        if (File.Exists(USE_AVS) == false)
          throw new LGLException("USE_AVS creating error");
      }
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
      Output_RawFrame = setting.Output_RawFrame;
      Output_Frame = setting.Output_Frame;
      Output_Tvtp = setting.Output_Tvtp;
      Output_Jls = setting.Output_Jls;

      //chapter directory
      ChapDir_Tvtp = setting.ChapDir_Tvtp;
      ChapDir_Misc = setting.ChapDir_Misc;
      ChapDir_Tvtp = Directory.Exists(ChapDir_Tvtp) ? ChapDir_Tvtp : TsDir;
      ChapDir_Misc = Directory.Exists(ChapDir_Misc) ? ChapDir_Misc : TsDir;

      //misc
      Detector_MultipleRun = setting.Detector_MultipleRun;
      Mode_CleanWorkItem = setting.CleanWorkItem;
    }


    /// <summary>
    /// エラーチェック
    /// </summary>
    private static void Log_and_ErrorCheck()
    {
      Log.WriteLine("    No  = 【    " + PartNo + "    】");
      if (Is1stPart)
      {
        Log.WriteLine("        " + TsPath);
        Log.WriteLine("    InputPlugin  :  " + InputPlugin.ToString());
        Log.WriteLine("    Detector     :  " + Detector.ToString());
      }
      if (IsLastProcess)
        Log.WriteLine("    IsLast       :  " + IsLastProcess);
      Log.WriteLine();
      if (Is1stPart)
        Log.WriteLine(Bat.LogoSelector.ResultLog);

      if (IsD2v)
        if (File.Exists(D2vPath) == false)
          throw new LGLException("d2v dose not exist: " + D2vName);
      if (IsLwi)
        if (File.Exists(LwiPath) == false)
          throw new LGLException("lwi dose not exist: " + LwiName);
      if (InputPlugin == Plugin.None)
        throw new LGLException("None InputPlugin");
      if (Detector == Detector.None)
        throw new LGLException("None Detector");
      if (AvsVpy == AvsVpy.None)
        throw new LGLException("None AvsVpy");
      if (IsD2v && DetectorIsAutoSelect)
        throw new LGLException("Cannot select d2v with Both");
      if (IsD2v && IsJLS)
        throw new LGLException("Cannot select d2v with JLS");
      if (DetectorIsAutoSelect)
        if (IsLG == false && IsJLS == false)
          throw new LGLException("Fail to select detector.   Check Logo and Param path");
    }



  }

}