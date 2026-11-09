---
title: プロデュースのサーバー処理
status: draft
updated: 2026-11-09
---

# プロデュースのサーバー処理

[produce.md](../../systems/produce.md)(ゲームの仕様)と [produce-domain.md](../produce-domain.md)(Shared.Domain の週の進行・乱数・状態・API 案)を、サーバー(`server/Uta.Server`)でどう処理するかを書いた実装設計書。ゲームのルール・値・局面の遷移の正は produce.md、乱数・操作の記録・保存する状態の中身の正は produce-domain.md、**サーバーの保存の形・テーブル・処理の手順・DTO の形の正はこのページ**。RPC の一覧とエラーの reason は [api-design.md](../api-design.md)。実装計画は [2026-11-03-produce-snapshot.md](../../../plans/2026-11-03-produce-snapshot.md)・[2026-11-03-server-produce-start.md](../../../plans/2026-11-03-server-produce-start.md)・[2026-11-03-server-produce-play.md](../../../plans/2026-11-03-server-produce-play.md)。

## 1. 方針 [決定。2026-11-03]

- 週の進行・レッスン・試験・評価は `Uta.Shared.Domain` の `ProduceEngine` がすべて行う。サーバーは「状態の読み込み → 操作の受け付け(`Apply`) → 状態の保存」と、終了のときの報酬の付与・結果の保存だけを持つ。サーバー固有のルールは報酬と経験値の付与(4章)だけ(プレイヤーLv の計算はクライアントも使うので `Uta.Shared.Domain`。4.1)
- 1 回の操作は 1 つのトランザクション(VolTech-MagicOnion の `RunAsync`)で行う。状態の保存・報酬・結果・冪等性の保存済みレスポンスは同じトランザクション。同じプレイヤーの要求は基盤が直列にする([api-design.md](../api-design.md) 3章)ので、`players` は明示的にロックしない
- 保存の形は、`ProduceSnapshot`([produce-domain.md](../produce-domain.md) 6.1)から作る MessagePack(整数キー)。DTO は復元した `ProduceState` とエンジンの表示用の関数から作る([produce-domain.md](../produce-domain.md) 6章)
- 乱数は 1 つのマスターシードから `RandomStreams.Create` で作るストリーム。サーバーは `IRandomStreamSource` を実装し、要求の終わりにストリームの状態を保存する(2章)
- 層: `Api`(DTO ↔ コマンドの変換・ヘッダーの読み取り)、`Application`(ユースケース。マスタは DI で受けた `MemoryDatabase` から直接引く。ADR 0020)、`Domain`(乱数ストリームの管理、リポジトリの IF とそのシグネチャの型)、`Infrastructure`(SQL・MessagePack・Shared.Domain の `ProduceCatalog` の組み立て)。依存は [repository-layout.md](../repository-layout.md) 3章・4章のとおり(Application は `Uta.Shared.Contracts` を参照しない)

## 2. 乱数 [決定。2026-11-03]

- **マスターシード**: `StartAsync` で `IRandomSource.Create().NextUInt64()` の 1 回分を使う(`SecureRandomSource` は暗号用乱数からシードを作る。結合テストは `ScriptedRandomSource` の先頭の値で固定する)。64 ビット、状態に保存する
- **ストリームの管理**: `Uta.Server.Domain.Produce.SessionRandomStreams`(`IRandomStreamSource` の実装)。マスターシードと、保存済みのストリームの状態(`RandomStreamState`: 種類・週・挑戦回数・`xoshiro256**` の状態 4 つ・引いた回数)から作る。`Get(key)` は、保存済みならその状態から `Xoshiro256StarStar` を作り、無ければ `RandomStreams.Create(masterSeed, key)` で作る。同じキーには同じオブジェクトを返す(契約: [produce-domain.md](../produce-domain.md) 実装メモ)
- **保存**: `Export()` が、`Get` で返した(または読み込んだ)ストリーム**すべて**の現在の状態を、キーの `ToUInt64()` の昇順で返す。ストリームは捨てない(1 回のプロデュースで数十個・数 KB)。要求を**受け付けたとき(`Accepted`)と開始したときだけ**保存する。拒否した要求は何も保存しない

## 3. テーブルと保存の形 [決定。2026-11-03]

マイグレーション `server/Uta.Server/Migrations/0006_create_produce_tables.sql`:

