﻿using System;
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
    const double CurrentRev = 11.6;

    public double Rev = 0.0;
    public string memo11 = "  Set InputPlugin     d2v  or  lwi  ";
    public string memo12 = "  Set LogoDetector    JLS  or  LG   ";
    public string memo13 = "      'cannot set d2v with JLS'     ";
    public int Enable = 1;
    public string InputPlugin = "  lwi         ";
    public string LogoDetector = "  LogoGuillo  ";
    public int Detector_MultipleRun = 1;
    public string space_1 = "";

    //edit chapter
    public double Regard_NsecCM_AsMain = 14.0;
    public double Regard_NsecMain_AsCM = 29.0;
    public string space_2 = "";

    //output chapter
    public int Out_Tvtp = 1;
    public int Out_Ogm = 1;
    public int Out_Frame = 1;
    public int Out_RawFrame = 0;
    public string space_3 = "";

    //chapter directory
    //public int Out_tvtp_toTsDir = 1;
    //public int Out_misc_toTsDir = 1;
    public string DirPath_Tvtp = @"   C:\Tvtp_Dir            ";
    public string DirPath_Misc = @"   C:\Ogm_and_Frame_Dir   ";
    public string space_4 = "";

    public int CleanWorkItem = 3;


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

      //追加された項目、削除された項目を書き換え。
      if (file.Rev != CurrentRev)
      {
        file.Rev = CurrentRev;
        XmlRW.Save(xmlpath, file);
      }

      return file;
    }


  }


}
