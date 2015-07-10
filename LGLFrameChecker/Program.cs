using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Reflection;

#region region_title
#endregion

namespace LGLFrameChecker
{
  class Program
  {


    static void Main(string[] args)
    {
      Console.WriteLine("LGLFrameChecker");
      string checkResult = CheckFrame();
      Console.WriteLine(checkResult);

      //フレームチェック成功、ファイル書込み
      if (checkResult.IndexOf("Error") == -1)
        File.WriteAllText("__FrameCheckResult.sys.txt", checkResult);

      //２０秒後に終了
      Task.Factory.StartNew(() => { Thread.Sleep(20 * 1000); Environment.Exit(0); });
      Console.Read();
    }






    static string CheckFrame()
    {
      //カレントパス
      string AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
      string AppDir = Path.GetDirectoryName(AppPath);
      string AppName = Path.GetFileName(AppPath);
      string LWorkDir = AppDir;
      //LWorkDir = @"";
      Directory.SetCurrentDirectory(LWorkDir);





      //d2v lwi  iPluginチェック
      string iPluginType = "";
      string[] d2vfiles = Directory.GetFiles(LWorkDir, "*.d2v_*__*.avs");		   //d2v
      string[] lwifiles = Directory.GetFiles(LWorkDir, "*.lwi_*__*.avs");		   //lwi
      bool existD2v = 0 < d2vfiles.Count(), existLwi = 0 < lwifiles.Count();

      if (existD2v == true && existLwi == false)
      {
        iPluginType = "d2v";
        Console.WriteLine("  mode:  d2v");
      }
      else if (existD2v == false && existLwi == true)
      {
        iPluginType = "lwi";
        Console.WriteLine("  mode:  lwi");
      }
      else if (existD2v == true && existLwi == true)
      {
        return "Error:  found both d2v and lwi";
      }
      else if (existD2v == false && existLwi == false)
      {
        return "Error:  not found  *.p*.d2v_*__*.avs";
      }






      //frameset_all取得
      var frameset_all = FrameSet.Create(LWorkDir, -1, iPluginType);
      if (frameset_all.HaveValidData == false) return frameset_all.ErrMessage;


      //FrameSetList
      var FrameSetList = new List<FrameSet>();

      //
      //比較ループ
      //
      for (int i = 1; i <= 200; i++)
      {
        //frameset_one取得
        var frameset_one = FrameSet.Create(LWorkDir, i, iPluginType);
        if (frameset_one.HaveValidData == false) { FrameSetList.Add(null); continue; }

        //比較
        var matchResult = Compare(frameset_all.boolList, frameset_one.boolList, frameset_one.beginEnd);

        //結果格納
        frameset_one.MatchResult = matchResult;
        FrameSetList.Add(frameset_one);
      }

      //後ろからまわしてnullなら削除
      while (0 < FrameSetList.Count
        && FrameSetList[FrameSetList.Count - 1] == null)
      {
        FrameSetList.RemoveAt(FrameSetList.Count - 1);
      }

      if (FrameSetList.Count() == 0) return "Error:  Not found  *.p*.frame.txt";






      //
      //結果一覧作成
      var text = new StringBuilder();
      text.AppendLine();
      text.AppendLine("Result");
      text.AppendLine("                     Match( %)                 Match(frame)");
      text.AppendLine(" No    Time       Main   CM  Not          Main       CM      Not");

      //fs_one
      foreach (var fs_one in FrameSetList)
      {
        if (fs_one == null) { text.AppendLine(); continue; }
        text.AppendLine(fs_one.GetResult());
      }


      //total
      int Match_Main = FrameSetList.Select(fs => { if (fs != null) return fs.Match_Main; else return 0; }).Sum();
      int Match___CM = FrameSetList.Select(fs => { if (fs != null) return fs.Match___CM; else return 0; }).Sum();
      int Match__Not = FrameSetList.Select(fs => { if (fs != null) return fs.Match__Not; else return 0; }).Sum();
      int Match__Sum = FrameSetList.Select(fs => { if (fs != null) return fs.Match__Sum; else return 0; }).Sum();
      double MatchR_Main = 1.0 * Match_Main / Match__Sum * 100;
      double MatchR___Cm = 1.0 * Match___CM / Match__Sum * 100;
      double MatchR__Not = 1.0 * Match__Not / Match__Sum * 100;
      string line = string.Format("{0,3:N0}  {1}      {2,3:N0}  {3,3:N0}  {4,3:N0} ,      {5,7:N0}  {6,7:N0}  {7,7:N0}",
                                  "total", "      ",
                                  MatchR_Main, MatchR___Cm, MatchR__Not,
                                  Match_Main, Match___CM, Match__Not
                                );
      text.AppendLine();
      text.AppendLine(line);

      return text.ToString();
    }