```sql
CREATE TABLE produce_sessions (
    session_id char(26) PRIMARY KEY,
    player_id char(26) NOT NULL REFERENCES players (player_id),
    status int NOT NULL,
    week int NOT NULL,
    turn int NOT NULL,
    app_version int NOT NULL,
    state_version int NOT NULL,
    rule_version int NOT NULL,
    version int NOT NULL DEFAULT 0,
    state bytea NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_produce_sessions_active ON produce_sessions (player_id) WHERE status = 0;

CREATE TABLE produce_results (
    session_id char(26) PRIMARY KEY REFERENCES produce_sessions (session_id),
    player_id char(26) NOT NULL REFERENCES players (player_id),
    ending int NOT NULL,
    exam_outcome int NOT NULL,
    evaluation_value int NOT NULL,
    produce_rank int NOT NULL,
    final_score int NOT NULL,
    final_rank int NOT NULL,
    gacha_currency int NOT NULL,
    gold int NOT NULL,
    support_points int NOT NULL,
    exp int NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_produce_results_player ON produce_results (player_id, created_at DESC);
```

- `status`: 0 進行中 / 1 完了 / 2 放棄 / 3 破棄(`ProduceSessionStatus`)。`produce_results.ending` は 1〜3 で同じ値。進行中のセッションは 1 ユーザー 1 つ(部分ユニークインデックスで守る)
- `week` / `turn`: 保存のたびに状態の `Week` と `Lesson?.Turn ?? 0` を入れる(調査用。判定には使わない)
- `exam_outcome` / `produce_rank`: `ExamOutcome` / `ProduceRank` の数値(放棄・破棄は 0)。`final_score` / `final_rank`: 最終試験を受けていなければ 0
- 楽観ロック: `UPDATE produce_sessions SET ..., version = version + 1, updated_at = now() WHERE session_id = @id AND version = @version`。0 行なら `PRODUCE_STALE_OPERATION`
- **バージョンの定数**(`Uta.Server.Application.Produce.ProduceVersions`): `StateVersion = 1`(`state` の形を変えたら上げる)、`RuleVersion = 2`(2026-11-09 に 1 から上げた。4.1。[repository-layout.md](../repository-layout.md) 10.5。`Uta.Shared.Domain`・マスタ・サーバーのルールを変えたコミットで上げる)。セッションの 3 つのバージョン(`app_version`・`state_version`・`rule_version`)のどれかが今と違えば「古い」セッション。`state_version` が違うときは `state` を読まない。`state_version` が同じでも `state` が読めない(MessagePack の例外)ときは、`Data = null` にして古いセッションと同じに扱う(`SignInAsync` が起動できなくなるのを防ぐ)
- **`state` の形**(`Uta.Server.Infrastructure.Produce` の `[MessagePackObject]` の型。整数キーは 0 から宣言順):
  - `ProduceStateBlob`: `MasterSeed`(`ulong`)、`Streams`(`RandomStreamBlob[]`)、`Snapshot`(`ProduceSnapshotBlob`)
  - `RandomStreamBlob`: `Kind`(`int`)、`Week`、`Attempt`、`S0` `S1` `S2` `S3`(`ulong`)、`DrawCount`(`long`)
  - `ProduceSnapshot`([produce-domain.md](../produce-domain.md) 6.1)の 10 個の型それぞれに、同じ名前+`Blob` の型を作る。プロパティは同じ型・同じ順で、キーは 0 から宣言順。enum は `int`、`null` を許す型は `null` のまま。スナップショットとの変換はこの型のペアごとの手書きの関数で、`ProduceStateSerializer`(`Serialize(ProduceSessionData) → byte[]`、`Deserialize(byte[]) → ProduceSessionData`)にまとめる

## 4. 報酬とプレイヤーLv [決定。2026-11-03]

終了(`Phase` が `Finished`)になった操作と同じトランザクションで、次を行う([produce.md](../../systems/produce.md) 6.1)。

1. 報酬: `EvaluationRankReward`(マスタ。`ProduceRank` で引く。`ProduceCompletion` が `MemoryDatabase` の `EvaluationRankRewardTable.FindByRank` で引く)の行。ランクは `ProduceState.Result.Rank`
2. プレイヤー経験値: `PlayerExpRules.AddExp(totalExp, gain, maxExp)`(4.1)。`totalExp` は `players.exp`(経験値の累計)、`maxExp` は `PlayerLevelCatalog.MaxExp`
3. `players` の更新: `gacha_currency`・`gold`・`support_points` に報酬を加え、`exp` を 2 の結果にする(1 文の `UPDATE`)
4. `produce_results` に 1 行(終わり方は完了、`exam_outcome` は `ProduceState.Result.Outcome`)
5. `produce_sessions` の `status` を完了にする(状態も最後のものを保存する)
6. ゲームログ 1 件: kind `produce`、reason `produce_reward`、`Target` はセッション ID、フィールドは `rank`・`evaluation_value`・`gacha_currency`・`gold`・`support_points`・`exp`(付与した量)と `level_after`(2 の結果から `PlayerLevelCatalog.GetLevel` で求めた Lv)・`exp_after`(2 の結果の累計)

