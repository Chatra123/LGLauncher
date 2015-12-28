using System;
using System.Text;

namespace LGLauncher
{
  /*
   * エラー処理に例外を使う
   * 想定できるエラーはLGLExceptionを発生させてtry{}catch{}で捕まえる。
   * 録画中にダイアログを表示したくないので、 Windowsのエラーダイアログは表示させない。
   *
   *
   * LGLException以外の想定外の例外はOnUnhandledException()で処理する。
   *    LGLException　　　　　　  通常のログに追記　　　エラーダイアログを出さない
   *    OnUnhandledException()    専用のerrlogを作成　　エラーダイアログを出す
   */

  internal class LGLException : Exception
  {
    private object[] InfoList;

    public LGLException(Exception innerException)
      : base(string.Empty, innerException) { }

    public LGLException(params object[] info)
      : base()
    {
      InfoList = info;
    }

    public override string ToString()
    {
      try
      {
        if (InfoList == null) return base.ToString();

        var sb = new StringBuilder();
        sb.AppendLine(base.ToString());
        sb.AppendLine("/▽  info  ▽/");

        foreach (var info in InfoList)
          sb.AppendLine("    " + info.ToString());
        sb.AppendLine("/△  info  △/");

        return sb.ToString();
      }
      catch
      {
        return " aggregate error :"+base.ToString();
      }
    }


  }
}