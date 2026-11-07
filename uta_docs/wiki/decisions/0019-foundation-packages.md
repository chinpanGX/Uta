---
title: ADR 0019 共通基盤を NuGet パッケージと git URL で取り込む
status: accepted
updated: 2026-11-07
---

# ADR 0019: 共通基盤を NuGet パッケージと git URL で取り込む

[ADR 0005](0005-foundation-submodule.md)（Git サブモジュール）を置き換える。

## 状況

共通基盤の 4 リポジトリは `external/` のサブモジュールで、Unity は `file:`、サーバーは `ProjectReference` で参照していた。サブモジュールが Uta の固定より古い checkout では、パッケージが無くなって Unity が Safe Mode に入る。2026-11-07 に `C:\Work\Uta` で起きた（`MasterMemoryAddons` が `package.json` の無い古いコミットのままだった）。worktree ごとの初期化も要っていた。

## 決定

- `external/` とサブモジュールをやめる。`VolTech-MagicOnion`・`MasterMemoryAddons`・`Supplement`・`UnityScreenNavigator` は別リポジトリのまま、Uta からはパッケージとして使う
- **Unity**: `https://github.com/chinpanGX/<リポジトリ>.git?path=<パッケージのパス>#<コミット>` で参照する（`com.uta.shared.*` は同じリポジトリなので `file:` のまま）
- **サーバー・`shared/`**: `VolTech.Server`・`VolTech.Server.Abstractions`・`MasterMemory.Addons.Core` を、Uta の `packages/` に置いた `.nupkg` から参照する。`nuget.config` がこのフォルダをソースに足し、版は `Directory.Packages.props` で固定する
- **マスタの生成**: `MasterMemory.Addons.Tools` を dotnet tool にして、`.config/dotnet-tools.json` で固定する
- 版は `0.1.0-g<短いコミット>`。コミットは共通基盤の既定のブランチに入ったものにする
- `.nupkg` は `tools/pack-foundation.sh <リポジトリ> <コミット>` で作る。指定のコミットを clone して pack し、`packages/`・`Directory.Packages.props`・`.config/dotnet-tools.json`・`manifest.json` を揃える

## 理由

- `git clone` だけでサーバーも Unity も揃い、古いサブモジュールで壊れることがない
- nuget.org に公開すると、API キーの用意と、版を公開する作業が要る。`packages/` ならアカウントが要らない
- `PackageId` が同じなので、後で nuget.org に移すときは `nuget.config` と `packages/` を変えるだけで済む

## 検討した代替案

- nuget.org に公開する: 開発中の共通基盤を更新のたびに公開版にすることになる。将来の選択肢として残す
- GitHub Packages に公開する: public でも restore に PAT が要り、ローカルとクラウドの両方に設定が要る
- サブモジュールを残す: 同じ失敗が人の手順に残る

## 影響・リスク

- 共通基盤を更新するたびに `.nupkg` が Uta の履歴に増える。`MasterMemory.Addons.Tools` は 1 版あたり約 7.6MB。履歴が大きくなったら nuget.org に移す
- `VolTech-MagicOnion` の `Abstractions` はサーバーと Unity で同じものを使うので、`.nupkg` と `manifest.json` のコミットを揃える。ずれると RPC の型が食い違う
- 共通基盤を試すには、push して固定を上げる。ローカルの変更を Unity で試すときは、手元で `manifest.json` を一時的に `file:` に変える（コミットしない）
- サーバーのデバッグで共通基盤のコードに入れない（`Uta.slnx` から外した）
- 共通基盤のソースは checkout に無くなる。読むときは GitHub か別の clone を見る
- `Library/PackageCache` のパッケージは読み取り専用

## 見直す条件

- `.nupkg` で履歴が重くなったとき
- 共通基盤を別のアプリでも使い始めたとき（nuget.org への公開）
- 共通基盤の並行開発で、pack の手間が大きな負担になったとき