放棄・破棄は報酬なし(`produce_results` に 0 で残す。ゲームログは書かない)。

### 4.1 プレイヤーLv の計算 [決定。2026-11-09。経験値の累計だけを持つのは 2026-11-03 のユーザーの判断。Lv の計算の型・付与の置き場・`RuleVersion`・DTO・マイグレーションは 2026-11-09 のユーザーの判断。[produce.md](../../systems/produce.md) 7章]

- **`players` は Lv を持たない**。`exp` は経験値の累計。Lv は使う場所で累計から求める(サーバーでは `StartAsync` の開始時の入力と、4章のゲームログ。クライアントではホームの表示。[screen-design.md](../screen-design.md) 7.5)
- **Lv の計算は `Uta.Shared.Domain` の不変の型 `PlayerLevelCatalog`**(サーバーとクライアントで同じ計算を使うため。名前は [naming.md](../naming.md) の「定義の集まり」の `<名詞>Catalog`。`shared/Uta.Shared.Domain/Player/PlayerLevelCatalog.cs`、名前空間 `Uta.Shared.Domain.Player`、`sealed class`。使い手が 1 つの実装しか持たないのでインターフェースは作らない):
  - `static PlayerLevelCatalog Create(IReadOnlyList<int> fromExps)`: `fromExps[i]` は Lv `i+1` の `FromExp`。次のどれかに当たれば `ArgumentException`: `fromExps` が空、`fromExps[0]` が 0 でない、`fromExps` が狭義の単調増加でない。`fromExps` は写して持つ
  - `int MaxLevel`(= `fromExps` の個数)、`int MaxExp`(= `fromExps` の最後の値。最大 Lv の行は `FromExp` = `ToExp` なので、累計の上限。[produce-tables.md](../master/produce-tables.md)。`ToExp` の検査はマスタの検証が持ち、この型は受けない)
  - `int GetLevel(int totalExp)`: `fromExps[i] <= totalExp` を満たす最大の `i` に 1 を足して返す(`fromExps[0]` は 0 なので必ず 1 以上)。`totalExp` が `MaxExp` を超えていても `MaxLevel` を返す。`totalExp` が負なら `ArgumentOutOfRangeException`。マスタは連続している(`ToExp` + 1 = 次の `FromExp`)ので `ToExp` は見ない
  - **作るのは各側の Factory で、1 度だけ作ってシングルトンにする**(`CardCatalog` と同じ形。[ADR 0020](../../decisions/0020-masterdata-referenced-outside-shared-domain.md))。サーバー: `server/Uta.Server/Infrastructure/MasterData/PlayerLevelCatalogFactory.cs` の `static PlayerLevelCatalog Create(MemoryDatabase master)`。`master.PlayerLevelExpTable.All` は主キー(`Level`)の昇順(MasterMemory の表は主キーで並んでいる。既存のテスト `MasterDataTests.PlayerLevelExp_AllIsInLevelOrder` が確かめている)なので並べ替えない。`fromExps` = 各行の `FromExp`。`CardCatalogFactory` と同じ場所で `AddSingleton(sp => PlayerLevelCatalogFactory.Create(sp.GetRequiredService<MemoryDatabase>()))` に登録し、`StartProduceUseCase`・`ProduceCompletion` はコンストラクターで受ける。クライアントの Factory と登録は、ホームの Lv の表示を作る計画で足す([screen-design.md](../screen-design.md) 7.5)
