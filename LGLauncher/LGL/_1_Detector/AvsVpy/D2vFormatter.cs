using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace LGLauncher
{
  using OctNov.IO;

  internal class D2vFormatter
  {
    /// <summary>
    /// フォーマットを整える
    /// </summary>
    /// <returns>作成したd2vパス</returns>
    public static string Format(string baseD2vPath)
    {
      //読
      var readText = FileR.ReadAllLines(baseD2vPath);
      if (readText == null) throw new LGLException("d2v read error");

      //簡易チェック
      {
        //最低行数
        if (readText.Count < 30)
          throw new LGLException("d2v text is less than 30 lines");

        //フォーマット
        bool isD2v = true;
        for (int i = 18; i < 30; i++)
        {
          isD2v &= Regex.IsMatch(readText[i], @"\d+ \d+ \d+ \d+ \d+ \d+ \d+ .*");
        }
        if (isD2v == false)
          throw new LGLException("d2v format error");
      }

      //FINISHEDがあるか？
      bool hasFinished = false;
      {
        //末尾を走査
        for (int idx = readText.Count - 1; readText.Count - 2 < idx; idx--)
        {
          string line = readText[idx];
          if (Regex.IsMatch(line, @"FINISHED.*", RegexOptions.IgnoreCase))
            hasFinished = true;
        }
      }

      //終端のフォーマットを整える
      //  FINISHEDがなければ末尾は切り捨て
      var formatText = readText;
      if (hasFinished == false)
      {
        //ファイル終端を整える
        formatText = formatText.GetRange(0, formatText.Count - 3); 
        if (hasFinished == false)
          formatText[formatText.Count - 1] += " ff";
        //動作に支障がないので、
        //　・Location=0,0,0,0は書き替えない
        //　・FINISHEDは書かない
      }

      //書
      string outPath = PathList.WorkPath + ".d2v";
      File.WriteAllLines(outPath, formatText, TextEnc.Shift_JIS);

      return outPath;
    }
  }
}