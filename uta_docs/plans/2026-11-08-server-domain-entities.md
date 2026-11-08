---
title: 実装計画: サーバーのエンティティとリポジトリの IF を Domain に移す（ADR 0021 のサーバー側）
status: ready
updated: 2026-11-08
spec: ../wiki/decisions/0021-domain-entities-and-repositories.md, ../wiki/architecture/naming.md, ../wiki/architecture/repository-layout.md
---

# 実装計画: サーバーのエンティティとリポジトリの IF を Domain に移す

## この計画で決めたこと

- **範囲は [ADR 0021](../wiki/decisions/0021-domain-entities-and-repositories.md) の「サーバー」の節だけ**。リポジトリの IF 5 つと、そのシグネチャに出る型を `server/Uta.Server/Domain/` に移す。エンティティを改名し、`ListAsync` を `FindAllAsync` にする。IF のメソッドの形（SQL の形）は変えない
- **改名**（[naming.md](../wiki/architecture/naming.md)「今のコードとの差」の行）: `OwnedIdol` → `IdolEntity`、`OwnedSupportCard` → `SupportCardEntity`、`PlayerSnapshot` → `PlayerEntity`、`GachaHistoryRow` → `GachaHistoryRecord`、`IIdolRepository.ListAsync`・`ISupportCardRepository.ListAsync` → `FindAllAsync`
- **エンティティにするのは、クライアントにも同じエンティティがある所持データ（プレイヤー・育成アイドル・サポートカード）だけ。それ以外の DB の行の写しは Record にする**（ADR 0021・naming.md 5章の未決事項を決める）[決定。2026-11-08。ユーザーの判断]。`GachaHistoryRow` は `GachaHistoryRecord` に改名し、`ProduceSessionRecord`・`ProduceResultRecord` は名前を変えない。理由: 履歴・結果・セッションはサーバーだけが持ち、クライアントのリポジトリや player_diff と対応しない。振る舞いも持たず、更新の正しさは 1 文の SQL（条件付きの更新・楽観ロック）が守っている
- **`ProduceSessionData` は名前を変えない**（DB の行の写しではなく `state` 列の中身なので、上の区分の対象外。naming.md 5章に例外として書く）[決定。2026-11-08。ユーザーの判断]
- **サーバーの `PlayerEntity` に `UniqueId` を足さない**。改名だけで、項目は今の 5 つ（Level・Exp・Gold・SupportPoints・GachaCurrency）のまま [決定。2026-11-08。ユーザーの判断]。理由: 呼ぶ側は `playerId` を既に持っていて、読む場所がない
- サーバーの `UniqueId` は `Ulid` 型のまま（naming.md 2章「Contracts の ID」）。UseCase の名前もそのまま（naming.md 2章）
- `LevelUpCost`・`InitialGrant`・`GachaDefinition` など、Catalog の IF のシグネチャにだけ出る型は Application に残す（リポジトリの IF に出ないので ADR 0021 の対象外。Catalog は ADR 0020 の計画でやめる）
- ファイルの分け方: interface は 1 つずつのファイルにする。同じ主題の小さな record は 1 つのファイルに同居してよい（`ProduceSessionData.cs` の 3 つ）。interface と record は同じファイルに置かない（`IPlayerRepository.cs` の末尾の `PlayerBalance` は `PlayerBalance.cs` に出す）。`Application/Produce/ProduceReward.cs` に残る `IProduceRewardCatalog`・`IPlayerLevelCatalog` は、ファイルを型ごとに分ける
- **順番**: 実装は、[共通レスポンスの計画](2026-11-08-common-response-player-diff.md)の実装の PR（`claude/common-response-player-diff`。サーバーのタスクは実装済み）が `main` に入ってから `main` で始める。入る前に始めるなら、そのブランチから積む（CLAUDE.md の積み PR）。どちらでも、`Infrastructure/PlayerDiffCollector.cs` があるものとして書く。ADR 0020 のサーバー側の計画は、この計画の実装が `main` に入ってから切る（同じファイルを触るため）

## ユーザーの確認が要ること

なし（上の [決定] は 2026-11-08 に確認済み）。

## 1. 目的と前提

ADR 0021 はサーバーのリポジトリの IF とエンティティを Domain に置くと決めたが、「サーバーのコードはサーバー側の実装計画まで今の形のまま」とした（[ADR 0022](../wiki/decisions/0022-client-external-concretes-in-infrastructure.md)「accepted にする時点と、進めてよいこと」）。この計画がその実装計画。

