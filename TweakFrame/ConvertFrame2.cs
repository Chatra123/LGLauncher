using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace TweakFrame
{
  static class ConvertFrame2
  {

    /// <summary>
    /// 短いＭａｉｎをつぶす
    /// </summary>
    /// <param name="frameList">元になるフレームリスト</param>
    /// <param name="mini_main_sec">指定秒数以下のＭａｉｎをつぶす</param>
    /// <returns>
    ///   成功  -->  List<int>
    ///   失敗  -->  null
    /// </returns>
    public static List<int> FlatOut_Main(
      List<int> frameList,
      double mini_main_sec,
      double border_begin,
      double border_end)
    {
      if (frameList == null) return null;
      if (frameList.Count % 2 == 1) return null;
      if (frameList.Count <= 2) return frameList;

      var newList = new List<int>();
      int mini_main = (int)(mini_main_sec * 29.970);// sec --> frame
      for (int i = 0; i < frameList.Count; i += 2)
      {
        //border_begin to border_endの範囲
        if (border_begin <= frameList[i] && frameList[i] <= border_end)
        {
          int mainLen = frameList[i + 1] - frameList[i];
          if (mini_main < mainLen)
          {
            newList.Add(frameList[i]);
            newList.Add(frameList[i + 1]);
          }
        }
        else
        {
          //範囲外なのでそのまま
          newList.Add(frameList[i]);
          newList.Add(frameList[i + 1]);
        }
      }
      return newList;
    }



    /// <summary>
    /// 短いＣＭをつぶす
    /// </summary>
    /// <param name="frameList">元になるフレームリスト</param>
    /// <param name="mini_cm_sec">指定秒数以下のＣＭをつぶす</param>
    /// <returns>
    ///   成功  -->  List<int>
    ///   失敗  -->  null
    /// </returns>
    /// <remarks>開始直後のＣＭはつぶさない。</remarks>
    public static List<int> FlatOut_CM__(
      List<int> frameList,
      double mini_cm_sec,
      double border_begin,
      double border_end)
    {
      if (frameList == null) return null;
      if (frameList.Count % 2 == 1) return null;
      if (frameList.Count <= 2) return frameList;

      //「frameList[i]」と「newListの末尾」の差が CM Length
      //
      // CM Lengthが長ければ newListに本編 frameList[i], frameList[i+1]を加える。
      //　　　　　　短ければ newList[last]を次の本編終端 frameList[i + 1]に置き換え短いＣＭをつぶす。
      //ただし、開始直後のＣＭはそのまま、
      //開始直後に短いＣＭがあっても本編には加えない。
      var newList = new List<int>();
      int mini_cm = (int)(mini_cm_sec * 29.970);// sec --> frame

      //1st main
      newList.Add(frameList[0]);
      newList.Add(frameList[1]);
      for (int i = 2; i < frameList.Count; i += 2)
      {
        //border_begin to border_endの範囲
        if (border_begin <= frameList[i] && frameList[i] <= border_end)
        {
          int cmLen = frameList[i] - newList.Last();
          if (mini_cm < cmLen)
          {
            //長
            //ＣＭを採用し通常の　本編始端＆終端　を加える
            newList.Add(frameList[i]);
            newList.Add(frameList[i + 1]);
          }
          else
          {
            //短
            //ＣＭを無視し本編内とする。次の本編終端に置き換え短いＣＭをつぶす。
            newList[newList.Count - 1] = frameList[i + 1];
          }
        }
        else
        {
          newList.Add(frameList[i]);
          newList.Add(frameList[i + 1]);
        }
      }

      return newList;
    }






  }
}
