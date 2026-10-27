---
title: リポジトリ構成・レイヤー・共通基盤（実装設計書 共通・基盤編）
status: draft
updated: 2026-10-27
---

# リポジトリ構成・レイヤー・共通基盤（実装設計書 共通・基盤編）

実装設計書（4編）の1編目。4編すべてが前提にする「どこに何を置くか」「何が何を参照してよいか」を定める。

- ゲームの仕様は [systems/](../systems/produce.md) が正で、ここには書かない
- サーバー基盤の共通機能（認証・冪等性・エラー・ヘッダー）は [server-foundation.md](server-foundation.md) が正で、ここでは繰り返さない

## 凡例

- **[決定]**: [CLAUDE.md](../../../CLAUDE.md)・accepted の ADR、または 2026-10-27 にユーザーが確認した方針（後者は [ADR 0007](../decisions/0007-masterdata-with-addons.md)〜[ADR 0010](../decisions/0010-database.md) に記録）
- **[Uta案]**: この叩き台の提案。採否はレビューと ADR で決める
- 推測は「推測」と明記する

## 1. 設計書の構成（4編）[決定]

| 編 | ページ | 対象 | 対応する主なコード |
|---|---|---|---|
| 共通・基盤 | 本ページ | 構成、依存ルール、共有コードの制約、機能ID | リポジトリ全体 |
| マスター設計 | [masterdata-pipeline.md](masterdata-pipeline.md) | マスタの定義・生成・配信 | `masterdata/`、各側の生成物 |
| API設計 | [api-design.md](api-design.md) | サービスIF・DTO、プレイヤーデータ、ゲームルールの置き場 | `shared/Uta.Shared`、`server/` |
| 画面設計 | [screen-design.md](screen-design.md) | 画面・遷移・クライアントの層 | `client/uta/` の Presentation |

- 進め方は [CLAUDE.md](../../../CLAUDE.md) の「実装前のルール」に従う（仕様 → 本編 → ADR → 実装 → ズレを反映 → `log.md`）
- **機能ID**: 仕様ページの ID（`PRD-xx` / `GAC-xx` / `IDL-xx` / `SUP-xx`）を、4編すべての見出し・表に付ける。層で分けた4編を、機能単位で横断して追うため。基盤の機能（認証など）は ID を付けず [server-foundation.md](server-foundation.md) を参照する

## 2. リポジトリ構成

[CLAUDE.md](../../../CLAUDE.md) の構成に、`shared/`・`server/`・`client/` の中身を足したもの。`masterdata/` の最上位への配置は [決定]（[ADR 0007](../decisions/0007-masterdata-with-addons.md)）。

```
uta/
├─ CLAUDE.md
├─ external/                 # [決定] サブモジュール（ADR 0005）
│  ├─ VolTech-MagicOnion/    #   サーバー基盤、Unity 側 Abstractions
│  ├─ MasterMemoryAddons/    #   マスタのコード生成・検証・バイナリ生成
│  ├─ Supplement/            #   Unity 補助パッケージ
│  └─ UnityScreenNavigator/  #   画面遷移
├─ docs/
├─ masterdata/               # [決定] マスタの正本。Unity にも shared/ にも入れない
│  ├─ Definitions/           #   全列入りの C# 定義（[ClientOnly]/[ServerOnly] 付き）
│  ├─ csv/                   #   スプレッドシートから書き出した CSV
│  └─ masterdata.yaml        #   出力先の設定
├─ shared/                   # [決定] Unity・サーバー共有。C# 9 の範囲（.editorconfig）
│  ├─ Uta.Domain/            # [Uta案] 純粋なゲームルール
│  └─ Uta.Shared/            # [Uta案] サービスIF・DTO（通信契約）
├─ server/                   # [決定] .NET 10
│  └─ Uta.Server/            # [Uta案] 1プロジェクト。フォルダで層を分ける
│     ├─ Api/                #   MagicOnion のサービス実装
│     ├─ Application/        #   ユースケース
│     ├─ Domain/             #   サーバー固有のルール（抽選、天井など）
│     ├─ Infrastructure/     #   DB、マスタの読み込み
│     └─ MasterData/Generated/   生成物（サーバー用）
└─ client/uta/               # [決定] Unity プロジェクトのルート
   └─ Assets/Scripts/
      ├─ Application/        # [Uta案] ユースケース、ポート
      ├─ Infrastructure/     #   API 呼び出し、マスタ、ローカル保存
      │  └─ MasterData/Generated/   生成物（クライアント用）
      ├─ Presentation/       #   View、Presenter
      └─ Bootstrap/          #   Composition Root
```

