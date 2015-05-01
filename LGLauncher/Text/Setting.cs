using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace LGLauncher
{
  //================================
  //設定ファイル
  //================================
  [Serializable]
  public class Setting
  {
    public int bEnable = 1;
    public int bPrefer_d2v = 1;
    public int iPriority = -1;                             //Normal: 2   BelorNormal: 1   Low: 0   Parent: -1
    public int iLogoGuillo_MultipleRun = 1;
    public int bUseTSDir_asChapDir = 1;
    public string sChapDir_Path = @"   C:\chap_dir   ";
    public string sFrameDir_Path = @"   C:\frame_dir   ";
    public int iDeleteWorkItem = 1;


    public static Setting file;


    //設定ファイル名
    public static readonly string
            AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location,
            AppName = Path.GetFileNameWithoutExtension(AppPath),
            DefXmlName = AppName + ".xml";
    //
    //設定ファイルから読込む
    public static Setting LoadFile()
    {

      if (File.Exists(Setting.DefXmlName) == false)        //デフォルト設定ファイルがない？
      {
        var defSetting = new Setting();
        XmlFile.Save(Setting.DefXmlName, defSetting);      //ファイル保存
      }
      string xmlpath = Setting.DefXmlName;


      file = XmlFile.Load<Setting>(xmlpath);               //xml読込

      //文字列がスペースのみだと読込み時にstring.Emptyになり、xmlに書き込むと<sChapDir_Path />になる。
      //  スペースを加えて<sChapDir_Path>    </sChapDir_Path>になるようにする。
      file.sChapDir_Path = (string.IsNullOrWhiteSpace(file.sChapDir_Path))
                              ? new String(' ', 8) : file.sChapDir_Path;
      file.sFrameDir_Path = (string.IsNullOrWhiteSpace(file.sFrameDir_Path))
                              ? new String(' ', 8) : file.sFrameDir_Path;

      XmlFile.Save(xmlpath, file);                         //xml上書き、存在しない項目はxmlに追加される


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



  //================================
  //パス一覧
  //================================
  #region PathList
  public static class PathList
  {

    public static string
      //Input file
                          TsPath, TsDir, TsName, TsNameWithoutExt, TsShortName,
                          SubDir,
                          D2vPath, D2vDir, D2vName,
                          LwiPath, LwiDir, LwiName,
                          LwiFooterPath, LwiFooterDir, LwiFooterName,
                          SrtPath, SrtDir, SrtName,
      //Work item
                          AppPath, AppDir, AppName,
                          LSystemDir, LTopWorkDir, LWorkDir,
                          WorkPath, WorkName,
                          WorkPath_m1, WorkName_m1,
      //LogoGuillo
                          LogoGuillo,
                          AVS2X, AVSPLG,
                          LogoSelector,
                          ChapDir;

    public static int No;
    public static string Channel, Program;
    public static bool D2vMode, IsLast;


    //ディレクトリ設定
    public static void InitializeDir()
    {
      //app path
      AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
      AppDir = Path.GetDirectoryName(PathList.AppPath);
      AppName = Path.GetFileName(PathList.AppPath);

      //カレントディレクトリ設定
      //  アプリのパスに変更
      Directory.SetCurrentDirectory(PathList.AppDir);

      //LSystemDir、LTopWorkDir作成
      LTopWorkDir = Path.Combine(AppDir, "LWork");
      LSystemDir = Path.Combine(AppDir, "LSystem");
      if (Directory.Exists(LTopWorkDir) == false) Directory.CreateDirectory(PathList.LTopWorkDir);
      if (Directory.Exists(LSystemDir) == false) Directory.CreateDirectory(PathList.LSystemDir);
    }


    //パス作成
    //　設定ファイルを読み込んだ後に実行
    public static bool Make()
    {
      //PartNo
      if (No == 0) { Log.WriteLine("No. is not assigned"); return false; }
      else if (No < -1) { Log.WriteLine("invalid No.  No = " + No); return false; }
      //ファイルチェック
      if (File.Exists(TsPath) == false) { Log.WriteLine("File.Exists(TsPath) == false"); return false; }
      if (D2vPath != null && File.Exists(D2vPath) == false) { Log.WriteLine("File.Exists(D2vPath) == false"); return false; }
      if (LwiPath != null && File.Exists(LwiPath) == false) { Log.WriteLine("File.Exists(LwiPath) == false"); return false; }
      //拡張子チェック
      if (TsPath != null && Path.GetExtension(TsPath).ToLower() != ".ts") { Log.WriteLine("ext != .ts"); return false; }
      if (D2vPath != null && Path.GetExtension(D2vPath).ToLower() != ".d2v") { Log.WriteLine("ext != .d2v"); return false; }
      if (LwiPath != null && Path.GetExtension(LwiPath).ToLower() != ".lwi") { Log.WriteLine("ext != .lwi"); return false; }
      if (SrtPath != null && Path.GetExtension(SrtPath).ToLower() != ".srt") { Log.WriteLine("ext != .srt"); return false; }


      //
      //Input file
      //
      string tmpPath;
      SubDir = SubDir ?? "disable::";                      //nullなら無効なパス文字を入れる。


      //TsPath
      TsDir = Path.GetDirectoryName(TsPath);
      TsName = Path.GetFileName(TsPath);
      TsNameWithoutExt = Path.GetFileNameWithoutExtension(TsPath);
      TsShortName = (5 < TsName.Length) ? TsName.Substring(0, 5) : TsName;

      //D2vPath
      //  file is in SubDir ?
      tmpPath = Path.Combine(SubDir, TsName + ".pp.d2v");
      D2vPath = D2vPath ??
                    (File.Exists(tmpPath) ? tmpPath : null);
      //  make path by ts pame if null
      D2vPath = D2vPath ?? TsPath + ".pp.d2v";
      D2vDir = Path.GetDirectoryName(D2vPath);
      D2vName = Path.GetFileName(D2vPath);

      //LwiPath
      tmpPath = Path.Combine(SubDir, TsName + ".pp.lwi");
      LwiPath = LwiPath ??
                    (File.Exists(tmpPath) ? tmpPath : null);
      LwiPath = LwiPath ?? TsPath + ".pp.lwi";
      LwiDir = Path.GetDirectoryName(LwiPath);
      LwiName = Path.GetFileName(LwiPath);

      //LwiFooter
      LwiFooterPath = LwiPath + "footer";
      LwiFooterDir = Path.GetDirectoryName(LwiFooterPath);
      LwiFooterName = Path.GetFileName(LwiFooterPath);

      //SrtPath
      tmpPath = Path.Combine(SubDir, TsNameWithoutExt + ".srt");
      SrtPath = SrtPath ??
                    (File.Exists(tmpPath) ? tmpPath : null);
      SrtPath = SrtPath ?? Path.Combine(TsDir, TsNameWithoutExt + ".srt");
      SrtDir = Path.GetDirectoryName(SrtPath);
      SrtName = Path.GetFileName(SrtPath);

      //mode
      D2vMode = (0 < Setting.file.bPrefer_d2v);

      //ファイル再チェック
      //　SrtPathはファイルがなくてもいい
      if (D2vMode)
      {
        //D2vPath
        if (File.Exists(PathList.D2vPath) == false)
        { Log.WriteLine("File.Exists(PathList.D2vPath) == false2"); return false; }
      }
      else
      {
        //LwiPath
        if (File.Exists(PathList.LwiPath) == false)
        { Log.WriteLine("File.Exists(PathList.LwiPath) == false2"); return false; }

        //LSMASHSource.dll
        var LSmashDll = Path.Combine(PathList.LSystemDir, "LSMASHSource.dll");
        if (File.Exists(LSmashDll) == false) { Log.WriteLine("Not Exist LSMASHSource.dll"); return false; }

        //LwiPath = TsPath + ".lwi"だと処理できない。
        if ((PathList.LwiPath).ToLower() == (TsPath + ".lwi").ToLower())
        { Log.WriteLine("should be LwiPath != TsPath + \".lwi\""); return false; }
      }



      //
      //Work item
      //    .\LWork\TsName\TsName.p1.info.txtが２５５文字を超えることがあったので短縮名を使う。
      //    衝突は考えない、重複チェックはしてない。
      string workDirName, timecode, timecode_full, md5;
      var tsinfo = new FileInfo(TsPath);
      timecode = tsinfo.CreationTime.ToString("ddHHmm");                                 //作成日　識別用
      timecode_full = tsinfo.CreationTime.ToString("yyyyMMdd_ddddHHmmssfffffff");        //for MD5
      md5 = ComputeMD5(TsPath + timecode_full).Substring(0, 10);                         //MD5 by ファイルパス＋作成日

      workDirName = timecode + "_" + TsShortName + "_" + md5;
      LWorkDir = Path.Combine(LTopWorkDir, workDirName);
      if (Directory.Exists(LWorkDir) == false) Directory.CreateDirectory(PathList.LWorkDir);       //WorkDir作成


      //WorkPath作成
      WorkName = (1 <= No) ? TsShortName + ".p" + No : TsShortName + ".all";
      WorkPath = Path.Combine(LWorkDir, WorkName);
      WorkName_m1 = TsShortName + ".p" + (No - 1);
      WorkPath_m1 = Path.Combine(LWorkDir, WorkName_m1);



      //
      //LogoGuillo
      //
      //LogoGuillo
      LogoGuillo = Path.Combine(LSystemDir, "logoGuillo.exe");
      if (File.Exists(LogoGuillo) == false) Log.WriteLine("File.Exists(LogoGuillo) == false");     //無くても継続

      //avs2pipemod
      var avs2pipemod = Path.Combine(LSystemDir, "avs2pipemod.exe");
      AVS2X = avs2pipemod;
      if (File.Exists(AVS2X) == false) Log.WriteLine("File.Exists(AVS2X) == false");

      //USE_AVS    LTopWorkDirに作成
      AVSPLG = Path.Combine(LTopWorkDir, "USE_AVS");
      if (File.Exists(AVSPLG) == false) File.Create(AVSPLG).Close();

      //LogoSelector
      //  AppDir、SystemDirにあるLogoSelectorを取得
      //　複数ある場合の優先順位は、
      //　　（高）  .exe .vbs .js  （低）
      var lsPathList = new string[]{
                                Path.Combine(LSystemDir, "LogoSelector.exe"),
                                Path.Combine(LSystemDir, "LogoSelector.vbs"),
                                Path.Combine(LSystemDir, "LogoSelector.js"),
                                };
      //  ファイルの有無でフィルター、最初の要素を取得
      LogoSelector = lsPathList.Where((lspath) => File.Exists(lspath)).FirstOrDefault();


      //ChapDir
      if (0 < Setting.file.bUseTSDir_asChapDir)
        ChapDir = TsDir;
      else if (Directory.Exists(Setting.file.sChapDir_Path))
        ChapDir = Setting.file.sChapDir_Path;
      else
        ChapDir = string.Empty;
      Setting.file.sChapDir_Path = null;                   //アクセスしないようにnullをいれる


      //IsLast
      //  引数で指定されてる or No == -1
      PathList.IsLast |= (PathList.No == -1);


      return true;
    }


    //
    //MD5作成
    //
    static string ComputeMD5(string srcText)
    {
      //文字列をbyte型配列に変換する
      byte[] data = System.Text.Encoding.UTF8.GetBytes(srcText);
      var md5 = System.Security.Cryptography.MD5.Create();
      byte[] bytes_md5 = md5.ComputeHash(data);                                          //ハッシュ値を計算する
      md5.Clear();
      string result = BitConverter.ToString(bytes_md5).ToLower().Replace("-", "");       //16進数の文字列に変換
      return result;
    }



    //
    //一覧作成　デバッグ用
    //
    static new string ToString()
    {
      var text = new StringBuilder();
      text.AppendLine("TsPath        = " + TsPath);
      text.AppendLine("ItemDir       = " + SubDir);
      text.AppendLine("D2vPath       = " + D2vPath);
      text.AppendLine("LwiPath       = " + LwiPath);
      text.AppendLine("LwiFooterPath = " + LwiFooterPath);
      text.AppendLine("SrtPath       = " + SrtPath);
      return text.ToString();
    }

  }
  #endregion


















}
