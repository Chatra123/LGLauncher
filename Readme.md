﻿
## LGLauncher

作成中の d2v, srtファイルを元にLogoGuilloを実行します。



------------------------------------------------------------------
### 必要なソフト
* AviSynth  (32bit)  
    d2v, DGDecode.dllが読み込める
* LogoGuillo.exe  
    ロゴデータ、パラメーターファイル
* avs2pipemod.exe (32bit)



------------------------------------------------------------------
### 使用前の準備
1. logoGuillo.exe、avs2pipemod.exeをLSystemフォルダに入れる。
2. LogoSelector.txtの"[LogoDir]"を変更する。



------------------------------------------------------------------
### 使い方
LGLauncher.exe  -No 1  -ts "C:\Video.ts"  -channel "abc"  -program "defgh"



------------------------------------------------------------------
### 引数
大文字、小文字の違いは無視されます。

    -no 1  
-noを増やすことで前回からの増加分を処理する。  
-1 なら全体を処理します。必ず作成の完了したd2vを指定してください。  


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
LogoSelecterに渡すチャンネル名  

    -program "defgh"  
LogoSelecterに渡すプログラム名  

    -last  
フレームファイルの出力名が TsName.ts.frame.txtになる。  



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
-1: Parent  

    iLogoGuillo_MultipleRun  1  
LogoGuilloの同時実行数、確実な制御はできません。０以下ならバッチ作成まで実行します。

    bUseTSDir_asChapDir  1  
Tvtplay用チャプターファイルをtsファイルのフォルダに作成します。


    sChapDir_Path  "C:\ChapDir"  
Tvtplay用チャプターファイルを作成するフォルダを指定します。  
bUseTSDir_asChapDir = 0  にしてください。  


    sFrameDir_Path  "C:\FrameDir"  
フレームファイルをコピーするフォルダを指定します。


    iDeleteWorkItem  1  
2: 古い作業ファイル削除　＆　使い終わったファイル削除  
1: 古い作業ファイル削除  
0: 削除しない  



------------------------------------------------------------------
### LogoSelector

* LSystemフォルダにある実行ファイル又はスクリプトを取得します。  
    複数ある場合の優先順位は、  
    （高）　.exe  .vbs  .js　（低）

* LogoSelector.exe  
    引数に "チャンネル名"  "番組名"  "tsパス"を渡します。  
    返り値の１行目にロゴデータのパス、２行目にパラメーターのパスを返してください。  


* LogoSelector.vbs　＆　LogoSelector.js  
    cscriptの引数に"スクリプトパス"  "チャンネル名"  "番組名"  "tsパス"を渡します。  
    返り値の１行目にロゴデータのパス、２行目にパラメーターのパスを返してください。



------------------------------------------------------------------
### メモ
* Tvtplay用のチャプターはフレームファイルから５５秒以下のＣＭ、
２９秒以下の本編を除去してから作成しています。
処理を変更する場合はフレームファイルを取得してチャプターを作成してください。

* フレームファイルの出力名は TsName.ts.partframe.txt、  
引数に -No -1または -lastがあると TsName.ts.frame.txtになります。  
文字コード　Shift-JIS

* 作業ファイルのパスが２５５文字を超えると正常に動きません。深いフォルダにおかないでください。

* LSystemフォルダにSystemIdleMonitor.exeがあれば、ＣＰＵ使用率が６０％以下になるまで待機し、x264、ffmpegが実行されていないことを確認してからLogoGuilloを実行します。

* LogoGuillo実行中のLGLancherを強制終了すると他のLogoGuillo起動開始が３０分程度遅くなります。すべてのLGLancherが終了するとリセットされます。


* 実行間隔による差
    * フレーム認識  
        実行間隔が短いと明るいシーンや映像後半でずれやすくなる。
    * 処理時間  

|  実行間隔  |  処理時間  |
|:----------:|:----------:|
|    60 min  |    1.0 倍  |
|    20      |    1.1     |
|    10      |    1.4     |
|     5      |    1.4     |
|     3      |    1.4     |
|     1      |    2.0     |



------------------------------------------------------------------
### lwi
- lwiで処理するには
    - 設定ファイルのbPrefer_d2vを0にする。
    - LSystemフィルダにLSMASHSource.dllを入れる。
    - lwiとdllの対応するインデックスバージョンは同じものを使用してください。


- d2vよりも２倍程速く処理できます。

- フッターファイルがあれば使用します。

- tsとlwiが同じフォルダにある場合にlwiのファイル名をTsName.ts.lwiにしないでください。
AviSynthのファイル読込時にシステム側で使用します。


- 作業ファイルのサイズ
    - １時間番組を
        - ５分ごとに処理したときは３００ＭＢ  
        - １分ごとに処理したときは　　１ＧＢ  
    - ２時間番組を
        - ５分ごとに処理したときは　　１ＧＢ  
        - １分ごとに処理したときは　　５ＧＢ  
    必要に応じてiDeleteWorkItemを設定してください。



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




