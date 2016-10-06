using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace TweakFrame
{
  static class SideTool
  {
    /// <summary>
    /// Show
    /// </summary>
    public static string ShowFrame(List<int> frame, string title = "")
    {
      List<int> main, cm__;
      Get_Main_CM_Len(frame, out main, out cm__);


      string sp0 = new string(' ', 2);
      string sp1 = new string(' ', 6);
      string sp2 = new string(' ', 12);
      var text = new StringBuilder();

      text.AppendLine(title);
      text.Append(string.Format("frame({0,2}) : {1}", main.Count, sp1));
      for (int i = 0; i < frame.Count; i += 2)
        text.Append(string.Format("{0,6}{2}{1,6}{2}{3}", frame[i], frame[i + 1], sp0, sp1));
      text.AppendLine();

      text.Append(string.Format("main ({0,2}) : {1}", main.Count, sp1 + sp0 + sp0));
      main.ForEach(f => text.Append(string.Format("{0,6}{2}{1}{2}{3}", f, sp1, sp0, sp1)));
      text.AppendLine();

      text.Append(string.Format("cm   ({0,2}) : {1}", cm__.Count, ""));
      cm__.ForEach(f => text.Append(string.Format("{0,6}{2}{1}{2}{3}", f, sp1, sp0, sp1)));
      text.AppendLine();

      text.Append(new string('-', 100));
      text.AppendLine();
      System.Diagnostics.Trace.WriteLine(text.ToString());
      return text.ToString();
    }


    /// <summary>
    /// split frame to Main, CM list
    /// </summary>
    static void Get_Main_CM_Len(
      List<int> frame,
      out List<int> main,
      out List<int> cm)
    {
      main = new List<int>();
      cm = new List<int>();
      if (frame.Count == 0) return;

      for (int i = 0; i < frame.Count; i += 2)
      {
        //main
        int len_m = frame[i + 1] - frame[i];
        main.Add(len_m);
        //cm
        if (i == 0)
        {
          if (frame[i] == 0)
            cm.Add(0);
          else
            cm.Add(frame[i]);
        }
        else
        {
          int len = frame[i] - frame[i - 1];
          cm.Add(len);
        }
      }
      //last cm
      {
        int total = frame.Last();
        int len = total - frame.Last();
        cm.Add(len);
      }
    }



    /// <summary>
    /// GetTotalMainLen  frame
    /// </summary>
    public static int GetTotalMainLen(List<int> frame)
    {
      int sum = 0;
      for (int i = 0; i < frame.Count; i += 2)
      {
        int len = frame[i + 1] - frame[i];
        sum += len;
      }
      return sum;
    }




  }
}
