---
title: ADR 0019 Unity から共通基盤を git URL で参照する
status: accepted
updated: 2026-11-07
---

# ADR 0019: Unity から共通基盤を git URL で参照する

## 状況

Unity の `manifest.json` は、共通基盤の 5 パッケージを `file:../../../external/...` で参照していた。この形だと、サブモジュールが Uta の固定より古いままの checkout では、パッケージが無くなって Unity が Safe Mode に入る。2026-11-07 に `C:\Work\Uta` で起きた（`external/MasterMemoryAddons` が `package.json` の無い古いコミットのままだった）。

## 決定

- `com.chinpangx.voltech.magiconion`・`com.chinpangx.mastermemory.addons`・`com.chinpangx.supplement`・`com.chinpangx.supplement.messagepack`・`com.harumak.unityscreennavigator` は、`https://github.com/chinpanGX/<リポジトリ>.git?path=<パッケージのパス>#<コミット>` で参照する
- 固定はコミットの完全なハッシュで行う。タグは待たない（[repository-layout.md](../architecture/repository-layout.md) 3.1 と同じ）
- `manifest.json` に書くコミットは、共通基盤の既定のブランチに入ったものにする
- 同じリポジトリの `com.uta.shared.*` は `file:` のままにする

[ADR 0005](0005-foundation-submodule.md) のうち、Unity のパッケージ参照方法だけをこの ADR で決める。サブモジュールでの取り込みは変えない。サーバー（`VolTech.Server`・`MasterMemory.Addons.Core`）・`shared/Uta.Shared.Contracts`・`masterdata/gen.ps1` が `external/` を参照しているため。

## 理由

- Unity の解決がサブモジュールの状態に左右されなくなる。worktree ごとの初期化の漏れでも Safe Mode に入らない
- 4 つのリポジトリはすべて public で、他の UPM パッケージと同じ書き方で固定できる

## 検討した代替案

- `file:` のままにして、サブモジュールの更新を徹底する: 同じ失敗が人の手順に残る
- VolTech-MagicOnion と MasterMemoryAddons だけ `file:` に残す: 固定は 1 か所で済むが、壊れ方の原因が残る

## 影響・リスク

- **固定が 2 か所になる**: サブモジュールのポインタと `manifest.json` のコミットを揃える。特に `VolTech-MagicOnion` の `Abstractions` はサーバーと Unity で同じものを使うので、ずれると RPC の型が食い違う
- 共通基盤を Unity で試すには、push してコミットを上げる。ローカルの変更はそのままでは Unity に入らない。試すときは、手元で `manifest.json` を一時的に `file:` に変える（コミットしない）
- `Library/PackageCache` のパッケージは読み取り専用。編集は別の clone で行う

## 見直す条件

- サブモジュールをやめて、サーバー側も NuGet などのパッケージ参照に変えるとき
- 共通基盤の並行開発で、push の手間が大きな負担になったとき
