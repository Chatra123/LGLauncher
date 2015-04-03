using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;


#region region_title
#endregion

namespace LGLauncher
{
	class Program
	{
		static void Main(string[] args)
		{
			/*テスト用引数*/
			//var testArgs = new List<string>() { "-no", "1", "-last" , "-ts",
			//																		@".\cap8s.ts",
			//																		"-ch", "CBC", "-program", "program" };
			//args = testArgs.ToArray();


			// try ~ catch で捕捉されてない例外を処理する
			AppDomain.CurrentDomain.UnhandledException += ExceptionInfo.CurrentDomain_UnhandledException;


			//
			//初期化
			ArgsParse(args);																			//引数解析
			PathList.InitializeDir();

			Setting.LoadFile();																		//設定読込
			if (Setting.file.bEnable <= 0) return;

			bool makepath = PathList.Make();                      //パス作成
			if (makepath == false) { Log.WriteLine("  →PathList.Make()  fail"); return; }

			bool lockfile = LockTheFile();												//ファイルの移動禁止
			if (lockfile == false) { Log.WriteLine("  →LockTheFile()  fail"); return; }

			DeleteWorkItem_Beforehand();													//分割処理の初回ならファイル削除

			//ログ
			if (PathList.No == -1 || PathList.No == 1) Log.WriteLine(PathList.TsPath);
			Log.WriteLine("  No=【    " + PathList.No + "    】");




			//
			//メイン処理
			//
			new Action(() =>
			{
				//avs作成
				string avsPath = "";
				if (PathList.D2vMode)
				{
					//d2vからavs作成
					var d2vAvsPath = AvsWithD2v.Make();
					if (File.Exists(d2vAvsPath) == false) { Log.WriteLine("  →AvsWithD2v.Make()  fail"); return; }	//avs作成失敗
					avsPath = d2vAvsPath;
				}
				else
				{
					//lwiからavs作成
					var lwiAvsPath = AvsWithLwi.Make();
					if (File.Exists(lwiAvsPath) == false) { Log.WriteLine("  →AvsWithLwi.Make()  fail"); return; }	//avs作成失敗
					avsPath = lwiAvsPath;
				}

				//タイムシフトsrt作成
				string srtPath = TimeShiftSrt.Make();																		//作成できなくても継続


				//LogoGuillo起動バッチ作成
				var batPath = LGLauncherBat.Make(avsPath, srtPath);
				if (File.Exists(batPath) == false) { Log.WriteLine("  →LGLauncherBat.Make()  fail"); return; }		//bat作成失敗



				//LogoGuillo同時起動数の制限
				bool ready = WaitForReady();																						//セマフォ取得
				if (ready == false) { Log.WriteLine("  →WaitForReady()  fail"); return; }


				//LogoGuillo起動
				bool launch;
				if (PathList.D2vMode)
				{
					launch = LaunchLogoGuillo(batPath);
				}
				else
				{
					AvsWithLwi.SetLwi();
					launch = LaunchLogoGuillo(batPath);
					AvsWithLwi.BackLwi();
				}

				if (LGLSemaphore != null) LGLSemaphore.Release();												//セマフォ解放
				if (launch == false) { Log.WriteLine("  →LaunchLogoGuillo()  fail"); return; }

			})();



			//
			//フレーム合成＆チャプターファイル作成
			bool concat = EditFrame.Concat();
			if (concat == false) { Log.WriteLine("  →EditFrame()  fail"); return; }


			//
			//ファイル削除
			Log.Close();																					//*.logも削除するので閉じる。
			DeleteWorkItem_Lastly();

		}



		//================================
		//コマンドライン引数
		//================================
		#region ArgsParser
		//static class ArgsParser
		//{
		//
		//引数解析
		public static void ArgsParse(string[] args)
		{
			for (int i = 0; i < args.Count(); i++)
			{
				string name, param = "";
				int parse;
				name = args[i].ToLower();																		//引数を小文字に変換
				param = (i + 1 < args.Count()) ? args[i + 1] : "";

				if (name.IndexOf("-") == 0 || name.IndexOf("/") == 0)
					name = name.Substring(1, name.Length - 1);								//  - / をはずす
				else
					continue;																									//  - / がない


				switch (name)
				{
					//小文字で比較
					case "no":
						if (int.TryParse(param, out parse))
							PathList.No = parse;
						break;

					case "ts":
						PathList.TsPath = param;
						break;

					case "d2v":
						PathList.D2vPath = param;
						break;

					case "lwi":
						PathList.LwiPath = param;
						break;

					case "srt":
						PathList.SrtPath = param;
						break;

					case "subdir":
						PathList.SubDir = param;
						break;

					case "ch":
					case "channel":
						PathList.Channel = param;
						break;

					case "program":
						PathList.Program = param;
						break;

					case "last":
						PathList.IsLast = true;
						break;

					default:
						break;

				}//switch
			}//for

		}
		//}//class
		#endregion



