using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;


namespace LGLauncher
{
  class AvsWithLwi : AbstractAvsMaker
  {
    public override string AvsPath { get; protected set; }
    public override int[] TrimFrame { get; protected set; }
    public override int[] TrimFrame_m1 { get; protected set; }


    /// <summary>
    /// Trim付きavs作成
    /// </summary>
    /// <returns>作成したavsパス</returns>
    public override void Make()
    {
      //ファイルチェック
      //LwiPath
      if (File.Exists(PathList.LwiPath) == false) throw new LGLException("LwiPath not exist");

      //LSMASHSource.dll
      var LSmashDll = Path.Combine(PathList.LSystemDir, "LSMASHSource.dll");
      if (File.Exists(LSmashDll) == false) throw new LGLException("LSMASHSource.dll not exist");

      //LwiPath == TsPath + ".lwi"だと処理できない。
      if ((PathList.LwiPath).ToLower() == (PathList.TsPath + ".lwi").ToLower())
        throw new LGLException();





      //フォーマットを整える
      string formatLwiPath = FormatLwi();

      //フレーム数取得用のavs作成
      string infoAvsPath = CreateInfoAvs_lwi();

      //avs実行
      SetLwi();
      MakeAvsCommon.RunInfoAvs(infoAvsPath);
      BackLwi();



      //フレーム数取得
      var avsInfo = MakeAvsCommon.GetAvsInfo(PathList.WorkName + ".lwiinfo.txt");
      int totalframe = (int)avsInfo[0];


      //前回のトリム用フレーム数取得
      this.TrimFrame_m1 = (2 <= PathList.No)
                              ? MakeAvsCommon.GetTrimFrame_fromName(PathList.WorkName_m1 + ".lwi_*__*.avs")
                              : null;

      //トリム用フレーム数取得
      this.TrimFrame = MakeAvsCommon.GetTrimFrame(totalframe, TrimFrame_m1);



      //Trim付きavs作成
      this.AvsPath = CreateTrimAvs_lwi(TrimFrame);

    }








