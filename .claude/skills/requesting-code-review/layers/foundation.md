# foundation レイヤーのチェックリスト（共通基盤）

別リポジトリの自作の共通基盤。Uta 以外のプロジェクトでも使う前提で育てる。サーバーは `packages/` の `.nupkg`、Unity は `manifest.json` の git URL で固定する（[ADR 0019](../../../../uta_docs/wiki/decisions/0019-foundation-packages.md)）。

| リポジトリ | 中身 | そのリポジトリのルール |
|---|---|---|
| `VolTech-MagicOnion` | サーバー基盤（MagicOnion）、Unity と共有する `Abstractions` | `CLAUDE.md` |
| `Supplement` | Unity 補助パッケージ（UPM） | `CLAUDE.md` |
| `UnityScreenNavigator` | 画面遷移（Haruma-K 版のフォーク） | `README.md` / `README_JA.md` |
| `MasterMemoryAddons` | MasterMemory のアドオン（生成・検証・バイナリ） | `CLAUDE.md` |

**そのリポジトリの `CLAUDE.md`・docs が、このレイヤーのコーディング規約の正。** Uta の CLAUDE.md の規約と違うところは、そのリポジトリのルールに従う。

## 範囲の取り方

Uta の差分には固定の変化（古いコミット → 新しいコミット）しか出ない。別の clone で見る。

```bash
git log --oneline <古いコミット>..<新しいコミット>
git diff <古いコミット>..<新しいコミット>
```

## 固定の更新（ADR 0019）

- [ ] 新しいコミットが、そのリポジトリの既定のブランチに入っているか
- [ ] `.nupkg`・`Directory.Packages.props`・`.config/dotnet-tools.json`・`manifest.json`・`packages-lock.json` が同じコミットに揃っているか（`tools/pack-foundation.sh` で作ったか）
- [ ] 意図しない共通基盤の固定が一緒に動いていないか

## 汎用性（共通基盤であること）

- [ ] **Uta 固有の知識が共通基盤に入っていないか**（ゲームのルール、Uta の画面名・型名、Uta のマスタのテーブル）。固有のものは Uta の `shared/`・`server/`・`client/` に置く
- [ ] 拡張点（インターフェース、設定、コールバック）で差し替えられる形になっているか。Uta の都合を直接書き込んでいないか
- [ ] 公開 API に XML ドキュメントコメントがあるか（Supplement は CS1591 で確かめる規約）

## 互換性

- [ ] 公開 API の互換が壊れる変更（名前・引数・戻り値の変更、削除、既定値の変更、シリアライズ形式の変更）があるか。あるなら:
  - そのリポジトリの changelog（Supplement は `docs/changelog.md`、VolTech-MagicOnion は `CHANGELOG.md`）の「互換性が壊れる変更」に理由つきで書かれているか
  - 版（タグ）の上げ方が妥当か
  - Uta 側の呼び出しが同じ変更で追従しているか
- [ ] MessagePack のシリアライズ形式（`Key` の番号）を変えていないか。変えるなら保存済みデータ（セーブデータ、DB の `bytea`、マスタのバイナリ）を読めるか
- [ ] 依存パッケージの版（MessagePack、MagicOnion、MasterMemory、VContainer、UniTask）を上げたなら、ほかの共通基盤と Uta でそろうか（repository-layout.md 8章の要検証）

## Unity と共有する部分

- [ ] Unity にソースで入る部分（VolTech-MagicOnion の `Abstractions`、MasterMemoryAddons の `Attributes`・`Core`・Unity 側、Supplement、UnityScreenNavigator）が **C# 9 / netstandard2.1 の範囲**か。`record`・`init` を使わない（VolTech-MagicOnion の規約）
- [ ] サーバー専用の依存（Npgsql など）が、Unity と共有するアセンブリに入っていないか
- [ ] リフレクションに依存していないか、IL2CPP / AOT で動くか（Source Generator / 事前生成を優先）
- [ ] Unity 7（`7000.0.0a12`）と、そのリポジトリが対応を約束する Unity の版の両方で問題がないか
- [ ] UPM パッケージの `package.json` の版・依存、`.meta`、asmdef の変更が整合しているか

## サーバー基盤（VolTech-MagicOnion）

- [ ] フィルターの順序（RpcLogFilter → ServerStatusFilter → AuthFilter → DateChangeFilter → ハンドラー）を変えていないか。変えるなら設計書と [server-foundation.md](../../../../uta_docs/wiki/architecture/server/server-foundation.md) の更新が要る
- [ ] エラーの変換（`BusinessException` → `FAILED_PRECONDITION` + ErrorInfo、想定外 → `INTERNAL`）、ヘッダー、冪等性、トランザクションの約束を壊していないか
- [ ] 認証・トークンの扱いに穴がないか（期限切れ、`[SkipAuth]` の範囲）

## テスト

- [ ] そのリポジトリのテストの回し方（各 CLAUDE.md の「ビルド・テスト・実行」、Supplement は `docs/maintainers/testing.md`）に従って確かめられているか
- [ ] 実行していなければ「未確認」と書き、コマンドをそのまま挙げる

## ドキュメント

- [ ] そのリポジトリの docs・changelog が同じ変更で更新されているか（各リポジトリのルール）
- [ ] Uta の wiki（server-foundation.md、masterdata-pipeline.md など）が共通基盤の変更を前提にしているなら、Uta 側も更新されているか
