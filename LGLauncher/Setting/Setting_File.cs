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
    const double CurrentVer = 2.0;

    public double Ver = 0.0;
    public int Enable = 1;
    public string memo1 = "  set sAvs_iPlugin     d2v            or  lwi         ";
    public string memo2 = "  set sLogoDetector    Join_Logo_Scp  or  LogoGuillo  ";
    public string memo3 = "                       JLS            or  LG          ";
    public string memo4 = "  cannot set d2v with Join_Logo_Scp                   ";
    public string Avs_iPlugin = "  lwi         ";
    public string LogoDetector = "  LogoGuillo  ";
    public string space_1 = "";

    //edit chapter
    public double Regard_NsecCM_AsMain = 14.0;
    public double Regard_NsecMain_AsCM = 29.0;
    public string space_2 = "";

    //output chapter
    public int Out_tvtp = 1;
    public int Out_ogm = 1;
    public int Out_frame = 1;
    public int Out_rawframe = 0;
    public string space_3 = "";

    //chapter directory
    public int Out_tvtp_toTsDir = 1;
    public int Out_misc_toTsDir = 1;
    public string DirPath_tvtp = @"   C:\tvtp_Dir               ";
    public string DirPath_misc = @"   C:\ogm_and_frame_Dir      ";
    public string space_4 = "";

    public int DeleteWorkItem = 3;


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
      //デフォルト名を使用
      if (string.IsNullOrEmpty(xmlpath))
      {
        xmlpath = Default_XmlPath;

        if (File.Exists(xmlpath) == false)
        {
          var def_Setting = new Setting_File();
          XmlRW.Save(xmlpath, def_Setting);
        }
      }

      var file = XmlRW.Load<Setting_File>(xmlpath);

      //新たに追加された項目、削除された項目を書き換え。
      if (file.Ver != CurrentVer)
      {
        file.Ver = CurrentVer;
        XmlRW.Save(xmlpath, file);
      }

      return file;
    }


  }


}