		//================================
		//ファイルの移動を禁止、ファイルシェアのチェック
		//================================
		#region LockTheFile
		static FileStream lock_ts, lock_d2v, lock_lwi, lock_lwifooter, lock_srt;			//プロセス終了でロック解放
		static bool LockTheFile()
		{
			//ts
			try
			{
				lock_ts = new FileStream(PathList.TsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			}
			catch { Log.WriteLine("ts file open error"); return false; }


			//d2v
			if (PathList.D2vMode == true)
				try
				{
					lock_d2v = new FileStream(PathList.D2vPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				}
				catch { Log.WriteLine("d2v file open error"); return false; }

			//lwi
			if (PathList.D2vMode == false)
				try
				{
					lock_lwi = new FileStream(PathList.LwiPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				}
				catch { Log.WriteLine("lwi file open error"); return false; }

			//lwifooter
			if (PathList.D2vMode == false)
				if (File.Exists(PathList.LwiFooterPath))		//ファイルが無い場合もある
					try
					{
						lock_lwifooter = new FileStream(PathList.LwiFooterPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
					}
					catch { Log.WriteLine("lwi footer file open error"); return false; }


			//srt
			if (File.Exists(PathList.SrtPath))						//ファイルが無い場合もある
				try
				{
					/*　srtファイルはCaption2Ass_PCR_pfによって削除される可能性があるのでファイルサイズを調べてからロックする。*/
					var fi = new FileInfo(PathList.SrtPath);
					if (3 < fi.Length)	//gt 3byte bom size
						lock_srt = new FileStream(PathList.SrtPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				}
				catch { Log.WriteLine("srt file open error"); return false; }


			return true;

		}


		#endregion



		//================================
		//LogoGuillo同時起動数の制限
		//================================
		#region WaitForReady
		static Semaphore LGLSemaphore = null;
		static bool WaitForReady()
		{
			//同時起動数
			int multiRun = Setting.file.iLogoGuillo_MultipleRun;
			if (multiRun <= 0) { Log.WriteLine("multiRun <= 0"); return false; }


			//セマフォを取得
			//		LGLauncher同士での衝突回避
			var GetSemaphore = new Func<Semaphore>(() =>
			{
				var semaphore = new Semaphore(multiRun, multiRun, "LGL-A8245043-3476");
				var waitBegin = DateTime.Now;

				while (semaphore.WaitOne(60 * 1000) == false)
				{
					//タイムアウト？
					if (30 < (DateTime.Now - waitBegin).TotalMinutes)
					{
						//プロセスを強制終了するとセマフォが解放されない。
						//　全てのLGLauncherが終了するとリセットされる。
						Log.WriteLine(DateTime.Now.ToString("G"));
						Log.WriteLine("timeout of semaphore release");
						semaphore = null;
						break;
					}
				}

				return semaphore;
			});


			//プロセス数が規定値未満か？
			//		LogoGuillo単体、外部ランチャーとの衝突回避
			var LogoGuilloHasExited = new Func<bool, bool>((extraWait) =>
			{
				int PID = Process.GetCurrentProcess().Id;
				var rand = new Random(PID + DateTime.Now.Millisecond);

				var prclist = Process.GetProcessesByName("LogoGuillo");				//確認	.exeはつけない
				if (prclist.Count() < multiRun)
				{
					Thread.Sleep(rand.Next(5 * 1000, 10 * 1000));
					if (extraWait)
						Thread.Sleep(rand.Next(0 * 1000, 30 * 1000));							//追加の待機
					prclist = Process.GetProcessesByName("LogoGuillo");					//再確認
					if (prclist.Count() < multiRun) return true;
				}

				return false;
			});


			//システムがアイドル状態か？
			var SystemIsIdle = new Func<bool>(() =>
			{
				//SystemIdleMonitor.exeは起動時の負荷が高い
				string monitor_path = Path.Combine(PathList.LSystemDir, "SystemIdleMonitor.exe");
				string monitor_arg = "";
				if (File.Exists(monitor_path) == false) return true;

				var prc = new Process();
				prc.StartInfo.FileName = monitor_path;
				prc.StartInfo.Arguments = monitor_arg;
				prc.StartInfo.CreateNoWindow = true;
				prc.StartInfo.UseShellExecute = false;
				prc.Start();
				prc.WaitForExit();
				return prc.ExitCode == 0;

			});


			//
			//WaitForReady Main loop
			//

			LGLSemaphore = GetSemaphore();							//セマフォを取得

			while (true)//タイムアウトなし
			{
				bool extraWait = (LGLSemaphore == null);
				//                                          LogoGuilloプロセス数をチェック、終了を待機
				while (LogoGuilloHasExited(extraWait) == false) { Thread.Sleep(17890); }
				//                                          システム負荷が高い、１０分待機
				if (SystemIsIdle() == false) { Thread.Sleep(10 * 60 * 1000); continue; }
				//                                          LogoGuilloプロセス数を再チェック
				if (LogoGuilloHasExited(extraWait) == false) { continue; }
				break;//																		システムチェックＯＫ
			}

			return true;
		}
		#endregion



		//================================
		//LogoGuillo起動
		//================================
		#region LaunchLogoGuillo
		static bool LaunchLogoGuillo(string batPath)
		{
			if (File.Exists(batPath) == false) return false;

			var prc = new Process();
			prc.StartInfo.FileName = batPath;
			prc.StartInfo.CreateNoWindow = true;
			prc.StartInfo.UseShellExecute = false;
			prc.Start();
			prc.WaitForExit();

			//終了コード
			if (prc.ExitCode == 0)
			{
				//正常終了
				return true;
			}
			else if (prc.ExitCode == -9)
			{
				//ロゴ未検出
				Log.WriteLine("LogoGuillo ExitCode = " + prc.ExitCode + " :  ロゴ未検出");
				return false;
			}
			else if (prc.ExitCode == -1)
			{
				//何らかのエラー
				Log.WriteLine("★LogoGuillo ExitCode = " + prc.ExitCode + " :  エラー");
				//File.Create(Path.Combine(PathList.AppDir, "★errLG__" + PathList.TsName)).Close();
				return false;
			}
			else
			{
				//強制終了すると ExitCode = 1
				Log.WriteLine("LogoGuillo ExitCode = " + prc.ExitCode + " :  Unknown code");
				return false;
			}


		}
		//logoGuillo_v210_r1  readme_v210.txt
		// ◎終了コード
		// 0：正常終了
		//-9：ロゴ未検出
		//-1：何らかのエラー
		#endregion




		//================================
		//作業ファイル削除
		//================================
		#region DeleteWorkItem

		//
		//分割処理の初回ならファイル削除
		static void DeleteWorkItem_Beforehand()
		{
			//LWorkDir
			if (PathList.No == 1)
			{
				Delete_file(0.0, PathList.LWorkDir, "*.p?*.*");
			}

		}


		//
		//終了時にファイル削除
		static void DeleteWorkItem_Lastly()
		{
			//使い終わったファイルを直ぐに削除
			if (2 <= Setting.file.iDeleteWorkItem)
			{
				//LWorkDir
				//　全ての作業ファイル削除
				if (PathList.IsLast)
				{
					Delete_file(0.0, PathList.LWorkDir, "_" + PathList.TsShortName + "*");
					Delete_file(0.0, PathList.LWorkDir, PathList.TsShortName + "*");
				}
				//　１つ前の作業ファイル削除
				else if (2 <= PathList.No)
					Delete_file(0.0, PathList.LWorkDir, PathList.WorkName_m1 + "*", "catframe.txt");
			}

			//古いファイル削除
			if (1 <= Setting.file.iDeleteWorkItem)
			{
				if (PathList.No == 1 || PathList.IsLast)
				{
					const double ndaysBefore = 2.0;
					//LTopWorkDir
					Delete_file(ndaysBefore, PathList.LTopWorkDir, "*.all.*");
					Delete_file(ndaysBefore, PathList.LTopWorkDir, "*.p?*.*");
					Delete_file(ndaysBefore, PathList.LTopWorkDir, "*.sys.*");
					Delete_emptydir(PathList.LTopWorkDir);

					//Windows Temp
					Delete_file(ndaysBefore, Path.GetTempPath(), "logoGuillo_*.avs");
					Delete_file(ndaysBefore, Path.GetTempPath(), "logoGuillo_*.txt");
					Delete_file(ndaysBefore, Path.GetTempPath(), "DGI_pf.tmp*");
					Delete_file(ndaysBefore, Path.GetTempPath(), "DGI_pf.log");
				}
			}

		}



		//
		//削除処理の実行部
		//searchKey:　*が使える		ingnoreKey:　*は使えない
		static void Delete_file(double nDaysBefore, string directory, string searchKey, string ignoreKey = null)
		{
			if (Directory.Exists(directory) == false) return;
			Thread.Sleep(500);

			//ファイル取得
			var dirInfo = new DirectoryInfo(directory);
			var files = dirInfo.GetFiles(searchKey, SearchOption.AllDirectories);

			foreach (var onefile in files)
			{
				if (onefile.Exists == false) continue;
				if (ignoreKey != null && 0 <= onefile.Name.IndexOf(ignoreKey)) continue;

				//nDaysBeforeより前のファイル？
				bool over_creation = nDaysBefore < (DateTime.Now - onefile.CreationTime).TotalDays;
				bool over_lastwrite = nDaysBefore < (DateTime.Now - onefile.LastWriteTime).TotalDays;
				if (over_creation && over_lastwrite)
				{
					try { onefile.Delete(); }	//ファイル削除
					catch { }									//ファイル使用中
				}
			}

		}



		//
		//空フォルダ削除
		static void Delete_emptydir(string directory)
		{
			if (Directory.Exists(directory) == false) return;

			var dirInfo = new DirectoryInfo(directory);
			var dirs = dirInfo.GetDirectories("*", SearchOption.AllDirectories);

			foreach (var onedir in dirs)
			{
				if (onedir.Exists == false) continue;

				//空フォルダ？
				var files = onedir.GetFiles();
				if (files.Count() == 0)
				{
					try { onedir.Delete(); }
					catch { }
				}
			}

		}

		#endregion



	}










}