- 関係する ADR: [0021](../wiki/decisions/0021-domain-entities-and-repositories.md)（本体）、[0008](../wiki/decisions/0008-shared-domain-and-onion.md)（層）、[0010](../wiki/decisions/0010-database.md)（楽観ロック）
- 前提: [共通レスポンスの計画](2026-11-08-common-response-player-diff.md)の実装（「この計画で決めたこと」の順番）。あちらの `Infrastructure/PlayerDiffCollector.cs` は、リポジトリの IF 3 つを受け、`idols.ListAsync` を呼び、`using Uta.Server.Application.Idol;`・`.Player;`・`.Support;` を書いている（`claude/common-response-player-diff` の `ad8cc0f`）。この計画で一緒に直す

### 決めないこと（範囲外）

- ADR 0020 のサーバー側（`I<名詞>Catalog`・`Master<名詞>Catalog`・`GachaDefinition` などの廃止、`MasterData_IsReferencedOnlyByInfrastructure` の変更）。別の計画にする
- naming.md「今のコードとの差」のうち、上の改名以外の行（`GachaStatus`・`ProduceView`・`ProduceEndSummary`・`GrantOutcome`・`UtaServiceAttribute` など）
- 2026-11-03 の計画（`claude/domain-model-naming`。main に入っていない）の決定のうち、11-08 の決定とぶつかるもの（UseCase → Service の改名、サーバーの ULID を `string` にすること、所持品の型を Shared.Domain に置くこと）。どれも naming.md・ADR 0021 に置き換わったので引き継がない。同じ計画の「Lv を経験値から計算する」などの `PlayerEntity` の後の論点も、この計画では扱わない

## 2. 触るファイル

移動は `git mv`（履歴を残す）。移動したファイルは、名前空間を移動先のフォルダに合わせる（`Uta.Server.Domain.<フォルダ>`）。

| 操作 | 今 | 移動先・中身 |
|---|---|---|
| 移動 | `server/Uta.Server/Application/Player/IPlayerRepository.cs` | `server/Uta.Server/Domain/Player/IPlayerRepository.cs`。末尾の `PlayerBalance` は下の新しいファイルへ出す。`PlayerSnapshot` → `PlayerEntity` |
| 作成 | — | `server/Uta.Server/Domain/Player/PlayerBalance.cs`（`IPlayerRepository.cs` の末尾から移す。中身は同じ） |
| 移動・改名 | `server/Uta.Server/Application/Player/PlayerSnapshot.cs` | `server/Uta.Server/Domain/Player/PlayerEntity.cs`。`record PlayerSnapshot` → `record PlayerEntity`（項目は同じ） |
| 移動 | `server/Uta.Server/Application/Idol/IIdolRepository.cs` | `server/Uta.Server/Domain/Idol/IIdolRepository.cs`。`ListAsync` → `FindAllAsync`、`OwnedIdol` → `IdolEntity` |
| 移動・改名 | `server/Uta.Server/Application/Idol/OwnedIdol.cs` | `server/Uta.Server/Domain/Idol/IdolEntity.cs`。`record OwnedIdol` → `record IdolEntity` |
| 移動 | `server/Uta.Server/Application/Support/ISupportCardRepository.cs` | `server/Uta.Server/Domain/Support/ISupportCardRepository.cs`。`ListAsync` → `FindAllAsync`、`OwnedSupportCard` → `SupportCardEntity` |
| 移動・改名 | `server/Uta.Server/Application/Support/OwnedSupportCard.cs` | `server/Uta.Server/Domain/Support/SupportCardEntity.cs`。`record OwnedSupportCard` → `record SupportCardEntity` |
| 移動 | `server/Uta.Server/Application/Gacha/IGachaRepository.cs` | `server/Uta.Server/Domain/Gacha/IGachaRepository.cs`。`GachaHistoryRow` → `GachaHistoryRecord` |
| 移動・改名 | `server/Uta.Server/Application/Gacha/GachaHistoryRow.cs` | `server/Uta.Server/Domain/Gacha/GachaHistoryRecord.cs`。`record GachaHistoryRow` → `record GachaHistoryRecord` |
| 移動 | `server/Uta.Server/Application/Produce/IProduceSessionRepository.cs` | `server/Uta.Server/Domain/Produce/IProduceSessionRepository.cs` |
| 移動 | `server/Uta.Server/Application/Produce/ProduceSessionData.cs` | `server/Uta.Server/Domain/Produce/ProduceSessionData.cs`（3 つの record。名前は同じ） |
| 移動 | `server/Uta.Server/Application/Produce/ProduceSessionStatus.cs` | `server/Uta.Server/Domain/Produce/ProduceSessionStatus.cs` |
| 移動 | `server/Uta.Server/Application/Produce/ProduceReward.cs` | `server/Uta.Server/Domain/Produce/ProduceReward.cs`。`record ProduceReward` だけを残す |
| 作成 | — | `server/Uta.Server/Application/Produce/IProduceRewardCatalog.cs`・`IPlayerLevelCatalog.cs`（旧 `ProduceReward.cs` の 2 つの interface を 1 つずつ。名前空間は `Uta.Server.Application.Produce` のまま、中身・XML ドキュメントは同じ） |
| 変更 | `server/Uta.Server/Application/**/*.cs`（下の一覧） | 型名の置き換えと `using` |
| 変更 | `server/Uta.Server/Infrastructure/{Player,Idol,SupportCard,Gacha,ProduceSession}Repository.cs`、`Infrastructure/Produce/ProduceStateSerializer.cs`、`Infrastructure/MasterData/MasterProduceRewardCatalog.cs` | 型名、`ListAsync` → `FindAllAsync`、`using` |
| 変更 | `server/Uta.Server/Infrastructure/PlayerDiffCollector.cs`（共通レスポンスの計画が作る） | `idols.ListAsync` → `idols.FindAllAsync`、型名、`using`（`Uta.Server.Application.Idol`・`.Player`・`.Support` を `Uta.Server.Domain.Idol`・`.Player`・`.Support` に） |
| 変更 | `server/Uta.Server/Program.cs` | `using`（DI の登録の型名は変わらない） |
| 変更 | `server/Uta.Server.Tests/Integration/ProduceOperationTests.cs`、`Infrastructure/ProduceStateSerializerTests.cs`、`MasterData/MasterProduceRewardCatalogTests.cs` | `using` |
| 変更 | wiki（タスク 2） | メインのセッションが直す |

