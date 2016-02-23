/*
 * AvsWithD2v, AvsWithLwiの動作
 * 
 * 
 * ・作成途中の d2v, Lwi　を読み込む。 
 * 
 * ・フォーマットを整えてd2vとして使用できる形にする。 
 *   　d2v  --> 最終行を削除
 *   　lwi  --> 最後のindex= 以降を削除 
 *   
 * ・d2vを使用してavsを作成し、フレーム数を取得
 * 
 * ・取得フレーム数までをTrim()したavsを作成して完成
 *  
 * 
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace LGLauncher
{
  using OctNov.IO;

  internal class AvsWithLwi : AbstractAvsMaker
  {
    public override string AvsPath { get; protected set; }            //作成したAVSのパス
    public override int[] TrimFrame { get; protected set; }           //トリム用フレーム数

    /// <summary>
    /// Trim付きavs作成
    /// </summary>
    public override void Make()
    {
      //ファイルチェック
      //LwiPath
      if (File.Exists(PathList.LwiPath) == false)
        throw new LGLException("LwiPath dose not exist");

      //dll
      if (File.Exists(PathList.LSMASHSource_dll) == false)
        throw new LGLException("LSMASHSource.dll dose not exist");

      //lwiファイル名が LwiPath == TsPath + ".lwi"だと処理できない。
      if ((PathList.LwiPath).ToLower() == (PathList.TsPath + ".lwi").ToLower())
        throw new LGLException("lwi name is incorrect");


      //Avs作成処理
      //フォーマットを整える
      string formatLwiPath = FormatLwi();

      //フレーム数取得用のavs作成
      string infoAvsPath = CreateInfoAvs_lwi();

      //avs実行
      try
      {
        SetLwi();
        MakeAvsCommon.RunInfoAvs(infoAvsPath);
      }
      finally
      {
        BackLwi();
      }

      //フレーム数取得
      var avsInfo = MakeAvsCommon.GetAvsInfo(PathList.WorkName + ".lwiinfo.txt");
      int totalframe = (int)avsInfo[0];

      //前回のトリム用フレーム数取得    previous 1
      int[] TrimFrame_prv1 = (2 <= PathList.PartNo)
                                 ? MakeAvsCommon.GetTrimFrame_fromAvsName(PathList.WorkName_prv1 + ".lwi_*__*.avs")
                                : null;

      //トリム用フレーム計算
      this.TrimFrame = MakeAvsCommon.CalcTrimFrame(totalframe, TrimFrame_prv1);

      //Trim付きavs作成
      this.AvsPath = CreateTrimAvs_lwi(TrimFrame);

    }



    #region FormatLwi

    /// <summary>
    /// lwiのフォーマットを整える
    /// </summary>
    /// <returns>フォーマット済みlwiのパス</returns>
    private string FormatLwi()
    {
      //lwiの３，４行目
      //
      //<ActiveVideoStreamIndex>+0000000000</ActiveVideoStreamIndex>
      //<ActiveAudioStreamIndex>+0000000001</ActiveAudioStreamIndex>
      //
      //はindex作成中のCreateLwiによって随時書き換えられる可能性が極わずかにあるが考慮しない。
      //書き換えの途中で読込み、不正なファイルになってもLSMASHSource.dllによって再作成されるだけ。
      //問題にはならないはず。

      /*
       *                       lwiのファイルサイズ
       * ts  60min  5.68 GB    lwi  40.3 MB  535,345 line
       *      1min                   6.7 MB    8,922 line
       *      1sec                   0.1 MB      150 line
       */
      string outlwiPath = PathList.WorkPath + ".lwi";
      var reader = new FileR(PathList.LwiPath);
      var writer = new FileW(outlwiPath);
      writer.SetNewline_n();

      var writeBuff = new List<string>();
      var readBuff = new List<string>();

      try
      {
        //最初の５００行だけ
        readBuff = reader.ReadNLines(500);

        //チェック
        {
          //最低行数
          if (readBuff.Count < 500)
          {
            throw new LGLException("lwi text is less than 500 lines");
          }
          //フォーマットの簡易チェック
          bool matchHeader = true;
          matchHeader &= Regex.IsMatch(readBuff[0], @"<LibavReaderIndexFile=\d+>");
          matchHeader &= Regex.IsMatch(readBuff[1], @"<InputFilePath>.*</InputFilePath>");
          matchHeader &= Regex.IsMatch(readBuff[2], @"<LibavReaderIndex=.*>");
          if (matchHeader == false)
          {
            throw new LGLException("lwi format error");
          }
        }

        //読込みループ
        writeBuff = readBuff;
        {
          while (true)
          {
            readBuff = reader.ReadNLines(500);       //５００行ずつ読込む

            if (readBuff.Count() == 500)
            {
              writer.WriteText(writeBuff);           //write file
              writeBuff = readBuff;                  //copy reference
              readBuff = new List<string>();         //initialize buff
            }
            else
            {
              //reach EOF
              writeBuff.AddRange(readBuff);
              break;
            }
          }
          reader.Close();
        }

        //
        //lwiファイル終端
        //　最後の"index=..."行以降を削除
        //  PathList.PartALL でも末尾は切り捨て
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
            throw new LGLException("cant find Index= line in the lwi");
          }
        }

        //
        //lwiファイル作成
        {
          var footer_bin = ReadFile_footer();         //footerファイル読込み

          if (footer_bin != null)
          {
            //footer読込成功
            //lwiの残り書込み
            writer.WriteText(writeBuff);
            writer.Close();

            //footerをバイナリーモードで書込み
            FileW.AppendBytes(outlwiPath, footer_bin);
          }
          else
          {
            //失敗、footer作成
            var footer_text = Create_footer(writeBuff);
            if (footer_text == null)
            {
              throw new LGLException("fail to create footer_text");
            }

            //書込み
            writer.WriteText(writeBuff);
            writer.WriteText(footer_text);
            writer.Close();
          }
        }

        return outlwiPath;
      }
      finally
      {
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
    private byte[] ReadFile_footer()
    {
      const string Tag = "</LibavReaderIndexFile>\n";
      byte[] footer = null;

      //footerは数秒間隔でファイル全体が更新されるので、
      //</LibavReaderIndexFile>を確認するまで繰り返す。
      for (int i = 0; i < 5; i++)
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
            footer = null;             //チェック失敗、リトライ
        }

        Thread.Sleep(1000);
      }

      return footer;
    }

    /// <summary>
    /// footer作成　footerファイルが無いときに使用。
    /// </summary>
    /// <param name="lwiText"></param>
    /// <returns></returns>
    private string Create_footer(List<string> lwiText)
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

      string footer_rpl = footer_const;
      footer_rpl = Regex.Replace(footer_rpl, @"[ \t　]", "", RegexOptions.IgnoreCase);         //VisualStudio上での表示用スペース削除
      footer_rpl = Regex.Replace(footer_rpl, @"#Width#", Width, RegexOptions.IgnoreCase);
      footer_rpl = Regex.Replace(footer_rpl, @"#Height#", Height, RegexOptions.IgnoreCase);
      footer_rpl = Regex.Replace(footer_rpl, @"#Format#", Format, RegexOptions.IgnoreCase);

      return footer_rpl;
    }

    #endregion FormatLwi


    #region CreateInfoAvs_lwi

    /// <summary>
    /// フレーム数取得用のAVS作成
    /// </summary>
    /// <returns>作成したAVSのパス</returns>
    private string CreateInfoAvs_lwi()
    {
      //リソース読込み
      var avsText = FileR.ReadFromResource("LGLauncher.ResourceText.BaseGetInfo.avs");

      //AVS書き換え
      string lwiName = PathList.TsName + ".lwi";

      for (int i = 0; i < avsText.Count; i++)
      {
        var line = avsText[i];

        line = Regex.Replace(line, "#AvsWorkDir#", PathList.LWorkDir, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#InputPlugin#", PathList.LSMASHSource_dll, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#SystemDir#", PathList.LSystemDir, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#lwi#", "", RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#TsPath#", PathList.TsPath, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#LwiName#", lwiName, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#InfoName#", PathList.WorkName + ".lwiinfo.txt", RegexOptions.IgnoreCase);

        avsText[i] = line;
      }

      //書込み
      string outAvsPath = PathList.WorkPath + ".lwiinfo.avs";
      File.WriteAllLines(outAvsPath, avsText, TextEnc.Shift_JIS);

      return outAvsPath;
    }

    /*
     * avs内のWriteFileStart()のファイル名が長いと*.d2vinfo.txtのファイル名が途中で切れる。
     * ファイルパスの長さが255byteあたりでファイル名が切れる
     */

    #endregion CreateInfoAvs_lwi


    #region CreateTrimAvs_lwi

    /// <summary>
    /// トリムつきAVS作成
    /// </summary>
    private string CreateTrimAvs_lwi(int[] trimFrame)
    {
      int beginFrame = trimFrame[0];
      int endFrame = trimFrame[1];

      //トリムつきAVS作成
      //avs共通部分
      var avsText = MakeAvsCommon.CreateTrimAvs(trimFrame);

      //lwi部分
      for (int i = 0; i < avsText.Count; i++)
      {
        var line = avsText[i];
        line = Regex.Replace(line, "#InputPlugin#", PathList.LSMASHSource_dll, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#lwi#", "", RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#TsPath#", PathList.TsPath, RegexOptions.IgnoreCase);

        avsText[i] = line;
      }

      //長さチェック
      //　 30frame以下だと logoGuilloの avs2pipemodがエラーで落ちる。
      //　120frame以下なら no frame errorと表示されて終了する。
      //　150frame以上に設定する。
      int avslen = endFrame - beginFrame;

      //5sec以上か？
      if (30 * 5 <= avslen)
      {
        //avs書込み
        string outAvsPath = PathList.WorkPath + ".lwi_" + beginFrame + "__" + endFrame + ".avs";
        File.WriteAllLines(outAvsPath, avsText, TextEnc.Shift_JIS);

        return outAvsPath;
      }
      else
      {
        throw new LGLException("short video length.  -lt 150 frame");
      }
    }

    #endregion CreateTrimAvs_lwi


    #region Set & Back lwi

    private static FileStream lock_lwi;

    /// <summary>
    /// lwiをTsDirに移動
    /// </summary>
    public static void SetLwi()
    {
      string srcPath = PathList.WorkPath + ".lwi";
      string dstPath = PathList.TsPath + ".lwi";

      try
      {
        //すでにTsDirにlwiファイルがあるなら削除
        if (File.Exists(dstPath)) File.Delete(dstPath);
        Thread.Sleep(500);

        File.Move(srcPath, dstPath);
        Thread.Sleep(500);

        lock_lwi = new FileStream(dstPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);   //ファイルロック
      }
      catch
      {
        //ファイルがロックされてる
        throw new LGLException("lwi file is locked. could'nt delete or move.");
      }

      return;
    }

    /// <summary>
    /// lwiをTsDirから戻す
    /// </summary>
    public static void BackLwi()
    {
      string srcPath = PathList.TsPath + ".lwi";
      string dstPath = PathList.WorkPath + ".lwi";
      if (File.Exists(srcPath) == false) return;           //TsDirにlwiファイルがない

      try
      {
        if (lock_lwi != null) lock_lwi.Close();            //ファイルロック解除
        Thread.Sleep(500);

        File.Move(srcPath, dstPath);
        Thread.Sleep(500);
      }
      catch
      {
        //ファイルがロックされてる
        throw new LGLException("lwi file is locked. could'nt move back.");
      }

      return;
    }

    #endregion Set & Back lwi



  }
}