    #region Compare
    /// <summary>
    /// 全体フレームと個別フレームを比較
    /// </summary>
    /// <param name="FrameAll_Bool">全体フレームリストのブールリスト</param>
    /// <param name="frameOne_bool">個別フレームリストのブールリスト</param>
    /// <param name="frameOne_beginEnd">個別フレームリストの開始、終了フレーム数</param>
    /// <returns></returns>
    static int[] Compare(bool[] FrameAll_Bool, bool[] frameOne_bool, int[] frameOne_beginEnd)
    {
      int fbegin = frameOne_beginEnd[0];
      int fend = frameOne_beginEnd[1];
      int Match_main = 0, Match___cm = 0, Match__not = 0;


      for (int f = fbegin; f <= fend; f++)
      {
        //  main → true,  cm → falseに変換
        bool f_all, f_one;

        //FrameAll_Bool内に対象フレームの値があるか？
        if (f < FrameAll_Bool.Count())
          f_all = FrameAll_Bool[f];
        else
          f_all = false;		                     // as cm


        //インデックスの変換
        //FrameAllのフレーム数 ( 0 to 1000 )を frameOne内( 0 to 100)のインデックス番号に変換
        //  fbegin = 500, fend = 600
        //  FrameAll_Bool[567]  →  frameOne_bool[67] 
        int idx_at_frameOne = f - fbegin;
        if (idx_at_frameOne < frameOne_bool.Count())
          f_one = frameOne_bool[idx_at_frameOne];
        else
          f_one = false; 		                     // as cm


        //match?
        if (f_all == true && f_one == true)
          Match_main++;
        else if (f_all == false && f_one == false)
          Match___cm++;
        else
          Match__not++;
      }

      return new int[] { Match_main, Match___cm, Match__not };
    }
    #endregion

  }







  #region FrameSet
  /// <summary>
  /// FrameListデータ格納用
  /// </summary>
  class FrameSet
  {
    int No;
    public List<int> List;
    public bool[] boolList;
    public string ErrMessage;
    public int[] beginEnd;
    public int[] MatchResult;

    //データが取得できたか？
    public bool HaveValidData { get { return List != null && List.Count % 2 == 0 && beginEnd != null; } }

    //endframeの時間
    public TimeSpan EndFrameTime { get { return new TimeSpan(0, 0, (int)(1.0 * beginEnd[1] / 29.970)); } }

    //Match
    public int Match_Main { get { return MatchResult[0]; } }
    public int Match___CM { get { return MatchResult[1]; } }
    public int Match__Not { get { return MatchResult[2]; } }
    public int Match__Sum { get { return MatchResult.Sum(); } }
    public double MatchR_Main { get { return 1.0 * Match_Main / Match__Sum * 100; } }
    public double MatchR___Cm { get { return 1.0 * Match___CM / Match__Sum * 100; } }
    public double MatchR__Not { get { return 1.0 * Match__Not / Match__Sum * 100; } }

    //Result
    public string GetResult()
    {
      string line = string.Format("{0,3:N0}  {1}      {2,3:N0}  {3,3:N0}  {4,3:N0} ,      {5,7:N0}  {6,7:N0}  {7,7:N0}",
                                  No, EndFrameTime.ToString("T"),
                                  MatchR_Main, MatchR___Cm, MatchR__Not,
                                  Match_Main, Match___CM, Match__Not
                                );
      return line;
    }



