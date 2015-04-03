using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace LGLauncher
{
	static class TimeShiftSrt
	{
		static public string Make()
		{
			//ファイルチェック
			//　srtはファイルロックをしないことがあるので読込み前に削除されている可能性がある。
			if (File.Exists(PathList.SrtPath) == false) { return ""; }


			//	partNo == -1ならsrtファイルをコピーしてreturn
			if (PathList.No == -1)
			{
				string copyDstPath = Path.Combine(PathList.LWorkDir, PathList.SrtName);
				try { File.Copy(PathList.SrtPath, copyDstPath); }
				catch { Log.WriteLine("srt file copy error"); return ""; }
				return copyDstPath;
			}


			//ファイル読込み
			var srtText = FileR.ReadAllLines(PathList.SrtPath, TextEnc.UTF8_bom);
			if (srtText == null) { Log.WriteLine("srt read error"); return ""; }
			else if (srtText.Count <= 3) { return ""; }														//テキストが書き込まれてない


			//
			//フォーマット
			//
			//最後の時間行を抽出
			int idx_LastTimeline = 0;
			for (int idx = srtText.Count - 1; 0 <= idx; idx--)
			{
				string line = srtText[idx];
				//00:00:16,216 --> 00:00:18,218
				if (Regex.IsMatch(line, @"\d\d:\d\d:\d\d,\d\d\d\s*-->\s*\d\d:\d\d:\d\d,\d\d\d"))
				{
					idx_LastTimeline = idx;
					break;
				}
			}

			//最後の時間行idx - 2 行目までは確実に書き込まれている。
			//  最後のタイムコード以降を削り、タイムコード - 2までを取り出す。
			int idx_lastValidLine = idx_LastTimeline - 2;
			if (idx_lastValidLine < 0) { Log.WriteLine("srt does not have a valid line"); return ""; }//srt形式でない or テキストが４行以下
			var formatText = (PathList.IsLast) ? srtText : srtText.GetRange(0, idx_lastValidLine + 1);



			//
			//シフト
			//
			//時間をスライドさせて開始を０秒からにする
			var shiftText = formatText;
			if (2 <= PathList.No)							//	partNoが２以上のときのみ
			{
				//前回の終端フレーム数取得
				int[] frameset_m1 = (PathList.D2vMode) ?
					AvsCommon.GetFrame_byName(PathList.WorkName_m1 + ".d2v_*__*.avs") :
					AvsCommon.GetFrame_byName(PathList.WorkName_m1 + ".lwi_*__*.avs");
				if (frameset_m1 == null) { Log.WriteLine("srt frameset_m1 == null"); return ""; }
				double shiftSec = 1.0 * frameset_m1[1] / 29.970;

				//開始を０秒からにする
				shiftText = Shift_TimeCode(shiftText, shiftSec);
			}

			if (shiftText.Count == 0) { Log.WriteLine("shiftText.Count == 0"); return ""; }


			//出力ファイル名
			string dstPath = PathList.WorkPath + ".srt";


			//ファイル書込み
			FileW.WriteAllLines(dstPath, shiftText, TextEnc.UTF8_bom);
			return dstPath;

		}



		//======================================
		//srt全体の時間をずらす
		//======================================
		#region Shift_TimeCode
		//76																					BlockIndex
		//00:10:04,630 --> 00:10:07,500								1stTimecode
		//明日の日本列島､２つの低気圧に挟
		//まれて
		//																						blockend
		//77
		//00:10:07,500 --> 00:10:09,769								2ndTimecode
		//日本海側では午前中を中心に
		//
		static List<string> Shift_TimeCode(List<string> srtText, double shift_sec)
		{
			var shiftText = new List<string>();
			int BlockIndex_shift = 1;

			//最初のタイムコード行を検索
			for (int line1 = 1; line1 < srtText.Count; line1++)
			{

				//タイムコード？
				if (Regex.IsMatch(srtText[line1], @"\d\d:\d\d:\d\d,\d\d\d\s*-->\s*\d\d:\d\d:\d\d,\d\d\d"))
				{

					//タイムコードをシフトした値が０以上か？
					string shiftTimecode;
					bool isValid = ValidateTimecode(srtText[line1], out shiftTimecode, shift_sec);
					if (isValid == false) continue;													//０秒以下になったのでとばす

					//二つ目のタイムコードを探す
					int line_2ndTimecode = -1;
					for (int line2 = line1 + 1; line2 < srtText.Count; line2++)
					{
						if (Regex.IsMatch(srtText[line2], @"\d\d:\d\d:\d\d,\d\d\d\s*-->\s*\d\d:\d\d:\d\d,\d\d\d"))
						{//見つかった
							line_2ndTimecode = line2;
							break;
						}
					}


					int line_blockend;
					if (line_2ndTimecode != -1)														//2ndTimecodeがある
						line_blockend = line_2ndTimecode - 2;								//    2ndタイムコードの2つ前
					else																									//2ndTimecodeがない
						line_blockend = srtText.Count - 1;									//    リストの最後
					//ブロックを取り出す
					shiftText.Add("" + BlockIndex_shift);									//インデックス
					shiftText.Add(shiftTimecode);													//シフトタイムコード
					int blocksize = line_blockend - (line1 + 1) + 1;
					shiftText.AddRange(srtText.GetRange(line1 + 1, blocksize));
					BlockIndex_shift++;


					//検索行を進める
					line1 = line_blockend + 1;
				}
			}

			return shiftText;
		}



		//
		//タイムコードをシフトした値が０秒以上か？
		//
		static readonly int year_ = DateTime.Now.Year, month = DateTime.Now.Month, day__ = DateTime.Now.Day;
		static bool ValidateTimecode(string timecode, out string shiftTimecode, double shift_sec)
		{
			shiftTimecode = "";

			//DateTimeに変換
			string hourA, min_A, sec_A, ms__A, hourB, min_B, sec_B, ms__B;
			int hour1, min_1, sec_1, ms__1, hour2, min_2, sec_2, ms__2;


			//開始時間
			hourA = timecode.Substring(0, 2);
			min_A = timecode.Substring(3, 2);
			sec_A = timecode.Substring(6, 2);
			ms__A = timecode.Substring(9, 3);
			if (int.TryParse(hourA, out hour1) == false) return false;
			if (int.TryParse(min_A, out min_1) == false) return false;
			if (int.TryParse(sec_A, out sec_1) == false) return false;
			if (int.TryParse(ms__A, out ms__1) == false) return false;

			//終了時間
			hourB = timecode.Substring(17, 2);
			min_B = timecode.Substring(20, 2);
			sec_B = timecode.Substring(23, 2);
			ms__B = timecode.Substring(26, 3);
			if (int.TryParse(hourB, out hour2) == false) return false;
			if (int.TryParse(min_B, out min_2) == false) return false;
			if (int.TryParse(sec_B, out sec_2) == false) return false;
			if (int.TryParse(ms__B, out ms__2) == false) return false;


			//DateTime()に変換してshift_secずらす
			var timeZero = new DateTime(year_, month, day__, 0, 0, 0, 0);
			var time1 = new DateTime(year_, month, day__, hour1, min_1, sec_1, ms__1);
			var time2 = new DateTime(year_, month, day__, hour2, min_2, sec_2, ms__2);
			var time1_shift = time1.AddSeconds(-1 * shift_sec);
			var time2_shift = time2.AddSeconds(-1 * shift_sec);
			var span1_ms = (time1_shift - timeZero).TotalMilliseconds;
			var span2_ms = (time2_shift - timeZero).TotalMilliseconds;


			//シフトした後０秒以上ならshiftTimecode作成
			if (span1_ms <= 0 && 0 < span2_ms)
			{//開始時間が０以下になった
				shiftTimecode = "00:00:00,000"
									+ " --> "
									+ time2_shift.ToString("HH:mm:ss,fff");
				return true;
			}
			else if (0 < span1_ms && 0 < span2_ms)
			{//両方０以上
				shiftTimecode = time1_shift.ToString("HH:mm:ss,fff")
													+ " --> "
													+ time2_shift.ToString("HH:mm:ss,fff");
				return true;
			}
			else
			{//両方０以下
				return false;
			}

		}
		#endregion


	}







}