`Application/Idol/` は空になるので、フォルダごと無くなる（名前空間 `Uta.Server.Application.Idol` も無くなる）。新しく作る 3 ファイル（`PlayerBalance.cs`・`IProduceRewardCatalog.cs`・`IPlayerLevelCatalog.cs`）は、ほかの `.cs` と同じく BOM 付き UTF-8・改行 LF にする。

変えないファイル: `Api/*.cs`（移す型の名前を書いていない）、`Infrastructure/MasterData/MasterSupportCatalog.cs`・`MasterInitialGrantCatalog.cs` と `server/Uta.Server.Tests/MasterData/MasterSupportCatalogTests.cs`（`ISupportCatalog`・`IInitialGrantCatalog` は Application に残るので、`using Uta.Server.Application.Support;`・`.Player;` はそのまま）。

型名と `using` を直す Application のファイル（今の main の Grep の結果。`ListAsync` を呼ぶのは `SignInUseCase` だけ）:

- `Application/Player/`: `SignInResult.cs`（`PlayerSnapshot`・`OwnedIdol`・`OwnedSupportCard`）、`SignInUseCase.cs`（`ListAsync` 2 か所）、`SignUpUseCase.cs`
- `Application/Gacha/`: `DrawUseCase.cs`・`ExchangeUseCase.cs`（`GachaHistoryRow`）、`GetGachasUseCase.cs`、`ItemGranter.cs`
- `Application/Support/`: `LevelUpUseCase.cs`、`LimitBreakUseCase.cs`
- `Application/Produce/`: `StartProduceUseCase.cs`（`OwnedSupportCard`）、`GetProduceStateUseCase.cs`、`AbandonProduceUseCase.cs`、`ProduceOperationUseCase.cs`、`ProduceCompletion.cs`、`ProduceSessionDiscarder.cs`、`ProduceVersions.cs`、`ProduceView.cs`

## 3. 型とシグネチャ

型の中身（record の項目・順番・XML ドキュメント・メソッドのシグネチャ）は、改名のほかは変えない。

