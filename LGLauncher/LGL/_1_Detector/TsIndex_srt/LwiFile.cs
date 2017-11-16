using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;


namespace LGLauncher
{
  using OctNov.IO;

  #region LwiFileMover
  /// <summary>
  /// lwiファイルの管理
  /// lwiファイルはTSと同じフォルダに無ければいけないので移動 or コピーする。
  /// </summary>
  static class LwiFileMover
  {
    private static FileStream lock_lwi;

    /// <summary>
    /// lwi  WorkDir --> TsDir に移動
    /// </summary>
    public static void Set()
    {
      if (PathList.IsLwi == false) return;
      if (PathList.IsAll && PathList.LwiPath == PathList.TsPath + ".lwi") return;

      string srcPath = PathList.LwiPathInLWork;
      string dstPath = PathList.TsPath + ".lwi";
      bool isSameRoot = Path.GetPathRoot(srcPath).ToLower()
                           == Path.GetPathRoot(dstPath).ToLower();
      try
      {
        if (File.Exists(dstPath))
          File.Delete(dstPath);
        Thread.Sleep(500);

        if (isSameRoot)
          File.Move(srcPath, dstPath);
        else
          File.Copy(srcPath, dstPath);

        Thread.Sleep(500);
        lock_lwi = new FileStream(dstPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      }
      catch
      {
        throw new LGLException("lwi file: fail to delete or move.");
      }
    }


    /// <summary>
    /// lwi  TsDir --> WorkDir に戻す
    /// </summary>
    public static void Back()
    {
      if (PathList.IsLwi == false) return;
      if (PathList.IsAll && PathList.LwiPath == PathList.TsPath + ".lwi") return;

      string srcPath = PathList.TsPath + ".lwi";
      string dstPath = PathList.LwiPathInLWork;
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
        Thread.Sleep(500);
      }
      catch
      {
        throw new LGLException("lwi file: fail to move back.");
      }
    }

  }
  #endregion




  #region LwiFile


  /*
   * lwi
   *  - 作成途中のlwiを読み込む。 
   *  - 最後のindex= 以降を削除してlwiとして使用できる形にする。 
   */
  class LwiFile
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
     *lwiのファイルサイズ
     *     60minで 40 - 70 MB                      
     * ts  60min    lwi  60.00 MB    783,927 line
     *      1min          1.00 MB     13,065 line
     *      1sec          0.16 MB        217 line
     */

    /// <summary>
    /// フォーマットを整える
    /// </summary>
    public void Format()
    {
      //IsAll
      if (PathList.IsAll)
      {
        //コピーして終了
        File.Copy(PathList.LwiPath, PathList.LwiPathInLWork, true);
        return;
      }

      //IsPart
      //  作成済みのlwiかどうかはチェックしていないので、
      //  -lastのlwiでも最後の"index=..."行以降を必ず切り捨てる。
      var reader = new LTextR(PathList.LwiPath);
      var writer = new LTextW(PathList.LwiPathInLWork);
      if (reader.IsOpen == false || writer.IsOpen == false)
        throw new LGLException("lwi reader,writer open error");
      writer.SetNewline_n();

      try
      {
        var readBuff = new List<string>();
        var writeBuff = new List<string>();
        readBuff = reader.ReadLines(100);
        if (readBuff.Count < 100)
          throw new LGLException("lwi text is less than 100 lines");
        //簡易ファイルチェック
        {
          bool isLwi = true;
          isLwi &= Regex.IsMatch(readBuff[0], @"<LSMASHWorksIndexVersion=.*>");
          isLwi &= Regex.IsMatch(readBuff[1], @"<LibavReaderIndexFile=\d+>");
          isLwi &= Regex.IsMatch(readBuff[2], @"<InputFilePath>.*</InputFilePath>");
          isLwi &= Regex.IsMatch(readBuff[3], @"<LibavReaderIndex=.*>");
          if (isLwi == false)
            throw new LGLException("lwi format error");
        }
        /*
         * バックグラウンドで複数動かすため適度に速度を抑える。
         * サイズは100MB程と小さいがThread.Sleep();の１行加えるだけなので入れておく。
         * 10 MB/sec * 1.00 sec  =  10.0 MB    130,000 line
         * 10        * 0.10      =   1.0        13,000 
         * 10        * 0.01      =   0.1         1,300 
         * 10        * 0.06      =   0.6         8,000
         * Thread.Sleep(60);
         * readBuff = reader.ReadLines(8 * 1000);
         * で実測 6.0 - 8.0 MB/secほど。
         * */
        //読
        writeBuff = readBuff;
        while (true)
        {
          Thread.Sleep(60);
          readBuff = reader.ReadLines(8 * 1000);
          if (readBuff.Count() == 8 * 1000)
          {
            writer.WriteLine(writeBuff);           //write file
            writeBuff = readBuff;                  //copy reference
            readBuff = new List<string>();         //initialize buff reference
          }
          else
          {
            //EOF
            reader.Close();
            break;
          }
        }

        //lwi末尾
        //　writeBuff + readBuffで１００行以上は確実にある。
        writeBuff.AddRange(readBuff);
        //最後の"index=..."行以降を削除
        const string Ptn = @"Index=\d+,Type=\d+,Codec=\d+,";
        string matchLine = writeBuff.LastOrDefault(line => Regex.Match(line, Ptn).Success);
        if (matchLine != null)
        {
          int matchIdx = writeBuff.LastIndexOf(matchLine);
          writeBuff.RemoveRange(matchIdx, writeBuff.Count - matchIdx);
        }
        else
        {
          throw new LGLException("cant find 'Index=' line");
        }

        //lwi末尾、フッター書込
        byte[] footer_bin = ReadFile_footer();
        if (footer_bin != null)
        {
          writer.WriteLine(writeBuff);
          writer.WriteByte(footer_bin);
          writer.Close();
        }
        else
        {
          string footer_new = Create_footer(writeBuff);
          writer.WriteLine(writeBuff);
          writer.WriteLine(footer_new);
          writer.Close();
        }
#pragma warning disable 0162           //警告0162：到達できないコード
        //copy  TsShortName.lwi --> TsShortName.p2.lwi
        if (Debug.CopyIndex)
        {
          string outPath_part = PathList.WorkPath + ".lwi";
          File.Copy(PathList.LwiPathInLWork, outPath_part, true);
        }
#pragma warning restore 0162
      }//try
      finally
      {
        /* LGLException発生時のClose() */
        reader.Close();
        writer.Close();
      }
    }//func