- **経験値の付与はサーバーの Domain**(報酬の付与はサーバーだけが行う。[repository-layout.md](../repository-layout.md) 6.1): `server/Uta.Server/Domain/Player/PlayerExpRules.cs`(名前空間 `Uta.Server.Domain.Player`)の `static int AddExp(int totalExp, int gain, int maxExp)`。`gain` が負なら `ArgumentOutOfRangeException`。`totalExp >= maxExp` なら `totalExp` をそのまま返す(上限をマスタで下げても累計を減らさない)。それ以外は `Math.Min(totalExp + gain, maxExp)`(報酬の表の値では桁あふれしないので見ない)。2026-11-03 の `Uta.Server.Domain.Produce.PlayerLevelRules`(`AddExp(level, exp, gain, requiredExps)`)と `PlayerLevelRulesTests` は消す
- **サーバーの型の変更**:
  - `PlayerEntity(int Exp, int Gold, int SupportPoints, int GachaCurrency)`(`Level` を消す。`Exp` は累計)。`PlayerRepository.FindAsync` の `SELECT` から `level` を消す
  - `IPlayerRepository.GrantProduceRewardAsync(TxContext tx, Ulid playerId, ProduceReward reward, int exp)`(`level` の引数を消す。`exp` は付与したあとの累計)。`UPDATE` から `level = @level` を消す
  - `ProduceEndResult`（2026-11-09 に `ProduceEndSummary` から改名）から `PlayerLevel` を消す。`PlayerExp` は付与したあとの累計(放棄は今の累計)
  - `PlayerDiffCollector` は `Level` を詰めない(`PlayerDiffDto` から消すため)
  - 4章 6 の `level_after` は `PlayerLevelCatalog.GetLevel(exp_after)`
  - `ProduceSnapshot.PlayerLevel`(開始時の入力の写し)は変えない
- **DTO は累計だけを送る** [決定。2026-11-09。ユーザーの判断]: `SignInResponse`・`PlayerDiffDto`・`ProduceResultResponse` から Lv の項目を消し、経験値の項目は累計にする。クライアントが Lv を `PlayerLevelCatalog.GetLevel` で求める。MessagePack のキーは消した項目の後ろを 1 つずつ詰める(リリース前で、古いクライアントとの互換を持たない。手元のアプリは入れ直す。[repository-layout.md](../repository-layout.md) 10.5)
  - `SignInResponse`: `Level` を消す。`Exp`(累計)が `Key(0)` になり、`PlayerId` は `Key(8)`
  - `PlayerDiffDto`: `Level` を消す。`HasBalance` が真なら次の 4 つ(`Exp`・`Gold`・`SupportPoints`・`GachaCurrency`)が有効。`Exp` は累計で `Key(1)`、`RemovedSupportCardUniqueIds` は `Key(7)`([server-foundation.md](server-foundation.md) 共通レスポンス項目)
  - `ProduceResultResponse`: `PlayerLevel`(`Key(10)`)を消す。`PlayerExp` は付与したあとの累計で `Key(10)`(6章)
  - クライアントの `PlayerEntity` から `Level` を消す(`Exp` は累計。[screen-design.md](../screen-design.md) 7.5)。`TitlePresenter` のサインインのログからは `level=` を消す(クライアントの `PlayerLevelCatalog` はホームの表示の計画で足すため)
- **マスタとクライアントの版**: クライアントとサーバーは同じアプリのバージョンのマスタを使う([repository-layout.md](../repository-layout.md) 10.5)ので、表示の Lv とサーバーの Lv は食い違わない。古いアプリのまま繋いだときに表示がずれるのは、10.5 で受け入れていることと同じ
- **`RuleVersion` を 1 → 2 に上げる** [決定。2026-11-09。ユーザーの判断]: マスタの定義・`Uta.Shared.Domain`・サーバーのルールを変えるので、3章の規則どおり。進行中のセッションは古いセッションとして破棄される
- **マイグレーション** [決定。2026-11-09。ユーザーの判断]: `server/Uta.Server/Migrations/` の次の番号(今は `0008_drop_player_level.sql`。ほかのブランチが先に足したら、その次の番号)で、`players` の `level` の列を消し、既存の行の `exp` を 0 にする(今の「今のレベルの中の経験値」は捨てる。リリース前で、残す価値のあるデータが無いため)。中身は `ALTER TABLE players DROP COLUMN level;` と `UPDATE players SET exp = 0;`。累計に換算する `exp + 20 × level × (level − 1)` は、今のマスタの値(Lv L → L+1 に `40 × L`)と合うことを確かめたが、使わない
- **テスト**(移し先と境界):
  - `PlayerLevelCatalog`: `shared/Uta.Shared.Domain.Tests/Player/PlayerLevelCatalogTests.cs`。マスタの値に結びつけず、小さな表 `fromExps` = [0, 40, 120] で、`GetLevel` が 0 → 1、39 → 1、40 → 2、119 → 2、120 → 3、121 → 3、`MaxLevel` = 3、`MaxExp` = 120。負の `totalExp` は例外。`Create` の 3 つの検査がそれぞれ例外になること
  - 今のマスタの値で組み立てられること: `server/Uta.Server.Tests/MasterData/` に、`PlayerLevelCatalogFactory.Create(マスタ)` の `MaxLevel` が 100・`MaxExp` が 198000 であることを確かめるテストを 1 本
  - `PlayerExpRules.AddExp`: `server/Uta.Server.Tests/Domain/PlayerExpRulesTests.cs`。(0, 40, 198000) → 40、(197990, 60, 198000) → 198000、(198000, 10, 198000) → 198000、(200000, 10, 198000) → 200000(減らさない)、負の `gain` は例外
  - マスタ: `MasterDataTests` の `playerLevelExp` の 99 行を前提にしたテスト(行数と全行の式 `40 × Level`)を、100 行・Lv L の `FromExp` = `20 × L × (L − 1)`、`ToExp`(L < 100 は `20 × L × (L + 1) − 1`、Lv 100 は 198000)に変える。`MasterDataInvariantTests` に [produce-tables.md](../master/produce-tables.md) の表をまたぐ検査を足す。クライアントの `MasterDataLoaderTests` の `playerLevelExp` の行数(99)も 100 にする
  - `ProduceStartTests` の `rule_version` が 1 であることの確認は 2 に、`result.PlayerLevel` を読む確認は `PlayerExp`(累計)に直す
  - 結合テストで `players.level` を読む・書くもの(`CommonResponseTests`・`ProduceOperationTests`・`ProducePlayTests`、`PlayerSignUpSignInTests` の列の件数の確認(5 列 → 4 列))は、`exp`(累計)と `PlayerLevelCatalog` で求めた Lv で確かめる形に直す。最大 Lv にする準備(`UPDATE players SET level = 100, exp = 0`)は `UPDATE players SET exp = 198000`
  - クライアントの EditMode のテストで `PlayerEntity.Level`・`SignInResponse.Level`・`PlayerDiffDto.Level` を使うものは、`Exp` だけを見る形に直す

