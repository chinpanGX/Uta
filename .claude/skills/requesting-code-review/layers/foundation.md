# foundation レイヤーのチェックリスト（`external/`）

Git サブモジュールで取り込んだ自作の共通基盤。Uta 以外のプロジェクトでも使う前提で育てる（[ADR 0005](../../../../uta_docs/wiki/decisions/0005-foundation-submodule.md)）。

| サブモジュール | 中身 | そのリポジトリのルール |
|---|---|---|
| `external/VolTech-MagicOnion` | サーバー基盤（MagicOnion）、Unity と共有する `Abstractions` | `external/VolTech-MagicOnion/CLAUDE.md` |
| `external/Supplement` | Unity 補助パッケージ（UPM） | `external/Supplement/CLAUDE.md` |
| `external/UnityScreenNavigator` | 画面遷移（Haruma-K 版のフォーク） | `README.md` / `README_JA.md` |
| `external/MasterMemoryAddons` | MasterMemory のアドオン（生成・検証・バイナリ） | `external/MasterMemoryAddons/CLAUDE.md` |

**そのサブモジュールの `CLAUDE.md`・docs が、このレイヤーのコーディング規約の正。** Uta の CLAUDE.md の規約と違うところは、サブモジュール側に従う。まず該当するサブモジュールのルールを読む。

## 範囲の取り方

Uta の差分にはポインタの変化（古いコミット → 新しいコミット）しか出ない。

```bash
git diff --submodule=log [BASE_SHA]..[HEAD_SHA] -- external/<名前>
git -C external/<名前> log --oneline <古いポインタ>..<新しいポインタ>
git -C external/<名前> diff <古いポインタ>..<新しいポインタ>
```

## ポインタの更新（ADR 0005）

- [ ] 新しいポインタのコミットが、サブモジュールのリモートに push 済みか（`git -C external/<名前> branch -r --contains <新しいポインタ>`）。push 前だと、ほかの環境で参照先が存在しない
- [ ] ポインタが、ブランチの先頭かタグなど、意図したコミットを指しているか（作業中の detached HEAD のコミットではないか）
- [ ] 意図しないサブモジュールのポインタが一緒に動いていないか
- [ ] サブモジュールを追加・削除したなら、`.gitmodules` と ADR 0005 の表が更新されているか

## 汎用性（共通基盤であること）

- [ ] **Uta 固有の知識が共通基盤に入っていないか**（ゲームのルール、Uta の画面名・型名、Uta のマスタのテーブル）。固有のものは Uta の `shared/`・`server/`・`client/` に置く
- [ ] 拡張点（インターフェース、設定、コールバック）で差し替えられる形になっているか。Uta の都合を直接書き込んでいないか
- [ ] 公開 API に XML ドキュメントコメントがあるか（Supplement は CS1591 で確かめる規約）

## 互換性

- [ ] 公開 API の互換が壊れる変更（名前・引数・戻り値の変更、削除、既定値の変更、シリアライズ形式の変更）があるか。あるなら:
  - サブモジュール側の changelog（Supplement は `docs/changelog.md`、VolTech-MagicOnion は `CHANGELOG.md`）の「互換性が壊れる変更」に理由つきで書かれているか
  - 版（タグ）の上げ方が妥当か
  - Uta 側の呼び出しが同じ変更で追従しているか
- [ ] MessagePack のシリアライズ形式（`Key` の番号）を変えていないか。変えるなら保存済みデータ（セーブデータ、DB の `bytea`、マスタのバイナリ）を読めるか
- [ ] 依存パッケージの版（MessagePack、MagicOnion、MasterMemory、VContainer、UniTask）を上げたなら、ほかのサブモジュールと Uta でそろうか（repository-layout.md 8章の要検証）

## Unity と共有する部分

- [ ] Unity にソースで入る部分（VolTech-MagicOnion の `Abstractions`、MasterMemoryAddons の `Attributes`・`Core`・Unity 側、Supplement、UnityScreenNavigator）が **C# 9 / netstandard2.1 の範囲**か。`record`・`init` を使わない（VolTech-MagicOnion の規約）
- [ ] サーバー専用の依存（Npgsql など）が、Unity と共有するアセンブリに入っていないか
- [ ] リフレクションに依存していないか、IL2CPP / AOT で動くか（Source Generator / 事前生成を優先）
- [ ] Unity 7（`7000.0.0a12`）と、そのサブモジュールが対応を約束する Unity の版の両方で問題がないか
- [ ] UPM パッケージの `package.json` の版・依存、`.meta`、asmdef の変更が整合しているか

## サーバー基盤（VolTech-MagicOnion）

- [ ] フィルターの順序（RpcLogFilter → ServerStatusFilter → AuthFilter → DateChangeFilter → ハンドラー）を変えていないか。変えるなら設計書と [server-foundation.md](../../../../uta_docs/wiki/architecture/server-foundation.md) の更新が要る
- [ ] エラーの変換（`BusinessException` → `FAILED_PRECONDITION` + ErrorInfo、想定外 → `INTERNAL`）、ヘッダー、冪等性、トランザクションの約束を壊していないか
- [ ] 認証・トークンの扱いに穴がないか（期限切れ、`[SkipAuth]` の範囲）

## テスト

- [ ] そのサブモジュールのテストの回し方（各 CLAUDE.md の「ビルド・テスト・実行」、Supplement は `docs/maintainers/testing.md`）に従って確かめられているか
- [ ] 実行していなければ「未確認」と書き、コマンドをそのまま挙げる

## ドキュメント

- [ ] サブモジュール側の docs・changelog が同じ変更で更新されているか（各リポジトリのルール）
- [ ] Uta の wiki（server-foundation.md、masterdata-pipeline.md など）が共通基盤の変更を前提にしているなら、Uta 側も更新されているか