    /// <summary>
    /// footerファイル読込    バイナリーモードで読み込む
    /// </summary>
    /// <returns>
    /// 読込成功　→　byte[]
    ///     失敗　→　null
    /// </returns>
    /// <remarks>
    ///   lwiファイルの<ExtraDataList>だけはバイナリーで書かれているので
    ///   バイナリーモードで読み込む。
    /// </remarks>
    private byte[] ReadFile_footer()
    {
      const string Tag = "</LibavReaderIndexFile>\n";
      byte[] footer = null;

      //footerファイルは数秒間隔でファイル全体が更新されるので、
      //</LibavReaderIndexFile>を確認するまで繰り返す。
      for (int i = 0; i < 3; i++)
      {
        if (File.Exists(PathList.LwiFooterPath) == false)
          return null;
        footer = TextR.ReadBytes(PathList.LwiFooterPath);
        if (footer != null)
        {
          var footer_ascii = System.Text.Encoding.ASCII.GetString(footer);
          bool hasTag = footer_ascii.IndexOf(Tag) == (footer_ascii.Length - Tag.Length);
          if (hasTag)
            break;       //チェックＯＫ
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
    private string Create_footer(List<string> lwiText)
    {
      /*
        lwiText line sample
          Key=0,Pic=3,POC=0,Repeat=1,Field=1,Width=1440,Height=1080,Format=yuv420p,ColorSpace=1
      */
      //Width, Height, Format取得
      string w = "", h = "", f = "";
      const string Ptn = @"Key=\d+,.*,Width=(\d+),Height=(\d+),Format=([\w\d]+),.*";
      var matchLine = lwiText.LastOrDefault(line => Regex.Match(line, Ptn).Success);
      if (matchLine != null)
      {
        var m = Regex.Match(matchLine, Ptn);
        if (m.Groups.Count == 4)            //found match
        {
          w = m.Groups[1].ToString();
          h = m.Groups[2].ToString();
          f = m.Groups[3].ToString();
        }
        else
          throw new LGLException("invalid regex match count");
      }
      else
      {
        throw new LGLException("regex not match");
      }

      const string template =
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
      string footer = template;
      footer = Regex.Replace(footer, @"[ \t　]", "");  //VisualStudio上での表示用スペース削除
      footer = Regex.Replace(footer, @"#Width#", w);
      footer = Regex.Replace(footer, @"#Height#", h);
      footer = Regex.Replace(footer, @"#Format#", f);
      return footer;
    }
  }
  #endregion





}