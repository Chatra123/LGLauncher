using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Reflection;

namespace LGLauncher
{
  static partial class EditFrame
  {
    public static bool Concat()
    {
      //
      //パス作成
      //フレームパス
      //logoGuilloにより作成される  *.p6.frame.txt
      string addFramePath = PathList.WorkPath + ".frame.txt";

      //前回のcatframe  *.p5.catframe.txt
      string oldConcatPath = "";
      if (PathList.No == -1 || PathList.No == 1) oldConcatPath = "";           //前回のcatframeはない
      else if (2 <= PathList.No)
      {
        //PathList.PartNo - 1 以前も検索する。
        //　エラーで*.catframe.txtが作成できなくても、後続のLGL処理を継続させる。
        for (int i = PathList.No - 1; 0 < i; i--)
        {
          oldConcatPath = Path.Combine(PathList.LWorkDir, PathList.TsShortName + ".p" + i + ".catframe.txt");
          if (File.Exists(oldConcatPath) == true) break;                       //found
        }
      }

      //新しいcatframe  *.p6.catframe.txt
      string newConcatPath = PathList.WorkPath + ".catframe.txt";

      //チャプターパス
      string outChapDir = PathList.ChapDir;
      string outChapPath = Path.Combine(outChapDir, PathList.TsNameWithoutExt + ".chapter");


      //ファイル名からフレーム数取得
      int beginFrame = int.MaxValue, endFrame = int.MaxValue;                  //値の異常がわかるように０でなくint.MaxValueを初期値にする。
      if (1 <= PathList.No)
      {
        int[] frameset = (PathList.D2vMode) ?
            AvsCommon.GetFrame_byName(PathList.WorkName + ".d2v_*__*.avs") :
            AvsCommon.GetFrame_byName(PathList.WorkName + ".lwi_*__*.avs");
        if (frameset != null) { beginFrame = frameset[0]; endFrame = frameset[1]; }
        else { Log.WriteLine("frameSet == null,  not detect begin frame"); }   //映像が短くd2vが作成されないとフレーム数取得はできない
      }




      //
      //読込み
      //　addFrameListがなくても処理は継続する。
      //　　LogoGuilloが映像からロゴをみつけれない場合、*.frame.txtは作成されていない
      List<int> oldConcatList = GetFrameList(oldConcatPath);
      List<int> addFrameList = GetFrameList(addFramePath);

      if (oldConcatList == null && addFrameList == null)
      { Log.WriteLine("oldConcatList == null && addFrameList == null"); return false; }  //両方なければ終了

      //oldConcatListがなければ、空のリストを作成
      if (oldConcatList == null) oldConcatList = new List<int>();


      //
      //連結 with offset
      //　　addFrameListがあれば連結、なければnewConcatListのまま
      var newConcatList = oldConcatList;
      if (addFrameList != null)
      {
        if (1 <= PathList.No && beginFrame != int.MaxValue)                    //beginFrameが取得できている？
          addFrameList = addFrameList.Select((f) => f + beginFrame).ToList();  //beginFrame分増やす
        newConcatList.AddRange(addFrameList);
      }




      //
      //編集
      //フレームリストのつなぎ目をけす
      newConcatList = EditFrame.FlatOut_CM__(newConcatList, 0.5);
      if (newConcatList == null) { Log.WriteLine("newConcatList == null"); return false; }


      //
      //frame書込み
      var newConcatText = newConcatList.Select(
                            (frame) => frame.ToString()).ToList();             //List<int>  -->  List<string>に変換
      FileW.WriteAllLines(newConcatPath, newConcatText);                       //Shift-JIS
      //外部フォルダにも出力
      if (Directory.Exists(Setting.file.sFrameDir_Path))
      {
        string copyName = (PathList.IsLast) ? PathList.TsName + ".frame.txt"
                                            : PathList.TsName + ".partframe.txt";
        string copyPath = Path.Combine(Setting.file.sFrameDir_Path, copyName);
        try { FileW.WriteAllLines(copyPath, newConcatText); }                   //Shift-JIS
        catch { Log.WriteLine("catch  TextW.WriteALL(copyPath, newConcatText);"); }
      }




      //
      //編集
      //短いＭａｉｎ、ＣＭをけす
      var editList = newConcatList;
      editList = EditFrame.FlatOut_CM__(editList, 55.0);
      editList = EditFrame.FlatOut_Main(editList, 29.0);
      if (editList == null) { Log.WriteLine("editList == null"); return false; }


      //
      //TvtPlayChap用に編集
      //　　終端がＣＭの途中か？
      if (1 <= PathList.No && 0 < editList.Count)
      {
        int main_endframe = editList[editList.Count - 1];
        bool CM_is_tipend = (main_endframe != endFrame);   //mainの終端！＝avsの終端
        if (CM_is_tipend) editList.Add(endFrame);          //  終端がＣＭの途中　→　スキップ用にendFrame追加
        else editList.RemoveAt(editList.Count - 1);        //  終端が本編の途中　→　最後のフレーム削除
      }

      //
      //chapter書込み
      string chapText = EditFrame.ConvertToTvtPlayChap(editList);              //TvtPlayChap用文字列に変換
      if (Directory.Exists(PathList.ChapDir))                                  //フォルダがある？
        FileW.WriteAllLines(outChapPath, chapText, TextEnc.UTF8_bom);          //BOMつきUTF8


      return true;

    }



    //======================================
    //*.frame.txtを取得する
    //======================================
    #region GetFrameList
    static List<int> GetFrameList(string framePath)
    {
      //convert List<string>  to  List<int>
      var ConvertToIntList = new Func<List<string>, List<int>>(
        (stringList) =>
        {
          var intList = new List<int>();
          int result;

          foreach (var str in stringList)
          {
            string line = str.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;                     //空白ならスキップ
            if (int.TryParse(line, out result) == false) return null;          //変換失敗
            intList.Add(result);
          }

          return intList;
        });


      //読込み
      if (File.Exists(framePath) == false) return null;    //ファイルチェック
      var frameText = FileR.ReadAllLines(framePath);       //List<string>でファイル取得
      if (frameText == null) return null;


      //List<int>に変換
      var frameList = ConvertToIntList(frameText);
      //エラーチェック
      if (frameList == null) return null;
      if (frameList.Count % 2 == 1) return null;           //奇数ならエラー
      return frameList;
    }
    #endregion



  }//class








}

