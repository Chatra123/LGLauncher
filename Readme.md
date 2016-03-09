﻿
## LGLauncher

作成中の d2v, lwi, srtファイルを元にLogoGuilloを実行します。


------------------------------------------------------------------
### 使い方　　コマンドライン

LGLauncher.exe  -AutoNo         -ts "C:\Video.ts"  -channel "abc"  -program "defgh"


最後のみ  
LGLauncher.exe  -AutoNo  -last  -ts "C:\Video.ts"  -channel "abc"  -program "defgh"



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
   を入れる。

2. LogoSelector.exe を実行し設定ファイルを生成。  
   LogoSelector.txtの"[LogoDir]"にlgd,paramファイルのあるフォルダを設定する。


------------------------------------------------------------------
### 引数１

    -No 1
-noを増やすことで前回からの増加分を処理する。  
-1 なら全体を処理します。
通常は自動的に決定されるので-Noの指定は必要ありません。  


    -last
最後の処理であることを明示します。  
-lastがあればOgm chapterを出力し、  
join_logo_scpならばJL_標準.txtを使い再実行します。  


    -All  
ファイル全体を処理します。  
-No -1 と同様。  


    -ts "C:\video.ts"
tsファイルのパス  
tsファイルのパスを元に各ファイルパスを作成します。  
"C:\video.ts.pp.d2v"  
"C:\video.ts.pp.lwi"  
"C:\video.ts.pp.lwifooter"  
"C:\video.ts.srt"  
tsと同じフォルダに d2v, lwi, srtをおいてください。  


    -ch "abc"
    -channel "abc"
LogoSelecterに渡すチャンネル名  

    -program "defgh"
LogoSelecterに渡すプログラム名  


    
------------------------------------------------------------------
### 引数２

    -d2v "D:\rec\video.ts.d2v"
d2vファイルパスの個別指定


    -lwi "D:\rec\video.ts.lwi"
lwiファイルパスの個別指定


    -srt "D:\rec\video.ts.srt"
srtファイルパスの個別指定

    -SequenceName  abcdef
作業フォルダ名の一部に使用します。  
基本的には無くてかまいません。
    
    

------------------------------------------------------------------
### 設定１

    Enable  1  
有効 、無効


    Avs_iPlugin  lwi  
d2v:  d2vで処理  
lwi:  lwiで処理  


    LogoDetector  LogoGuillo  
JLS          :  Join_Logo_Scpで処理  
Join_Logo_Scp:  Join_Logo_Scpで処理  
LG           :  LogoGuilloで処理  
LogoGuillo   :  LogoGuilloで処理  



------------------------------------------------------------------
#####  設定２　チャプター出力

    Regard_NsecCM_AsMain  14.0  
１４．０秒以下のＣＭ部を除去  


    Regard_NsecMain_AsCM  29.0  
２９．０秒以下の本編部を除去  
    

    Out_tvtp  1  
Tvtplay用チャプターファイルを出力する。  


    Out_ogm  1  
Ogm形式のチャプターファイルを出力する。  


    Out_frame  1  
短い本編、ＣＭを除去したフレームファイルを出力する。  


    Out_rawframe  0  
編集前のフレームファイルを出力する。  


    Out_tvtp_toTsDir  1  
Tvtplay用チャプターファイルをＴＳファイルのフォルダに作成します。  


    Out_misc_toTsDir  1  
Ogm chapter、フレームファイルをＴＳファイルのフォルダに作成します。  


    ChapDir_Path  "C:\tvtp_Dir"  
Tvtplay用チャプターファイルを出力するフォルダを指定します。  
Out_tvtp_toTsDir = 0  にしてください。  


    DirPath_misc  "C:\ogm_and_frame_Dir"  
Ogm chapter、フレームファイルを出力するフォルダを指定します。  
Out_misc_toTsDir = 0  にしてください。  


    DeleteWorkItem  3  
3: 古い作業ファイル削除　＆　使い終わったファイル削除  
2: 古い作業ファイル削除　＆　サイズの大きいファイル削除  
1: 古い作業ファイル削除  
0: 削除しない  



------------------------------------------------------------------
### LSystemフォルダ
　フォルダ以下に各バイナリファイルを置いてください。  
　子フォルダ内も自動的に検索します。


##### 必要  AVS Input Plugin  
    DGDecode.dll  
    LSMASHSource.dll  


##### 必要  
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
    JL_標準_Recording.txt  



------------------------------------------------------------------
### メモ

* LSystemフォルダにSystemIdleMonitor.exeがあれば、ＣＰＵ使用率が６０％以下になるまで待機
してからLogoGuilloを実行します。


* Tvtp、ogm chapter、フレームファイルは短い本編、ＣＭを除去してから作成しています。

* 文字コード
 * Tvtp chapter                  UTF-8 bom
 * Ogm chapter, Frame text       Shift-JIS

 
* 作業ファイルのパスが２５５文字を超えると正常に動きません。深いフォルダにおかないでください。


* LogoGuillo, avs2pipemod側でエラーが発生しても自動で再実行するので、多少のエラーなら無視できます。


* LogoGuillo実行間隔による差
    * フレーム認識  
        - 5min to 1minはＣＭが本編として組み込まれる量が多くなっていく。
        - 10minならほぼ差が出ない。
        
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


- 作業ファイルのサイズ
    - １時間番組を
        - １０分ごとに処理したときは　　１５０ＭＢ  
        - 　１分ごとに処理したときは　　　　１ＧＢ  
    - ２時間番組を
        - １０分ごとに処理したときは　　５５０ＭＢ  
        - 　１分ごとに処理したときは　　　　５ＧＢ  
    必要に応じてiDeleteWorkItemの設定をしてください。
    
    
    
------------------------------------------------------------------
### join_logo_scp

- 設定ファイルで  
 ``` Avs_iPluginに lwi ```  
 ``` LogoDetectorに  JLS ```  
 を設定する。

- LSystemフォルダに以下のファイルを入れてください。子フォルダでもかまいません。
  - avsinp.aui
  - chapter_exe.exe
  - JL_標準.txt
  - JL_標準_Recording.txt
  - join_logo_scp.exe
  - logoframe.exe


- chpater_exe.exeは同梱のものでなくてもかまいません。安定して動くものを使用してください。  
  テスト環境では終了時にエラーが発生したので、同梱のchpater_exeは終了処理を変更しただけです。
  
- JL_標準_Recording.txtは JL_標準.txtから必要なさそうな項目をコメントアウトしただけで、  
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
* L-SMASH-Works

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
    
    
  


 