
## LGLauncher

作成中の d2v, lwi, srtファイルを元にLogoGuilloを実行します。



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
### 使い方　　コマンドライン

LGLauncher.exe  -No 1  -ts "C:\Video.ts"  -channel "abc"  -program "defgh"


２回目以降、 -No を増やします。  
LGLauncher.exe  -No 2  -ts "C:\Video.ts"  -channel "abc"  -program "defgh"



------------------------------------------------------------------
### 引数


    -No 1
-noを増やすことで前回からの増加分を処理する。  
-1 なら全体を処理します。

    -AutoNo
-noを作業フォルダ内のファイルから決定します。

    -last
最後の処理であることを明示します。  


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
基本的には無くても問題ありません。
    
    

------------------------------------------------------------------
### 設定

    bEnable  1  
有効 、無効


    iPriority  -1  
プロセス優先度を指定。LogoGuilloに継承されます。  
優先度は下げるのみです。指定優先度よりすでに低い場合は処理しません。  
 2:  Normal  
 1:  BelorNormal  
 0:  Low  
-1:  Auto by Windows  


    sAvs_iPlugin  lwi  
d2v:  d2vで処理  
lwi:  lwiで処理  


    sLogoDetector  LogoGuillo  
JLS          :  Join_Logo_Scpで処理  
Join_Logo_Scp:  Join_Logo_Scpで処理  
LG           :  LogoGuilloで処理  
LogoGuillo   :  LogoGuilloで処理  


    iDetector_MultipleRun  1  
Windows内での LogoGuilloの同時実行数


#####  チャプター出力設定

    dRegard_NsecCM_AsMain  14.0
１４．０秒以下のＣＭ部を除去 
    
    
    dRegard_NsecMain_AsCM  29.0
２９．０秒以下の本編部を除去 
    

    bOut_tvtp  1  
Tvtplay用チャプターファイルを出力する。  


    bOut_ogm  1  
Ogm形式のチャプターファイルを出力する。  


    bOut_frame  1  
短い本編、ＣＭを除去したフレームファイルを出力する。  


    bOut_rawframe  0  
編集前のフレームファイルを出力する。  


    bOut_tvtp_toTsDir  1  
Tvtplay用チャプターファイルをＴＳファイルのフォルダに作成します。  


    bOut_misc_toTsDir  1  
Ogm chapter、フレームファイルをＴＳファイルのフォルダに作成します。  


    sChapDir_Path  "C:\tvtp_Dir"  
Tvtplay用チャプターファイルを出力するフォルダを指定します。  
bOut_tvtp_toTsDir = 0  にしてください。  


    sDirPath_misc  "C:\ogm_and_frame_Dir"  
Ogm chapter、フレームファイルを出力するフォルダを指定します。  
bOut_misc_toTsDir = 0  にしてください。  


    iDeleteWorkItem  2  
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
* Tvtp、Ogg chapterは２９秒以下の本編、１４秒以下のＣＭを除去してから作成しています。

* 文字コード
 * Tvtp chapter                  UTF-8 bom
 * Ogm chapter, Frame text       Shift-JIS

  
* 作業ファイルのパスが２５５文字を超えると正常に動きません。深いフォルダにおかないでください。


* LSystemフォルダにSystemIdleMonitor.exeがあれば、ＣＰＵ使用率が６０％以下になるまで待機
してからLogoGuilloを実行します。


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
### LogoSelector

* LSystemフォルダにあるLogoSelector.exe又は、LogoSelector.vbs、LogoSelector.jsを実行します。  


* LogoSelector.exe  
    引数に "チャンネル名"  "番組名"  "tsパス"を渡します。  
    返り値の１行目にロゴデータ、２行目にパラメーターのフルパスを返してください。  


* LogoSelector.vbs　＆　LogoSelector.js  
    cscriptの引数に"スクリプトパス"  "チャンネル名"  "番組名"  "tsパス"を渡します。  
    返り値の１行目にロゴデータ、２行目にパラメーターのフルパスを返してください。

    

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
 ``` sAvs_iPluginに lwi ```  
 ``` sLogoDetectorに  JLS ```  
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
  それ以外はJL_標準.txtと同じです。


  
------------------------------------------------------------------
### 謝辞
このソフトウェアを動作させるには、

* avs2pipemod
* avsinp.aui
* chapter_exe
* DGDecode
* join_logo_scp
* LogoGuillo
* LSmash-Works

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
    
    
  


 