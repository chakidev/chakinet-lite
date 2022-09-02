# chakinet-lite
## ChaKi.NET liteとは
- ChaKi.NET liteは、[UD Corpus](https://universaldependencies.org) の検索を主な用途としてChaKi.NETの機能を軽量化したものです。

## ダウンロード
- ChaKi.NET liteの本体は、本サイトの[リリースページ](https://github.com/chakidev/chakinet-lite/releases) からmsiパッケージをダウンロード&インストールすることで使用できるようになります。
  - Windows10以降(64bit版のみ)をサポートしています。
- すぐに利用可能な形のUD Treebanks v.2.9データベースファイルは以下からダウンロードできます。(サイズが大きいため3つのzipファイルに分割しています。)
  1. Afrikans - Greek  
     https://drive.google.com/file/d/1DhOzpfbEkaAvmvN_FnPId9BrMlpNZ4jW  
     SHA256 Hash: 8420a5173ef8f482d392ac639f33f97aef454a35c3c143a2f0e7e4a3e53986b6
  2. Guajajara - Norwegian  
     https://drive.google.com/file/d/1eeZkZiit2ZRDwwJosHPgZfyHCZqY9TO3  
     SHA256 Hash: e06c9fbfbba378329b62fa1d83bb53e5180c29d548b1739173727ea022ad7085
  3. Old_Church_Slavonic - Yupik  
     https://drive.google.com/file/d/11loTiAUph7xMtUA8xxThX_0Yo7PvUkaV  
     SHA256 Hash: 1ada6921963e274e120ab2fef42e1f541750b1ecece33aef686f692842ef9985

## 最新ビルドを試用する方法
- ここでは、GitHubのビルドサーバーから最新ビルドをダウンロードして試す方法を記述します。インストーラは使用しません。
- Actions(https://github.com/chakidev/chakinet-lite/actions) ページに行き、最新ビルドを選択します。（上にある方が新しい）

![img001](img001.png)

- Artifactsセクションのbuild-resultをクリックするとzipファイルのダウンロードが開始されます。

![img002](img002.png)

- zipの内容を適当なフォルダに展開し、ChaKi.NETlite.exe を実行します。
  - 展開先フォルダの内容は展開前にクリアすることを推奨します。
  - インストーラでなくネットからダウンロードしたファイルを直接実行することになるため、「WindowsによってPCが保護されました」というダイアログが出ますが、「詳細」をクリックすれば下記の画面になりますので、実行できるようになります。

  ![img003](img003.png)
