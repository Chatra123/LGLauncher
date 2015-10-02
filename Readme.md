
## LGLauncher

作成中の d2v, srtファイルを元にLogoGuilloを実行します。



------------------------------------------------------------------
### 必要なソフト
* AviSynth  [32bit]  
    
* LogoGuillo.exe  [32bit or 64bit] 
    ロゴデータ、パラメーターファイル
    
* avs2pipemod.exe  [32bit]



------------------------------------------------------------------
### 使用前の準備
1. logoGuillo.exe、  
   avs2pipemod.exe、  
   DGDecode.dll  
   をLSystemフォルダに入れる。

2. LogoSelector.exe を実行し設定ファイルを生成。  
   LogoSelector.txtの"[LogoDir]"をlgdファイルのあるフォルダに設定する。



------------------------------------------------------------------
### 使い方
LGLauncher.exe  -No 1  -ts "C:\Video.ts"  -channel "abc"  -program "defgh"



------------------------------------------------------------------
### 引数
大文字、小文字の違いは無視されます。

    -no 1  
-noを増やすことで前回からの増加分を処理する。  
-1 なら全体を処理します。必ず作成完了したd2vを指定してください。  


    -ts "C:\video.ts"  
tsファイルのパス  
tsファイルのパスを元に各ファイルパスを作成します。  
"C:\video.ts.pp.d2v"  
"C:\video.ts.pp.lwi"  
"C:\video.ts.pp.lwifooter"  
"C:\video.srt"  
tsと同じフォルダに d2v, lwi, srtをおいてください。  


    -d2v "D:\rec\video.ts.d2v"  
d2vファイルパスの個別指定


    -lwi "D:\rec\video.ts.lwi"  
lwiファイルパスの個別指定


    -srt "D:\rec\video.srt"  
srtファイルパスの個別指定


    -ch "abc"  
    -channel "abc"  
LogoSelecterに渡すチャンネル名  

    -program "defgh"  
LogoSelecterに渡すプログラム名  

    -last  
フレームファイルの出力名が TsName.ts.frame.txtになる。  
通常の出力名は TsName.ts.partframe.txtです。  




------------------------------------------------------------------
### 設定

    bEnable  1  
1: 有効  
0: 無効


    bPrefer_d2v  1  
1: d2vで処理  
0: lwiで処理  


    iPriority  -1  
プロセス優先度を指定。LogoGuilloに継承されます。  
 2: Normal  
 1: BelorNormal  
 0: Low  
-1: Auto by Windows


    iLogoGuillo_MultipleRun  1  
Windows内でのLogoGuilloの同時実行数、確実な制御はできません。


    bUseTSDir_asChapDir  1  
Tvtplay用チャプターファイルをtsファイルのフォルダに作成します。


    sChapDir_Path  "C:\ChapDir"  
Tvtplay用チャプターファイルを作成するフォルダを指定します。  
bUseTSDir_asChapDir = 0  にしてください。  


    sFrameDir_Path  "C:\FrameDir"  
フレームファイルを出力するフォルダを指定します。  
出力名は TsName.ts.frame.txtです。


    iDeleteWorkItem  1  
2: 古い作業ファイル削除　＆　使い終わったファイル削除  
1: 古い作業ファイル削除  
0: 削除しない  




------------------------------------------------------------------
### LogoSelector

* LSystemフォルダにあるLogoSelector.exe又は、LogoSelector.vbs、LogoSelector.jsを実行します。  
    複数ある場合の優先順位は、  
    （高）　.exe  .vbs  .js　（低）


* LogoSelector.exe  
    引数に "チャンネル名"  "番組名"  "tsパス"を渡します。  
    返り値の１行目にロゴデータ、２行目にパラメーターのフルパスを返してください。  


* LogoSelector.vbs　＆　LogoSelector.js  
    cscriptの引数に"スクリプトパス"  "チャンネル名"  "番組名"  "tsパス"を渡します。  
    返り値の１行目にロゴデータ、２行目にパラメーターのフルパスを返してください。


    
    
------------------------------------------------------------------
### メモ
* Tvtplay用のチャプターはフレームファイルから２９秒以下のＣＭ、
２９秒以下の本編を除去してから作成しています。
処理を変更する場合はフレームファイルを取得してチャプターを作成してください。


* フレームファイルのファイル名は*.ts.partframe.txt、最終ファイルは*.ts.frame.txt。
  文字コード　shift-jis

  
* 作業ファイルのパスが２５５文字を超えると正常に動きません。深いフォルダにおかないでください。


* LSystemフォルダにSystemIdleMonitor.exeがあれば、ＣＰＵ使用率が６０％以下になるまで待機し、  
 x264、ffmpegが実行されていないことを確認してからLogoGuilloを実行します。


* LogoGuillo実行間隔による差
    * フレーム認識  
        - 実行間隔が短いと真っ白なシーンや映像後半でずれやすくなる。  
        - 5min to 1minはＣＭが本編として組み込まれる量が多くなっていく。
        
    * 処理時間の増加  

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

- lwiで処理するには
    - 設定ファイルのbPrefer_d2vを0にする。
    - LSystemフィルダにLSMASHSource.dllを入れる。


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
    必要に応じてiDeleteWorkItemの設定を変更してください。




------------------------------------------------------------------
### 謝辞
このソフトウェアを動作させるには、

* avs2pipemod
* DGDecode
* LogoGuillo
* LSmash-Works

が必要です。各作者にお礼申し上げます。




------------------------------------------------------------------
### ライセンス
    GPL v3
    Copyright (C) 2014  CHATRA

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program. If not, see [http://www.gnu.org/licenses/].