| 型 | 新しい名前空間・ファイル | 変えること |
|---|---|---|
| `IPlayerRepository` | `Uta.Server.Domain.Player` / `Domain/Player/IPlayerRepository.cs` | `FindAsync` の戻り値 `PlayerSnapshot?` → `PlayerEntity?`。`using Uta.Server.Application.Produce;` → `using Uta.Server.Domain.Produce;`（`ProduceReward`） |
| `PlayerEntity`（旧 `PlayerSnapshot`） | `Uta.Server.Domain.Player` / `PlayerEntity.cs` | `public sealed record PlayerEntity(int Level, int Exp, int Gold, int SupportPoints, int GachaCurrency);` |
| `PlayerBalance` | `Uta.Server.Domain.Player` / `PlayerBalance.cs` | なし |
| `IIdolRepository` | `Uta.Server.Domain.Idol` / `Domain/Idol/IIdolRepository.cs` | `Task<IReadOnlyList<IdolEntity>> FindAllAsync(TxContext tx, Ulid playerId);`（`/// <summary>idol_id の昇順。</summary>` は残す） |
| `IdolEntity`（旧 `OwnedIdol`） | `Uta.Server.Domain.Idol` / `IdolEntity.cs` | `public sealed record IdolEntity(Ulid UniqueId, int MasterId, int Training, int Awakening, int Shards);` |
| `ISupportCardRepository` | `Uta.Server.Domain.Support` / `Domain/Support/ISupportCardRepository.cs` | `Task<IReadOnlyList<SupportCardEntity>> FindAllAsync(TxContext tx, Ulid playerId);`、`Task<SupportCardEntity?> FindAsync(...)` |
| `SupportCardEntity`（旧 `OwnedSupportCard`） | `Uta.Server.Domain.Support` / `SupportCardEntity.cs` | `public sealed record SupportCardEntity(Ulid UniqueId, int MasterId, int Level, int LimitBreak);` |
| `IGachaRepository` | `Uta.Server.Domain.Gacha` / `Domain/Gacha/IGachaRepository.cs` | `Task AddHistoryAsync(TxContext tx, GachaHistoryRecord row);`（引数名 `row` は変えない） |
| `GachaHistoryRecord`（旧 `GachaHistoryRow`） | `Uta.Server.Domain.Gacha` / `GachaHistoryRecord.cs` | 名前だけ |
| `IProduceSessionRepository` | `Uta.Server.Domain.Produce` / `IProduceSessionRepository.cs` | なし |
| `ProduceSessionData`・`ProduceSessionRecord`・`ProduceResultRecord` | `Uta.Server.Domain.Produce` / `ProduceSessionData.cs` | なし。`using Uta.Server.Domain.Produce;` は同じ名前空間になるので消す |
| `ProduceSessionStatus` | `Uta.Server.Domain.Produce` / `ProduceSessionStatus.cs` | なし |
| `ProduceReward` | `Uta.Server.Domain.Produce` / `ProduceReward.cs` | なし（ファイルには record だけ。`using Uta.Shared.Domain.Produce;` は使わなくなるので消す） |
| `IProduceRewardCatalog`・`IPlayerLevelCatalog` | `Uta.Server.Application.Produce`（そのまま）/ 型ごとのファイル | なし。`IProduceRewardCatalog.cs` には `using Uta.Server.Domain.Produce;`（`ProduceReward`）と `using Uta.Shared.Domain.Produce;`（`ProduceRank`）を書く |

- 依存: Domain のリポジトリの IF は `VolTech.Server.Transaction.TxContext` を引数に使う（ADR 0021 で許可。`using VolTech.Server.Transaction;` は移したファイルに今あるまま）。Domain から `TxContext.Connection` に触れるコードは書かない（ADR 0021。触れるのは Infrastructure だけ）
- 移す型は、どれも Application の型を参照しない（参照先は `Ulid`・`TxContext`・Domain の `RandomStreamState`・Shared.Domain の `ProduceSnapshot`・`ExamOutcome`・`ProduceRank`）。`ArchitectureTests.Domain_DoesNotDependOnOuterLayersOrDataAccess` は変えずに通る
- `using` の直し方: 型を参照しているファイルに、移動先の名前空間の `using`（`Uta.Server.Domain.Player`・`.Idol`・`.Support`・`.Gacha`・`.Produce` のうち使うもの）を足す。`Uta.Server.Application.Idol` を指す `using` は消す（名前空間が無くなる）。ほかの `Uta.Server.Application.*` の `using` は、そのファイルがまだその名前空間の型を使っていれば残し、使わなくなったら消す（`.editorconfig` は使わない `using`（IDE0005）を警告にしていないので、ビルドでは見つからない。移した型だけのために書かれていた `using` を、ファイルの中の型名で確かめて消す）。`using` は 1 行目の BOM の直後から並んでいるので、1 行目も見る
- アーキテクチャテストは足さない（ADR 0021 の「`TxContext.Connection` に触れない」は NetArchTest で検査できないので、コードレビューで守る）

