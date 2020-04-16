# notes-viewer

GitHub の認証不要な API を利用して GitHub のリポジトリー内の Markdown をブログ形式で見るためのウェブアプリケーションです。
実際の動作例は https://notes.recyclebin.jp/ を参照してください。

## 特徴

- SPA
- WebAssembly

## 利用上の制限

認証なしの API コールは同一 IP から 1 時間あたり 60 回までに制限されています（トークン利用時は 5,000 回）。
API の利用制限はフッターに表示されているので参考にしてください。

## 開発

### 環境

- .NET Core 3.1

### 設定ファイル

appsettings.Development.json または appsettings.Production.json ファイルに下記項目を追加します。
Development は Debug ビルド、 Production は Release ビルドに対応します。

```json
{
  "Note": {
    "Title": "ブログタイトル",
    "Copyright": "コピーライト表記",
    "GitHub": "GitHub のアカウント URL",
    "Twitter": "Twitter のアカウント URL",
    "Owner": "GitHub アカウント",
    "Repository": "GitHub リポジトリー",
    "AccessToken": "OAuth トークン"
  }
}
```

`Owner` と `Repository` のみ必須です。

OAuth トークンは https://github.com/settings/tokens から発行できます。
公開リポジトリーを対象にするのであればスコープの設定は不要です。
