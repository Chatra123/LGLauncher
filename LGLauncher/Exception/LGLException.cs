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
   *   LGLException以外の想定外の例外はOnUnhandledException()で処理する。
   *   
   *    LGLException　　　　　　　通常のログに追記　　　エラーダイアログを表示しない
   *    OnUnhandledException()　　専用のerrlogを作成　　エラーダイアログを表示する
   */

  class LGLException : Exception
  {
    public LGLException() { }
    public LGLException(string message) : base(message) { }
    public LGLException(string message, Exception inner) : base(message, inner) { }

    public override string ToString()
    {
      var text = new StringBuilder();
      text.AppendLine(base.ToString());
      text.AppendLine("＞  Exception Message  ＜");
      text.AppendLine(base.Message);
      text.AppendLine("＞                     ＜");
      return text.ToString();
    }


  }
}