---
title: ADR 0005 基盤とパッケージを Git サブモジュールで取り込む
status: proposed
updated: 2026-10-26
---

# ADR 0005: 基盤とパッケージを Git サブモジュールで取り込む

## 状況

[ADR 0004](0004-server-foundation.md) でサーバー基盤（MagicOnion + PostgreSQL）の採用を決めたが、取り込み方が未確定だった。基盤は実装と Unity クライアント（UPM）を含む別リポジトリとして存在する。

## 決定（提案）

- 基盤は別リポジトリのまま、Uta から **Git サブモジュール**として取り込む
- 取り込む対象は、サーバー基盤だけでなく、作者自身が開発している共通パッケージ（Unity・サーバー双方）を含む。Uta の開発を通じて育てることを前提にする
- 配置先は `external/<リポジトリ名>/`。2026-10-26 に次の 4 つを登録した（`.gitmodules` が正）:

  | パス | 役割 | 参照ブランチ／タグ（登録時） |
  |---|---|---|
  | `external/VolTech-MagicOnion` | サーバー基盤（MagicOnion）と Unity クライアント（開発中。現時点は `Abstractions` のみ） | main |
  | `external/Supplement` | Unity 補助パッケージ（起動時初期化、セーブデータ、MessageBroker、アセット読み込みなど） | main（v1.5.0） |
  | `external/UnityScreenNavigator` | 画面遷移ライブラリ（Haruma-K 版のフォーク） | develop |
  | `external/MasterMemoryAddons` | MasterMemory のアドオン（クライアント用・サーバー用のコード生成、CSV 検証、バイナリ生成） | main |

- 基盤のタグ（またはコミット）を固定して参照する。更新は Uta 側で意図して行う
- 基盤の開発は次のどちらでもよい:
  - **A. サブモジュールの中で開発する**: `external/` の基盤でブランチを切って実装・push し、Uta でポインタを更新する。Uta で動かしながら確かめたいときに使う
  - **B. 別の場所に clone して開発する**: 通常のリポジトリとして開発し、完成したタグ／コミットに Uta のポインタを更新する。基盤を大きく変えるときに使う
- **基盤側を先に push してから、Uta のポインタを更新・push する**（逆にすると、他の環境で参照先のコミットが存在しない状態になる）
- サブモジュールは detached HEAD で入るため、A で作業する前にブランチを切る（`git switch -c <branch>`）

## 理由

- 基盤を複数プロジェクトで共有でき、Uta 固有のコードと履歴が混ざらない
- 基盤は互換性のない変更が続いているため、参照するコミットを固定できる方が安全
- Uta の `shared/` には Uta 固有の型だけを置ける

## 検討した代替案

- 案A: ソースを Uta にコピーする。基盤の更新を追従しにくく、Uta 側で改変しがちになるため不採用
- 案B: NuGet / UPM のパッケージとして参照する。配布の準備が必要で、個人開発の現段階では重いため不採用（将来の選択肢）

## 影響・リスク

- clone 時に `--recurse-submodules` が必要。CI やビルド環境でも同様
- サブモジュールのリポジトリへ認証が通る必要がある（非公開リポジトリの場合）
- ポインタだけを push すると他の環境で壊れる。push の順序（基盤 → Uta）を守る必要がある
- worktree を併用する場合、サブモジュールは worktree ごとに `git submodule update --init` が必要で、git 側の対応も不完全。基盤の開発は B（別 clone とその worktree）を基本にする
- `.editorconfig` が階層で効くため、基盤側の `.editorconfig`（`root = true` の有無）と Uta 側の規約の関係を確認する
- Unity からは基盤の `Abstractions`（UPM パッケージ）を参照する。サブモジュール内のパッケージをローカルパスで参照できるか、実機ビルドを含めて確認する（推測）

## 未決事項

- 固定するタグ／コミット（登録時点では各リポジトリの先頭を参照している。リリース済みのタグがあるのは Supplement のみ）→ 2026-10-31: タグを待たずコミットで固定し、区切りで基盤の版を出すと決めた（[repository-layout.md](../architecture/repository-layout.md) 3.1）
- URL の形式の統一（`Supplement`・`UnityScreenNavigator` は SSH、`VolTech-MagicOnion`・`MasterMemoryAddons` は HTTPS。CI や別環境での認証に影響する）
- サブモジュール内の `.git` はディレクトリのまま（`git submodule absorbgitdirs` は未実施）
- `Supplement`・`UnityScreenNavigator` は Unity プロジェクトごと入る。Uta の `client` からはローカルパス参照にする想定（推測）。参照方法は実機ビルドで確認する
- `MasterMemoryAddons` とマスタデータ配信の関係（[index](../index.md) の `architecture/masterdata-pipeline.md` を起こすときに決める）
- Unity のパッケージ参照方法（`client/Packages/manifest.json` のローカルパス参照など）
- Uta の `shared/` と基盤の `Abstractions` の役割分担

## 見直す条件

- サブモジュール運用が重くなったとき（パッケージ化を検討）
- 基盤を Uta 専用に改変する必要が出たとき
