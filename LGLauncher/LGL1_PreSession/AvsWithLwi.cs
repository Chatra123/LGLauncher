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
  class AvsWithLwi
  {
    //
    //Trim付きavs作成
    //
    static public string Make()
    {
      //ファイルチェック
      //LwiPath
      if (File.Exists(PathList.LwiPath) == false) { Log.WriteLine("AvsWithLwi  File.Exists(PathList.LwiPath"); return ""; }
      //LSMASHSource.dll
      var LSmashDll = Path.Combine(PathList.LSystemDir, "LSMASHSource.dll");
      if (File.Exists(LSmashDll) == false) { Log.WriteLine("AvsWithLwi  Not Exist LSMASHSource.dll"); return ""; }



      //フォーマットを整える
      string formatLwiPath = FormatLwi();
      if (File.Exists(formatLwiPath) == false) { Log.WriteLine("File.Exists(formatLwiPath) == false"); return ""; }


      //フレーム数取得用のavs作成
      string infoAvsPath = CreateInfoAvs_lwi();
      if (File.Exists(infoAvsPath) == false) { Log.WriteLine("File.Exists(infoAvsPath) == false"); return ""; }


      //avs実行
      SetLwi();
      bool success = AvsCommon.RunInfoAvs(infoAvsPath);
      BackLwi();
      if (success == false) { Log.WriteLine("RunInfoAvs(infoAvsPath)  success = false"); return ""; }


      //フレーム数取得
      var avsInfo = AvsCommon.GetAvsInfo(PathList.WorkName + ".lwiinfo.txt");
      if (avsInfo == null) { Log.WriteLine("avsInfo = null"); return ""; }
      int totalframe = (int)avsInfo[0];


      //トリム用フレーム数取得
      var trimBeginEnd = AvsCommon.GetTrimFrame(totalframe, PathList.WorkName_m1 + ".lwi_*__*.avs");
      if (trimBeginEnd == null) { Log.WriteLine("trimBeginEnd == null"); return ""; }


      //Trim付きavs作成
      string trimAvsPath = CreateTrimAvs_lwi(trimBeginEnd);


      return trimAvsPath;

    }



    //======================================
    //lwiのフォーマットを整える
    //　lwiファイルの簡易チェック
    //======================================
    #region FormatLwi
    static string FormatLwi()
    //static string FormatLwi(byte[] footer_bin)
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


      //行数チェック
      if (readBuff.Count < 500)
      {
        reader.Close();
        writer.Close();
        Log.WriteLine("lwi line is lt 500");
        return "";                               //return
      }
      //フォーマットの簡易チェック
      bool matchHeader = true;
      matchHeader &= Regex.IsMatch(readBuff[0], @"<LibavReaderIndexFile=\d+>");
      matchHeader &= Regex.IsMatch(readBuff[1], @"<InputFilePath>.*</InputFilePath>");
      matchHeader &= Regex.IsMatch(readBuff[2], @"<LibavReaderIndex=.*>");
      if (matchHeader == false)
      {
        reader.Close();
        writer.Close();
        Log.WriteLine("is not lwi format");
        return "";                               //return
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
      //

      //　最後の"index=..."行以降を削除
      string pattern = @"Index=\d+,Type=\d+,Codec=\d+,";
      var matchLine_idx = writeBuff.LastOrDefault(line => Regex.Match(line, pattern).Success);

      if (matchLine_idx != null)                           //found
      {
        int matchIdx = writeBuff.LastIndexOf(matchLine_idx);
        writeBuff.RemoveRange(matchIdx, writeBuff.Count - matchIdx);
      }
      else
      {
        Log.WriteLine(@"not found Index=\d+...");
        reader.Close();
        writer.Close();
        return "";                                         //return
      }



      //
      //footer binaryファイル読込用関数
      var ReadFile_footer = new Func<byte[]>(
        () =>
        {
          byte[] footer_file = null;
          var tag = "</LibavReaderIndexFile>\n";

          //読込が成功するまで何回か繰り返す
          //  footerは数秒間隔で更新されている
          for (int i = 0; i < 3; i++)
          {
            if (File.Exists(PathList.LwiFooterPath) == false) return null;

            //read file
            footer_file = FileR.ReadBytes(PathList.LwiFooterPath);

            //成功
            if (footer_file != null)
            {
              //footer_text末尾の"</LibavReaderIndexFile>\n"を確認する
              var footer_ascii = System.Text.Encoding.ASCII.GetString(footer_file);
              bool foundtag = footer_ascii.IndexOf(tag) == (footer_ascii.Length - tag.Length);
              if (foundtag) break;                         //チェックＯＫ
              else footer_file = null;
            }

            //読込み失敗 or チェック失敗
            Thread.Sleep(4 * 1000);
          }

          return footer_file;
        });


      //
      //footer作成用関数
      var Create_footer = new Func<List<string>, string>(
        (lwiText) =>
        {
          //Width, Height, Format取得
          string Width = "", Height = "", Format = "";
          //Key=0,Pic=3,POC=0,Repeat=1,Field=1,Width=1440,Height=1080,Format=yuv420p,ColorSpace=1
          pattern = @"Key=\d+,.*,Width=(\d+),Height=(\d+),Format=([\w\d]+),.*";
          var matchLine_whf = lwiText.LastOrDefault(line => Regex.Match(line, pattern).Success);

          if (matchLine_whf != null)                       //found
          {
            var m = Regex.Match(matchLine_whf, pattern);
            if (m.Groups.Count == 4)                       //match
            {
              Width = m.Groups[1].ToString();
              Height = m.Groups[2].ToString();
              Format = m.Groups[3].ToString();
            }
          }
          else                                             //not found
          {
            Log.WriteLine(@"not found Width, Height, Format");
            return null;                                   //return
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
        });



      //footerファイル読込み
      var footer_bin = ReadFile_footer();

      //読込み成功
      if (footer_bin != null)
      {
        //lwiの残り＆footer書込み
        writer.WriteText(writeBuff);
        reader.Close();
        writer.Close();
        FileW.AppendBytes(outlwiPath, footer_bin);
      }
      else
      {
        //失敗、footer作成
        var footer_text = Create_footer(writeBuff);
        if (footer_text == null)
        {
          reader.Close();
          writer.Close();
          return "";
        }
        //lwiの残り＆footer書込み
        writer.WriteText(writeBuff);
        writer.WriteText(footer_text);
        reader.Close();
        writer.Close();
      }

      return outlwiPath;
    }
    #endregion



    //======================================
    //フレーム数取得用のAVS作成
    //======================================
    #region CreateInfoAvs_lwi
    static string CreateInfoAvs_lwi()
    {
      //リソース読込み
      var avsText = FileR.ReadFromResource("baseGetInfo.avs");

      //avsパス作成
      string outAvsPath = PathList.WorkPath + ".lwiinfo.avs";

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
      FileW.WriteAllLines(outAvsPath, avsText);

      return outAvsPath;
    }
    /*
     * avs内のWriteFileStart()のファイル名が長いと*.d2vinfo.txtのファイル名が途中で切れる。
     * ファイルパスの長さが255byteあたりでファイル名が切れる
     */
    #endregion



    //======================================
    //トリムつきAVS作成
    //======================================
    #region CreateTrimAvs_lwi
    static string CreateTrimAvs_lwi(int[] trimBeginEnd)
    {
      int beginFrame = trimBeginEnd[0];
      int endFrame = trimBeginEnd[1];

      //リソース読込み
      var avsText = FileR.ReadFromResource("baseTrimAvs.avs");

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
      //　150frame以上に設定する。
      int avslen = endFrame - beginFrame;
      if (150 <= avslen)     //5sec以上か?
      {
        //avs書込み
        string outAvsPath = PathList.WorkPath + ".lwi_" + beginFrame + "__" + endFrame + ".avs";
        FileW.WriteAllLines(outAvsPath, avsText);
        return outAvsPath;
      }
      else
      { //ビデオの長さが短い
        //　次回処理のGetTrimFrame()のために*.avsを作成しておく。
        //  前回の終了フレームとしてbeginFrameを参照してもらう。
        string outAvsPath = PathList.WorkPath + ".lwi_" + beginFrame + "__" + beginFrame + ".avs";
        FileW.WriteAllLines(outAvsPath, avsText);
        Log.WriteLine("short video length");
        return "";
      }
    }
    #endregion



    //======================================
    //lwiをTsDirに移動＆バック
    //======================================
    #region Set & Back

    static FileStream lock_lwi;

    public static void SetLwi()
    {
      string srcPath = PathList.WorkPath + ".lwi";
      string dstPath = PathList.TsPath + ".lwi";

      try
      {
        if (File.Exists(dstPath)) File.Delete(dstPath);    //すでにTsDirにlwiファイルがある
        File.Move(srcPath, dstPath);                       //リネーム＆移動
        lock_lwi = new FileStream(dstPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);   //ファイルロック
        return;
      }
      catch { return; }                                    //ファイル使用中、削除失敗
    }


    public static void BackLwi()
    {

      string srcPath = PathList.TsPath + ".lwi";
      string dstPath = PathList.WorkPath + ".lwi";
      if (File.Exists(srcPath) == false) return;           //TsDirにlwiファイルがない

      try
      {
        if (lock_lwi != null) lock_lwi.Close();            //ファイルロック解除
        File.Move(srcPath, dstPath);                       //移動＆リネーム
        return;
      }
      catch
      {
        Log.WriteLine("★catch__BackLwi");
        //File.Create(Path.Combine(PathList.AppDir, "★errBackLwi__" + PathList.TsName + ".err")).Close();
        return;
      }
      finally { if (lock_lwi != null) lock_lwi.Close(); }  //ファイルロック解除
    }
    #endregion

  }





}