    #region FormatLwi
    /// <summary>
    /// lwiのフォーマットを整える
    /// </summary>
    /// <returns>フォーマット済みlwiのパス</returns>
    string FormatLwi()
    {
      /*
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
      readBuff = reader.ReadNLines(500);         //５００行ずつ読込む



      try
      {
        //
        //最低行数チェック
        if (readBuff.Count < 500)
        {
          //reader.Close();
          //writer.Close();
          throw new LGLException("lwi text is less than 500 lines");
        }
        //フォーマットの簡易チェック
        bool matchHeader = true;
        matchHeader &= Regex.IsMatch(readBuff[0], @"<LibavReaderIndexFile=\d+>");
        matchHeader &= Regex.IsMatch(readBuff[1], @"<InputFilePath>.*</InputFilePath>");
        matchHeader &= Regex.IsMatch(readBuff[2], @"<LibavReaderIndex=.*>");
        if (matchHeader == false)
        {
          //reader.Close();
          //writer.Close();
          throw new LGLException("lwi format error");
        }


        //
        //読込みループ
        writeBuff = readBuff;
        while (true)
        {
          readBuff = reader.ReadNLines(500);

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



        //
        //lwiファイル終端
        //　最後の"index=..."行以降を削除
        //  PathList.No = -1 でも切り捨てる
        string pattern = @"Index=\d+,Type=\d+,Codec=\d+,";
        var matchLine = writeBuff.LastOrDefault(line => Regex.Match(line, pattern).Success);

        if (matchLine != null)
        {
          int matchIdx = writeBuff.LastIndexOf(matchLine);
          writeBuff.RemoveRange(matchIdx, writeBuff.Count - matchIdx);
        }
        else
        {
          //reader.Close();
          //writer.Close();
          throw new LGLException();
        }




        //
        //lwiファイル作成
        var footer_bin = ReadFile_footer();         //footerファイル読込み

        if (footer_bin != null)
        {
          //footer読込成功
          //lwiの残り書込み
          writer.WriteText(writeBuff);
          //reader.Close();
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
            //reader.Close();
            //writer.Close();
            throw new LGLException();
          }

          //書込み
          writer.WriteText(writeBuff);
          writer.WriteText(footer_text);
          //reader.Close();
          //writer.Close();
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
    byte[] ReadFile_footer()
    {
      const string tag = "</LibavReaderIndexFile>\n";
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
          //テキスト末尾の"</LibavReaderIndexFile>\n"を確認
          var footer_ascii = System.Text.Encoding.ASCII.GetString(footer);
          bool havetag = footer_ascii.IndexOf(tag) == (footer_ascii.Length - tag.Length);
          if (havetag) break;                              //チェックＯＫ
          else footer = null;
        }

        //チェック失敗
        Thread.Sleep(500);
      }

      return footer;
    }



    /// <summary>
    /// footer作成　footerファイルが無いときに使用。
    /// </summary>
    /// <param name="lwiText"></param>
    /// <returns></returns>
    string Create_footer(List<string> lwiText)
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
          throw new LGLException();
      }
      else
      {
        throw new LGLException();
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
    #endregion








    #region CreateInfoAvs_lwi
    /// <summary>
    /// フレーム数取得用のAVS作成
    /// </summary>
    /// <returns>作成したAVSのパス</returns>
    string CreateInfoAvs_lwi()
    {
      //リソース読込み
      var avsText = FileR.ReadFromResource("LGLauncher.ResourceText.BaseGetInfo.avs");



      //AVS書き換え
      string lwiName = PathList.TsName + ".lwi";

      for (int i = 0; i < avsText.Count; i++)
      {
        var line = avsText[i];
        line = Regex.Replace(line, "#AvsWorkDir#", PathList.LWorkDir, RegexOptions.IgnoreCase);
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
    #endregion








    #region CreateTrimAvs_lwi
    /// <summary>
    /// トリムつきAVS作成
    /// </summary>
    /// <param name="trimBeginEnd">トリムする開始、終了フレーム数</param>
    /// <returns>作成したAVSのパス</returns>
    string CreateTrimAvs_lwi(int[] trimBeginEnd)
    {
      int beginFrame = trimBeginEnd[0];
      int endFrame = trimBeginEnd[1];

      //リソース読込み
      var avsText = FileR.ReadFromResource("LGLauncher.ResourceText.BaseTrimAvs.avs");

      //AVS書き換え
      for (int i = 0; i < avsText.Count; i++)
      {
        var line = avsText[i];
        line = Regex.Replace(line, "#AvsWorkDir#", PathList.LWorkDir, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#SystemDir#", PathList.LSystemDir, RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#lwi#", "", RegexOptions.IgnoreCase);
        line = Regex.Replace(line, "#TsPath#", PathList.TsPath, RegexOptions.IgnoreCase);

        if (1 <= PathList.No)
        {
          line = Regex.Replace(line, "#EnableTrim#", "", RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#BeginFrame#", "" + beginFrame, RegexOptions.IgnoreCase);
          line = Regex.Replace(line, "#EndFrame#", "" + endFrame, RegexOptions.IgnoreCase);
        }
        avsText[i] = line;
      }


      //長さチェック
      //　30frame以下だとlogoGuilloでavs2pipemodがエラーで落ちる。
      //　120frame以下ならno frame errorと表示されて終了する。
      //　ここでは150frame以上に設定する。
      int avslen = endFrame - beginFrame;
      if (150 <= avslen)     //5sec以上か?
      {
        //avs書込み
        string outAvsPath = PathList.WorkPath + ".lwi_" + beginFrame + "__" + endFrame + ".avs";
        File.WriteAllLines(outAvsPath, avsText, TextEnc.Shift_JIS);

        return outAvsPath;

      }
      else
      {
        //ビデオの長さが短い
        //　次回処理のGetTrimFrame()のために*.avsを作成しておく。
        //  前回の終了フレームとしてbeginFrameを参照してもらう。
        string outAvsPath = PathList.WorkPath + ".lwi_" + beginFrame + "__" + beginFrame + ".avs";
        File.WriteAllLines(outAvsPath, avsText, TextEnc.Shift_JIS);

        throw new LGLException();
      }
    }
    #endregion






    #region Set & Back lwi
    static FileStream lock_lwi;

    /// <summary>
    /// lwiをTsDirに移動
    /// </summary>
    public static void SetLwi()
    {
      string srcPath = PathList.WorkPath + ".lwi";
      string dstPath = PathList.TsPath + ".lwi";

      if (File.Exists(dstPath)) File.Delete(dstPath);      //すでにTsDirにlwiファイルがある。
      File.Move(srcPath, dstPath);
      lock_lwi = new FileStream(dstPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);   //ファイルロック

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

      if (lock_lwi != null) lock_lwi.Close();              //ファイルロック解除
      File.Move(srcPath, dstPath);

      return;
    }
    #endregion

  }





}





