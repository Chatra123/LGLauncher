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
    const double CurrentRev = 19.0;

    public double Rev = 0.0;
    public string memo31 = "  InputPlugin is  d2v or lwi         ";
    public string memo32 = "  Detector    is  JLS or LG or Auto  ";
    public string memo33 = "      cannot set d2v with JLS        ";
    public string memo34 = "      cannot set d2v with Auto       ";
    public int Enable = 1;
    public string InputPlugin = "  lwi  ";
    public string Detector = "  LG  ";
    public int Detector_MultipleRun = 1;
    public string space_1 = "";

    //edit chapter
    public double Regard_NsecMain_AsCM = 20.0;
    public double Regard_NsecCM_AsMain = 14.0;
    public string space_2 = "";

    //output chapter
    public int Output_Tvtp = 2;
    public int Output_Frame = 1;
    public int Output_RawFrame = 0;
    public int Output_Jls = 0;
    public string space_3 = "";

    //chapter directory
    public string ChapDir_Tvtp = @"  C:\Tvtp_Directory\            ";
    public string ChapDir_Misc = @"  C:\Frame_and_misc_Directory\  ";
    public string space_4 = "";
    //work item
    public int CleanWorkItem = 2;


    //XMLファイル名
    private static readonly string
            AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location,
            AppDir = Path.GetDirectoryName(AppPath),
            AppName = Path.GetFileNameWithoutExtension(AppPath),
            Default_XmlName = AppName + ".xml",
            Default_XmlPath = Path.Combine(AppDir, Default_XmlName);

    /// <summary>
    /// 設定ファイルを読込
    /// </summary>
    /// <return>
    ///   succcess  -->  Setting_File
    ///   fail      -->  null
    /// </return>
    public static Setting_File LoadFile(string xmlpath = null)
    {
      xmlpath = xmlpath ?? Default_XmlPath;
      //新規作成
      if (Path.GetFileName(xmlpath) == Default_XmlName
        && File.Exists(xmlpath) == false)
        XmlRW.Save(xmlpath, new Setting_File());

      var file = XmlRW.Load<Setting_File>(xmlpath);

      //追加された項目、削除された項目を書き換え。
      if (file != null && file.Rev != CurrentRev)
      {
        file.Rev = CurrentRev;
        XmlRW.Save(xmlpath, file);
      }
      return file;
    }


  }


}
