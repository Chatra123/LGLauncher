using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace LGLauncher
{
  using Mono.Options;

  /// <summary>
  /// コマンドライン引数を処理
  /// </summary>
  class Setting_CmdLine
  {
    public bool IsPart { get; private set; }
    public bool IsLast { get; private set; }
    public string TsPath { get; private set; }
    public string D2vPath { get; private set; }
    public string LwiPath { get; private set; }
    public string SrtPath { get; private set; }
    public string Channel { get; private set; }
    public string Program { get; private set; }
    public bool DisableSplit { get; private set; }

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
    private void Parse(string[] args)
    {
      //引数の１つ目がファイルパス？
      if (1 <= args.Count())
      {
        this.TsPath = File.Exists(args[0]) ? args[0] : "";
      }

      //    /*Mono.Options*/
      //case insensitive
      //”オプション”　”説明”　”オプションの引数に対するアクション”を定義する。
      //OptionSet_icaseに渡すオプションは小文字で記述し、
      //オプションの最後に=をつける。 bool型ならつけない。
      var optionset = new OptionSet_icase();
      optionset
        .Add("part", "is part", (v) => this.IsPart = v != null)
        .Add("last", "is last part", (v) => this.IsLast = v != null)
        .Add("ts=", "ts path", (v) => this.TsPath = v)
        .Add("d2v=", "d2v path", (v) => this.D2vPath = v)
        .Add("lwi=", "lwi path", (v) => this.LwiPath = v)
        .Add("srt=", "srt path", (v) => this.SrtPath = v)
        .Add("ch=", "channel", (v) => this.Channel = v)
        .Add("channel=", "channel", (v) => this.Channel = v)
        .Add("program=", "program", (v) => this.Program = v)
        .Add("disablesplit", "", (v) => this.DisableSplit = v != null)
        .Add("and_more", "help message", (v) => { /*action*/ });

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
    /// 結果一覧を出力
    /// </summary>
    public string Result()
    {
      var text = new StringBuilder();
      text.AppendLine("  [ App CommandLine ]");
      text.AppendLine("    IsPart    =  " + IsPart);
      text.AppendLine("    IsLast    =  " + IsLast);
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