## 4. タスク

### タスク 1: 型とリポジトリの IF を Domain に移し、改名する（実装担当）

触るファイル: 2章の表のうち wiki 以外のすべて。

手順:

1. 2章の表の「移動」「移動・改名」を `git mv` で行う。行き先のフォルダ `Domain/Player/`・`Domain/Idol/` は新しく作る
2. 移したファイルの `namespace` を 3章の表のとおりにし、型名を改名する。`PlayerBalance` を `PlayerBalance.cs` に出す。旧 `ProduceReward.cs` から 2 つの interface を `Application/Produce/IProduceRewardCatalog.cs`・`IPlayerLevelCatalog.cs` に出す
3. サーバーとテストのすべての `.cs` で、旧い型名を新しい名前に置き換える: `PlayerSnapshot` → `PlayerEntity`、`OwnedIdol` → `IdolEntity`、`OwnedSupportCard` → `SupportCardEntity`、`GachaHistoryRow` → `GachaHistoryRecord`。**単語で一致するものだけ**を置き換える（Contracts の `OwnedIdolDto`・`OwnedSupportCardDto` は変えない）
4. `IIdolRepository`・`ISupportCardRepository`・`IdolRepository`・`SupportCardRepository` の `ListAsync` を `FindAllAsync` にし、`SignInUseCase` の 2 か所と `PlayerDiffCollector` の 1 か所の呼び出しを合わせる。テストに `ListAsync` の呼び出しは無い
5. 3章の「`using` の直し方」のとおりに `using` を直し、`dotnet build Uta.slnx` が警告 0・エラー 0 になるまで繰り返す
6. ローカル変数名・引数名（`row`・`player`・`card` など）、SQL、DB の列名は変えない

境界の扱い: 振る舞いは変えない（型の置き場と名前だけ）。テストを足さない。

確かめ方:

- `dotnet build Uta.slnx` が警告 0・エラー 0
- 次の Grep が 0 件（`obj/`・`bin/` を除く）:

  ```bash
  grep -rnwE "PlayerSnapshot|OwnedIdol|OwnedSupportCard|GachaHistoryRow|ListAsync" server --include=*.cs | grep -vE "/(obj|bin)/"
  grep -rn "Uta.Server.Application.Idol" server --include=*.cs | grep -vE "/(obj|bin)/"
  ```

- 次の Grep で、リポジトリの IF 5 つがすべて `server/Uta.Server/Domain/` の下にあること:

  ```bash
  grep -rln "interface I\(Player\|Idol\|SupportCard\|Gacha\|ProduceSession\)Repository" server/Uta.Server
  ```

- `dotnet test --project server/Uta.Server.Tests` が通る（アーキテクチャテストと結合テスト。結合テストは Docker が要る。Docker が使えないときは、通ったと書かず、そう報告する）

### タスク 2: 設計書を直す（メインのセッション）

コードと同じ PR に入れる。直す箇所:

