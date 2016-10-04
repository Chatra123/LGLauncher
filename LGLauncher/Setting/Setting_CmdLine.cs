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
  class Setting_CmdLine
  {
    public bool IsLast { get; private set; }
    public bool IsAll { get; private set; }
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
    public Setting_CmdLine(string[] args)
    {
      Parse(args);
    }


    /// <summary>
    /// 引数解析
    /// </summary>
    /// <param name="args">解析する引数</param>
    private void Parse(string[] args)
    {
      //    /*Mono.Options*/
      //case insensitive
      //”オプション”　”説明”　”オプションの引数に対するアクション”を定義する。
      //OptionSet_icaseに渡すオプションは小文字で記述し、
      //オプションの最後に=をつける。 bool型ならつけない。
      var optionset = new OptionSet_icase();
      optionset
        .Add("all", "Is all", (v) => this.IsAll = v != null)
        .Add("last", "Is last part", (v) => this.IsLast = v != null)

        .Add("ts=", "ts file path", (v) => this.TsPath = v)
        .Add("d2v=", "d2v file path", (v) => this.D2vPath = v)
        .Add("lwi=", "lwi file path", (v) => this.LwiPath = v)
        .Add("srt=", "srt file path", (v) => this.SrtPath = v)

        .Add("ch=", "channel name", (v) => this.Channel = v)
        .Add("channel=", "channel name", (v) => this.Channel = v)
        .Add("program=", "program name", (v) => this.Program = v)

        .Add("sequencename=", "", (v) => this.SequenceName = v)
        .Add("and_more", "help mes", (v) => { /*action*/ });

      try
      {
        //パース仕切れなかったコマンドラインはList<string>で返される。
        var extra = optionset.Parse(args);
      }
      catch (OptionException e)
      {
        Log.WriteLine("CommandLine parse error");
        Log.WriteLine("  " + e.Message);
        Log.WriteLine();
      }
    }



    /// <summary>
    /// コマンドライン一覧を出力
    /// </summary>
    public new string ToString()
    {
      var text = new StringBuilder();
      text.AppendLine("  [ App Command Line ]");
      text.AppendLine("    All       =  " + IsAll);
      text.AppendLine("    Last      =  " + IsLast);
      text.AppendLine("    Sequence  =  " + SequenceName);

      text.AppendLine("    TsPath    =  " + TsPath);
      text.AppendLine("    D2vPath   =  " + D2vPath);
      text.AppendLine("    LwiPath   =  " + LwiPath);
      text.AppendLine("    SrtPath   =  " + SrtPath);

      text.AppendLine("    Channel   =  " + Channel);
      text.AppendLine("    Program   =  " + Program);
      return text.ToString();
    }



  }
}
