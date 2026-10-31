# Uta — プロジェクト運用ルール（スキーマ層）

このファイルは、LLM（Claude）がこのリポジトリで作業するときの最上位ルールです。
作業前に必ず `uta_docs/wiki/index.md` を読んでください。詳しいルールは、作業に応じて下のページを読みます。

## プロジェクト概要

- 名称: Uta（うた）
- 目的: 育成（プロデュース）・ガチャのゲームシステム基礎部分を実装する、技術先行の個人開発プロジェクト（3Dライブは保留: [ADR 0003](uta_docs/wiki/decisions/0003-defer-3d-live.md)）
- 目的（追加）: Unity 7 を早期段階で触る。Unity のバージョンは Unity 7 で確定（2026-10-27。現在 `7000.0.0a12`）。クライアント基盤・サーバー基盤も Unity 7 で試す（[ADR 0006](uta_docs/wiki/decisions/0006-adopt-unity-7-early.md)、accepted）。Unity 7 Preview は 12 月予定
- 対象プラットフォーム: Android / iOS / Windows
- 技術構成: Unity クライアント + ASP.NET Core サーバー（C#、.NET 10）、通信は MagicOnion（gRPC）。サーバーの土台は [architecture/server-foundation.md](uta_docs/wiki/architecture/server-foundation.md)
- 開発体制: 個人開発

## 作業に応じて読むファイル

| 作業 | 読むファイル |
|---|---|
| 設計・仕様を確認する、未決事項に触れる | `uta_docs/wiki/index.md` → 関連ページ（全体は `overview/game-spec.md`、各システムは `systems/`）。決定は ADR（`decisions/`）が正 |
| 技術選定・方針変更をする | `uta_docs/wiki/decisions/`（既存 ADR を確認。新規は `0000-template.md` を使う） |
| C# のコードを書く | 下の「C# コーディング規約」と `.editorconfig` |
| wiki を変更する、コードを変更した後 | 下の「wiki の運用」「実装前のルール」 |
| 用語を使う・追加する | `uta_docs/wiki/glossary.md` |
| コードの変更をレビューに出す（マージ前など） | `.claude/skills/requesting-code-review/SKILL.md`（レイヤーごとのチェックリストは `layers/`） |
| 実装計画を書く、実装を Sonnet に任せる | 下の「実装前のルール」と `uta_docs/plans/README.md`（計画の書式。[ADR 0014](uta_docs/wiki/decisions/0014-design-heavy-sonnet-implementation.md)） |
| 設計書（wiki の仕様ページ・実装計画・raw の設計・ADR の案）をレビューに出す | `.claude/skills/requesting-design-review/SKILL.md`（観点ごとのチェックリストは `lenses/`、レビュー担当の Goldfish は `.claude/agents/uta-goldfish-*.md`） |
| wiki を点検する（Lint）、ページや ADR を足した・消した、マージした後 | `.claude/skills/linting-wiki/SKILL.md`（リンク切れ・index 登録・frontmatter・ADR の番号をスクリプトで点検） |
| egm（`/egm:egm-*`）を使う | `.claude/skills/uta-elephant/SKILL.md`（置き場所・書式・レビューに渡す文脈の Uta 独自ルール） |

## 常に守ること

- 返答・ドキュメント・コミットメッセージは日本語で書く。
- 勝手に決めない。未決事項や、決定済みの方針（ADR）に反する変更・大きな設計変更は、ユーザーに確認する。
- コードを変更したら、関連する仕様ページを同じ変更で更新する（手順は「実装前のルール」）。
- 存在しないビルドコマンドやファイルを、あるものとして書かない。
- **.NET SDK / Unity がない環境では、ビルド・テストの結果を推測で「通った」と書かない。** 確認できていないと明記し、ユーザーに実行を依頼する。
- シークレット（接続文字列・鍵・パスワード）をリポジトリに入れない。

## IP方針（最重要）

