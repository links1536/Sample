# Sample

Universal Render Pipeline と Scriptable Build Pipeline の技術サンプルです

## 初回起動

初回起動時は NuGet 未復元によりコンパイルエラーになります
以下の手順で

1. Unity Hub から プロジェクトフォルダを開きます
2. コンパイルエラーが出た場合は `Safe Mode` ではなく `Ignore` を選んでください
3. `NuGet for Unity` により `Assets/packages.config` の依存関係が復元されます
4. Git URL の UPM package 解決と NuGet 復元完了後、再コンパイルされます

## 再生方法

エディタの再生ボタンを押すことで `Initialize` シーンが読み込まれ、その後サンプルが設定されたシーンをロードします


## リソース管理

AssetBundleを使用しています
/Assets
	└AssetBundle		CDNによる配信想定(今回のサンプルでは公開用のサーバーを用意していない関係で未使用)
	└InAppResource		アプリ埋め込みリソース

`メニューバー/Build/Player` でビルドする際に `InAppResource` は AssetBundle としてビルドされ
`StreamingAssets` へ埋め込みのリソースとして内包されます

補足:
現在の `Initialize` シーンでは `AriaAssetProvider.LocalLoadMode` が `true` になっているため
エディタでの再生時は `Assets/InAppResource/` を直接参照します
