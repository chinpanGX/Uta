# Uta — プロジェクト運用ルール（スキーマ層）

このファイルは、LLM（Claude）がこのリポジトリで作業するときの最上位ルールです。
作業前に必ず `uta_docs/wiki/index.md` を読んでください。詳しいルールは、作業に応じて下のページを読みます。

## プロジェクト概要

- 名称: Uta（うた）
- 目的: 育成（プロデュース）・ガチャのゲームシステム基礎部分を実装する、技術先行の個人開発プロジェクト（3Dライブは保留: [ADR 0003](uta_docs/wiki/decisions/0003-defer-3d-live.md)）
- 目的（追加）: Unity 7 を早期段階で触る。Unity のバージョンは Unity 7 で確定（2026-10-27。現在 `7000.0.0a12`）。クライアント基盤・サーバー基盤も Unity 7 で試す（[ADR 0006](uta_docs/wiki/decisions/0006-adopt-unity-7-early.md)、accepted）。Unity 7 Preview は 12 月予定
- 対象プラットフォーム: Android / iOS / Windows
- 技術構成: Unity クライアント + ASP.NET Core サーバー（C#、.NET 10）、通信は MagicOnion（gRPC）。サーバーの土台は [architecture/server/server-foundation.md](uta_docs/wiki/architecture/server/server-foundation.md)
- 開発体制: 個人開発

## 作業に応じて読むファイル

| 作業 | 読むファイル |
|---|---|
| 設計・仕様を確認する、未決事項に触れる | `uta_docs/wiki/index.md` → フォルダの `README.md` → 関連ページ（全体は `overview/game-spec.md`、ゲームの仕様は `systems/`、実装設計書は `architecture/`。何の正がどのページかは `architecture/README.md`）。決定は ADR（`decisions/`）が正 |
| 技術選定・方針変更をする | `uta_docs/wiki/decisions/`（既存 ADR を確認。新規は `0000-template.md` を使う） |
| C# のコードを書く | 下の「C# コーディング規約」と `.editorconfig` |
| wiki を変更する、コードを変更した後 | 下の「wiki の運用」「実装前のルール」 |
| 用語を使う・追加する | `uta_docs/wiki/glossary.md` |
| コードの変更をレビューに出す（マージ前など） | `.claude/skills/requesting-code-review/SKILL.md`（レイヤーごとのチェックリストは `layers/`） |
| 実装計画を書く、実装を Sonnet に任せる | 下の「実装前のルール」と `uta_docs/plans/README.md`（計画の書式。[ADR 0017](uta_docs/wiki/decisions/0017-spec-design-plan-stages.md)） |
| 設計書（wiki の仕様ページ・実装計画・raw の設計・ADR の案）をレビューに出す | `.claude/skills/requesting-design-review/SKILL.md`（観点ごとのチェックリストは `lenses/`、レビュー担当の Goldfish は `.claude/agents/uta-goldfish-*.md`） |
| wiki を点検する（Lint）、ページや ADR を足した・消した、マージした後 | `.claude/skills/linting-wiki/SKILL.md`（リンク切れ・目次（README）への登録・frontmatter・ADR の番号をスクリプトで点検） |
| egm（`/egm:egm-*`）を使う | `.claude/skills/uta-elephant/SKILL.md`（置き場所・書式・レビューに渡す文脈の Uta 独自ルール） |

## 常に守ること

- 返答・ドキュメント・コミットメッセージは日本語で書く。
- 勝手に決めない。未決事項や、決定済みの方針（ADR）に反する変更・大きな設計変更は、ユーザーに確認する。
- コードを変更したら、関連する実装設計書（`architecture/`）を同じ変更で更新する。ゲームの挙動が変わったときは仕様（`systems/`）も更新する（手順は「実装前のルール」）。
- 存在しないビルドコマンドやファイルを、あるものとして書かない。
- **.NET SDK / Unity がない環境では、ビルド・テストの結果を推測で「通った」と書かない。** 確認できていないと明記し、ユーザーに実行を依頼する。
- シークレット（接続文字列・鍵・パスワード）をリポジトリに入れない。
- 日本語のファイルを Bash で読むときは、`cut -c` などで行を文字数で切らない（UTF-8 の途中で切れて文字化けし、読み直しになる）。必要な行だけを `sed -n 'N,Mp'` や Read の範囲指定で読む。

