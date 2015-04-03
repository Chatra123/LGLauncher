using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;

namespace LGLauncher
{

	static class LGLauncherBat
	{
		static public string Make(string avsPath, string srtPath)
		{
			//ファイルチェック
			if (File.Exists(avsPath) == false) { Log.WriteLine("LGLauncherBat  File.Exists(avsPath) == false"); return ""; } 		//avsがなければ終了


			//srtをavsと同じ名前にリネーム
			if (File.Exists(srtPath))
			{
				string avsWithoutExt = Path.GetFileNameWithoutExtension(avsPath);
				string newSrtPath = Path.Combine(PathList.LWorkDir, avsWithoutExt + ".srt");
				try
				{
					if (File.Exists(newSrtPath)) File.Delete(newSrtPath);
					File.Move(srtPath, newSrtPath);
				}
				catch { }
			}


			//
			//baseLGLauncher.bat読込み
			//　ファイルがあればファイルから読込み
			//	　　　　　なければ内部リソースから読込み
			var batText = new List<string>();
			string batPath = Path.Combine(PathList.AppDir, "baseLGLauncher.bat");
			if (File.Exists(batPath))
			{
				batText = FileR.ReadAllLines(batPath);									//ファイル
				if (batText == null) { Log.WriteLine("baseLGLauncher.bat read error"); return ""; }
			}
			else
				batText = FileR.ReadFromResource("baseLGLauncher.bat");	//リソース



			//ロゴデータ取得
			if (string.IsNullOrWhiteSpace(PathList.Channel)) { Log.WriteLine("string.IsNullOrWhiteSpace(PathList.Channel)"); return ""; }
			var logoAndParam = GetLogoAndParam(PathList.Channel, PathList.Program, PathList.TsPath);
			if (logoAndParam == null) { Log.WriteLine("logoAndParam == null"); return ""; }
			if (logoAndParam.Count < 2) { Log.WriteLine("logoAndParam.Count < 2"); return ""; }

			string logoPath = logoAndParam[0];
			string paramPath = logoAndParam[1];
			if (File.Exists(logoPath) == false) { Log.WriteLine("not found *.lgd : " + logoPath); return ""; }
			if (File.Exists(paramPath) == false) { Log.WriteLine("not found *.autoTune.param : " + paramPath); return ""; }
			if (File.Exists(PathList.LogoGuillo) == false) { Log.WriteLine("not found LogoGuillo"); return ""; }



			//#LOGOG_PATH#
			string LOGOG_PATH = @"..\..\LSystem\LogoGuillo.exe";
			//#AVS2X_PATH#
			string AVS2X_PATH = @"..\..\LSystem\avs2pipemod.exe";
			//"#AVSPLG_PATH#"
			string AVSPLG_PATH = @"..\..\LWork\USE_AVS";
			//"#VIDEO_PATH#"
			string VIDEO_PATH = avsPath;//相対バスだとLOGOGが進まなかった。フルパスで指定
			//"#LOGO_PATH#"
			string LOGO_PATH = logoPath;
			//"#PRM_PATH#"
			string PRM_PATH = paramPath;
			//"#OUTPUT_PATH#"
			string OUTPUT_PATH = PathList.WorkName + ".frame.txt";



			//bat書き換え
			for (int i = 0; i < batText.Count; i++)
			{
				var line = batText[i];
				//LGL
				line = Regex.Replace(line, "#WorkDir#", PathList.LWorkDir, RegexOptions.IgnoreCase);
				line = Regex.Replace(line, "#PartNo#", "" + PathList.No, RegexOptions.IgnoreCase);
				line = Regex.Replace(line, "#TsShortName#", PathList.TsShortName, RegexOptions.IgnoreCase);
				//LOGOG
				line = Regex.Replace(line, "#LOGOG_PATH#", LOGOG_PATH, RegexOptions.IgnoreCase);
				line = Regex.Replace(line, "#AVS2X_PATH#", AVS2X_PATH, RegexOptions.IgnoreCase);
				line = Regex.Replace(line, "#AVSPLG_PATH#", AVSPLG_PATH, RegexOptions.IgnoreCase);
				line = Regex.Replace(line, "#VIDEO_PATH#", VIDEO_PATH, RegexOptions.IgnoreCase);
				line = Regex.Replace(line, "#LOGO_PATH#", LOGO_PATH, RegexOptions.IgnoreCase);
				line = Regex.Replace(line, "#PRM_PATH#", PRM_PATH, RegexOptions.IgnoreCase);
				line = Regex.Replace(line, "#OUTPUT_PATH#", OUTPUT_PATH, RegexOptions.IgnoreCase);
				batText[i] = line;
			}



			//出力ファイル名
			string outBatPath;
			outBatPath = PathList.WorkPath + ".bat";

			//ファイル書込み
			FileW.WriteAllLines(outBatPath, batText);



			return outBatPath;
		}



		//======================================
		//ロゴデータ取得
		//======================================
		#region GetLogoAndParam
		static List<string> GetLogoAndParam(string channel, string program, string tsPath)
		{
			//ファイルチェック
			if (File.Exists(PathList.LogoSelector) == false)
			{
				Log.WriteLine("not found LogoSelector");
				return null;
			}


			//パス、コマンド引数
			string exepath = "", arg = "";
			var ext = Path.GetExtension(PathList.LogoSelector).ToLower();

			if (ext == ".exe")
			{
				exepath = PathList.LogoSelector;
				arg = string.Format("  \"{0}\"   \"{1}\"   \"{2}\"  ",
															channel, program, tsPath);
			}
			else if (ext == ".vbs" || ext == ".js")
			{
				exepath = "cscript.exe";
				arg = string.Format("  \"{0}\"   \"{1}\"   \"{2}\"   \"{3}\"  ",
															PathList.LogoSelector, channel, program, tsPath);
			}
			else
				exepath = "ext does not correspond";


			//実行
			Log.WriteLine("LogoSelector:");
			Log.WriteLine(exepath);
			Log.WriteLine("arg   :");
			Log.WriteLine(arg);
			string result = Get_stdout(exepath, arg);
			Log.WriteLine("return:");
			Log.WriteLine(result);
			var split = result.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();

			return split;
		}
		#endregion



		//======================================
		//プロセス実行  標準出力を読み取る
		//======================================
		#region Get_stdout
		static string Get_stdout(string exepath, string arg)
		{
			var prc = new Process();
			prc.StartInfo.FileName = exepath;
			prc.StartInfo.Arguments = arg;
			//シェルコマンドを無効に、入出力をリダイレクトするなら必ずfalseに設定
			prc.StartInfo.UseShellExecute = false;
			prc.StartInfo.RedirectStandardOutput = true;					//入出力のリダイレクト
			prc.Start();
			//標準出力を読み取る、プロセス終了まで待機
			string result = prc.StandardOutput.ReadToEnd();
			prc.WaitForExit();
			prc.Close();
			return result;
		}
		#endregion




	}
}












