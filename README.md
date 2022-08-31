# chakinet-lite
## ChaKi.NET liteとは
- ChaKi.NET liteは、[UD Corpus](https://universaldependencies.org) の検索を主な用途としてChaKi.NETの機能を軽量化したものです。

## ダウンロード
- ChaKi.NET liteの本体は、本サイトの[リリースページ](https://github.com/chakidev/chakinet-lite/releases) からmsiパッケージをダウンロード&インストールすることで使用できるようになります。
  - Windows10以降(64bit版のみ)をサポートしています。
- すぐに利用可能な形のUD Treebanks v.2.9データベースファイルは以下からダウンロードできます。(サイズが大きいため3つのzipファイルに分割しています。)
  1. Afrikans - Greek  
     https://drive.google.com/file/d/1FWBKJs2Ua6YMrHG3s06ZffCWjqalzr7p  
     SHA256 Hash: b3eda098a4036d7e89c4f3f2c3681328e2e5c87875dd0d5e80b206a10cec75b1
  2. Guajajara - Norwegian  
     https://drive.google.com/file/d/12Gns1mQbfvL-Ryf9zXfCQbM5o77yJThy  
     SHA256 Hash: 56c9432a33c4da12c815ca6bb9d56f188d1a41a5a6fbfbe1d1f690ff6811cf68
  3. Old_Church_Slavonic - Yupik  
     https://drive.google.com/file/d/17Tn6MC0HJTm8HQu-RN_Agv5n1TFuN6kv  
     SHA256 Hash: 367c62296c2922532727c1d9b7f9b9b4d99a82052240cef4728eafcd3cf5dd8c

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