模倣してよいのは**ゲームシステムの構造のみ**（育成ループ、ガチャ、ライブ演出の仕組み）。
キャラクター、名称、楽曲、UI素材、ロゴ、世界観、シナリオは**すべてオリジナル**にする。
既存作品の素材・名称・固有の表現を、コード・ドキュメント・アセットに入れない。
詳細は `uta_docs/wiki/decisions/0001-ip-policy.md`。例外: 初期カードセットの数値（コスト・効果量）は参考作品の値を使う（`uta_docs/wiki/decisions/0015-reference-card-values.md`）。作品名は書かない。

## リポジトリ構成

```
uta/
├─ CLAUDE.md
├─ .editorconfig / .gitattributes / .gitignore  # .editorconfig は client/・server/・shared/ にも置く
├─ external/      # Git サブモジュール（自作の共通パッケージ・サーバー基盤）
├─ masterdata/    # マスタの正本（定義・CSV・設定）。Unity にも shared/ にも入れない（ADR 0007）。未作成
├─ uta_docs/      # ドキュメント（Obsidian の vault。.obsidian/ は .gitignore で除外）
│  ├─ raw/        # 変更しない一次資料（調査ログ、観察メモ）
│  ├─ plans/      # 実装計画（どのファイルに何を書くか。書式は plans/README.md）
│  ├─ reviews/    # 設計書レビューの結果（実施日ごとのフォルダ。目次は各フォルダの README.md）
│  └─ wiki/       # LLMが維持する整理済み知識
│     ├─ index.md
│     ├─ log.md
│     ├─ glossary.md
│     ├─ overview/      # ゲーム全体の概要仕様
│     ├─ systems/       # produce / gacha（live は保留）
│     ├─ architecture/  # 実装設計書（共通・基盤 / マスター / API / 画面）、サーバー基盤
│     └─ decisions/     # ADR
├─ server/        # ASP.NET Core + MagicOnion（プロジェクトは未作成。今は .editorconfig のみ）
├─ client/        # Unity 関連（Unity プロジェクトのルートは client/uta/）
└─ shared/        # Uta.Domain（共有ドメイン）・Uta.Shared（サービスIF・DTO。未作成）。マスタ型は置かない（ADR 0007）
```

## wiki の運用（3操作）

1. **Ingest**: 調べたこと・決めたことは、まず `uta_docs/raw/` に置き、`uta_docs/wiki/` に反映する。反映したら `index.md` と `log.md` を更新する。
2. **Query**: 設計・実装の根拠は wiki から引く。wiki に無いことは推測で実装せず、先に wiki へ起こす。
3. **Lint**: 定期的に、矛盾・古い記述・孤立ページ・index 未登録ページを点検して修正する。機械的に調べられるもの（リンク切れ・index 登録・frontmatter・ADR の番号）は `linting-wiki` のスクリプトで、矛盾・古い記述は `requesting-design-review` の整合性の観点で見る。

## 実装前のルール（ドキュメント駆動）

**設計は厚く書き、実装は Sonnet のサブエージェントに任せる**（[ADR 0014](uta_docs/wiki/decisions/0014-design-heavy-sonnet-implementation.md)、proposed）。判断は仕様と実装計画に寄せ、実装担当に設計判断を残さない。

1. 仕様ページ（`systems/` または `architecture/`）を書く、または更新する
2. 技術選定や方針変更は ADR を追加する（既存 ADR は書き換えず、Superseded にして新規ADRを作る）
3. 実装計画を `uta_docs/plans/` に書く（書式と、計画が要る変更は `uta_docs/plans/README.md`）
4. 仕様と実装計画を `requesting-design-review` でレビューに出す。実装計画は、実装可能性の観点（Sonnet）が「質問せずに書ける」と判定するまで直し、`status: ready` にする
5. 計画のタスクごとに Sonnet のサブエージェントに実装させる（Agent ツールで `model: sonnet`）。実装担当は、計画にない設計判断に出会ったら止まって報告する。計画を直してから続ける
6. `requesting-code-review` でコードレビューに出す
7. 実装結果と仕様のズレを wiki に反映し、計画を `status: done` にする
8. `log.md` に1行追記する

