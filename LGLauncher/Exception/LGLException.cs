using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGLauncher
{
  /*
   * ◇例外について
   *   想定できるエラーはLGLExceptionを発生させてtry{}catch{}で捕まえる。
   *   録画中にダイアログを表示したくないので、Windowsのエラーダイアログは表示させない。
   *
   *
   * LGLException以外の想定外の例外はOnUnhandledException()で処理する。
   *    LGLException　　　　　　  通常のログに追記　　　エラーダイアログを表示しない
   *    OnUnhandledException()    専用のerrlogを作成　　エラーダイアログを表示する
   */

  internal class LGLException : System.Exception
  {
    public LGLException() { }
    public LGLException(string message) : base(message) { }
    public LGLException(string message, System.Exception inner) : base(message, inner) { }

    public override string ToString()
    {
      var text = new StringBuilder();
      text.AppendLine(base.ToString());
      text.AppendLine("▽▽  Message  ▽▽");
      text.AppendLine(base.Message);
      text.AppendLine("△△  Message  △△");

      return text.ToString();
    }


  }
}