- **マスタ型を `shared/` に置かない**のが、ADR 0002 / CLAUDE.md からの変更点。サーバー専用の列・テーブルをクライアントから見えなくするため（[ADR 0007](../decisions/0007-masterdata-with-addons.md)）
- `shared/` の取り込み方（Unity: ローカルパッケージの `file:` 参照、サーバー: `Compile Include` または `ProjectReference`）は、基盤の「ソースで共有する」方式（[server-foundation.md](server-foundation.md)、基盤の設計書）に合わせる想定。サブモジュール外のパスを Unity から参照できるか、実機ビルドを含めて確認が必要（推測。ADR 0005 の未決事項と同じ）

## 3. アセンブリと依存 [決定／アセンブリ名は Uta案]

**どこで動くか・Unity と共有するかで分け、機能ごとには分けない**（MasterMemoryAddons の [構成ルール](../../../external/MasterMemoryAddons/docs/dev/structure.md)、基盤の設計書の「2つに分ける理由」と同じ考え方）。

| アセンブリ | 置き場 | 形式 | 参照してよいもの |
|---|---|---|---|
| `Uta.Domain` | `shared/Uta.Domain` | netstandard2.1、C# 9 | なし（BCL のみ） |
| `Uta.Shared` | `shared/Uta.Shared` | netstandard2.1、C# 9 | `Uta.Domain`、基盤の `Abstractions`、MagicOnion / MessagePack |
| `Uta.Server` | `server/Uta.Server` | net10.0 | `Uta.Domain`、`Uta.Shared`、基盤のサーバー側、生成したマスタ |
| クライアント `Application` | `Assets/Scripts/Application` | asmdef | `Uta.Domain`、UniTask |
| クライアント `Infrastructure` | `Assets/Scripts/Infrastructure` | asmdef | `Application`、`Uta.Domain`、`Uta.Shared`、基盤のクライアント側、MasterMemory と生成物、`Supplement` |
| クライアント `Presentation` | `Assets/Scripts/Presentation` | asmdef | `Application`、`Uta.Domain`、`UnityScreenNavigator`、UniTask |
| クライアント `Bootstrap` | `Assets/Scripts/Bootstrap` | asmdef | すべて |

- サーバーを1プロジェクトにするのは基盤と同じ理由（プロジェクトを細かく分けると `internal` が使えず、依存も切れない）。層の分離は**フォルダと名前空間**で行い、依存の向きは**アーキテクチャテスト**で守る（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)。ツールは基盤の設計書が挙げる NetArchTest を想定）
- クライアントは、コンパイル時に依存を強制できる手段が asmdef しかないため、層ごとに asmdef を分ける
- DI は VContainer、非同期処理は UniTask [決定。[ADR 0009](../decisions/0009-client-stack.md)]

## 4. 層の依存ルール [決定。[ADR 0008](../decisions/0008-shared-domain-and-onion.md)]

依存は**常に内側へ**。`Domain` → `Application` → `Infrastructure` / `Presentation` → `Bootstrap`。

| 層 | 責務 | 禁止 |
|---|---|---|
| Domain | ゲームルール（計算式、状態遷移、検証）、値オブジェクト、enum、ID 型 | UnityEngine、MasterMemory とその生成物、MagicOnion、DB、`float` / `double` |
| Application | ユースケース。外部とのやりとりは**ポート（インターフェース）**で定義する。UniTask は使ってよい | UnityEngine の API（asmdef では強制できないため、レビューまたはアナライザーで守る）、Infrastructure・Presentation・`Uta.Shared` への参照 |
| Infrastructure | ポートの実装（API 呼び出し、マスタ読み込み、ローカル保存）。DTO ↔ Domain のマッピングはここで行う | Presentation への参照 |
| Presentation | View（表示）と Presenter（Application を呼ぶ） | Infrastructure・`Uta.Shared`（通信 DTO）への参照 |
| Bootstrap | 起動、DI の組み立て（Composition Root）。すべてを参照してよい | — |

