TSファイルからニコニココメントのログを取得したいけど、NicojShift.exeの使い方がさっぱりわからん人向けツール
chromeからクッキー情報を取得するからchrome必須

nicojikkyo-download.exe をNicojShift.exeとJKCommentGetter.rbがあるディレクトリに置いて、tsファイルをドラッグ＆ドロップすると受信出来るはず
テキストボックスの赤文字はNicojShift.exeの標準エラー出力。黒文字はNicojShift.exeの標準出力 と、このアプリのログ。灰色はこのアプリのちょっとしたログ

コマンドラインは以下の通り
nicojikkyo-download.exe "nicojShiftExe=C:\hoge\kage\NicojShift.exe" "C:\video\hoge.ts"
nicojShiftExe の値が C:\hoge\kage\NicojShift.exe となる。
key="val" ではなく、"key=val"で書く想定
フォルダを指定するときは"key=C:\windows\" ではなく"key=c:\windows" と書く。最後に\があるとエスケープされるので引数全体が壊れる
この書式でない時は対象の動画ファイル扱い。ファイルが存在しているかのチェックだけする。

パラメーターは以下のとおり
nicojShiftExe  NicojShift.exeのパスを指定する。指定なしの時はカレントディレクトリ＋NicojShift.exe
wDirectory     NicojShift.exeに指定する/Wのパラメーター。ここでフォルダを指定する。指定なしの時はカレントディレクトリ。
chromeCookie       Chromeのクッキーファイルを指定する。chromeのクッキーファイルは拡張子もないアレね。指定なしの時はC:\users\の中のデフォルト
jkcommentgetterrb  JKCommentGetter.rbのパス。指定なしの時はカレントディレクトリ＋JKCommentGetter.rb



chromeのセッションは、パラメーターで指定されたクッキーファイルから読み込む
chromeが起動中だとクッキーファイルがロックされてるからアプリケーションのディレクトリにコピーしてアクセスする
このアプリケーションの名前が hoge.exe になっている時、hoge_cookie というファイルが作られる

rubyファイルのセッションは、JKCommentGetter.rbをテキスト的に解析して読み込む
一行目から順番にチェックして、最初に"def getCookie"がある行 の、次にTrim()した先頭が'(シングルクォート)の時の行から引っ張ってくる
JKCommentGetter.rbの構造を改造してたらもちろんコケる

セッションをJKCommentGetter.rbに書き込み ボタンを押すと、取得したchromeのセッションを上書きする