## 5. 処理の手順 [決定。2026-11-03]

共通: ユースケースの引数に `appVersion`(`x-app-version`。`Api` が読む)を渡す。**古いセッション**は `app_version`・`state_version`・`rule_version` のどれかが今と違うもの(3章)。

### `StartAsync`

1. `ScenarioMasterId` がマスタに無ければ `PRODUCE_INVALID_ARGUMENT`
2. 編成の検証([idol-support.md](../../systems/idol-support.md) 2.1 の 1〜6。どれに当たっても `PRODUCE_INVALID_FORMATION`)。1 育成アイドルを所持していない、2 所持IDが 6 個以上、3 所持IDが自分の所持でない(形が違う文字列を含む)、4 同じ所持IDが 2 回、5・6 は `FormationValidator.Validate`(カード ID の重複・プランの不一致)
3. `RentalCandidateMasterId` が 0 でなければ `PRODUCE_INVALID_ARGUMENT`([idol-support.md](../../systems/idol-support.md) 2.1 の 7。編成の検証のあと)
4. 進行中のセッションがあるとき: 古ければ破棄する(下の「破棄」。破棄は 1〜3 の検証と同じトランザクションなので、検証に落ちれば破棄も巻き戻る)。古くなければ `PRODUCE_ALREADY_IN_PROGRESS`
5. プレイヤーの経験値の累計を `players` から読み、`PlayerLevelCatalog.GetLevel` で Lv を求める(4.1)。`ProduceStartInput(scenarioId, idolId, level, supportCards)` を作る(`supportCards` は所持IDの順に、`player_support_cards` の `card_id`・`level`・`limit_break`。開始時の性能の写し。完了条件 3)
6. マスターシードを作り、`ProduceEngine.Start`。スナップショットを保存(新しいセッション ID(ULID)、`app_version` は引数、`state_version`・`rule_version` は定数)
7. Response は 6 の状態。`Common` は基盤が入れる(`StartAsync` は通貨を変えないので、通常は null)

同じプレイヤーの別の `x-request-id` の `StartAsync` が同時に来ることは、基盤の直列化([api-design.md](../api-design.md) 3章)で起きない。部分ユニークインデックス違反は想定せず、起きたら内部エラーのままにする

### `GetStateAsync`(`[NoSideEffects]`)

進行中のセッションが無い、または古い(または古くて読めない)とき: `Phase` が `Nothing` で、ほかが空のレスポンス。あれば復元して返す。何も書かない。

### 操作の RPC(`ChooseWeekActionAsync`・`ResolveCardOfferAsync`・`DecideExamRetryAsync`・`ExecuteLessonActionAsync`)

