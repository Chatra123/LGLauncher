using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace LGLauncher
{
  internal class Program
  {
    private static void Main(string[] args)
    {
      ///*テスト用引数*/
      //var testArgs = new List<string>() { "-no", "1", "-last" , "-ts",
      //                                    @".\cap8s.ts",
      //                                    "-ch", "CBC", "-program", "program" };
      //args = testArgs.ToArray();

      //例外を捕捉する
      AppDomain.CurrentDomain.UnhandledException += ExceptionInfo.OnUnhandledException;

      //
      //初期化
      //
      var cmdline = new CommandLine(args);                 //引数解析
      try
      {
        var setting = Setting.LoadFile();
        if (setting.bEnable <= 0) return;

        PathList.Make(cmdline, setting);                   //パス作成

        Log.WriteLine("  No=【    " + PathList.No + "    】");
        if (PathList.No == -1 || PathList.No == 1)
          Log.WriteLine(PathList.TsPath);

        ProhibitFileMove.Lock();

        DeleteWorkItem.Clean_Beforehand();
      }
      catch (LGLException e)                               //LGLExceptionのみ捕捉、その他はOnUnhandledExceptionで捕捉する。
      {
        Log.WriteLine();
        Log.WriteLine(cmdline.ToString());
        Log.WriteLine();
        Log.WriteException(e);
        Environment.Exit(1);                               //強制終了
      }


      //
      //メイン処理
      //
      int[] trimFrame = null;
      try
      {
        trimFrame = LGLaunch();
      }
      catch (LGLException e)
      {
        //例外が発生してもアプリを終了させない。後処理に続ける。
        Log.WriteLine();
        Log.WriteException(e);
      }


      //
      //後処理
      //
      try
      {
        //フレーム合成＆チャプターファイル作成
        EditFrame_main.Concat(trimFrame);
      }
      catch (LGLException e)
      {
        Log.WriteLine();
        Log.WriteLine(cmdline.ToString());
        Log.WriteLine();
        Log.WriteException(e);
        Environment.Exit(1);                               //強制終了
      }

      //ファイル削除
      Log.Close();
      DeleteWorkItem.Clean_Lastly();
    }


    /// <summary>
    /// LogoGuillo起動処理
    /// </summary>
    private static int[] LGLaunch()
    {
      //avs
      var avsMaker = PathList.Mode_D2v
                          ? new AvsWithD2v() as AbstractAvsMaker
                          : new AvsWithLwi() as AbstractAvsMaker;
      avsMaker.Make();

      //srt
      var srtPath = TimeShiftSrt.Make(avsMaker.TrimFrame_m1);

      //bat
      var batPath = BatLogoGuillo.Make(avsMaker.AvsPath, srtPath);


      try
      {
        bool isReady = SystemChecker.GetReady();           //セマフォ取得
        if (isReady == false) return null;

        //LogoGuillo実行
        if (PathList.Mode_D2v)
        {
          //d2v
          LogoGuillo.Launch(batPath);
        }
        else
        {
          //lwi
          AvsWithLwi.SetLwi();
          LogoGuillo.Launch(batPath);
          AvsWithLwi.BackLwi();
        }

        return avsMaker.TrimFrame;
      }
      finally
      {
        SystemChecker.ReleaseSemaphore();                  //セマフォ解放
      }
    }

  }//class

  #region コマンドライン

  internal class CommandLine
  {
    public int No { get; private set; }
    public string TsPath { get; private set; }
    public string D2vPath { get; private set; }
    public string LwiPath { get; private set; }
    public string SrtPath { get; private set; }
    public string Channel { get; private set; }
    public string Program { get; private set; }
    public bool IsLast { get; private set; }

    public CommandLine(string[] args)
    {
      Parse(args);
    }

    /// <summary>
    /// コマンドライン解析
    /// </summary>
    /// <param name="args">解析するコマンドライン</param>
    private void Parse(string[] args)
    {
      for (int i = 0; i < args.Count(); i++)
      {
        string key, sValue;
        bool canParse;
        int iValue;

        key = args[i].ToLower();
        sValue = (i + 1 < args.Count()) ? args[i + 1] : "";
        canParse = int.TryParse(sValue, out iValue);

        //  - / をはずす
        if (key.IndexOf("-") == 0 || key.IndexOf("/") == 0)
          key = key.Substring(1, key.Length - 1);
        else
          continue;

        //小文字で比較
        switch (key)
        {
          case "no":
            if (canParse)
              this.No = iValue;
            break;

          case "ts":
            this.TsPath = sValue;
            break;

          case "d2v":
            this.D2vPath = sValue;
            break;

          case "lwi":
            this.LwiPath = sValue;
            break;

          case "srt":
            this.SrtPath = sValue;
            break;

          case "ch":
          case "channel":
            this.Channel = sValue;
            break;

          case "program":
            this.Program = sValue;
            break;

          case "last":
            this.IsLast = true;
            break;

          default:
            break;
        }//switch
      }//for
    }//func

    /// <summary>
    /// コマンドライン一覧を出力する。
    /// </summary>
    /// <returns></returns>
    public new string ToString()
    {
      var sb = new StringBuilder();
      sb.AppendLine("  App Command Line");
      sb.AppendLine("    No      = " + No);
      sb.AppendLine("    TsPath  = " + TsPath);
      sb.AppendLine("    D2vPath = " + D2vPath);
      sb.AppendLine("    LwiPath = " + LwiPath);
      sb.AppendLine("    SrtPath = " + SrtPath);
      sb.AppendLine("    Channel = " + Channel);
      sb.AppendLine("    Program = " + Program);
      sb.AppendLine("    IsLast  = " + IsLast);
      return sb.ToString();
    }
  }//class

  #endregion コマンドライン
}//namespace