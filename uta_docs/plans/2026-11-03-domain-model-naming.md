---
title: 実装計画: Application の命名（Service）と Domain の型の置き場
status: draft
updated: 2026-11-03
spec: ../wiki/architecture/repository-layout.md
---

# 実装計画: Application の命名（Service）と Domain の型の置き場

## 1. 目的と前提

PR #24 の確認で出た指摘のうち、通信基盤に依存しないものをまとめて直す。

1. Application 層の `*UseCase` を `*Service` に統一する（サーバー・クライアント）
2. サーバーの中の ULID の値を 26 文字の `string` で持つ。`Ulid` 型は生成と形式の検査だけに使う
3. Entity・値オブジェクトを Domain に移す。両側で使うものは Shared.Domain、サーバーだけのものは Server.Domain（[repository-layout.md](../wiki/architecture/repository-layout.md) 6.1）

- 関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)（Shared.Domain とオニオン）
- 前提: PR #24（`claude/client-signup-signin`）の上に積む。ブランチは `claude/domain-model-naming`、PR の base も `claude/client-signup-signin`

### 決めないこと（後の計画）

- **`PlayerEntity`**: `PlayerSnapshot`（サーバー・クライアント）を `PlayerEntity` にする作業は、この計画でしない。通信基盤（VolTech-MagicOnion）の共通レスポンス項目と PlayerDiff（基盤のセッションで進行中）の形が決まってから、別の計画にする。決まっていること・論点は 7 章に残す
- Client.Domain（`Uta.Client.Domain` の asmdef）: この計画で移すクライアントの型は Shared.Domain に行くだけなので、作らない。`PlayerEntity` の計画で要るかを決める
- `ItemGranter`・`ProduceCompletion`・`ProduceSessionDiscarder`・`ProduceViewBuilder`・`ProduceCommandNormalizer`（Application の補助のクラス。`*UseCase` ではない）の名前は変えない
- 実施済みの計画（`status: done` の `uta_docs/plans/*.md`）の `UseCase` の記述は、当時の記録として直さない

## 2. 決まったこと（ユーザーの判断。2026-11-03）

- Application のクラスは `*Service` に統一する。Api の MagicOnion の実装（`PlayerService`・`GachaService`・`SupportService`・`ProduceService`）と名前は重ならない（Application は動作名、Api は名詞）。名前空間も別（`Uta.Server.Application.*` と `Uta.Server.Api`）。役割の違い（Api は通信の入口、Application は手順）は設計書で書き分ける
- `UseCaseError`・`UseCaseException` は `ServiceError`・`ServiceException` にする
- ULID は値を `string`（26 文字、大文字。`Ulid.ToString()` の形）で持つ。`Ulid` 型は生成（`Ulid.NewUlid().ToString()`）と、外から来た文字列の形式の検査・正規化（`Ulid.TryParse`）だけに使う。Shared.Domain とクライアントにパッケージは足さない。性能: 所持カードは MVP で数百件の規模で、DB（`char(26)`）・DTO（`string`）とも元から文字列なので、変換が減る
- 基盤（VolTech-MagicOnion）の `AuthContext.PlayerId`・`TxContext.PlayerId`・`IPlayerResolver.ResolveAsync` は `Ulid` のまま（基盤は変えない）。Uta の `Api` で `.ToString()` して渡す。`PlayerResolver`（Infrastructure）は `IPlayerResolver` の実装なので引数は `Ulid` のまま、SQL のパラメーターに `.ToString()` を渡す
- Entity・値オブジェクトの置き場は 3.3 の表のとおり

## 3. 型とシグネチャ

### 3.1 改名（UseCase → Service）

| 旧 | 新 | 場所 |
|---|---|---|
| `SignUpUseCase` | `SignUpService` | `server/Uta.Server/Application/Player/` |
| `SignInUseCase` | `SignInService` | 同上 |
| `DrawUseCase` | `DrawService` | `server/Uta.Server/Application/Gacha/` |
| `ExchangeUseCase` | `ExchangeService` | 同上 |
| `GetGachasUseCase` | `GetGachasService` | 同上 |
| `LevelUpUseCase` | `LevelUpService` | `server/Uta.Server/Application/Support/` |
| `LimitBreakUseCase` | `LimitBreakService` | 同上 |
| `StartProduceUseCase` | `StartProduceService` | `server/Uta.Server/Application/Produce/` |
| `GetProduceStateUseCase` | `GetProduceStateService` | 同上 |
| `AbandonProduceUseCase` | `AbandonProduceService` | 同上 |
| `ProduceOperationUseCase` | `ProduceOperationService` | 同上 |
| `UseCaseError` | `ServiceError` | `server/Uta.Server/Application/Common/` |
| `UseCaseException` | `ServiceException` | 同上 |
| `SignInUseCase`（クライアント） | `SignInService` | `client/uta/Assets/Scripts/Application/Auth/` |
| `EnterPlayerUseCase` | `EnterPlayerService` | `client/uta/Assets/Scripts/Application/Player/` |
| `EnterPlayerUseCaseTests` | `EnterPlayerServiceTests` | `client/uta/Assets/Tests/EditMode/` |