1. 進行中のセッションが無ければ `PRODUCE_NOT_IN_PROGRESS`。古ければ `PRODUCE_SESSION_DISCARDED`(**何も書かない**。業務エラーはトランザクションを巻き戻すため、破棄の書き込みは次の `SignInAsync` / `StartAsync` が行う [決定。2026-11-03。ユーザーの判断。api-design.md 5章を直した])
2. 要求の `OperationNumber` が状態の `NextOperationNumber` と違えば `PRODUCE_STALE_OPERATION`
3. スナップショットを復元(`ToState`)し、`SessionRandomStreams` を作る
4. コマンドを作る: `Api` が DTO から作る(`OfferPick` など。`ChooseWeekAction` は、使わない引数の正規化に局面の情報が要るので `Application` の `ProduceCommandNormalizer.Normalize` が行う: `WeekSelect` で枠が範囲内なら、枠の種類がレッスンなら `ChooseLesson(slot, param)`、授業なら `ChooseClass(slot, option)`、それ以外なら `ChooseOther(slot)`。それ以外のときは受けた値のまま渡し、`Apply` が拒否する)
5. `ProduceEngine.Apply`。`UnexpectedPhase` → `PRODUCE_UNEXPECTED_PHASE`、`InvalidArgument` → `PRODUCE_INVALID_ARGUMENT`(どちらも何も保存しない)
6. `Finished` になったら 4章を行う
7. スナップショットとストリームの状態を、楽観ロックつきで保存。0 行なら `PRODUCE_STALE_OPERATION`
8. Response を作る。`Common` は基盤が入れる(4章の報酬を付けた操作の応答だけ、報酬後の通貨と経験値の累計が入る。それ以外の操作では null)。`ExamResult` は、この操作で `LastExam` が新しくなったとき(参照が変わったとき)と、`Phase` が `ExamResult` のとき

### `AbandonAsync`

進行中が無ければ `PRODUCE_NOT_IN_PROGRESS`、古ければ `PRODUCE_SESSION_DISCARDED`(何も書かない)。あれば `status` を放棄にし(楽観ロックつき)、`produce_results` に放棄で 1 行(0 で埋める)。Response は `ProduceResultResponse`(終わり方 = 放棄、プレイヤーの現在の経験値の累計)。

### 破棄(`SignInAsync`・`StartAsync` の中)

古い進行中のセッションを見つけたら、`status` を破棄にし(楽観ロックつき)、`produce_results` に破棄で 1 行(0 で埋める)。報酬なし。`SignInAsync` は、`DiscardedProduce` を true、`HasActiveProduce` を false にして返す。古くなければ `HasActiveProduce` を true にする。**`SignInAsync` は、進行中のセッションがあるときだけ `x-app-version` を読む**(無い・整数でなければ `PRODUCE_INVALID_ARGUMENT`)。

### `x-app-version` の読み方

`Api` の `AppVersionHeader.Read(ServiceContext)`(`Context.CallContext.RequestHeaders.GetValue(ApiHeaders.AppVersion)`。ヘッダーは `int` で読めなければ `null`)。`ProduceService` の RPC は、`null` なら `PRODUCE_INVALID_ARGUMENT`。

## 6. DTO の形 [決定。2026-11-03]

`shared/Uta.Shared.Contracts/Produce/` に置く。MessagePack の `[SerializationConstructor]` と整数キー(0 から宣言順)、配列は `T[]`([api-design.md](../api-design.md) 1.1 と同じ作法)。enum は `Uta.Shared.Domain` の enum をそのまま使う(`Uta.Shared.Contracts` は `Uta.Shared.Domain` を参照する)。Contracts に新しく置く enum は 3 つ: `CardOfferActionKind`(`Nothing` 0 / `Pick` 1 / `Reroll` 2 / `Remove` 3 / `Decline` 4)、`LessonActionKind`(`Nothing` 0 / `UseCard` 1 / `UseDrink` 2 / `Skip` 3)、`ProduceEnding`(`Nothing` 0 / `Completed` 1 / `Abandoned` 2 / `Discarded` 3。`ProduceSessionStatus` の 1〜3 と同じ値)。

Request([produce-domain.md](../produce-domain.md) 8章のとおり):

| 型 | フィールド(宣言順) |
|---|---|
| `StartProduceRequest` | `IdolMasterId`(`int`)、`SupportCardUniqueIds`(`string[]`)、`RentalCandidateMasterId`(`int`)、`ScenarioMasterId`(`int`) |
| `ChooseWeekActionRequest` | `OperationNumber`、`SlotIndex`、`OptionIndex`、`Param`(`ParamKind`) |
| `ResolveCardOfferRequest` | `OperationNumber`、`Kind`(`CardOfferActionKind`)、`OfferIndex` |
| `DecideExamRetryRequest` | `OperationNumber`、`Retry`(`bool`) |
| `ExecuteLessonActionRequest` | `OperationNumber`、`Kind`(`LessonActionKind`)、`CardInstanceId`、`DrinkSlotIndex` |