## セッションの区切り（コンテキストの節約）

1 回の呼び出しごとに、それまでの会話をすべて読み直す。会話が長いほど、1 回の呼び出しが高くつく（2026-10-31〜11-01 のセッションでは、文脈が 50〜59 万トークンのまま enum の計画を実装し、90 回の呼び出しで約 4,900 万トークンを読み直した）。

- **作業の区切りでは、次の作業を新しいセッションで始める。** 区切りは、PR を作った・マージした、実装計画を `status: done` にした、レビューの判断を反映し終えた、のどれか。Claude は区切りに来たら、続けずに新しいセッション（`start_session`）を提案する。引き継ぐことは、wiki・計画・PR に書いてあるものとし、ブリーフには読むファイルのパスを書く
- 同じセッションで続けるときは、ユーザーに `/compact` を頼む
- 会話が長くなってからは、小さい Bash・Read を何回も回さない。読むものはまとめて 1 回で読み、直すものはまとめて 1 回で直す

## IP方針（最重要）

模倣してよいのは**ゲームシステムの構造のみ**（育成ループ、ガチャ、ライブ演出の仕組み）。
キャラクター、名称、楽曲、UI素材、ロゴ、世界観、シナリオは**すべてオリジナル**にする。
既存作品の素材・名称・固有の表現を、コード・ドキュメント・アセットに入れない。
詳細は `uta_docs/wiki/decisions/0001-ip-policy.md`。例外: ゲームシステムの数値（カード・週構成・排出率・強化量など）は参考作品の値を使う（`uta_docs/wiki/decisions/0016-reference-game-values.md`）。作品名は書かない。

## リポジトリ構成

```
uta/
├─ CLAUDE.md
├─ .editorconfig / .gitattributes / .gitignore  # .editorconfig は client/・server/・shared/ にも置く
├─ external/      # Git サブモジュール（共通基盤: 自作の Unity・サーバーのパッケージ）
├─ masterdata/    # マスタの正本（定義・CSV・設定）。Unity にも shared/ にも入れない（ADR 0007）。未作成
├─ uta_docs/      # ドキュメント（Obsidian の vault。.obsidian/ は .gitignore で除外）
│  ├─ raw/        # 変更しない一次資料（調査ログ、観察メモ）
│  ├─ plans/      # 実装計画（どのファイルに何を書くか。書式は plans/README.md）
│  ├─ reviews/    # 設計書レビューの結果（実施日ごとのフォルダ。目次は各フォルダの README.md）
│  └─ wiki/       # LLMが維持する整理済み知識
│     ├─ index.md     # 入口。ページの一覧は各フォルダの README.md
│     ├─ glossary.md
│     ├─ overview/      # ゲーム全体の仕様
│     ├─ systems/       # ゲームの仕様（produce / lesson-card / idol-support / gacha。live は保留）
│     ├─ architecture/  # 実装設計書（共通の編、Shared.Domain の機能ごとの設計）
│     │  ├─ master/     # マスタ設計（定義・生成・配信、システムごとのテーブルと列）
│     │  └─ server/     # サーバーの実装設計（サーバー基盤、ガチャ・強化の処理）
│     └─ decisions/     # ADR
├─ server/        # ASP.NET Core + MagicOnion（Uta.Server・Uta.Server.Tests・compose.yaml）
├─ client/        # Unity 関連（Unity プロジェクトのルートは client/uta/）
└─ shared/        # Uta.Shared.Domain（Shared.Domain）・Uta.Shared.Contracts（サービスIF・DTO。今は中身が空）。マスタ型は置かない（ADR 0007）
```

## wiki の運用（3操作）