- ファイル名もクラス名に合わせる（`git mv`）。クライアントは `.cs.meta` も `git mv` で同じ名前に変える（中の `guid` は変えない。参照が切れないように）
- `ServiceError` の列挙子の名前・順番は変えない
- 変数名・引数名（`signIn`・`levelUp` など）は今のまま。`useCase` という名前のローカル変数（クライアントのテスト）は `service` にする
- コメント・XML ドキュメントの「UseCase」「ユースケース」は「Service」「サービス」にする（例: `UtaServiceAttribute` の summary）

### 3.2 ULID を string にする

ポート・Entity・値オブジェクト・リポジトリ・Application の引数の `Ulid`（`Ulid?`）を `string`（`string?`）にする。対象（`Ulid` が出る行。テストを除く）:

| ファイル | 変えること |
|---|---|
| `Application/Player/IPlayerRepository.cs` | 全メソッドの `Ulid playerId` → `string playerId` |
| `Application/Idol/IIdolRepository.cs` | 同上 |
| `Application/Gacha/IGachaRepository.cs` | 同上 |
| `Application/Support/ISupportCardRepository.cs` | `Ulid playerId`・`Ulid uid` → `string` |
| `Application/Produce/IProduceSessionRepository.cs` | `Ulid playerId`・`Ulid sessionId` → `string` |
| `Application/**/*Service.cs`（3.1 の改名後）・`ItemGranter`・`ProduceCompletion`・`ProduceSessionDiscarder` | 引数の `Ulid` → `string`。生成は `Ulid.NewUlid().ToString()` |
| `StartProduceService` | `HashSet<Ulid>` → `HashSet<string>`。`Ulid.TryParse(text, out var uid)` で検査し、集合と以降の処理には `uid.ToString()`（正規化した文字列）を使う。検査に落ちたときの動き（今のエラー）は変えない |
| `OwnedSupportCard`（3.3 で移動） | `Ulid Uid` → `string Uid` |
| `GachaResult`（3.3 で移動） | `Ulid? SupportCardUid` → `string? SupportCardUid`。ゲームログの `SupportCardUid?.ToString()` → `SupportCardUid` |
| `GachaHistoryRow`（3.3 で移動） | `Ulid HistoryId`・`Ulid PlayerId` → `string` |
| `ProduceSessionRecord`・`ProduceResultRecord`（3.3 で移動） | `Ulid SessionId`・`Ulid PlayerId` → `string`。`ProduceResultRecord.Empty` の引数も |
| `Infrastructure/*Repository.cs` | 引数を `string` に。SQL のパラメーターの `playerId.ToString()` などは `playerId` をそのまま渡す。行の読み出しの `Ulid.Parse(row.X)` は `row.X` をそのまま使う |
| `Infrastructure/PlayerResolver.cs` | 引数は `Ulid` のまま（基盤の IF）。リポジトリを呼ぶところで `playerId.ToString()` |
| `Api/*Service.cs` | `Context.GetAuthContext().PlayerId` を受けたところで `.ToString()` して `string playerId` にする。`ProduceService.RunOperationAsync` の引数も `string` |
| `Api/SupportService.cs` | `ParseUid` を `private static string NormalizeUid(string value) => Ulid.TryParse(value, out var uid) ? uid.ToString() : Ulid.Empty.ToString();` にする（形が違う文字列は、どの所持IDとも一致しない `00000000000000000000000000` にして、今と同じく NOT_OWNED / INVALID_MATERIAL に落とす） |
| `Api/PlayerService.cs`・`Api/GachaService.cs` | DTO へ詰めるときの `x.Uid.ToString()` → `x.Uid`、`x.SupportCardUid?.ToString() ?? ""` → `x.SupportCardUid ?? ""` |