- **DTO ↔ Domain のマッピングは Infrastructure に閉じる** [決定。[ADR 0008](../decisions/0008-shared-domain-and-onion.md)]。DTO は enum とプリミティブ型で組み、マッピングを薄く保つ
- マスタの生成物（`MemoryDatabase`）は Infrastructure に置き、Application へは**ポート越しに必要な値だけ**を渡す。`Domain` / `Application` をマスタの形式から独立させるため

## 5. 共有コード（`shared/`）の制約 [決定／一部 Uta案]

既存のルール（[CLAUDE.md](../../../CLAUDE.md)、`.editorconfig`）:

- C# 9 の範囲（ファイルスコープ namespace、プライマリコンストラクタ、コレクション式、Index/Range は使わない）
- リフレクション依存を避ける（iOS の IL2CPP / AOT）

Uta で足す制約 [決定。[ADR 0008](../decisions/0008-shared-domain-and-onion.md)]:

- UnityEngine に依存しない。MasterMemory・生成物にも依存しない（サーバーから参照できなくなる）
- **`float` / `double` を使わない**。整数または固定小数点（[lesson-card.md](../systems/lesson-card.md) 9.6）。クライアント（IL2CPP）とサーバー（.NET 10）で結果を一致させるため
- **乱数は `IRandom` を注入し、決定的な疑似乱数 `xoshiro256**`** を使う（10.3）。`System.Random` は使わない（[lesson-card.md](../systems/lesson-card.md) 13章）。シードの生成はサーバー
- `[ServerOnly]` の列に依存するルールは `Domain` に入れない。必要な値は引数で受け取る
- `init` を使う場合は `IsExternalInit` の定義が必要（MasterMemoryAddons の docs で確認。置き方は、各アセンブリに `internal` で置く案。推測・要確認）
- `record`・`required`・nullable の値型（`int?` など）は、マスタの生成ソースでは使えない（アドオンの制約）。`shared/` でも同じ制約にそろえる [Uta案]

## 6. Domain の共有範囲 [決定]

`Uta.Domain` の主な中身は、[lesson-card.md](../systems/lesson-card.md) の効果エンジン（ゾーンの移動、ターン処理順、計算式）と、その状態遷移・使用条件の判定。

| 場所 | 使い方 |
|---|---|
| サーバー | 結果を**確定**する（権威） |
| クライアント | 同じルールで、使用可否の判定と予測の表示まで行う。**シードは持たない**ので、乱数が絡む結果（ドローなど）は予測しない |

- 範囲は [ADR 0008](../decisions/0008-shared-domain-and-onion.md) で決定（2026-10-27）。これに合わせて [CLAUDE.md](../../../CLAUDE.md) の実装方針と [produce.md](../systems/produce.md) 10章を更新した
- 結果の先行表示（シードをクライアントに渡す）は、山札の引きを先読みする不正を防げないため行わない
- **古いクライアントの計算式が、サーバーの最新の式とずれることがある**。予測の表示はずれてもよく、確定は必ずサーバーのレスポンスで行う。ずれが問題になる変更（ルールの変更）は、強制アップデート（基盤の `x-app-version`）で扱う
- [lesson-card.md](../systems/lesson-card.md) 15.7 の「同じシードと操作列で、同じ結果になる」は、**`Uta.Domain` の単体テスト（.NET 10）**で確かめる（10.4）。本番のクライアントはシードを持たないので、IL2CPP 上で乱数の結果を再現する必要はない。乱数が絡まない予測の一致は、整数演算と同じコードを使うことで担保する

### 6.1 固有の知識の置き場 [決定。[ADR 0008](../decisions/0008-shared-domain-and-onion.md)]

クライアントまたはサーバーだけが持つ知識は、**共通の `Uta.Domain` に入れず、各側の外側の層に置く**。依存は内向きなので、共通の層は固有の層を知らない。固有の層は `Uta.Domain` を参照してよく、逆は禁止。

| 種類 | 置き場 | 例 |
|---|---|---|
| 共通ルール | `shared/Uta.Domain` | カード効果、ターン処理順、計算式、使用可否の判定 |
| サーバー固有 | `server/Uta.Server/Domain/` | ガチャ抽選、交換pt・天井、シード生成、レンタル候補の決定、報酬付与 |
| クライアント固有 | クライアントの `Application` / `Presentation` | 表示用の整形、並び順、演出の制御、UI の状態、ローカル設定 |

置き場の判断基準（上から順に当てはめる）:

1. クライアントに渡してはいけない知識（秘匿）→ サーバー固有
2. 結果が両側で一致しなければならないルール → 共通
3. 表示にしか関係しない → クライアント固有

共通と固有が混ざるときの作法:

- 値やルールは共通の型で持ち、固有の属性は各側のエンティティが持つ（共通の型を包む＝合成）。例: サーバーのプロデュースのセッションは、共通の状態の型に、シードや乱数の位置を足して持つ
- 継承や `partial` は、アセンブリをまたげないので使わない。合成と拡張メソッドを使う
- 側ごとに実装が変わる部分は、インターフェースで差し替える（`IRandom`、検証のポリシーなど）
- `[ServerOnly]` のマスタの値は、共通のドメインに入れず、引数で受け取る（5章）

**移動のサイン**: 「クライアント固有」に置いたルールが、結果に影響し始めたら、共通へ移す。別々に実装すると食い違うため。

## 7. ビルド・CI（未実装）

CLAUDE.md のとおり、実際に動くコマンドはまだない。作ったら CLAUDE.md にも追記する。入れたい検証:

- `master-gen --check`（CSV の検証と、生成ソースが古くないかの検出）
- マスタのバイナリを `MemoryDatabase` で読めること（クライアント用・サーバー用の両方）
- `Uta.Domain` の単体テスト（同じシードと操作列で同じ結果になること）と、サーバーの結合テスト（10.4）
- サーバーの依存方向のアーキテクチャテスト
- `git clone --recurse-submodules` での初期化（CI も同様）
- クライアントの asmdef の参照が、4章の依存ルールに沿っているか（`Application` が UnityEngine の API を使っていないか。手段は未決）
- Windows / Android / iOS の実機ビルド（早期・継続的に）

## 8. 実装前に確かめること（要検証）

決定の前提になっているが、まだ動作を確かめていないもの。確かめたら結果を該当ページに書き、ここから消す。

| 項目 | 関係するページ | 確かめ方 |
|---|---|---|
| `shared/` を Unity からローカルパッケージ（`file:` 参照）で取り込めるか | 2章、[ADR 0005](../decisions/0005-foundation-submodule.md) | Windows / Android / iOS の実機ビルド |
| MasterMemoryAddons のバイナリを Unity で読めるか | [masterdata-pipeline.md](masterdata-pipeline.md) 6章 | 実機で `MasterDataBinary.Read` → `MemoryDatabase` |
| MasterMemory・MessagePack・MagicOnion のバージョンが揃うか | [masterdata-pipeline.md](masterdata-pipeline.md) 7章 | サーバーとクライアントでのビルド |
| `[Exists]` が、別の側にだけあるテーブル・列を参照した場合の挙動 | [masterdata-pipeline.md](masterdata-pipeline.md) 4章 | アドオンのテストまたはサンプル |
| UnityScreenNavigator・VContainer・UniTask が Unity 7（`7000.0.0a12`）で動くか | [ADR 0009](../decisions/0009-client-stack.md) | 実機ビルド |
| `IsExternalInit` の置き方（5章） | 5章 | Unity と .NET 10 の両方でのビルド |
| `Uta.Domain` の計算が IL2CPP と .NET 10 で一致するか | 6章、10.4 | 任意。実機で予測値とサーバーの結果を見比べる（Unity 側のテストは必須にしない） |

## 9. 共通規約の参照先

| 項目 | 参照先 |
|---|---|
| エラー、ヘッダー、認証、冪等性、トランザクション、フィルター順 | [server-foundation.md](server-foundation.md) |
| コード規約、言語バージョン、Git 運用 | [CLAUDE.md](../../../CLAUDE.md)、`.editorconfig` |
| サブモジュールの扱い | [ADR 0005](../decisions/0005-foundation-submodule.md) |
| マスタの定義・生成・配信 | [masterdata-pipeline.md](masterdata-pipeline.md) |
| 用語 | [glossary.md](../glossary.md) |

## 10. 共通の規約 [決定]

2026-10-27 に決めた、全編が前提にする規約。

### 10.1 ID の体系

| 対象 | 型 | 備考 |
|---|---|---|
| マスタの ID（カード、アイドル、ガチャなど） | `int` | マスタの列の型の制約（[masterdata-pipeline.md](masterdata-pipeline.md)）に合う。**採番の規則は作らない**（2026-10-27 決定） |
| サーバーが発行する ID（ユーザー、プロデュースのセッション、ガチャ履歴など） | ULID | 時刻順に並び、サーバーを分けても衝突しない |