1. **Ingest**: 調べたこと・決めたことは、まず `uta_docs/raw/` に置き、`uta_docs/wiki/` に反映する。反映したら、そのフォルダの `README.md`（ページを足した・消したとき）を更新する。変更の記録はコミットメッセージに書く（`log.md` は使わない。[ADR 0018](uta_docs/wiki/decisions/0018-change-log-from-git.md)）。
2. **Query**: 設計・実装の根拠は wiki から引く。wiki に無いことは推測で実装せず、先に wiki へ起こす。
3. **Lint**: 定期的に、矛盾・古い記述・孤立ページ・目次に未登録のページを点検して修正する。機械的に調べられるもの（リンク切れ・目次（README）への登録・frontmatter・ADR の番号）は `linting-wiki` のスクリプトで、矛盾・古い記述は `requesting-design-review` の整合性の観点で見る。

## 実装前のルール（ドキュメント駆動）

**設計は厚く書き、実装は Sonnet のサブエージェントに任せる。設計書は 3 段に分け、詳細は実装設計書の段階でレビューを回して決める**（[ADR 0017](uta_docs/wiki/decisions/0017-spec-design-plan-stages.md)。ADR 0014 を置き換え）。判断は設計書と実装計画に寄せ、実装担当に設計判断を残さない。

| 段 | 置き場 | 書くこと | レビュー |
|---|---|---|---|
| ゲームの仕様 | `wiki/systems/`・`wiki/overview/` | 何が起きるか（ルール・値・局面の遷移・実行要件）。実装設計書の一次情報。ゲームの仕様が変わるときだけ変える | 整合性の観点だけ |
| 実装設計書 | `wiki/architecture/` | どう作るか（層・型・RPC・DTO・テーブル・処理の手順・乱数・状態）。仕様の節にリンクする | 3 つの観点。詳細はここで決める。ページ全体は 2 回まで |
| 実装計画 | `uta_docs/plans/` | どのファイルに何を書くか（シグネチャ・DDL・手順・テスト） | 3 つの観点。実装可能性（Sonnet）が「質問せずに書ける」と判定するまで |

1. ゲームの仕様（`systems/`・`overview/`）を書く、または更新する
2. 実装設計書（`architecture/`）を書く、または更新する。何の正がどのページかは `architecture/README.md`
3. 技術選定や方針変更は ADR を追加する（既存 ADR は書き換えず、Superseded にして新規ADRを作る）
4. 実装計画を `uta_docs/plans/` に書く（書式と、計画が要る変更は `uta_docs/plans/README.md`）
5. 実装設計書と実装計画を `requesting-design-review` でレビューに出す（上の表の回し方）。実装計画は、実装可能性の観点が「質問せずに書ける」と判定するまで直し、`status: ready` にする
6. 計画のタスクごとに Sonnet のサブエージェントに実装させる（Agent ツールで `model: sonnet`）。実装担当は、計画にない設計判断に出会ったら止まって報告する。計画を直してから続ける
7. `requesting-code-review` でコードレビューに出す
8. 実装結果と設計書のズレを wiki に反映し（実装設計書を直す。ゲームの挙動が変わったときだけ仕様も直す）、計画を `status: done` にする
9. コミットメッセージに、何を変えたかと決めたこと・理由を書く（変更の記録は `git log` から引く。[ADR 0018](uta_docs/wiki/decisions/0018-change-log-from-git.md)）

仕様・実装設計書・実装計画を書くのはメインのセッション（Opus または Fable）。計画が要らない小さな変更（1 ファイルの修正など）は 4〜6 を飛ばしてよい。

## wiki ページの書式

- 先頭に frontmatter を置く:
  ```
  ---
  title: ページ名
  status: draft | stable | deprecated | deferred
  updated: YYYY-MM-DD
  ---
  ```
- 他ページへのリンクは相対パスの Markdown リンクにする
- 「決まっていること」と「未決事項」を分けて書く
- 推測は推測と明記する

## 実装方針

