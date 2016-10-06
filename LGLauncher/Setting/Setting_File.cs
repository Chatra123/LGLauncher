using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace LGLauncher
{
  using OctNov.IO;

  /// <summary>
  /// 設定ファイル
  /// </summary>
  [Serializable]
  public class Setting_File
  {
    const double CurrentRev = 14.2;
    
    public double Rev = 0.0;
    public string memo31 = "  Set InputPlugin     d2v  or  lwi  ";
    public string memo32 = "  Set Detector        JLS  or  LG   ";
    public string memo33 = "      cannot set d2v with JLS       ";
    public int Enable = 1;
    public string InputPlugin = "  lwi  ";
    public string Detector = "  LG  ";
    public int Detector_MultipleRun = 1;
    public string space_1 = "";

    //edit chapter
    public double Regard_NsecCM_AsMain = 14.0;
    public double Regard_NsecMain_AsCM = 20.0;
    public string space_2 = "";

    //output chapter
    public int Output_Tvtp = 2;
    public int Output_Ogm = 0;
    public int Output_Frame = 1;
    public int Output_RawFrame = 0;
    public int Output_Scp = 0;
    public string space_3 = "";

    //chapter directory
    public string ChapDir_Tvtp = @"  C:\Tvtp_Directory            ";
    public string ChapDir_Misc = @"  C:\Ogm_and_Frame_Directory   ";
    public string space_4 = "";

    public int CleanWorkItem = 2;


    //設定ファイル名
    private static readonly string
            AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location,
            AppDir = Path.GetDirectoryName(AppPath),
            AppName = Path.GetFileNameWithoutExtension(AppPath),
            Default_XmlName = AppName + ".xml",
            Default_XmlPath = Path.Combine(AppDir, Default_XmlName);

    /// <summary>
    /// 設定ファイルを読み込む
    /// </summary>
    /// <param name="xmlpath">ファイル名を指定</param>
    public static Setting_File LoadFile(string xmlpath = null)
    {
      //デフォルト名を使用、新規作成
      if (string.IsNullOrEmpty(xmlpath))
      {
        xmlpath = Default_XmlPath;

        if (File.Exists(xmlpath) == false)
          XmlRW.Save(xmlpath, new Setting_File());
      }

      var file = XmlRW.Load<Setting_File>(xmlpath);
      file = file ?? new Setting_File();

      //追加された項目、削除された項目を書き換え。
      //ユーザーが消したタグなども復元される。
      if (file.Rev != CurrentRev)
      {
        file.Rev = CurrentRev;
        XmlRW.Save(xmlpath, file);
      }

      return file;
    }


  }


}
