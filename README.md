# accacc_unity_asset
このアセットはAndroid/iPhone用加速度コントローラ「accacc」をUnityで使うためのものです。
このブランチ「udp_auto_server_search」は2017/07/28以降にリリースされたaccaccの自動サーバ検索に対応したものです。
このサンプルを使えば、ユーザにIPアドレス・ポート番号を入力させず、「Connect」ボタンの押下のみで接続することができます。

accacc:

http://wayabi.genin.jp/

## このアセットの使い方動画
https://www.youtube.com/watch?v=mZLHIlJFAtU

## サーバ
- AccAccServer.csをAdd Component
- AccAccUdpServer.csをAdd Component

## コントロール対象
### 単純な往復運動
sample_accacc.csをAdd Component

### アニメーション再生速度のコントロール
sample_accacc_speed.csをAdd Component