- サーバー権威: 育成の計算結果、ガチャ抽選、所持品の更新はすべてサーバーで行う。結果の確定は常にサーバー。
- クライアントは表示と入力に加え、Shared.Domain（`shared/Uta.Shared.Domain`）で**使用可否の判定と予測の表示**まで行う。**シードは持たない**（[ADR 0008](uta_docs/wiki/decisions/0008-shared-domain-and-onion.md)）。
- Shared の型は `shared/` に置く。Unity / サーバー双方から参照する。マスタ型は `masterdata/` の定義から各側へ生成する（[ADR 0007](uta_docs/wiki/decisions/0007-masterdata-with-addons.md)）。
- 層の構成と依存ルールは [architecture/repository-layout.md](uta_docs/wiki/architecture/repository-layout.md)。クライアントはオニオンアーキテクチャ（uGUI + UnityScreenNavigator・VContainer・UniTask。[ADR 0009](uta_docs/wiki/decisions/0009-client-stack.md)）。DB は PostgreSQL（[ADR 0010](uta_docs/wiki/decisions/0010-database.md)）、マイグレーションは DbUp（[ADR 0012](uta_docs/wiki/decisions/0012-migration-dbup.md)）。デプロイ先は Google Cloud の Cloud Run + Cloud SQL（[ADR 0011](uta_docs/wiki/decisions/0011-deploy-google-cloud.md)）。
- マスタの列の型は `int` / `long` / `string` / `bool` / enum のみ。Shared.Domain では `float` / `double` を使わない。
- ID はマスタが `int`、サーバーが発行するものは ULID。時刻はサーバーが正で JST 固定、日付の更新は 12 時。乱数は `xoshiro256**`。強制アップデートは実装しない（[repository-layout.md](uta_docs/wiki/architecture/repository-layout.md) 10章）。
- iOS（IL2CPP / AOT）制約を常に考慮する。リフレクション依存を避け、Source Generator / 事前生成を優先する。
- モバイル実機ビルドを早期・継続的に通す。

## C# コーディング規約

書式・命名の正は `.editorconfig`（ルートが共通、各フォルダが差分）。主なルール:

- 波括弧は Allman（常に次の行）。`var` を使う
- 型・メソッド・プロパティ・定数・`static readonly` は PascalCase。引数・ローカル変数は camelCase
- private / internal のフィールドは接頭辞なしの camelCase（`_` を付けない）
- interface は `I`、型パラメーターは `T` で始める
- 言語バージョンに関わるルールはフォルダごとの `.editorconfig` で分けている:
  - `client/`・`shared/`・`masterdata/`: Unity にソースで入るため **C# 9 の範囲に収める**（Unity 7 で C# 14 が使えると確認できたら見直す。[ADR 0006](uta_docs/wiki/decisions/0006-adopt-unity-7-early.md)）（ファイルスコープ namespace、プライマリコンストラクタ、コレクション式、Index/Range は使わない）
  - `server/`: .NET 10（C# 14）。新しい C# の書き方を許す（ファイルスコープ namespace など）
- `.cs` は BOM 付き UTF-8、改行は LF
- テストのメソッド名は `Method_Condition_Result` の形を許す

## Git

