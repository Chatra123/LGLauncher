
## LGLauncher

作成中の d2v, lwi, srtファイルを元にLogoGuilloを実行します。


------------------------------------------------------------------
### 使い方　　コマンドライン

LGLauncher.exe         -ts "C:\Video.ts"  -channel "abc"  -program "defgh"


最後のみ  
LGLauncher.exe  -last  -ts "C:\Video.ts"  -channel "abc"  -program "defgh"



------------------------------------------------------------------
### 必要なソフト

* AviSynth  [32bit]  
    
* LogoGuillo.exe  [32bit or 64bit]  
    ロゴデータ、パラメーターファイル
    
* avs2pipemod.exe  [32bit]


------------------------------------------------------------------
### 使用前の準備

1. LSystemフォルダに  
   logoGuillo.exe  
   avs2pipemod.exe  
   LSMASHSource.dll  
   を入れる。

2. LogoSelector.exe を実行し設定ファイルを生成、  
   LogoSelector.txtの"[LogoDir]"にlgd,paramファイルのあるフォルダを指定する。


------------------------------------------------------------------
### 引数１

    -ts "C:\video.ts"
tsファイルのパス  
tsファイルのパスを元に各ファイルパスを作成します。  
"C:\video.ts.pp.d2v"  
"C:\video.ts.pp.lwi"  
"C:\video.ts.pp.lwifooter"  
"C:\video.ts.srt"  
tsと同じフォルダに d2v, lwi, srtをおいてください。  


    -last
最後の処理であることを明示する。  
チャプター出力や、JL_標準.txtを用いてjoin_logo_scpの再実行などが行われます。


    -all  
ファイル全体を処理  
-lastの指定は必要ありません。  

 
    -ch "abc"
    -channel "abc"
LogoSelecterに渡すチャンネル名  


    -program "defgh"
LogoSelecterに渡すプログラム名  


    
------------------------------------------------------------------
#### 引数２

    -d2v "D:\rec\video.ts.d2v"
d2vファイルパスの個別指定


    -lwi "D:\rec\video.ts.lwi"
lwiファイルパスの個別指定


    -srt "D:\rec\video.ts.srt"
srtファイルパスの個別指定

    -SequenceName  abcdef012345
作業フォルダ名の一部に使用  
基本的には指定無しでも処理できます。
    
    

------------------------------------------------------------------
### 設定１

    Enable  1  
有効 、無効


    InputPlugin  lwi  
d2v :  d2vで処理  
lwi :  lwiで処理  


    LogoDetector  LG  
JLS :  Join_Logo_Scpで処理  
LG  :  LogoGuilloで処理  


    Detector_MultipleRun  1  
Winsows内での chapter_exe, LogoGuillo, logoframe同時実行数


------------------------------------------------------------------
####  設定２　チャプター出力

    Regard_NsecCM_AsMain  14.0  
１４．０秒以下のＣＭ部を除去  


    Regard_NsecMain_AsCM  29.0  
２９．０秒以下の本編部を除去  
    

    Output_Tvtp  2  
Tvtplay用チャプターファイルを出力  
短い本編、ＣＭは除去されています。  
0 : 出力しない  
1 : -lastのみ出力する  
2 : 毎回出力する  


    Output_Ogm  1  
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
編集前のフレームファイルを出力  
0 : 出力しない  
1 : -lastのみ出力する  
2 : 毎回出力する  


    DirPath_Tvtp  "C:\Tvtp_Dir"  
Tvtplay用チャプターファイルを出力するフォルダを指定  
フォルダが存在しない場合はＴＳと同じ場所に出力します。  


    DirPath_Misc  "C:\Ogm_and_Frame_Dir"  
Ogm chapter、フレームファイルを出力するフォルダを指定  
フォルダが存在しない場合はＴＳと同じ場所に出力します。  


    CleanWorkItem  2  
0: 削除しない  
1: 古い作業ファイル削除  
2: 古い作業ファイル削除　＆　使用済みのファイル削除  



------------------------------------------------------------------
### LSystemフォルダ
　フォルダ以下に各バイナリファイルを置いてください。  
　サブフォルダ内も自動的に検索します。


##### 必要  
    DGDecode.dll  
    LSMASHSource.dll  

    avs2pipemod.exe  
    LogoSelector.exe  


##### ファイルがあれば使用
    SystemIdleMonitor.exe  


##### LogoGuilloを使用する場合  
    logoGuillo.exe  


##### join_logo_scpを使用する場合  
    avsinp.aui  
    chapter_exe.exe  
    logoframe.exe  
    join_logo_scp.exe  
    JL__標準.txt  
    JL_標準_Rec.txt  



------------------------------------------------------------------
### メモ

* LSystemフォルダにSystemIdleMonitor.exeがあれば、ＣＰＵ使用率が６０％以下になるまで待機
してからLogoGuilloを実行します。


* 文字コード
 * Tvtp chapter                 : UTF-8 bom
 * Ogm chapter, Frame text      : Shift-JIS

 
* 作業ファイルのパスが２５０文字を超えると正常に動きません。深いフォルダにおかないでください。


* LogoGuillo, avs2pipemod側でエラーが発生しても自動で再実行するので、多少のエラーなら無視できます。


* LogoGuillo実行間隔による差
    * フレーム認識  
        - １分～５分はＣＭが本編として組み込まれる量が多い。１０分ならほぼ差が出ない。
        
    * 処理時間の増加率  

|  実行間隔  |  処理時間  |
|:----------:|:----------:|
|    60 min  |    1.0 倍  |
|    20      |    1.1     |
|    10      |    1.1     |
|     5      |    1.4     |
|     3      |    1.4     |
|     1      |    2.0     |



------------------------------------------------------------------
### lwi

- d2vよりも２倍程速く処理できます。

- フッターファイルがあれば使用します。

- tsとlwiが同じフォルダにある場合はlwiのファイル名をTsName.ts.lwiにしないでください。
AviSynthのファイル読込時にシステム側で使用します。

        
    
------------------------------------------------------------------
### join_logo_scp

- 設定ファイルで  
 ``` <InputPlugin>    lwi    </InputPlugin> ```  
 ``` <LogoDetector>    JLS    </LogoDetector> ```  
 を設定する。

- chpater_exe.exeは同梱のものでなくてもかまいません。安定して動くものを使用してください。  
  テスト環境では終了時にエラーが発生したので、同梱のchpater_exeは終了処理を変更しただけです。  

- JL_標準_Rec.txtは JL_標準.txtから必要なさそうな項目をコメントアウトしただけで、  
  それ以外は同じです。


  
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
    GPL v3
    Copyright (C) 2014  CHATRA
    http://www.gnu.org/licenses/
    
    
  


 