Response:

| 型 | フィールド(宣言順) |
|---|---|
| `ProduceStateResponse` | `OperationNumber`、`Phase`(`ProducePhase`)、`ScenarioMasterId`、`IdolMasterId`、`Week`、`Singing`、`Rhythm`、`Expression`、`Hp`、`MaxHp`、`ExamRetryLeft`、`Deck`(`DeckCardDto[]`)、`WeekActions`(`WeekActionDto[]`)、`ClassOptions`(`ClassOptionDto[]`)、`CardChoiceMasterIds`(`int[]`)、`Lesson`(`LessonDto`。無ければ `null`)、`ExamResult`(`ExamResultDto`。無ければ `null`)、`Result`(`ProduceResultResponse`。無ければ `null`)、`Common`(`AppCommonDto`。基盤が入れる。変わったものが無ければ `null`。[server-foundation.md](server-foundation.md) 共通レスポンス項目) |
| `DeckCardDto` | `InstanceId`、`CardMasterId`、`Enhanced`(`bool`)。`Deck` は `InstanceId` の昇順 |
| `WeekActionDto` | `SlotIndex`、`Kind`(`WeekChoiceKind`)、`Selectable`(`bool`)。`GetWeekChoices` の結果をそのまま |
| `ClassOptionDto` | `Index`、`HpCost`、`Param`(`ParamKind`)、`ParamGain`、`GivesCard`(`bool`)、`Selectable`(`bool`)。`GetClassOptions` の結果をそのまま |
| `LessonDto` | `Kind`(`LessonKind`)、`TargetParam`(`ParamKind`)、`MaxTurns`、`ClearTarget`、`PerfectTarget`、`ExamTurns`(`ExamTurnDto[]`。試験のときだけ)、`Turn`、`Shield`、`UsesLeft`、`ParamGained`(試験ではスコア)、`Hand`(`int[]`。手札の並びのまま)、`UsableHand`(`bool[]`。`Hand` と同じ長さ・同じ順。`LessonEngine.CanUse` が Ok のとき true。レッスン・試験のとき。クライアントの手札のグレーアウトに使う。[screen-design.md](../screen-design.md) 6章。2026-11-03 追加)、`DrawPile` / `Discard` / `Exiled`(`int[]`。インスタンス ID の昇順。山札の並びは返さない)、`StatusValues`(`int[]`。`StatusKind` の数値が添字)、`ParamBonuses`(`ParamBonusDto[]`)、`Persistents`(`PersistentDto[]`)、`LessonEnhanced`(`int[]`)、`NpcScores`(`int[]`。試験のときだけ。`ProduceFormulas.NpcScores(週の RivalBaseScore)`)。体力は `ProduceStateResponse` の `Hp` / `MaxHp`(レッスン・試験の間は `LessonState` の値) |
| `ExamTurnDto` | `Param`(`ParamKind`)、`ScoreBonusPercent` |
| `ParamBonusDto` | `Percent`、`RemainingTurns` |
| `PersistentDto` | `Trigger`(`PersistentTrigger`)、`RemainingTurns`、`RemainingCount`(無制限は -1) |
| `ExamResultDto` | `Kind`(`ExamKind`)、`Attempt`、`Score`、`Rank`、`NpcScores`(`int[]`)、`Passed`(`bool`) |
| `ProduceResultResponse` | `Ending`(`ProduceEnding`)、`ExamOutcome`、`EvaluationValue`、`Rank`(`ProduceRank`)、`FinalScore`、`FinalRank`、`GachaCurrency`、`Gold`、`SupportPoints`、`Exp`(以上は付与した量・結果)、`PlayerExp`(付与したあとのプレイヤーの経験値の累計。2026-11-09 に `PlayerLevel` を消した。4.1) |

- `Phase` が `Nothing` のレスポンスは、数値は 0、配列は空、オブジェクトは `null`
- エラーは `Uta.Shared.Contracts.Produce.ProduceErrors`(`static readonly ErrorCode`。domain `uta`、reason は [api-design.md](../api-design.md) 2章の `PRODUCE_` 7 つ)

## 7. ファイルの置き場

