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
    /// <summary>
    /// 前回までのフレームリストと新たなリストをつなげる。
    /// </summary>
    /// <returns></returns>
    public static void Concat(int[] trimFrame)
    {
      //パス作成
      //logoGuilloにより作成されるFrame           *.p6.frame.txt
      string addFramePath = PathList.WorkPath + ".frame.txt";

      //前回のcatframe                            *.p5.catframe.txt
      string oldConcatPath = "";

      if (PathList.No == -1 || PathList.No == 1)
        oldConcatPath = "";                                                    //前回のcatframeはない
      else if (2 <= PathList.No)
      {
        //PartNoより前も検索する。
        //　何らかのエラーで catframe.txtを作成されていなかったとしても、
        //　後続のLGLauncherの処理をとめないため。
        for (int i = PathList.No - 1; 0 < i; i--)
        {
          oldConcatPath = Path.Combine(PathList.LWorkDir,
                                       PathList.TsShortName + ".p" + i + ".catframe.txt");
          if (File.Exists(oldConcatPath)) break;                               //found
        }
      }

      //新しいcatframe                            *.p6.catframe.txt
      string newConcatPath = PathList.WorkPath + ".catframe.txt";



      //ファイル名からフレーム数取得
      int beginFrame = int.MaxValue, endFrame = int.MaxValue;                  //値の異常がわかるように０でなくint.MaxValueを初期値にする。
      if (1 <= PathList.No)
      {
        if (trimFrame != null)
        {
          beginFrame = trimFrame[0];
          endFrame = trimFrame[1];
        }
        else
        {
          //映像が短くd2vが作成されていないとフレーム数取得はできない
          Log.WriteLine("trimFrame == null,  not detect begin frame");
        }
      }



      //
      //読込み
      //　addFrameListがなくても処理は継続する。
      //　LogoGuilloが映像からロゴをみつけられない場合、*.frame.txtは作成されていない
      List<int> oldConcatList = GetFrameList(oldConcatPath);
      List<int> addFrameList = GetFrameList(addFramePath);
      if (oldConcatList == null && addFrameList == null)
        throw new LGLException("not detect frame file");

      oldConcatList = oldConcatList ?? new List<int>();



      //
      //連結 with offset
      //　　addFrameListがあれば連結、なければnewConcatListのまま
      var newConcatList = oldConcatList;

      if (addFrameList != null)
      {
        if (1 <= PathList.No && beginFrame != int.MaxValue)
          addFrameList = addFrameList.Select((f) => f + beginFrame).ToList();  //beginFrame分増やす

        newConcatList.AddRange(addFrameList);
      }




      //
      //編集
      //フレームリストのつなぎ目をけす
      newConcatList = EditFrame.FlatOut_CM__(newConcatList, 0.5);
      if (newConcatList == null)
        throw new LGLException("frame edit error");



      //
      //frame書込み
      var newConcatText = newConcatList.Select(
                            (frame) => frame.ToString()).ToList();             //List<int>  -->  List<string>に変換
      File.WriteAllLines(newConcatPath, newConcatText, TextEnc.Shift_JIS);


      //外部フォルダにも出力
      if (Directory.Exists(PathList.FrameDir))
      {
        string copyName = (PathList.Mode_IsLast) ? PathList.TsName + ".frame.txt"
                                            : PathList.TsName + ".partframe.txt";
        string copyPath = Path.Combine(PathList.FrameDir, copyName);

        try
        {
          File.WriteAllLines(copyPath, newConcatText, TextEnc.Shift_JIS);
        }
        catch
        {
          throw new LGLException("write error on FrameDir");
        }
      }




      //
      //編集
      //短いＭａｉｎ、ＣＭをけす
      var editList = newConcatList;
      editList = EditFrame.FlatOut_CM__(editList, 29.0);
      editList = EditFrame.FlatOut_Main(editList, 29.0);
      if (editList == null) throw new LGLException("frame edit error2");



      //
      //TvtPlayChap用に編集
      //　　終端がＣＭの途中か？
      if (1 <= PathList.No && 0 < editList.Count)
      {
        int main_endframe = editList[editList.Count - 1];
        bool CM_is_tipend = (main_endframe != endFrame);                       //mainの終端！＝avsの終端

        if (CM_is_tipend) editList.Add(endFrame);                              //  終端がＣＭの途中　→　スキップ用にendFrame追加
        else editList.RemoveAt(editList.Count - 1);                            //  終端が本編の途中　→　最後のフレーム削除
      }



      //
      //chapter書込み
      string outChapPath = Path.Combine(PathList.ChapDir,
                                        PathList.TsNameWithoutExt + ".chapter");

      string chapText = EditFrame.ConvertToTvtPlayChap(editList);              //TvtPlayChap用文字列に変換

      if (Directory.Exists(PathList.ChapDir))                                  //フォルダがある？
        File.WriteAllLines(outChapPath, new string[] { chapText }, TextEnc.UTF8_bom);

    }








    #region GetFrameList
    /// <summary>
    /// フレームファイルを取得する。
    /// </summary>
    /// <param name="framePath">取得するフレームファイルパス</param>
    /// <returns>
    /// 取得成功　→　リストをList<int>で返す。
    /// 　　失敗　→　null
    /// </returns>
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