    #region FrameSet作成
    /// <summary>
    /// FrameSet作成
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="no"></param>
    /// <param name="ipluginType"></param>
    /// <returns></returns>
    public static FrameSet Create(string dir, int no, string ipluginType)
    {
      //ファイル名作成
      //		*.all.frame.txt				*.all.lwi_0__57628.avs
      //		*.p1.frame.txt				*.p1.lwi_0__18808.avs
      string partID = (no == -1) ? "all" : "p" + no;
      string framelistName = "*." + partID + ".frame.txt";
      string avsName = "*." + partID + "." + ipluginType + "_*__*.avs";

      var frameset = new FrameSet();
      frameset.No = no;

      //file取得
      var files = Directory.GetFiles(dir, framelistName);
      if (files.Count() != 1)
      {
        frameset.ErrMessage = "Error:  not found  " + framelistName;
        return frameset;
      }

      //FrameList取得
      frameset.List = GetFrameList(files[0]);
      if (frameset.List == null)
      {
        frameset.ErrMessage = "Error:  frameList file  " + files[0];
        return frameset;
      }

      //beginEnd取得
      frameset.beginEnd = GetTrimFrame_fromName(dir, avsName);
      if (frameset.beginEnd == null)
      {
        frameset.ErrMessage = "Error:  fail to get beginEnd frame from avsName  " + avsName;
        return frameset;
      }

      //FrameList_bool作成
      frameset.boolList = ConvertToBoolArray(frameset.List, frameset.beginEnd);

      return frameset;
    }




    #region Utility
    /// <summary>
    /// List<int>のフレームリストをbool[] に変換
    /// </summary>
    /// <param name="framelist">変換元のフレームリスト</param>
    /// <param name="beginEnd">開始、終了フレーム数</param>
    /// <returns></returns>
    static bool[] ConvertToBoolArray(List<int> framelist, int[] beginEnd)
    {
      int TotalFrame = beginEnd[1] - beginEnd[0] + 1;
      var boolArray = new bool[TotalFrame];

      for (int i = 0; i < framelist.Count; i += 2)
      {
        int mainBegin = framelist[i];
        int mainEnd = framelist[i + 1];

        for (int f = mainBegin; f <= mainEnd; f++)
        {
          if (f < TotalFrame)
            boolArray[f] = true;
        }
      }
      return boolArray;
    }



    /// <summary>
    /// ファイル名からフレーム数取得
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="nameKey"></param>
    /// <returns></returns>
    public static int[] GetTrimFrame_fromName(string directory, string nameKey)
    {
      //ファイル検索
      var files = Directory.GetFiles(directory, nameKey);
      if (files.Count() != 1) return null;		             //見つからない or 多い


      //正規表現パターン
      //TsShortName.p1.d2v_0__2736.avs
      //TsShortName.p1.lwi_0__2736.avs
      var regex = new Regex(@".*\.\w+_(?<begin>\d+)__(?<end>\d+)\.avs", RegexOptions.IgnoreCase);
      //検索
      Match match = regex.Match(files[0]);


      //検索成功
      if (match.Success)
      {
        //数値に変換
        int ibegin, iend;
        string sbegin = match.Groups["begin"].Value;
        string send = match.Groups["end"].Value;

        if (int.TryParse(sbegin, out ibegin) == false) return null;  //パース失敗
        if (int.TryParse(send, out iend) == false) return null;
        return new int[] { ibegin, iend };
      }
      else
        return null;

    }



    /// <summary>
    /// *.frame.txtを取得する
    /// </summary>
    /// <param name="framePath"></param>
    /// <returns></returns>
    public static List<int> GetFrameList(string framePath)
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
            if (string.IsNullOrWhiteSpace(line)) continue;						         //空白ならスキップ
            if (int.TryParse(line, out result) == false) return null;	         //変換失敗
            intList.Add(result);
          }
          return intList;
        });


      //読込み
      if (File.Exists(framePath) == false) return null;								         //ファイルチェック
      var frameText = File.ReadAllLines(framePath, Encoding.GetEncoding("Shift_JIS")).ToList();	   //List<string>でファイル取得
      if (frameText == null) return null;

      //List<int>に変換
      var frameList = ConvertToIntList(frameText);
      //エラーチェック
      if (frameList == null) return null;
      if (frameList.Count % 2 == 1) return null;
      return frameList;
    }
    #endregion


    #endregion

  }//class FrameSet
  #endregion




}//namespace
