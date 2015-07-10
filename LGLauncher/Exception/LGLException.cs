﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGLauncher
{

  /*
   * 想定できる例外はLGLExceptionを発生させてtry{}catch{}で捕まえる。
   * 録画中にダイアログを表示したくないので、 Windowsのエラーダイアログは表示させない。
   * 
   * 
   * LGLException以外の想定外の例外はOnUnhandledException()で処理する。
   *    LGLException　　　　　　  通常のログに追記　　　エラーダイアログを出さない
   *    OnUnhandledException()    専用のerrlogを作成　　エラーダイアログを出す
   */

  class LGLException : Exception
  {
    object[] InfoList;

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

        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("  /  info  /  ");
        foreach (var info in InfoList)
          sb.AppendLine("        " + info.ToString());

        return base.ToString() + sb.ToString();
      }
      catch
      {
        return base.ToString();
      }
    }


  }

}
