using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace LGLauncher
{
  using OctNov.IO;


  #region LwiFile
  static class LwiFile
  {
    private static FileStream lock_lwi;

    /// <summary>
    /// lwiをTsDirに移動
    /// </summary>
    public static void Set_ifLwi()
    {
      if (PathList.InputPlugin != PluginType.Lwi) return;

      string srcPath = Path.Combine(PathList.LWorkDir, PathList.TsShortName + ".lwi");
      string dstPath = PathList.TsPath + ".lwi";
      bool isSameRoot = Path.GetPathRoot(srcPath).ToLower()
                           == Path.GetPathRoot(dstPath).ToLower();
      try
      {
        //すでにTsDirにlwiファイルがあるなら削除
        if (File.Exists(dstPath))
          File.Delete(dstPath);
        Thread.Sleep(200);

        if (isSameRoot)
          File.Move(srcPath, dstPath);
        else
          File.Copy(srcPath, dstPath);
        Thread.Sleep(200);

        lock_lwi = new FileStream(dstPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      }
      catch
      {
        throw new LGLException("lwi file is locked. fail to delete or move.");
      }
    }

    /// <summary>
    /// lwiをTsDirから戻す
    /// </summary>
    public static void Back_ifLwi()
    {
      if (PathList.InputPlugin != PluginType.Lwi) return;

      string srcPath = PathList.TsPath + ".lwi";
      string dstPath = Path.Combine(PathList.LWorkDir, PathList.TsShortName + ".lwi");
      bool isSameRoot = Path.GetPathRoot(srcPath).ToLower()
                           == Path.GetPathRoot(dstPath).ToLower();
      if (File.Exists(srcPath) == false) return;

      try
      {
        if (lock_lwi != null)
        {
          lock_lwi.Close();
          lock_lwi = null;
        }

        if (isSameRoot)
          File.Move(srcPath, dstPath);
        else
          File.Delete(srcPath);
        Thread.Sleep(200);
      }
      catch
      {
        throw new LGLException("lwi file is locked. fail to back.");
      }
    }


    /// <summary>
    /// LwiファイルにTsDirに移動してから実行
    /// PluginType.Lwiでない場合はそのままaction()
    /// </summary>
    [Obsolete]
    public static void Action_withSetLwi(Action action)
    {
      try
      {
        LwiFile.Set_ifLwi();
        action();
      }
      finally
      {
        LwiFile.Back_ifLwi();
      }
    }

  }
  #endregion LwiFile




  #region LwiFormatter
  static class LwiFormatter
  {
    //lwiの３，４行目
    //
    //<ActiveVideoStreamIndex>+0000000000</ActiveVideoStreamIndex>
    //<ActiveAudioStreamIndex>+0000000001</ActiveAudioStreamIndex>
    //
    //はindex作成中のCreateLwiによって随時書き換えられる可能性が極わずかにあるが考慮しない。
    //書き換えの途中で読込み、不正なファイルになってもLSMASHSource.dllによって再作成されるだけ。
    //問題にはならない。
    /*
     *                       lwiのファイルサイズ
     * ts  60min  8.70 GB    lwi  67.00 MB  867,272 line
     *      1min                   1.12 MB   14,454 line
     *      4sec                   0.08 MB    1,000 line
     */


    /// <summary>
    /// lwiのフォーマットを整える
    /// </summary>
    public static void Format()
    {
      string outPath = PathList.LwiPathInLWork;
      var reader = new FileR(PathList.LwiPath);
      var writer = new FileW(outPath);
      writer.SetNewline_n();

      try
      {
        var readBuff = new List<string>();
        var writeBuff = new List<string>();

        //最初の１０００行
        readBuff = reader.ReadNLines(1000);

        //簡易チェック
        {
          //最低行数
          if (readBuff.Count < 1000)
            throw new LGLException("lwi text is less than 1000 lines");

          //フォーマット
          bool isLwi = true;
          isLwi &= Regex.IsMatch(readBuff[0], @"<LibavReaderIndexFile=\d+>");
          isLwi &= Regex.IsMatch(readBuff[1], @"<InputFilePath>.*</InputFilePath>");
          isLwi &= Regex.IsMatch(readBuff[2], @"<LibavReaderIndex=.*>");
          if (isLwi == false)
            throw new LGLException("lwi format error");
        }

        //読
        var start = DateTime.Now;
        writeBuff = readBuff;
        while (true)
        {
          /*
           * バックグランドで複数動かすため適度に速度を抑える。
           *   10 MB/sec程度に抑えるため Sleep()
           *   10 MB/sec *    1 sec  =    10 MB  130,000 line
           *   10 MB/sec * 0.10 sec  =   1.0 MB   13,000 line
           *   10 MB/sec * 0.01 sec  =   0.1 MB    1,300 line
           */
          Thread.Sleep(10);
          readBuff = reader.ReadNLines(1000);

          if (readBuff.Count() == 1000)
          {
            writer.WriteText(writeBuff);           //write file
            writeBuff = readBuff;                  //copy reference
            readBuff = new List<string>();         //initialize buff reference
          }
          else
          {
            //reach EOF
            reader.Close();
            break;
          }
        }

        var elapse = (DateTime.Now - start).TotalMilliseconds;
        FileInfo fi = new FileInfo(PathList.LwiPath);
        double size = fi.Length / 1024 / 1024;

        Log.WriteLine("read & write lwi  " + string.Format("{0:f1} ms", elapse));
        Log.WriteLine("                  " + string.Format("{0:f1} MB", size));



        //lwi末尾
        //　writeBuff + readBuffで５００行以上は確実にある。
        writeBuff.AddRange(readBuff);

        //最後の"index=..."行以降を削除
        //PathList.IsAll でも末尾は切り捨てる。
        {
          string pattern = @"Index=\d+,Type=\d+,Codec=\d+,";
          var matchLine = writeBuff.LastOrDefault(line => Regex.Match(line, pattern).Success);

          if (matchLine != null)
          {
            int matchIdx = writeBuff.LastIndexOf(matchLine);
            writeBuff.RemoveRange(matchIdx, writeBuff.Count - matchIdx);
          }
          else
          {
            throw new LGLException("cant find 'Index=' line");
          }
        }

        //lwi末尾、フッター書込
        {
          var footer_bin = ReadFile_footer();         //footerファイル読込み bin

          if (footer_bin != null)
          {
            //footer読込成功
            //lwiの残りを書込み
            writer.WriteText(writeBuff);
            writer.Close();

            //footerをバイナリーモードで書込み
            FileW.AppendBytes(outPath, footer_bin);
          }
          else
          {
            //読込失敗、footer作成
            var footer_text = Create_footer(writeBuff);
            if (footer_text == null)
              throw new LGLException("fail to create footer_text");

            writer.WriteText(writeBuff);
            writer.WriteText(footer_text);
            writer.Close();
          }
        }

        //コピー
        {
          //デバッグ用のコピー  TsShortName.lwi  -->  TsShortName.p2.lwi
          string outPath_part = PathList.WorkPath + ".lwi";
          File.Copy(outPath, outPath_part, true);
        }

      }
      finally
      {
        /* LGLException発生時のClose() */
        reader.Close();
        writer.Close();
      }
    }//func


    /// <summary>
    /// footerファイル読込    binaryモードで読み込む
    /// </summary>
    /// <returns>
    /// 読込成功　→　byte[]
    ///     失敗　→　null
    /// </returns>
    /// <remarks>
    ///   lwiファイルの<ExtraDataList>だけはバイナリーで書かれているので
    ///   バイナリーモードで読み込む。
    /// </remarks>
    private static byte[] ReadFile_footer()
    {
      const string Tag = "</LibavReaderIndexFile>\n";
      byte[] footer = null;

      //footerは数秒間隔でファイル全体が更新されるので、
      //</LibavReaderIndexFile>を確認するまで繰り返す。
      for (int i = 0; i < 3; i++)
      {
        if (File.Exists(PathList.LwiFooterPath) == false) return null;

        //read
        footer = FileR.ReadBytes(PathList.LwiFooterPath);

        if (footer != null)
        {
          //テキスト末尾の Tagを確認
          var footer_ascii = System.Text.Encoding.ASCII.GetString(footer);
          bool hasTag = footer_ascii.IndexOf(Tag) == (footer_ascii.Length - Tag.Length);
          if (hasTag)
            break;                     //チェックＯＫ
          else
            footer = null;
        }

        Thread.Sleep(1000);
      }

      return footer;
    }

    /// <summary>
    /// footer作成　footerファイルが無いときに使用。
    /// </summary>
    private static string Create_footer(List<string> lwiText)
    {
      //
      //lwiText line sample
      //  Key=0,Pic=3,POC=0,Repeat=1,Field=1,Width=1440,Height=1080,Format=yuv420p,ColorSpace=1
      //
      //Width, Height, Format取得
      string Width = "", Height = "", Format = "";
      var pattern = @"Key=\d+,.*,Width=(\d+),Height=(\d+),Format=([\w\d]+),.*";
      var matchLine = lwiText.LastOrDefault(line => Regex.Match(line, pattern).Success);

      if (matchLine != null)                           //found
      {
        var m = Regex.Match(matchLine, pattern);
        if (m.Groups.Count == 4)                       //match
        {
          Width = m.Groups[1].ToString();
          Height = m.Groups[2].ToString();
          Format = m.Groups[3].ToString();
        }
        else
          throw new LGLException("invalid regex match count");
      }
      else
      {
        throw new LGLException("regex not match");
      }

      //footerの置換
      const string footer_const =
                  @"</LibavReaderIndex>
                    <StreamDuration=0,0>-1</StreamDuration>
                    <StreamDuration=1,1>-1</StreamDuration>
                    <StreamIndexEntries=0,0,0>
                    </StreamIndexEntries>
                    <StreamIndexEntries=1,1,0>
                    </StreamIndexEntries>
                    <ExtraDataList=0,0,1>
                      Size=0,Codec=2,4CC=0x2,Width=#Width#,Height=#Height#,Format=#Format#,BPS=0

                    </ExtraDataList>
                    </LibavReaderIndexFile>";

      string footer = footer_const;
      footer = Regex.Replace(footer, @"[ \t　]", "", RegexOptions.IgnoreCase);         //VisualStudio上での表示用スペース削除
      footer = Regex.Replace(footer, @"#Width#", Width, RegexOptions.IgnoreCase);
      footer = Regex.Replace(footer, @"#Height#", Height, RegexOptions.IgnoreCase);
      footer = Regex.Replace(footer, @"#Format#", Format, RegexOptions.IgnoreCase);
      return footer;
    }
  }
  #endregion LwiFormatter





}