using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGLauncher
{
  using Mono.Options;

  /// <summary>
  /// コマンドライン引数を処理
  /// </summary>
  internal class CommandLine
  {

    public int No { get; private set; }
    public bool AutoNo { get; private set; }
    public bool IsLast { get; private set; }
    public string SequenceName { get; private set; }  //作業フォルダ名後半のハッシュ用

    public string TsPath { get; private set; }
    public string D2vPath { get; private set; }
    public string LwiPath { get; private set; }
    public string SrtPath { get; private set; }

    public string Channel { get; private set; }
    public string Program { get; private set; }

    /// <summary>
    /// constructor
    /// </summary>
    public CommandLine(string[] args)
    {
      Parse(args);
    }


    /// <summary>
    /// 引数解析
    /// </summary>
    /// <param name="args">解析する引数</param>
    public void Parse(string[] args)
    {
      //    /*  Mono.Options  */
      //オプションと説明、そのオプションの引数に対するアクションを定義する
      //OptionSet_icaseに渡すオプションは小文字にすること。
      //オプションの最後に=をつける。 bool型ならつけない。
      //判定は case insensitive
      var optionset = new OptionSet_icase();

      optionset
        .Add("no=", "Sequence no", (int v) => this.No = v)
        .Add("autono", "Auto select no", (v) => this.AutoNo = v != null)
        .Add("last", "Is last process", (v) => this.IsLast = v != null)

        .Add("ts=", "ts file path", (v) => this.TsPath = v)
        .Add("d2v=", "d2v file path", (v) => this.D2vPath = v)
        .Add("lwi=", "lwi file path", (v) => this.LwiPath = v)
        .Add("srt=", "srt file path", (v) => this.SrtPath = v)

        .Add("ch=", "channel name", (v) => this.Channel = v)
        .Add("channel=", "channel name", (v) => this.Channel = v)
        .Add("program=", "program name", (v) => this.Program = v)

        .Add("sequencename=", "", (v) => this.SequenceName = v)      
        .Add("and_more", "help mes", (v) => { });

      try
      {
        //パース仕切れなかったコマンドラインはList<string>で返される。
        var extra = optionset.Parse(args);
      }
      catch (OptionException e)
      {
        //パース失敗
        Log.WriteLine("CommandLine parse error");
        Log.WriteLine("  " + e.Message);
        Log.WriteLine();
        return;
      }

    }



    /// <summary>
    /// コマンドライン一覧を出力
    /// </summary>
    /// <returns></returns>
    public new string ToString()
    {
      var sb = new StringBuilder();
      sb.AppendLine("  App Command Line");
      sb.AppendLine("    No       = " + No);
      sb.AppendLine("    AutoNo   = " + AutoNo);
      sb.AppendLine("    IsLast   = " + IsLast);
      sb.AppendLine("    Sequence = " + SequenceName);

      sb.AppendLine("    TsPath   = " + TsPath);
      sb.AppendLine("    D2vPath  = " + D2vPath);
      sb.AppendLine("    LwiPath  = " + LwiPath);
      sb.AppendLine("    SrtPath  = " + SrtPath);

      sb.AppendLine("    Channel  = " + Channel);
      sb.AppendLine("    Program  = " + Program);
      
      return sb.ToString();
    }



  }
}