仕様・実装計画を書くのはメインのセッション（Opus または Fable）。計画が要らない小さな変更（1 ファイルの修正など）は 3〜5 を飛ばしてよい。

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
- クライアントは表示と入力に加え、共有ドメイン（`shared/Uta.Domain`）で**使用可否の判定と予測の表示**まで行う。**シードは持たない**（[ADR 0008](uta_docs/wiki/decisions/0008-shared-domain-and-onion.md)）。
- 共有型は `shared/` に置く。Unity / サーバー双方から参照する。マスタ型は `masterdata/` の定義から各側へ生成する（[ADR 0007](uta_docs/wiki/decisions/0007-masterdata-with-addons.md)）。
- 層の構成と依存ルールは [architecture/repository-layout.md](uta_docs/wiki/architecture/repository-layout.md)。クライアントはオニオンアーキテクチャ（uGUI + UnityScreenNavigator・VContainer・UniTask。[ADR 0009](uta_docs/wiki/decisions/0009-client-stack.md)）。DB は PostgreSQL（[ADR 0010](uta_docs/wiki/decisions/0010-database.md)）、マイグレーションは DbUp（[ADR 0012](uta_docs/wiki/decisions/0012-migration-dbup.md)）。デプロイ先は Google Cloud の Cloud Run + Cloud SQL（[ADR 0011](uta_docs/wiki/decisions/0011-deploy-google-cloud.md)）。
- マスタの列の型は `int` / `long` / `string` / `bool` / enum のみ。共有ドメインでは `float` / `double` を使わない。
- ID はマスタが `int`、サーバーが発行するものは ULID。時刻はサーバーが正で JST 固定、日付の更新は 12 時。乱数は `xoshiro256**`。強制アップデートはアプリのバージョンで判定（[repository-layout.md](uta_docs/wiki/architecture/repository-layout.md) 10章）。
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
- サーバー基盤と共通パッケージは別リポジトリを Git サブモジュールとして `external/` に取り込む（[ADR 0005](uta_docs/wiki/decisions/0005-foundation-submodule.md)、proposed）。登録済みは `VolTech-MagicOnion`・`Supplement`・`UnityScreenNavigator`・`MasterMemoryAddons`（`.gitmodules` が正）。開発は、サブモジュールの中でも別の clone でもよいが、**サブモジュール側を先に push してから Uta のポインタを更新する**
  - clone: `git clone --recurse-submodules <Uta の URL>`（済みの clone では `git submodule update --init`）
  - サブモジュール内で作業する前にブランチを切る（`git switch -c <branch>`）。detached HEAD のまま commit しない
  - 追加: `git submodule add <URL> external/<名前>`（すでに clone 済みなら、その場所を指定すれば取り込める）。追加したら ADR 0005 の表も更新する
  - ポインタ更新: サブモジュール側を push → Uta で `git add external/<名前>` → commit
- worktree を使うときは Uta の外に置く（例: `../Uta-wt/<ブランチ名>`）。サブモジュールは worktree ごとに `git submodule update --init` が必要

## ビルド・テスト・実行

プロジェクト（`server/`・`client/`）を作成したら、実際に動くコマンドをここに追記する。

- `shared/Uta.Domain` のビルド: `dotnet build shared/Uta.Domain`
- `shared/Uta.Domain` の単体テスト: `dotnet test --project shared/Uta.Domain.Tests`（.NET 10 SDK。実行方式はルートの `global.json` で Microsoft.Testing.Platform を指定）
- wiki の点検（Lint）: `python -I .claude/skills/linting-wiki/lint_wiki.py`（Python 3。エラーがあれば終了コード 1）

## 用語集

用語は `uta_docs/wiki/glossary.md` に集約する。
