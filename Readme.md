
## LGLauncher

作成中の d2v, lwi, srtファイルを元にLogoGuilloを実行します。



------------------------------------------------------------------
### 使い方　　コマンドライン

lwi作成中  
    LGLauncher.exe  -part  -ts "C:\Video.ts"  -ch "abc"  
最後のみ  
    LGLauncher.exe  -last  -ts "C:\Video.ts"  -ch "abc"  


lwi作成済み  
    LGLauncher.exe         -ts "C:\Video.ts"  -ch "abc"  


------------------------------------------------------------------
### 必要なソフト

* AviSynth  
    
* LogoGuillo.exe  
    ロゴデータ、パラメーターファイル
    
* avs2pipemod.exe  


------------------------------------------------------------------
### 使用前の準備

1. LSystemフォルダに  
    - logoGuillo.exe  
    - avs2pipemod.exe  
    - LSMASHSource.dll  
   を入れる。

2. LSystemフォルダの LogoSelector.exeを実行してiniを作成し、[LogoDir]を設定する。  


------------------------------------------------------------------
### 引数１

    -ts "C:\video.ts"
tsファイルのフルパス  
tsのパスを元に各ファイルパスを作成します。  
"C:\video.ts.pp.d2v"  
"C:\video.ts.pp.lwi"  
"C:\video.ts.pp.lwifooter"  
"C:\video.srt"  
tsと同じフォルダに d2v, lwi, srtをおいてください。  


    -part  
lwi作成中のＴＳファイルを処理  


    -last  
最後の処理であることを明示する。  
チャプター出力や、JL_標準.txtを用いてjoin_logo_scpの再実行などが行われます。  


    -ch      "abc"
    -channel "abc"
LogoSelecterに渡すチャンネル名  


    
------------------------------------------------------------------
#### 引数２

    -SequenceName  20160701_2000
作業フォルダ名の一部に使用  
基本的に無くても処理できます。

    -program "defgh"
LogoSelecterに渡すプログラム名  


    -d2v "D:\rec\video.ts.d2v"
d2vパスの個別指定


    -lwi "D:\rec\video.ts.lwi"
lwiパスの個別指定


    -srt "D:\rec\video.srt"
srtパスの個別指定
    
    

------------------------------------------------------------------
### 設定１

    Enable  1  
有効 、無効


    InputPlugin  lwi  
d2v :  d2vで処理  
lwi :  lwiで処理  


    Detector  LG  
JLS :  Join_Logo_Scpで処理  
LG  :  LogoGuilloで処理  


    Detector_MultipleRun  1  
Windows内での chapter_exe, LogoGuillo, logoframe同時実行数


    Regard_NsecMain_AsCM  20.0  
２０．０秒以下の本編部を除去  


    Regard_NsecCM_AsMain  14.0  
１４．０秒以下のＣＭ部を除去  


    Output_Tvtp  2  
Tvtplay用チャプターファイルを出力  
短い本編、ＣＭは除去されています。  
0 : 出力しない  
1 : -lastのみ出力する  
2 : 毎回出力する  


    Output_Ogm  0  
Ogm形式のチャプターファイルを出力  
短い本編、ＣＭは除去されています。  
0 : 出力しない  
1 : -lastのみ出力する  
2 : N/A


    Output_Frame  1  
フレームファイルを出力  
短い本編、ＣＭは除去されています。  
0 : 出力しない  
1 : -lastのみ出力する  
2 : 毎回出力する  


    Output_RawFrame  0  
フレームファイルを出力  
短い本編、ＣＭは除去していません。  
0 : 出力しない  
1 : -lastのみ出力する  
2 : 毎回出力する  


    Output_Scp  0  
Chapter_exe、LogoFrameの結果を出力
0 : 出力しない  
1 : -lastのみ出力する  
2 : N/A


    ChapDir_Tvtp  C:\Tvtp_Directory\  
Tvtplayチャプターの出力フォルダ  
フォルダが存在しない場合はＴＳと同じ場所に出力します。  


    ChapDir_Misc  C:\Frame_and_misc_Directory\  
Ogm chapter、フレームファイルの出力フォルダ  
フォルダが存在しない場合はＴＳと同じ場所に出力します。  


    CleanWorkItem  2  
0: 削除しない  
1: 古い作業ファイル削除  
2: 古い作業ファイル削除　＆　使用済みのファイル削除  



------------------------------------------------------------------
### LSystemフォルダ
　フォルダ以下に必要なバイナリファイルを置いてください。  
　サブフォルダ内も検索します。

##### 必要 
    avs2pipemod.exe  
    LogoSelector.exe  
	
##### avisynth
    DGDecode.dll  
	MPEG2DecPlus.dll  
    LSMASHSource.dll  

##### ファイルがあれば使用
    SystemIdleMonitor.exe  

##### LogoGuillo
    logoGuillo.exe  

##### join_logo_scp
    avsinp.aui  
    chapter_exe.exe  
    logoframe.exe  
    join_logo_scp.exe  
    JL__標準.txt  
    JL_標準_Rec.txt  
        
    
------------------------------------------------------------------
### join_logo_scp

- 設定ファイルで  
 ``` <InputPlugin>    lwi    </InputPlugin> ```  
 ``` <Detector>       JLS    </Detector> ```  
 に設定する。
 
- LSystemフォルダに  
   * avs2pipemod.exe
   * avsinp.aui  
   * join_logo_scp.exe  
   * logoframe.exe  
   を入れる。   
 
- chpater_exe.exeは同梱のものでなくてもかまいません。安定して動くものを使用してください。  
  テスト環境で終了時にエラーが発生したので、同梱のchpater_exeは終了処理を変更しただけです。  

- JL_標準_Rec.txtは JL_標準.txtから必要なさそうな項目をコメントアウトしただけで、  
  それ以外は同じです。
 

 
------------------------------------------------------------------
### メモ
 
* 作業ファイルのパスが２５０文字を超えると正常に動きません。深いフォルダにおかないでください。


* LSystemフォルダにSystemIdleMonitor.exeがあれば、ＣＰＵ使用率が６０％以下になるまで待機
してからLogoGuilloを実行します。


  
------------------------------------------------------------------
### 謝辞
このソフトウェアを動作させるには、

* avs2pipemod
* avsinp.aui
* chapter_exe
* DGDecode
* join_logo_scp
* LogoGuillo
* L-SMASH Works
* MPEG2DecPlus

が必要です。各作者にお礼申し上げます。


  
------------------------------------------------------------------
### 使用ライブラリ

    Mono.Options  
    Authors:  
        Jonathan Pryor <jpryor@novell.com>  
        Federico Di Gregorio <fog@initd.org>  
        Rolf Bjarne Kvinge <rolf@xamarin.com>  
    Copyright (C) 2008 Novell (http://www.novell.com)  
    Copyright (C) 2009 Federico Di Gregorio.  
    Copyright (C) 2012 Xamarin Inc (http://www.xamarin.com)  
  

------------------------------------------------------------------
### ライセンス

    MIT Licence
    Copyright (C) 2014  CHATRA