- テスト（`server/Uta.Server.Tests`）の偽の実装（`ProduceOperationTests` の偽のリポジトリなど）のシグネチャも合わせる。`Ulid.NewUlid().ToString()`・`Ulid.TryParse` を使っているテストの行はそのまま
- 大文字・小文字: 値はすべてサーバーが `Ulid.ToString()`（大文字）で作るので、DB・DTO・メモリで同じ表記になる。クライアントから来る文字列は `Api`（`NormalizeUid`）か `StartProduceService` で正規化する

### 3.3 型の置き場（移動）

移動はファイルごと（`git mv`）で、名前空間を移動先に合わせる。型の中身（3.2 の string 化を除く）は変えない。

| 型 | 今 | 移動先（ファイル） | 名前空間 |
|---|---|---|---|
| `GachaPoolItem`・`GachaPool`・`CeilingItem`・`GachaDefinition` | `Application/Gacha/GachaDefinition.cs` | `server/Uta.Server/Domain/Gacha/GachaDefinition.cs` | `Uta.Server.Domain.Gacha` |
| `GachaResult` | `Application/Gacha/GachaResult.cs` | `Domain/Gacha/GachaResult.cs` | 同上 |
| `GachaHistoryRow` | `Application/Gacha/GachaHistoryRow.cs` | `Domain/Gacha/GachaHistoryRow.cs` | 同上 |
| `InitialGrant` | `Application/Player/InitialGrant.cs` | `Domain/Player/InitialGrant.cs` | `Uta.Server.Domain.Player` |
| `PlayerBalance` | `Application/Player/IPlayerRepository.cs` の末尾 | `Domain/Player/PlayerBalance.cs`（新規） | 同上 |
| `LevelUpCost` | `Application/Support/ISupportCatalog.cs` の先頭 | `Domain/Support/LevelUpCost.cs`（新規） | `Uta.Server.Domain.Support` |
| `ProduceReward` | `Application/Produce/ProduceReward.cs` | `Domain/Produce/ProduceReward.cs` | `Uta.Server.Domain.Produce` |
| `ProduceSessionData`・`ProduceSessionRecord`・`ProduceResultRecord` | `Application/Produce/ProduceSessionData.cs` | `Domain/Produce/ProduceSessionData.cs` | 同上 |
| `ProduceSessionStatus` | `Application/Produce/ProduceSessionStatus.cs` | `Domain/Produce/ProduceSessionStatus.cs` | 同上 |
| `OwnedIdol`（サーバー） | `Application/Idol/OwnedIdol.cs` | `shared/Uta.Shared.Domain/Player/OwnedIdol.cs`（新規） | `Uta.Shared.Domain.Player` |
| `OwnedSupportCard`（サーバー） | `Application/Support/OwnedSupportCard.cs` | `shared/Uta.Shared.Domain/Player/OwnedSupportCard.cs`（新規） | 同上 |
| `OwnedIdol`・`OwnedSupportCard`（クライアント） | `client/uta/Assets/Scripts/Application/Player/` | 削除し、Shared.Domain の型を使う（`.cs.meta` も削除） | — |

- `Application/Produce/ProduceReward.cs` に残る `IProduceRewardCatalog`・`IPlayerLevelCatalog` は、ファイルを `IProduceRewardCatalog.cs` と `IPlayerLevelCatalog.cs` に分ける（名前空間は `Uta.Server.Application.Produce` のまま）。元の `ProduceReward.cs` は Domain へ移した record だけになる
- Application に残す型（Entity・値オブジェクトではなく、サービスの戻り値・画面向けの組み立て）: `DrawResult`・`ExchangeResult`・`GachaStatus`・`LevelUpResult`・`LimitBreakResult`・`SignInResult`・`ProduceView`・`ProduceEndSummary`・`PlayerSnapshot`（7 章の計画まで）
- 移動で `using` が足りなくなるファイルには、移動先の名前空間の `using` を足す。サーバーの `Domain` から `Uta.Server.Application` を参照してはいけない（`ArchitectureTests.Domain_DoesNotDependOnOuterLayersOrDataAccess`）。移す型はどれも Application の型を参照しない（`GrantedItemKind`・`GrantOutcome`・`RandomStreamState` は Domain、`ProduceSnapshot`・`ExamOutcome`・`ProduceRank` は Shared.Domain）

Shared.Domain の 2 つの型（C# 9、`record`・`init` を使わない。[repository-layout.md](../wiki/architecture/repository-layout.md) 5章）:

```csharp
namespace Uta.Shared.Domain.Player
{
    /// <summary>所持している育成アイドル 1 体。</summary>
    public sealed class OwnedIdol
    {
        public OwnedIdol(int idolId, int training, int awakening, int shards)
        {
            IdolId = idolId;
            Training = training;
            Awakening = awakening;
            Shards = shards;
        }

        public int IdolId { get; }
        public int Training { get; }
        public int Awakening { get; }
        public int Shards { get; }
    }

    /// <summary>所持しているサポートカード 1 枚。同じカードを複数持てるので、所持ID（Uid）で区別する。</summary>
    public sealed class OwnedSupportCard
    {
        public OwnedSupportCard(string uid, int cardId, int level, int limitBreak)
        {
            if (uid == null)
            {
                throw new System.ArgumentNullException(nameof(uid));
            }

            Uid = uid;
            CardId = cardId;
            Level = level;
            LimitBreak = limitBreak;
        }

        /// <summary>所持ID。ULID の 26 文字（サーバーが発行）。</summary>
        public string Uid { get; }
        public int CardId { get; }
        public int Level { get; }
        public int LimitBreak { get; }
    }
}
```

- 値の等価（`record` の `Equals`）は持たせない。今のコードで `OwnedIdol`・`OwnedSupportCard` を等価で比べている場所はない（`Assert.Equal` は DTO の項目で比べている）
- クライアントの旧 `OwnedSupportCard.SupportCardUid` は `Uid` になる。`PlayerGateway.ToSnapshot` は `new OwnedSupportCard(dto.SupportCardUid, ...)` のまま（引数の順は同じ）。`PlayerGatewayTests` の `snapshot.SupportCards[1].SupportCardUid` は `.Uid` にする
- `server/Uta.Server` のサーバー側の `using Uta.Server.Application.Idol;`・`using Uta.Server.Application.Support;` が `OwnedIdol`・`OwnedSupportCard` のためだけにあったなら、`using Uta.Shared.Domain.Player;` に置き換える

## 4. 触るファイル

| 操作 | パス | 中身 |
|---|---|---|
| 変更（改名） | `server/Uta.Server/Application/**/*UseCase.cs` の 11 ファイル、`Common/UseCaseError.cs`・`Common/UseCaseException.cs` | 3.1 |
| 変更 | `server/Uta.Server/Program.cs` | DI の登録の型名（3.1） |
| 変更 | `server/Uta.Server/Api/*.cs` | 3.1 の型名、3.2 |
| 変更 | `server/Uta.Server/Application/**`（上以外）・`server/Uta.Server/Infrastructure/*.cs` | 3.2、3.3 の `using` |
| 移動・作成 | `server/Uta.Server/Domain/{Gacha,Player,Support,Produce}/*.cs` | 3.3 |
| 作成 | `shared/Uta.Shared.Domain/Player/OwnedIdol.cs`・`OwnedSupportCard.cs` | 3.3 |
| 削除 | `server/Uta.Server/Application/Idol/OwnedIdol.cs`・`Application/Support/OwnedSupportCard.cs` | 3.3 |
| 変更 | `server/Uta.Server.Tests/**/*.cs` | 型名・名前空間・偽の実装のシグネチャ |
| 変更（改名） | `client/uta/Assets/Scripts/Application/Auth/SignInUseCase.cs(.meta)`・`Application/Player/EnterPlayerUseCase.cs(.meta)`・`Tests/EditMode/EnterPlayerUseCaseTests.cs(.meta)` | 3.1 |
| 削除 | `client/uta/Assets/Scripts/Application/Player/OwnedIdol.cs(.meta)`・`OwnedSupportCard.cs(.meta)` | 3.3 |
| 変更 | `client/uta/Assets/Scripts/Application/Player/PlayerSnapshot.cs`・`Infrastructure/Player/PlayerGateway.cs`・`Presentation/Title/ConnectivityCheck.cs`・`TitlePresenter.cs`・`Bootstrap/BootstrapEntry.cs`・`Tests/EditMode/PlayerGatewayTests.cs` | 型名・`using Uta.Shared.Domain.Player;` |

Unity の asmdef は変えない（`Uta.Client.Application`・`Uta.Client.Infrastructure`・テストの asmdef は、もう `Uta.Shared.Domain` を参照している）。設計書（wiki）の更新はメインのセッションが行う（6 章）。

## 5. タスク

各タスクの終わりで `dotnet build Uta.slnx`（警告 0・エラー 0）が通ること。サーバーのテストは Docker が使えるとき `dotnet test --project server/Uta.Server.Tests` を回す（使えなければ、そう報告する）。

