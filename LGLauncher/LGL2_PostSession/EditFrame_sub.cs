using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGLauncher
{
	static partial class EditFrame
	{

		//======================================
		//TvtPlayチャプター形式に変換
		//======================================
		#region ConvertToTvtPlayChap
		public static string ConvertToTvtPlayChap(List<int> frameList)
		{
			//フレーム数を100msec単位の時間に変換
			//		300[frame]  ->  300 / 29.97 * 10  ->  100[100msec]
			var timeList = frameList.Select((frame) => { return (int)((1.0 * frame / 29.970) * 10.0); }).ToList();
			//intへの変換で丸められる。同じ値が続いていたら＋１
			for (int i = 1; i < timeList.Count; i++)
			{			// front 					 back
				if (timeList[i - 1] == timeList[i]) timeList[i]++;
			}


			//
			var chapText = new List<string>() { "c-" };
			for (int i = 0; i < timeList.Count; i++)
			{
				int time = timeList[i];		//100ms単位
				// 開始直後のＣＭスキップ用
				// 　最初のメインが２秒より後にあるなら追加、	1.6倍速だと14dix-でスキップしなので 20dix-挿入
				if (i == 0 && 20 < time) { chapText.Add("20dix-"); }

				// スキップチャプター
				if (i % 2 == 0) { chapText.Add("" + time + "dox-"); }		//even  cm out
				else { chapText.Add("" + time + "dix-"); }					  	//odd		cm in		
			}
			chapText.Add("0eox-c");		//閉じる


			//１行にする
			string oneliner = "";
			foreach (var text in chapText) oneliner += text;
			return oneliner;
		}
		/*
		 *	ＣＭから開始											ＣＭで終わり
		 *				0		1		2		3		4		5		6		7		8		9		10
		 * main					■	■					■
		 * cm				□					□	□			□
		 * 
		 * 　本編から開始											本編で終わり
		 *				0		1		2		3		4		5		6		7		8		9		10
		 * main			■	■	■					■	■
		 * cm										□	□
		 */
		/*TvtPlay  ChapterMap.cpp*/
		// [チャプターコマンド仕様]
		// ・ファイルの文字コードはBOM付きUTF-8であること
		// ・Caseはできるだけ保存するが区別しない
		// ・"c-"で始めて"c"で終わる
		// ・チャプターごとに"{正整数}{接頭英文字}{文字列}-"を追加する
		//   ・{接頭英文字}が"c" "d" "e"以外のとき、そのチャプターを無視する
		//     ・"c"なら{正整数}の単位はmsec
		//     ・"d"なら{正整数}の単位は100msec
		//     ・"e"なら{正整数}はCHAPTER_POS_MAX(動画の末尾)
		//   ・{文字列}は0～CHAPTER_NAME_MAX-1文字
		// ・仕様を満たさないコマンドは(できるだけ)全体を無視する
		// ・例1: "c-c" (仕様を満たす最小コマンド)
		// ・例2: "c-1234cName1-3456c-2345c2ndName-0e-c"
		//
		//
		/*TvtPlay  Readme.txt*/
		//TsSkipXChapter【ver.1.4～】
		//チャプタースキップする[=1]かどうか
		// スキップチャプター(名前が"ix"または"ox"で始まるもの)の間をスキップします。
		//
		//
		//開始直後の０秒目はスキップが機能しない
		//1.0倍速		14dix-
		//1.2				16dix-
		//1.4				18dix-
		//1.6				20dix-
		//
		#endregion







		//======================================
		//短いMain CM をつぶす
		//======================================
		#region FlatOut
		//
		//FlatOut_Main
		public static List<int> FlatOut_Main(List<int> frameList, double miniMain_sec)
		{
			//エラーチェック
			if (frameList == null) { return null; }
			if (frameList.Count == 0) { return null; }
			if (frameList.Count % 2 == 1) { return null; }


			var newList = new List<int>();
			for (int i = 0; i < frameList.Count; i += 2)
			{
				double mainLength_sec = 1.0 * (frameList[i + 1] - frameList[i]) / 29.970;

				if (miniMain_sec < mainLength_sec)
				{
					newList.Add(frameList[i]);
					newList.Add(frameList[i + 1]);
				}
			}

			return newList;
		}


		//
		//FlatOut_CM
		//  開始直後のCMはつぶさない。
		public static List<int> FlatOut_CM__(List<int> frameList, double miniCM_sec)
		{
			//エラーチェック
			if (frameList == null) { return null; }
			if (frameList.Count == 0) { return null; }
			if (frameList.Count % 2 == 1) { return null; }


			var newList = new List<int>();

			//
			//newListの最後のフレーム数とframeListのフレーム数を比較
			//cmLengthが短ければ newList[last]を次の本編終わり frameList[i+1]に置換する
			//　　　　　長ければ newListに本編 frameList[i], frameList[i+1]をいれる
			//開始直後のCMが短いのは無視する。

			//
			//最初のmainを入れる
			newList.Add(frameList[0]);
			newList.Add(frameList[1]);

			for (int i = 2; i < frameList.Count; i += 2)
			{
				double cmLength = 1.0 * (frameList[i] - newList[newList.Count - 1]) / 29.970;
				if (cmLength < miniCM_sec)
				{
					newList[newList.Count - 1] = frameList[i + 1];
				}
				else
				{
					newList.Add(frameList[i]);
					newList.Add(frameList[i + 1]);
				}
			}

			return newList;
		}


		#endregion












	}
}
