using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGLauncher
{
  /*
   * ◇例外について
   *   想定できるエラーはLGLExceptionをcatch{}して終了。
   *   LGLException以外の想定外の例外はOnUnhandledException()で処理した後にエラー終了。
   *   
   *    LGLException　　　　　　　通常のログに追記　　　通常終了
   *    OnUnhandledException()　　専用のerrlogを作成　　エラーダイアログを表示
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