| 層 | 置くもの |
|---|---|
| `Uta.Shared.Contracts/Produce/` | `IProduceService`、Request・Response・DTO、enum 3 つ、`ProduceErrors` |
| `Uta.Server/Api/` | `ProduceService`(`[GameServiceFilter]`)、`AppVersionHeader`、`ProduceResponseMapper`(`ProduceStateResult` → `ProduceStateResponse`、`ProduceEndResult` → `ProduceResultResponse`) |
| `Uta.Server/Application/Produce/` | `ProduceVersions`、`ProduceStateResult`、`ProduceEndResult`、`ProduceStateResultFactory`、`ProduceCommandNormalizer`、`ProduceSessionDiscarder`、`StartProduceUseCase`、`GetProduceStateUseCase`、`AbandonProduceUseCase`、`ProduceOperationUseCase`、`ProduceCompletion` |
| `Uta.Server/Domain/Produce/` | `RandomStreamState`、`SessionRandomStreams`、`ProduceSessionStatus`、`ProduceSessionData`・`ProduceSessionRecord`・`ProduceResultRecord`、`IProduceSessionRepository`、`ProduceReward`(2026-11-08。[ADR 0021](../../decisions/0021-domain-entities-and-repositories.md)) |
| `Uta.Server/Domain/Player/` | `PlayerExpRules`(2026-11-09。`Domain/Produce/PlayerLevelRules` を置き換える。4.1) |
| `Uta.Shared.Domain/Player/` | `PlayerLevelCatalog`(2026-11-09。4.1) |
| `Uta.Server/Infrastructure/` | `ProduceSessionRepository`、`Produce/ProduceStateBlobs.cs`(Blob の型)、`Produce/ProduceStateSerializer.cs`、`MasterData/ProduceCatalogFactory.cs`(`ProduceMasterRows` の詰め替え。2026-11-08 に `MasterProduceCatalog` から改名)、`MasterData/PlayerLevelCatalogFactory.cs`(2026-11-09。4.1)。報酬は `ProduceCompletion` がマスタから直接引く(2026-11-08 に `IProduceRewardCatalog`・`IPlayerLevelCatalog` をやめた) |

## 未決事項

なし

## 実装メモ

実装 [2026-11-03。計画 `plans/2026-11-03-produce-snapshot.md`・`server-produce-start.md`・`server-produce-play.md`]。設計とのズレと、設計に足したこと:

- **スナップショットの置き場**: `LessonSnapshot` も `shared/Uta.Shared.Domain/Produce/LessonSnapshot.cs`(produce-domain.md 6.1)。`From` に渡すカードのカタログは、エンジンの `ProduceCatalog.Cards` と同じインスタンスでなければならない(持続効果の由来を参照一致で見つけるため。サーバーは `ProduceCatalog` を Singleton で 1 つ持つ)
- **古いセッション**: `ProduceVersions.IsCurrent` は `Data != null` も条件に含む。`state_version` が違う・`state` が読めないときは `Data = null` で、`GetState` は `Nothing`、`SignInAsync`・`StartAsync` が破棄する。`ToState` が例外を投げる状態(マスタからカードが消えたなど)は、`rule_version` を上げる運用で防ぐ前提で、内部エラーのまま
- **`[GameServiceFilter]`**: `AttributeUsage` を Class | Method に広げ、`PlayerService.SignInAsync` のメソッドにだけ付けた(`SignUpAsync` を弾かないため)。`ToErrorCode` は `internal static`
- **操作の流れ**: `ProduceOperationUseCase`(古いセッション → 操作番号 → 復元 → 正規化 → `Apply` → 終了なら `ProduceCompletion` → 楽観ロックつき保存 → 応答)。`ExamResult` は `LastExam` の参照が変わったときと局面が `ExamResult` のときだけ。最後の応答(`DecideExamRetry` の応答)の `ExamResult` は `null`
- **楽観ロックの競合**: `FindActiveAsync` は行ロックを取らないので、事前に `version` を進めても競合にならない(読んだ値で更新に成功する)。競合になるのは読んだあとの割り込みだけで、テストは `IProduceSessionRepository` を包んで読んだ直後に別接続で `version` を進める(`ConflictingSessionRepository`)
- **テストの支え**: `ProduceDriver`(局面を見て次の RPC を呼ぶ自動プレイ)、`PlayerClient.WithAppVersion` / `OnChannel`、`RpcAssert`。保存した操作列を再生して保存した最終状態と一致することを `ProducePlayTests.Replay_FromSavedOperations_MatchesSavedState` で確かめる
- 未確認: Unity 7(IL2CPP)での `Uta.Shared.Contracts/Produce` のコンパイルと MessagePack の動作。同じプレイヤーの同時要求の実測(基盤の直列化と楽観ロックの設計は読んだ)。`state` の大きさ(数十ストリーム)の実測
