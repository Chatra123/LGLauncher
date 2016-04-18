using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;



namespace LGLFrameChecker
{
  class Program
  {
    private static void Main(string[] args)
    {
      Console.WriteLine("LGLFrameChecker");
      string result = CheckFrame();
      Console.WriteLine(result);


      if (result.IndexOf("Error") == -1)
      {
        //フレームチェック成功、ファイル書込み
        File.WriteAllText("__FrameCheckResult.sys.txt", result);
      }
      else
      {
        //on error
        //１０秒後に自動終了
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("auto exit after 10 sec");
        Task.Factory.StartNew(() => { Thread.Sleep(10 * 1000); Environment.Exit(0); });
      }

      Console.Read();
    }



    private static string CheckFrame()
    {
      string LWorkDir;
      {
        string AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string AppDir = Path.GetDirectoryName(AppPath);
        string AppName = Path.GetFileName(AppPath);
        LWorkDir = AppDir;
        //LWorkDir = @"";
      }
      //カレントパス
      Directory.SetCurrentDirectory(LWorkDir);


      //file check
      {
        string[] avsfiles = Directory.GetFiles(LWorkDir, "*.*__*.avs");
        if (avsfiles.Count() == 0)
          return "  Error:  not found  VideoName.p1.*__*.avs";
      }


      //frameset_all取得
      var frameset_all = FrameSet.Create(LWorkDir, -1);
      if (frameset_all.HasValidData == false) return frameset_all.ErrMessage;

      //FrameSetList
      var FrameSetList = new List<FrameSet>();

      //
      //PartNo
      //
      for (int partNo = 1; partNo <= 200; partNo++)
      {
        //fset_part取得
        var fs_part = FrameSet.Create(LWorkDir, partNo);
        if (fs_part.HasValidData == false) { FrameSetList.Add(null); continue; }

        //比較
        fs_part.MatchResult = Compare(frameset_all.boolList, fs_part.boolList, fs_part.beginEnd);

        //結果格納
        FrameSetList.Add(fs_part);
      }

      //後ろからまわしてnullなら削除
      {
        while (0 < FrameSetList.Count
          && FrameSetList[FrameSetList.Count - 1] == null)
        {
          FrameSetList.RemoveAt(FrameSetList.Count - 1);
        }

        if (FrameSetList.Count() == 0) 
          return "  Error:  Not found  VideoName.p*.frame.txt";
      }


      //
      //結果一覧作成
      //
      var text = new StringBuilder();
      {
        text.AppendLine();
        text.AppendLine("Result");
        text.AppendLine("                     Match( %)                 Match(frame)");
        text.AppendLine(" No    Time       Main   CM  Not          Main       CM       Not");

        //frameset
        foreach (var fs in FrameSetList)
        {
          if (fs == null) { text.AppendLine(); continue; }
          text.AppendLine(fs.GetResult());
        }

        //total
        {
          int Match_Main = FrameSetList.Select(fs => { if (fs != null) return fs.Match_Main; else return 0; }).Sum();
          int Match___CM = FrameSetList.Select(fs => { if (fs != null) return fs.Match___CM; else return 0; }).Sum();
          int Match__Not = FrameSetList.Select(fs => { if (fs != null) return fs.Match__Not; else return 0; }).Sum();
          int Match__Sum = FrameSetList.Select(fs => { if (fs != null) return fs.Match__Sum; else return 0; }).Sum();
          double MatchR_Main = 1.0 * Match_Main / Match__Sum * 100;
          double MatchR___Cm = 1.0 * Match___CM / Match__Sum * 100;
          double MatchR__Not = 1.0 * Match__Not / Match__Sum * 100;
          //total line
          string line = string.Format("{0,3:N0}  {1}      {2,3:N0}  {3,3:N0}  {4,3:N0} ,      {5,7:N0}  {6,7:N0}  {7,7:N0}",
                                      "total", "      ",
                                      MatchR_Main, MatchR___Cm, MatchR__Not,
                                      Match_Main, Match___CM, Match__Not
                                    );
          text.AppendLine();
          text.AppendLine(line);
        }
      }

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
    private static int[] Compare(bool[] FrameAll_Bool, bool[] frameOne_bool, int[] frameOne_beginEnd)
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

    #endregion Compare

  }



  #region FrameSet

  /// <summary>
  /// FrameListデータ格納用
  /// </summary>
  internal class FrameSet
  {
    private int No;
    public List<int> List;
    public bool[] boolList;
    public string ErrMessage;
    public int[] beginEnd;
    public int[] MatchResult;

    //正常なデータが取得できたか？
    public bool HasValidData { get { return List != null && List.Count % 2 == 0 && beginEnd != null; } }

    //endframeの時間
    public TimeSpan EndFrameTime { get { return new TimeSpan(0, 0, (int)(1.0 * beginEnd[1] / 29.970)); } }

