using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using LGLauncher.Frame;

namespace TweakFrame
{
  class Program
  {
    static void Main(string[] args)
    {
      //args = new string[] { @"sample.frame.txt" };
      Tweak_Main(args);
      //Test_Main(args);
    }


    /// <summary>
    /// Tweak_Main
    /// </summary>
    static void Tweak_Main(string[] args)
    {
      string path = args.FirstOrDefault();
      if (path == null) return;
      bool isTxt = Path.GetExtension(path).ToLower() == ".txt";
      if (isTxt == false) return;
      var frame = ConvertFrame.Read_FrameFile(path);
      if (frame == null) return;
      if (frame.Count <= 2) return;
      SideTool.ShowFrame(frame, "file");


      //tweak
      const double Regard_MainAsCm__Side = 20.0;
      const double Regard_CmAsMain__Side = 14.0;
      const double Regard_MainAsCm__Center = 60.0;
      const double Regard_CmAsMain__Center = 40.0;

      double total = frame.Last() + 29.970 * 60;//推定
      double first_10min = 30 * 60 * 10, Last_10min = total - 30 * 60 * 10;//in frames

      var tweak = frame.ToList();//copy
      for (int i = 0; i < 2; i++)
      {
        tweak = Tweaker.FlatOut_CM__(tweak, Regard_CmAsMain__Side, 0, first_10min);
        tweak = Tweaker.FlatOut_CM__(tweak, Regard_CmAsMain__Center, first_10min, Last_10min);
        tweak = Tweaker.FlatOut_CM__(tweak, Regard_CmAsMain__Side, Last_10min, total);

        tweak = Tweaker.FlatOut_Main(tweak, Regard_MainAsCm__Side, 0, first_10min);
        tweak = Tweaker.FlatOut_Main(tweak, Regard_MainAsCm__Center, first_10min, Last_10min);
        tweak = Tweaker.FlatOut_Main(tweak, Regard_MainAsCm__Side, Last_10min, total);
      }
      SideTool.ShowFrame(frame, "file");
      SideTool.ShowFrame(tweak, "tweak");


      //変化があった？
      int Len_file = SideTool.GetTotalMainLen(frame);
      int Len_tweak = SideTool.GetTotalMainLen(tweak);
      int diff = Math.Abs(Len_file - Len_tweak);
      bool isDiff = 3.0 * 29.970 < diff;    // Nsec以上の変化がある
      if (isDiff)
      {
        string dir = Path.GetDirectoryName(path);
        string name = Path.GetFileName(path);                                  //sample.frame.txt
        string basename = name.Substring(0, name.Length - ".frame.txt".Length);//sample
        //frame
        {
          string[] org_frameText = frame.Select(f => f.ToString()).ToArray();
          string[] twk_frameText = tweak.Select(f => f.ToString()).ToArray();
          string org_path = Path.Combine(dir, "Origin_" + basename + ".frame.txt");
          string twk_path = Path.Combine(dir, "Tweak__" + basename + ".frame.txt");
          File.WriteAllLines(org_path, org_frameText);
          File.WriteAllLines(twk_path, twk_frameText);
        }
        //tvtp
        {
          string org_tvtpText = ConvertFrame.To_TvtpChap(frame);
          string twk_tvtpText = ConvertFrame.To_TvtpChap(tweak);
          string org_path = Path.Combine(dir, "Origin_" + basename + ".chapter");
          string twk_path = Path.Combine(dir, "Tweak__" + basename + ".chapter");
          File.WriteAllText(org_path, org_tvtpText);
          File.WriteAllText(twk_path, twk_tvtpText);
        }
      }
    }


    /// <summary>
    /// Test_Main
    /// </summary>
    static void Test_Main(string[] args)
    {
      string path = args.FirstOrDefault();
      if (path == null) return;
      var frame = ConvertFrame.Read_FrameFile(path);
      if (frame == null) return;
      if (frame.Count <= 2) return;

      SideTool.ShowFrame(frame, "file");


      //var mn_1 = ConvertFrame.FlatOut_Main(frame, 120.0 / 30);
      //SideTool.ShowFrame(mn_1, "FlatOut_Main1");
      //var mn_2 = Tweaker.FlatOut_Main(frame, 120.0 / 30, 0.85, 1.0);
      //SideTool.ShowFrame(mn_2, "FlatOut_Main2");


      var cm_1 = ConvertFrame.FlatOut_CM__(frame, 200.0 / 30);
      SideTool.ShowFrame(cm_1, "FlatOut_CM__1");
      var cm_2 = Tweaker.FlatOut_CM__(frame, 200.0 / 30, 0.5, 0.95);
      SideTool.ShowFrame(cm_2, "FlatOut_CM__2");
    }








  }
}