- [ADR 0021](../wiki/decisions/0021-domain-entities-and-repositories.md): 先頭の注記に 1 行足す（サーバー側をこの計画で実装したこと、「影響・リスク」の Record かエンティティかの未決を、naming.md 5章と同じ区分で決めたこと（`ProduceSessionRecord`・`ProduceResultRecord`・`GachaHistoryRecord` は Record、`ProduceSessionData` は `state` 列の中身なので区分の対象外で名前はそのまま）。本文は書き換えない）
- [naming.md](../wiki/architecture/naming.md): 5章の「DB の 1 行の写しをエンティティにするか…」を、「この計画で決めたこと」の区分（エンティティはクライアントにも同じエンティティがある所持データだけ、ほかの行の写しは `<名詞>Record`。`ProduceSessionData` は `state` 列の中身で対象外、名前はそのまま。2026-11-08。ユーザーの判断）に書き換える。2章の「エンティティの ID」の行に、サーバーのエンティティは `UniqueId` を `Ulid` で持ち、サーバーの `PlayerEntity` は `UniqueId` を持たない（2026-11-08。ユーザーの判断）と足す。2章の「Contracts の ID」の行の「サーバーの所持品の型 `OwnedIdol`・`OwnedSupportCard`」（本文と置き場の欄の 2 か所）を `IdolEntity`・`SupportCardEntity` にする。「今のコードとの差」から `GachaHistoryRow`・`ListAsync`・`OwnedIdol`… の 3 行を消す。「未決事項」から同じ項目を消す。先頭の「サーバーに関わる規則は、サーバー側の実装計画までサーバーのコードは今のまま」を、ADR 0021 の分は済んだ（ADR 0020 の分は残る）と直す
- [repository-layout.md](../wiki/architecture/repository-layout.md): 2章の木の `Domain/` の注釈に「エンティティとリポジトリの IF（ADR 0021）」を足す。3章の Server.Domain の行の「未作成」を消す。3章の依存の表の `Application` の行の「ポートの引数に使う」を「リポジトリの IF の引数として受け渡す」に、`Domain` の行の「参照してよいもの」に `TxContext`（リポジトリの IF の引数。`Connection` に触れない）を、`Infrastructure` の行の「`Application`（ポートを実装する）」を「`Application`、`Domain`（リポジトリの IF を実装する）」にする。6.1 の表のサーバー固有の行の例に「エンティティ・リポジトリの IF」を足す。Domain の「参照してよいもの」に生成したマスタは足さない（ADR 0021「影響・リスク」が挙げるが、ADR 0020 の計画で足す）。3章の `Infrastructure` の行は共通レスポンスの計画も触るので、`main` に入っている文面に対して直す
- [glossary.md](../wiki/glossary.md): Server.Domain の項にエンティティとリポジトリの IF を足す。エンティティの項の「ID は…`UniqueId`（`string`）…（`PlayerEntity` は `UniqueId`…）」を、クライアントの話と分かるように「クライアントでは」を付け、サーバーは `UniqueId` を `Ulid` で持ち、`PlayerEntity` は `UniqueId` を持たないと足す。リポジトリの項の「サーバーは、ADR 0021 のサーバー側を実装するまで…」を、サーバーも Domain が IF を定義する（取得は `FindAsync`・`FindAllAsync`、読み取り専用の IF は無い）に直す。ポートの項の「サーバーは…リポジトリの IF を指す」を、サーバーでは Application が定義する外部の IF（`IRandomSource`・`I<名詞>Catalog`）に直す
- [server-foundation.md](../wiki/architecture/server/server-foundation.md) 「SQL を書く層」: 「Application はポート（インターフェース）越しに呼ぶ」を「Application は Domain のリポジトリの IF 越しに呼ぶ」に
- [produce-server.md](../wiki/architecture/server/produce-server.md) 7章の表: `ProduceSessionStatus`・`ProduceSessionData`・`ProduceSessionRecord`・`ProduceResultRecord`・`IProduceSessionRepository` を `Uta.Server/Domain/Produce/` の行へ移し、`ProduceReward` を同じ行に足す。冒頭の「層:」の行（17 行目）の `Domain` の説明に「リポジトリの IF とそのシグネチャの型」を足し、`Application` の「ポート」を「Catalog の IF」にする
- [gacha-support-server.md](../wiki/architecture/server/gacha-support-server.md): ADR 0021「影響・リスク」が挙げるが、型の置き場の表もリポジトリの IF の置き場の記述も無いので直さない
- [api-design.md](../wiki/architecture/api-design.md) 3章の `player_idols` の行: 「Application の `OwnedIdol` は `UniqueId` に持つ」を「Domain の `IdolEntity` は `UniqueId` に持つ」に
- 上のほかに、旧い名前（`OwnedIdol`・`OwnedSupportCard`・`PlayerSnapshot`・`GachaHistoryRow`・`ListAsync`）を書いている `uta_docs/wiki/` のページを Grep で探して直す（`decisions/` の本文と、`status: done` の計画は当時の記録として直さない）

## 5. テスト

足すテストはない。今のテストがすべて通ることで確かめる（`dotnet test --project server/Uta.Server.Tests`）。アーキテクチャテストの `Domain_DoesNotDependOnOuterLayersOrDataAccess`（Domain が Application を参照しない）が、移した型の依存を検査する。

## 6. 未決事項

なし。
