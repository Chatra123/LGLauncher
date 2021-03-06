﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

/*
 * d2v
 *  - 作成途中のd2vを読み込む。 
 *  - 最終行を削除してd2vとして利用できる形にする。 
 */
namespace LGLauncher
{
  class D2vFile
  {
    /// <summary>
    /// フォーマットを整える
    /// </summary>
    public void Format()
    {
      //読
      var readText = TextR.ReadAllLines(PathList.D2vPath);
      if (readText == null)
        throw new LGLException("d2v read error");
      if (readText.Count < 30)
        throw new LGLException("d2v text is less than 30 lines");

      //簡易ファイルチェック
      {
        bool isValid = true;
        for (int i = 18; i < 30; i++)
        {
          isValid &= Regex.IsMatch(readText[i], @"\d+ \d+ \d+ \d+ \d+ \d+ \d+ .*");
        }
        if (isValid == false)
          throw new LGLException("d2v format error");
      }

      //末尾にFINISHEDがあるか？
      bool hasFinished = false;
      {
        for (int idx = readText.Count - 1; readText.Count - 3 < idx; idx--)
        {
          string line = readText[idx];
          if (Regex.IsMatch(line, @"^FINISHED  /d.*", RegexOptions.IgnoreCase))
            hasFinished = true;
        }
      }

      //終端のフォーマットを整える
      var formatText = readText;
      if (hasFinished == false)
      {
        //FINISHEDがなければ末尾２行を切り捨て
        formatText = formatText.GetRange(0, formatText.Count - 2);
        formatText[formatText.Count - 1] += " ff";
        //動作に支障がないので、
        //　・Location=0,0,0,0は書き替えない
        //　・FINISHEDは書かない
      }

      //書
      File.WriteAllLines(PathList.D2vPathInLWork, formatText, TextEnc.Shift_JIS);
#pragma warning disable 0162           //警告0162：到達できないコード
      //copy  TsShortName.d2v --> TsShortName.p2.d2v
      if (Debug.CopyIndex)
      {
        string outPath_part = PathList.WorkPath + ".d2v";
        File.WriteAllLines(outPath_part, formatText, TextEnc.Shift_JIS);
      }
#pragma warning restore 0162
    }
  }
}