- 改行は LF 固定（`.gitattributes`）。`.bat` / `.cmd` のみ CRLF
- Unity の `.meta` ファイルは必ずコミットする。`client/uta/Library/` などの自動生成物は入れない（`.gitignore`）
- Unity が生成する `.csproj` / `.sln` は `client/<プロジェクト名>/` 直下（現在は `client/uta/`）のみ除外する。`server/` と `shared/` のプロジェクトファイルはコミットする
- コミットメッセージは日本語
- **GitHub の操作（PR・Issue・API）は `gh` コマンドを使う**（2026-11-01 に確定）。GitHub の MCP は使わない
- **前提のタスク（未マージの PR）がある作業は、積み PR にする**: 前提のブランチから新しいブランチを切り（`git switch -c <新ブランチ> <前提ブランチ>`）、PR の base も前提のブランチにする（`gh pr create --base <前提ブランチ>`）。PR の本文に前提の PR を書く。前提の PR を先に main へマージしていき、後続の PR は base を main にリターゲットする（`gh pr edit --base main`）。リターゲットとマージはユーザーが進める。前提がない作業は main（または指定の base）に向ける
- コミットメッセージは日本語。変更の記録を兼ねるので、1 行目に何を変えたか、本文に決めたこと・理由を書く（[ADR 0018](uta_docs/wiki/decisions/0018-change-log-from-git.md)）。
- 履歴は `git log -- uta_docs/wiki` などで引く
- サーバー基盤と共通パッケージは別リポジトリを Git サブモジュールとして `external/` に取り込む（[ADR 0005](uta_docs/wiki/decisions/0005-foundation-submodule.md)、proposed）。登録済みは `VolTech-MagicOnion`・`Supplement`・`UnityScreenNavigator`・`MasterMemoryAddons`（`.gitmodules` が正）。開発は、サブモジュールの中でも別の clone でもよいが、**サブモジュール側を先に push してから Uta のポインタを更新する**
  - 共通基盤は Uta の開発を通して整え、別のアプリでも使える形でリリースする。Uta 固有のものを共通基盤に入れない。固定はコミット（タグを待たない）。ルールは [repository-layout.md](uta_docs/wiki/architecture/repository-layout.md) 3.1
  - clone: `git clone --recurse-submodules <Uta の URL>`（済みの clone では `git submodule update --init`）
  - サブモジュール内で作業する前にブランチを切る（`git switch -c <branch>`）。detached HEAD のまま commit しない
  - 追加: `git submodule add <URL> external/<名前>`（すでに clone 済みなら、その場所を指定すれば取り込める）。追加したら ADR 0005 の表も更新する
  - ポインタ更新: サブモジュール側を push → Uta で `git add external/<名前>` → commit
- worktree を使うときは Uta の外に置く（例: `../Uta-wt/<ブランチ名>`）。サブモジュールは worktree ごとに `git submodule update --init` が必要

## ビルド・テスト・実行

プロジェクト（`client/`）を作成したら、実際に動くコマンドをここに追記する。

- `shared/Uta.Shared.Domain` のビルド: `dotnet build shared/Uta.Shared.Domain`
- `shared/Uta.Shared.Domain` の単体テスト: `dotnet test --project shared/Uta.Shared.Domain.Tests`（.NET 10 SDK。実行方式はルートの `global.json` で Microsoft.Testing.Platform を指定）
- サーバー・shared 全体のビルド: `dotnet build Uta.slnx`（警告 0・エラー 0）
- サーバーのテスト: `dotnet test --project server/Uta.Server.Tests`（アーキテクチャテストと結合テスト。結合テストは Docker が要る: Testcontainers が PostgreSQL を起動する）
- ローカルのサーバー: `docker compose --env-file server/.env -f server/compose.yaml up -d --wait`（`server/.env` は `.env.example` を写して `POSTGRES_PASSWORD` を書く）→ `dotnet user-secrets set "ConnectionStrings:Default" "<接続文字列>" --project server/Uta.Server` → `dotnet run --project server/Uta.Server -- migrate`（マイグレーション）→ `dotnet run --project server/Uta.Server`（`http://localhost:5000`）
- Unity 7 の Editor が開いているときのコンパイル確認: `unity recompile --format json`（`client/uta` で実行）。パッケージの再解決: `unity command package resolve`。コンソールの読み出し・Play モードの開始と停止は [unity7-evaluation.md](uta_docs/wiki/architecture/unity7-evaluation.md) 4章。**Windows の IL2CPP のビルドは通らない（`IL1009`。同 3.1）**
- wiki の点検（Lint）: `python -I .claude/skills/linting-wiki/lint_wiki.py`（Python 3。エラーがあれば終了コード 1）

## 用語集

用語は `uta_docs/wiki/glossary.md` に集約する。