    //Match
    public int Match_Main { get { return MatchResult[0]; } }
    public int Match___CM { get { return MatchResult[1]; } }
    public int Match__Not { get { return MatchResult[2]; } }
    public int Match__Sum { get { return MatchResult.Sum(); } }
    public double MatchR_Main { get { return 1.0 * Match_Main / Match__Sum * 100; } }
    public double MatchR___CM { get { return 1.0 * Match___CM / Match__Sum * 100; } }
    public double MatchR__Not { get { return 1.0 * Match__Not / Match__Sum * 100; } }

    //Result
    public string GetResult()
    {
      string line = string.Format("{0,3:N0}  {1}      {2,3:N0}  {3,3:N0}  {4,3:N0} ,      {5,7:N0}  {6,7:N0}  {7,7:N0}",
                                  No, EndFrameTime.ToString("T"),
                                  MatchR_Main, MatchR___CM, MatchR__Not,
                                  Match_Main, Match___CM, Match__Not
                                );
      return line;
    }

    #region FrameSet作成

    /// <summary>
    /// FrameSet作成
    /// </summary>
    public static FrameSet Create(string dir, int no)
    {
      var frameset = new FrameSet();
      frameset.No = no;

      //ファイル名作成
      //		*.all.frame.txt				*.all.0__57628.avs
      //		*.p1.frame.txt				*.p1.0__18808.avs
      string partID = (no == -1) ? "all" : "p" + no;
      string frameListName = "*." + partID + ".frame.txt";
      string avsName = "*." + partID + "." + "*__*.avs";

      //file取得
      var files = Directory.GetFiles(dir, frameListName);
      if (files.Count() != 1)
      {
        frameset.ErrMessage = "  Error:  not found  " + frameListName;
        return frameset;
      }

      //FrameList取得
      frameset.List = FrameFile_to_List(files[0]);
      if (frameset.List == null)
      {
        frameset.ErrMessage = "  Error:  frameList file  " + files[0];
        return frameset;
      }

      //beginEnd取得
      frameset.beginEnd = GetTrimFrame_fromName(dir, avsName);
      if (frameset.beginEnd == null)
      {
        frameset.ErrMessage = "  Error:  fail to get beginEnd frame from avsName  " + avsName;
        return frameset;
      }

      //FrameList_bool作成
      frameset.boolList = ConvertToBoolArray(frameset.List, frameset.beginEnd);

      return frameset;
    }


    /// <summary>
    /// List<int>　→　bool[]
    /// </summary>
    private static bool[] ConvertToBoolArray(List<int> framelist, int[] beginEnd)
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
    /// <param name="nameKey">対象のファイル名。ワイルドカード指定可</param>
    /// <returns>開始、終了フレーム数</returns>
    private static int[] GetTrimFrame_fromName(string directory, string nameKey)
    {
      //ファイル検索
      var files = Directory.GetFiles(directory, nameKey);
      if (files.Count() != 1) return null;

      //正規表現パターン
      //TsShortName.p1.0__1000.avs
      //TsShortName.all.0__1000.avs
      //  <begin>      0
      //  <end>     1000
      var regex = new Regex(@".*\.(?<begin>\d+)__(?<end>\d+)\.[(avs)|(vpy)]", RegexOptions.IgnoreCase);

      Match match = regex.Match(files[0]);
      if (match.Success)
      {
        //文字　→　数値
        string sbegin = match.Groups["begin"].Value;
        string send = match.Groups["end"].Value;
        try
        {
          int ibegin = int.Parse(sbegin);
          int iend = int.Parse(send);
          return new int[] { ibegin, iend };
        }
        catch
        {
          // parse error
          return null;
        }
      }
      else  // match error
        return null;
    }


    /// <summary>
    /// *.frame.txtを取得
    /// </summary>
    private static List<int> FrameFile_to_List(string framePath)
    {
      //List<string>  -->  List<int>
      var ConvertToIntList = new Func<List<string>, List<int>>(
        (stringList) =>
        {
          stringList = stringList.Select(
                                  (line) =>
                                  {
                                    //コメント削除、トリム
                                    int found = line.IndexOf("//");
                                    line = (0 <= found) ? line.Substring(0, found) : line;
                                    line = line.Trim();
                                    return line;
                                  })
                                  .Where((line) => string.IsNullOrWhiteSpace(line) == false)    //空白行削除
                                  .Distinct()                                                   //重複削除
                                  .ToList();

          var intList = new List<int>();
          try
          {
            foreach (var line in stringList)
              intList.Add(int.Parse(line));
          }
          catch
          {
            return null;  //変換失敗
          }

          return intList;
        });

      //check
      if (File.Exists(framePath) == false) return null;

      //読
      var frameText = File.ReadAllLines(framePath, Encoding.GetEncoding("Shift_JIS")).ToList();
      if (frameText == null) return null;

      //List<string>  -->  List<int>
      var frameList = ConvertToIntList(frameText);

      //エラーチェック
      if (frameList == null) return null;
      if (frameList.Count % 2 == 1) return null;           //奇数ならエラー
      return frameList;
    }

    #endregion FrameSet作成

  }//class FrameSet

  #endregion FrameSet


}//namespace