### タスク 1: サーバーの改名（UseCase → Service）

- 3.1 のサーバーの行を `git mv` と置換で直す。`Program.cs`・`Api/*.cs`・`UtaServiceAttribute.cs`（`catch (ServiceException ...)`・`ToErrorCode(ServiceError ...)`・summary）・テストを合わせる
- 確かめ: `grep -rn "UseCase" server/` が 0 件。ビルドとテスト

### タスク 2: サーバーの ULID を string に（3.2）

- 型は今の場所のまま、3.2 の表のとおりに直す（`OwnedSupportCard`（まだ Application）の `Ulid Uid` も `string Uid` にする）
- 境界: 形の違う所持ID（`"not-a-ulid"`）、他人の所持ID、存在しない所持ID は、今の結合テスト（`SupportTests`・`ProduceStartTests`）のとおりのエラーになること
- 確かめ: `grep -rn "Ulid" server/Uta.Server` に残るのが、`Ulid.NewUlid().ToString()`、`Ulid.TryParse`、`Ulid.Empty.ToString()`、`uid.ToString()`（正規化）、`PlayerResolver` の引数、`GetAuthContext().PlayerId.ToString()` の行だけ。ビルドとテスト

### タスク 3: サーバーの型の移動（3.3）と Shared.Domain の 2 型

- 3.3 の表のとおりに移動・作成・削除し、名前空間と `using` を直す。サーバーの `OwnedIdol`・`OwnedSupportCard`（record）を消して、Shared.Domain のクラスに置き換える（コンストラクターの引数の順は同じ）
- 確かめ: ビルド、`ArchitectureTests`（Domain が Application を参照しない）、テスト

### タスク 4: クライアント

- 3.1 のクライアントの行（`.meta` ごと `git mv`）、3.3 のクライアントの行を直す。`TitlePresenter`・`ConnectivityCheck`・`BootstrapEntry` の型名を合わせる
- `EnterPlayerServiceTests` の `useCase` 変数を `service` に
- 確かめ: Unity の Editor が開いていれば `client/uta` で `unity recompile --format json`（エラー 0）と EditMode のテスト（`unity command test run --mode EditMode`）。Editor が使えなければ「確認できていない」と報告する。`grep -rn "UseCase" client/uta/Assets` が 0 件

## 6. 設計書（メインのセッションが、実装の前に直す）

- `repository-layout.md`: 2章の木の「ユースケース」→「サービス（手順）」、4章の Application の責務、6.1 の表（Entity・値オブジェクトの置き場を足す。Server.Domain・Shared.Domain の例）、10.1 の ULID の表（サーバーの中も `string`。`Ulid` は生成と検査だけ。Shared.Domain の行: 「ID を `string` で持ってよい」）
- `glossary.md`: 「サービス（Application）」と「サービス（Api。MagicOnion の実装）」の違いを足す
- `screen-design.md`・`overview/screen-wireframes.md`・`server/server-foundation.md`・`server/produce-server.md`・`server/gacha-support-server.md`・`api-design.md`: 「ユースケース」「`*UseCase`」「`UseCaseException`」を直し、ファイルの一覧（produce-server.md の Application / Domain の表など）を 3.3 の置き場に合わせる

## 7. 後の計画（PlayerEntity）に残すこと

決まっていること（ユーザーの判断。2026-11-03）:

- `PlayerSnapshot` を `PlayerEntity` にし、サーバーとクライアントで同じ形にする（置き場は Shared.Domain）。`PlayerId`（`string`）を持つ
- 所持アイドル・所持サポートカードを `PlayerEntity` が持つ
- Lv は経験値から計算する。DB に Lv を持たない。計算の定義をマスタに足す（[produce.md](../wiki/systems/produce.md) 7章の「経験値の持ち方」を変える）
- 進行中のプロデュースの情報（`HasActiveProduce`・`DiscardedProduce`）は `PlayerEntity` に含めず、サインインのレスポンスで返す。クライアントで永続化する PlayerDiff に混ぜないため
- 更新の差分は通信基盤の共通レスポンス項目（PlayerDiff）で返す。基盤の作業を先に進める

論点（そのときに決める）: マスタの定義の形（到達の累計の列を足す / 表を置き換える / 読み込み時に計算）、DTO で Lv を送るか、`SignInResponse` に `PlayerId` を足すか、所持品を全件読む場面、`players` のマイグレーション（`exp + 20 × level × (level − 1)` で累計に）、Client.Domain を作るか

## 8. 未決事項

なし（7 章は別の計画）