- ULID の C# の型と、DB・DTO での持ち方（PostgreSQL の `uuid` に入れるか、文字列にするか。DTO では文字列か 16 バイトか）は未決。Unity と .NET の両方で使えるライブラリ（Cysharp の `Ulid` など）を使う案（推測）
- ULID を `Uta.Domain` が扱う場合は、`Uta.Domain` が外部ライブラリに依存しないよう、値オブジェクトで包むか、ID を持たない設計にする（未決）

### 10.2 時刻

- **時刻はサーバーが正**。クライアントの時刻で判定しない
- **タイムゾーンは JST 固定**
- **日付の更新は 12 時**（12:00 JST）。日ごとの区切り（日替わりの処理など）はこの時刻を境にする
- **マスタの日時は `string` で持つ**。形式は `yyyy-MM-dd HH:mm:ss` で、JST として解釈する（オフセットは書かない。例: `2026-11-01 12:00:00`）。読むときは `DateTime.ParseExact`（`CultureInfo.InvariantCulture`）で厳密に読み、JST の `DateTimeOffset` にする（[masterdata-pipeline.md](masterdata-pipeline.md) 3章）
- ガチャの期間終了時の、交換pt から交換メダルへの変換（[gacha.md](../systems/gacha.md)）も、サーバーの時刻で行う

### 10.3 乱数

- **アルゴリズムは `xoshiro256**`**（64 ビット。xorshift の後継）
- 実装は `Uta.Domain` に置き、サーバーで使う。範囲内の整数の取り出し方も、同じコードで固定する
- シードはサーバーが生成する（暗号用の乱数を使う。推測）。シードと乱数の位置はプロデュースのセッションに保存する（[api-design.md](api-design.md) 3章）
- `System.Random` は使わない

### 10.4 テスト

- **単体テストは専用のプロジェクト（dll）に分ける**。主な対象は `Uta.Domain`（同じシードと操作列で同じ結果になること、計算式）
- **サーバーは結合テストを書く**（DB を含めて、RPC から結果・保存までを確かめる）
- **Unity 側のテストは必須にしない**
- **フレームワークは xUnit v3（`xunit.v3`）**。最新を触る目的で、外部のリポジトリ（VolTech-MagicOnion・MasterMemoryAddons。いずれも xUnit v2 系）より新しい版を使う。版は実装時に NuGet で最新の安定版を確認して固定する
- **サーバーの結合テストは Testcontainers** で、テストのたびに Docker で PostgreSQL を立ち上げる。本番と同じ DB の挙動で確かめる（Docker が要る）
- テストプロジェクトの置き場は未決

### 10.5 バージョンと強制アップデート

- **強制アップデートは、アプリのバージョンで判定する**（基盤の `x-app-version`。[server-foundation.md](server-foundation.md)）
- マスタのスキーマ（定義）を変えたときや、`Uta.Domain` のルールを変えたときも、新しいアプリのバージョンを出して強制アップデートで揃える。マスタはいまアプリに同梱するので、スキーマの変更はアプリの更新と一緒になる（[masterdata-pipeline.md](masterdata-pipeline.md) 6章）

### 10.6 クライアントのローカル保存

- **共通基盤（`external/Supplement`）のセーブデータの仕組みを使う**。`IFileStorageService` / `SaveDataRepository`（AES で暗号化）。MessagePack 版（`Supplement.MessagePack`）は改ざん・破損を検知できる（[Supplement の docs](../../../external/Supplement/docs/features/save-data.md)）
- 何を保存するか（認証トークン、設定など）と、JSON 版と MessagePack 版のどちらを使うかは、**実装するときに決める**

## 未決事項

- `Uta.Shared` と基盤の `Abstractions` の役割分担（ADR 0005 の未決事項）。`Uta.Shared` は Uta 固有のサービスIF・DTOだけ、が素直（推測）
- `Application` で UnityEngine の API を使わないことを守る手段（アナライザーにするか、レビューだけにするか）
- サーバーのアーキテクチャテストの具体（NetArchTest など）
- ULID の持ち方（10.1）
- テストプロジェクトの置き場（10.4）
- ビルド・CI の実体（`architecture/build-ci.md` として起こす予定）
