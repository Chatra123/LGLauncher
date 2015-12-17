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
    public int bEnable = 1;
    public int iPriority = -1;
    public string memo1 = "  set sAvs_iPlugin     d2v            or  lwi         ";
    public string memo2 = "  set sLogoDetector    Join_Logo_Scp  or  LogoGuillo  ";
    public string memo3 = "                       JLS            or  LG          ";
    public string memo4 = "  cannot set d2v with Join_Logo_Scp                   ";
    public string sAvs_iPlugin = "  lwi         ";
    public string sLogoDetector = "  LogoGuillo  ";
    public int iDetector_MultipleRun = 1;
    public string space_1 = "";

    //enable chapter
    public int bOut_tvtp = 1;
    public int bOut_nero = 1;
    public int bOut_frame = 0;
    public int bOut_rawframe = 0;
    public int bOut_tvtp_toTsDir = 1;
    public int bOut_misc_toTsDir = 1;
    public string space_2 = "";

    //chapter directory
    public string sDirPath_tvtp = @"   C:\tvtp_Dir                ";
    public string sDirPath_misc = @"   C:\frame_and_nero_Dir      ";
    public string space_3 = "";

    public int iDeleteWorkItem = 2;


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
    /// <param name="xmlpath">読込むファイルを指定</param>
    public static Setting_File LoadFile(string xmlpath = null)
    {
      //デフォルト名を使用
      if (xmlpath == null)
      {
        xmlpath = Default_XmlPath;

        if (File.Exists(xmlpath) == false)
        {
          //設定ファイル作成
          var def_Setting = new Setting_File();
          XmlRW.Save(xmlpath, def_Setting);      //保存
        }
      }

      var file = XmlRW.Load<Setting_File>(xmlpath);

      XmlRW.Save(xmlpath, file);                 //古いバージョンのファイルなら新たに追加された項目がxmlに加わる。

      //プロセス優先度設定
      SetPriority(file.iPriority);

      return file;
    }


    /// <summary>
    /// プロセス優先度設定
    /// </summary>
    static void SetPriority(int ipriority)
    {
      var self = Process.GetCurrentProcess().PriorityClass;

      //優先度は下げるのみ。
      //優先度を上げることはしない。
      if (ipriority == 2)  // ==2  normal
      {
        //do nothing
      }
      if (ipriority == 1   // == 1 BelowNormal
        && self != ProcessPriorityClass.Idle)
      {
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
      }
      else if (ipriority == 0)  // == 0 Idle
      {
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;
      }
      else if (ipriority == -1)  // == 1 Auto by Windows
      {
        //do nothing
        //depend on windows setting
      }

      return;
    }






  }


}
