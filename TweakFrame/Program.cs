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
      AppDomain.CurrentDomain.UnhandledException += OctNov.Excp.ExceptionInfo.OnUnhandledException;

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
      const double Regard_MainAsCm__Center = 60.0;
      const double Regard_CmAsMain__Side = 14.0;
      const double Regard_CmAsMain__Center = 28.0;

      double total = frame.Last() + 29.970 * 60;//推定
      double First_5m = 30 * 60 * 5, Last_5m = total - 30 * 60 * 5;


      //double First_10m = 30 * 60 * 10, Last_10m = total - 30 * 60 * 10;
      double First_Nm = First_5m;
      double Last_Nm = Last_5m;


      var tweak = frame.ToList();//copy
      for (int i = 0; i < 2; i++)
      {
        tweak = ConvertFrame2.FlatOut_CM__(tweak, Regard_CmAsMain__Side, 0, First_Nm);
        tweak = ConvertFrame2.FlatOut_CM__(tweak, Regard_CmAsMain__Center, First_Nm, Last_Nm);
        tweak = ConvertFrame2.FlatOut_CM__(tweak, Regard_CmAsMain__Side, Last_Nm, total);

        tweak = ConvertFrame2.FlatOut_Main(tweak, Regard_MainAsCm__Side, 0, First_Nm);
        tweak = ConvertFrame2.FlatOut_Main(tweak, Regard_MainAsCm__Center, First_Nm, Last_Nm);
        tweak = ConvertFrame2.FlatOut_Main(tweak, Regard_MainAsCm__Side, Last_Nm, total);
      }
      SideTool.ShowFrame(frame, "file");
      SideTool.ShowFrame(tweak, "tweak");


      //変化があった？
      bool diff = 4 <= Math.Abs(frame.Count() - tweak.Count());
      if (diff)
      {
        //save tvtp chapter
        string dir = Path.GetDirectoryName(path);
        string name = Path.GetFileName(path);
        string basename = name.Substring(0, name.Length - ".frame.txt".Length);

        string org_tvtpText = ConvertFrame.To_TvtpChap(frame);
        string twk_tvtpText = ConvertFrame.To_TvtpChap(tweak);
        string org_path = Path.Combine(dir, "Original " + basename + ".chapter");
        string twk_path = Path.Combine(dir, "Edit     " + basename + ".chapter");
        File.WriteAllText(org_path, org_tvtpText);
        File.WriteAllText(twk_path, twk_tvtpText);
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
      //var mn_2 = ConvertFrame2.FlatOut_Main(frame, 120.0 / 30, 0.85, 1.0);
      //SideTool.ShowFrame(mn_2, "FlatOut_Main2");


      var cm_1 = ConvertFrame.FlatOut_CM__(frame, 200.0 / 30);
      SideTool.ShowFrame(cm_1, "FlatOut_CM__1");
      var cm_2 = ConvertFrame2.FlatOut_CM__(frame, 200.0 / 30, 0.5, 0.95);
      SideTool.ShowFrame(cm_2, "FlatOut_CM__2");
    }








  }
}
