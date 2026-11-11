---
title: 実装計画: プレイヤーは経験値の累計だけを持ち、Lv は累計から求める
status: ready
updated: 2026-11-09
spec: ../wiki/systems/produce.md, ../wiki/architecture/server/produce-server.md, ../wiki/architecture/master/produce-tables.md, ../wiki/architecture/api-design.md, ../wiki/architecture/screen-design.md
---

# 実装計画: プレイヤーは経験値の累計だけを持ち、Lv は累計から求める

## この計画で決めたこと

設計の正は [produce-server.md](../wiki/architecture/server/produce-server.md) 4.1（型・付与・DTO・マイグレーション・テストの移し先）。下はそこに無い、計画を書くときに決めたこと。

1. **クライアントの `PlayerLevelCatalog` の Factory・登録・ホームの Lv の表示を、この計画で足す**（2026-11-09。ユーザーの判断。Claude の推奨どおり）。wiki は「ホームの上部のステータスはまだ無いので別の計画で足す」としていたが、今の `HomeDtoFactory` は既に「プレイヤーLv 12　ガチャ通貨 3,250」を出している。`PlayerEntity.Level` を消すとこの表示が組めなくなるため、同じ変更で `PlayerLevelCatalog.GetLevel(exp)` に置き換え、表示を変えない。wiki の 2 か所（screen-design.md 7.5、produce-server.md 4.1）はこの計画の PR で直した
2. **順番: owned（順 6）の実装の PR の後、formation（順 8）の実装ブランチを切る前に、この計画を `main` へ入れる**（2026-11-09。ユーザーの判断。Claude の推奨どおり）。owned は client の `Domain/Player/`・`Infrastructure/Player/` を触らない（owned のブランチの差分で確かめた）。重なるのは `Bootstrap/BootstrapEntry.cs`（owned は登録の行を足し、この計画はマスタの読み込みの所に 2 行足す。別の行）と `Tests/EditMode/HomePresenterTests.cs`（owned は「アイドル」のボタンのテスト、この計画は `SetPlayer` と `CreatePresenter`）。formation・lesson の計画は `ProduceResultResponse.PlayerLevel` を使っているので、この計画の PR で両方の先頭に読み替えの節を足した（formation・lesson の実装は、この計画が `main` に入ってから）
2a. **実装ブランチは、[shared/ の改名の計画](2026-11-11-shared-naming-renames.md)の実装が main に入ってから切る**（2026-11-11。ユーザーの判断。順番: shared/ の改名 → この計画 → formation）。client の `GachaFixtures.cs` の `IdolResult`・`SupportCardResult` は、その計画で引数の型が `GachaResultKind` になっている
3. タスクの切り方: タスク 1（shared）、タスク 2（masterdata・server・Contracts）、タスク 3（client）。Contracts の形を変えるタスク 2 の後は、タスク 3 が終わるまで Unity のコンパイルが通らない。タスク 1〜3 は 1 本の実装の PR に入れる（2026-11-09。ユーザーの判断。Claude の推奨どおり。Contracts をタスク 3 に寄せてサーバーを先に別の PR にする案は、一時的なコードが要るので採らない）。Unity の外で確かめられるのはタスク 1・2 まで
3a. **実装ブランチは owned の実装ブランチ `claude/client-screens-owned` の上に積む**（2026-11-09。ユーザーの判断。Claude の推奨は「owned が `main` に入ってから `main` で切る」）。CLAUDE.md「Git」の積み PR: 空いている固定の worktree で `tools/worktree-pool.sh start <番号> claude/player-level-from-exp origin/claude/client-screens-owned`（owned の最新を push してもらってから）→ PR は `gh pr create --base claude/client-screens-owned`、本文に前提の PR（owned の実装の PR）を書く。owned が `main` に入ったら、ユーザーが base を `main` に付け替える（`gh pr edit --base main`）。タスク 1〜3 はすぐ始めてよい。owned のブランチに後からコミットが足されたら、タスク 3 の前に `git merge origin/claude/client-screens-owned` で取り込む（衝突は `BootstrapEntry.cs`・`HomePresenterTests.cs` の別の行なので、両方を残す。同じ行がぶつかったら止まって報告する）
3b. **ローカルの PostgreSQL は全 worktree で 1 つ**（`server/compose.yaml` のポートとボリュームが固定、`UserSecretsId` も同じ）。0008 を `migrate` すると `players.level` が無くなり、owned の worktree のサーバー（`SELECT level`）はサインインで失敗する。DbUp は戻さないので、owned 側で `migrate` し直しても戻らない。そのため、タスク 3 で 0008 を当てるのは owned の Play の確認が済んでからにする（2026-11-09。Claude が決めた。並行して進める 3a の帰結で、設計は変えない）。owned の確認を後からやり直すときは、`docker compose --env-file server/.env -f server/compose.yaml down -v` で DB を作り直し、owned のブランチで `migrate` する（データは消える。リリース前で残すものは無い）。タスク 1・2 の結合テストは Testcontainers の DB なので、この制約を受けない
4. クライアントの Factory は `client/uta/Assets/Scripts/Infrastructure/MasterData/PlayerLevelCatalogFactory.cs`（名前空間 `Uta.Client.Infrastructure`。今の `CardCatalogFactory` と同じ場所・同じ名前空間）。サーバーの Factory も今の `CardCatalogFactory`・`ProduceCatalogFactory` と同じ名前空間 `Uta.Server.Infrastructure`（wiki の置き場 `Infrastructure/MasterData/` のまま）
5. クライアントの登録は `BootstrapEntry` で、`CardCatalog` と同じ `try` の中で作り、`builder.RegisterInstance(playerLevelCatalog)` で具象のまま登録する（インターフェースは作らない: produce-server.md 4.1）
6. テストで Lv を作る表は、マスタの初期値と同じ式（Lv L の `FromExp` = `20 × L × (L − 1)`、100 行）を持つ `TestPlayerLevels.Catalog()`（`client/uta/Assets/Tests/EditMode/Support/TestPlayerLevels.cs`）にまとめる。`TestMasterData.Build(_ => { })` の空のマスタには `PlayerLevelExp` の行が無いので、マスタから作らない
7. マイグレーションの番号は `0008`（`server/Uta.Server/Migrations/` は 0007 まで。2026-11-09 の `origin/main` で確かめた）。タスク 2 を始めるときに 0008 が既にあれば、その次の番号にする（wiki 4.1 のとおり）
8. マイグレーションが既存の行の `exp` を 0 にすることは、結合テストで確かめない（Testcontainers の DB は毎回空から作るので、移行前の行が無い）。列が無くなったことだけを確かめる

## ユーザーの確認が要ること

なし（2 件とも 2026-11-09 に解決し、上の 1・2 に移した）

## 1. 目的と前提

- 何を実装するか: プレイヤーの経験値を累計で持ち、Lv を持たない形にする。Lv はマスタ `PlayerLevelExp`（Lv ごとの累計の範囲 `FromExp`・`ToExp`）から `Uta.Shared.Domain` の `PlayerLevelCatalog` で求める。経験値の付与はサーバーの `PlayerExpRules`（累計に足して上限で止める）。仕様は [produce.md](../wiki/systems/produce.md) 7章、設計は [produce-server.md](../wiki/architecture/server/produce-server.md) 4章・4.1、マスタは [produce-tables.md](../wiki/architecture/master/produce-tables.md) の `PlayerLevelExp`、DTO は [api-design.md](../wiki/architecture/api-design.md) 1章（`SignInResponse`）と [produce-server.md](../wiki/architecture/server/produce-server.md) 6章、クライアントは [screen-design.md](../wiki/architecture/screen-design.md) 7.5「プレイヤーLv」
- 決定の記録: [raw/2026-11-09-player-level-from-exp-decisions.md](../raw/2026-11-09-player-level-from-exp-decisions.md)
- 関係する ADR: [ADR 0007](../wiki/decisions/0007-masterdata-with-addons.md)（マスタの生成）、[ADR 0008](../wiki/decisions/0008-shared-domain-and-onion.md)（Shared.Domain）、[ADR 0012](../wiki/decisions/0012-migration-dbup.md)（DbUp）、[ADR 0020](../wiki/decisions/0020-masterdata-referenced-outside-shared-domain.md)（マスタから Shared.Domain の型を作る Factory）、[ADR 0021](../wiki/decisions/0021-domain-entities-and-repositories.md)（エンティティとリポジトリ）
- **決めないこと（範囲外）**:
  - ホームの上部のステータスの作り直し（並べ方・見た目）。この計画は今の `profileText` の文字を変えない
  - 機能の解放（Lv5 相談での強化など。produce.md 7章。MVP では作らない）
  - プロデュースの結果の画面の Lv の表示（lesson の計画が作る。この計画は lesson の計画の読み替えの節を足すだけ）
  - `ProduceSnapshot.PlayerLevel`（開始時の入力の写し）は変えない。`StateVersion` は上げない（`state` の形は変わらない）

## 2. 触るファイル

パスはリポジトリのルートから。`client/uta/Assets/` は `Assets/` と略す。

| 操作 | パス | 中身 | タスク |
|---|---|---|---|
| 作成 | `shared/Uta.Shared.Domain/Player/PlayerLevelCatalog.cs` | Lv の表（3.1） | 1 |
| 作成 | `shared/Uta.Shared.Domain.Tests/Player/PlayerLevelCatalogTests.cs` | 5章 | 1 |
| 変更 | `masterdata/Definitions/Produce/PlayerLevelExp.cs` | `RequiredExp` → `FromExp`・`ToExp`（3.2） | 2 |
| 変更 | `masterdata/csv/playerLevelExp.csv` | 100 行に作り直す（3.2） | 2 |
| 変更（再生成） | `server/Uta.Server/Infrastructure/MasterData/Generated/Produce/PlayerLevelExp.cs`・`Generated/MasterDataSchema.cs`・`masterdata.bytes`・`masterdata.manifest.json` | `gen.ps1` が作り直す（手で直さない） | 2 |
| 変更（再生成） | `Assets/Scripts/MasterData/Generated/Produce/PlayerLevelExp.cs`・`Generated/MasterDataSchema.cs`、`Assets/StreamingAssets/MasterData/masterdata.bytes`・`masterdata.manifest.json` | `gen.ps1` が作り直す。ファイルは増えないので `.meta` は増えない | 2 |
| 作成 | `server/Uta.Server/Migrations/0008_drop_player_level.sql` | 3.3 | 2 |
| 作成 | `server/Uta.Server/Domain/Player/PlayerExpRules.cs` | 3.4 | 2 |
| 削除 | `server/Uta.Server/Domain/Produce/PlayerLevelRules.cs` | `PlayerExpRules` に置き換える | 2 |
| 変更 | `server/Uta.Server/Domain/Player/PlayerEntity.cs` | `Level` を消す（3.5） | 2 |
| 変更 | `server/Uta.Server/Domain/Player/IPlayerRepository.cs` | `GrantProduceRewardAsync` の `level` を消す（3.5） | 2 |
| 変更 | `server/Uta.Server/Infrastructure/PlayerRepository.cs` | `SELECT`・`UPDATE`・`Row` から `level` を消す（3.5） | 2 |
| 作成 | `server/Uta.Server/Infrastructure/MasterData/PlayerLevelCatalogFactory.cs` | 3.6 | 2 |
| 変更 | `server/Uta.Server/Hosting/ServerHost.cs` | `PlayerLevelCatalog` をシングルトンに登録（3.6） | 2 |
| 変更 | `server/Uta.Server/Application/Produce/ProduceEndResult.cs` | `PlayerLevel` を消す（3.7） | 2 |
| 変更 | `server/Uta.Server/Application/Produce/ProduceCompletion.cs` | `PlayerExpRules`・`PlayerLevelCatalog` で付与とログ（3.7） | 2 |
| 変更 | `server/Uta.Server/Application/Produce/AbandonProduceUseCase.cs` | `ProduceEndResult` の引数（3.7） | 2 |
| 変更 | `server/Uta.Server/Application/Produce/StartProduceUseCase.cs` | 開始時の Lv を `PlayerLevelCatalog` で求める（3.7） | 2 |
| 変更 | `server/Uta.Server/Application/Produce/ProduceVersions.cs` | `RuleVersion` を 2 に | 2 |
| 変更 | `server/Uta.Server/Infrastructure/PlayerDiffCollector.cs` | `level` を詰めない（3.7） | 2 |
| 変更 | `server/Uta.Server/Api/PlayerService.cs` | `SignInResponse` の引数（3.7） | 2 |
| 変更 | `server/Uta.Server/Api/ProduceResponseMapper.cs` | `ProduceResultResponse` の引数（3.7） | 2 |
| 変更 | `shared/Uta.Shared.Contracts/Player/SignInResponse.cs` | `Level` を消してキーを詰める（3.8） | 2 |
| 変更 | `shared/Uta.Shared.Contracts/Common/PlayerDiffDto.cs` | 同上（3.8） | 2 |
| 変更 | `shared/Uta.Shared.Contracts/Produce/ProduceResultResponse.cs` | `PlayerLevel` を消してキーを詰める（3.8） | 2 |
| 作成 | `server/Uta.Server.Tests/Domain/PlayerExpRulesTests.cs` | 5章 | 2 |
| 削除 | `server/Uta.Server.Tests/Domain/PlayerLevelRulesTests.cs` | | 2 |
| 変更 | `server/Uta.Server.Tests/MasterData/MasterDataTests.cs`・`MasterDataInvariantTests.cs` | 5章 | 2 |
| 変更 | `server/Uta.Server.Tests/Integration/CommonResponseTests.cs`・`PlayerSignUpSignInTests.cs`・`ProduceOperationTests.cs`・`ProducePlayTests.cs`・`ProduceStartTests.cs` | 5章 | 2 |
| 作成 | `Assets/Scripts/Infrastructure/MasterData/PlayerLevelCatalogFactory.cs` | 3.9 | 3 |
| 変更 | `Assets/Scripts/Bootstrap/BootstrapEntry.cs` | 作って登録する（3.9） | 3 |
| 変更 | `Assets/Scripts/Domain/Player/PlayerEntity.cs` | `Level` を消す（3.9） | 3 |
| 変更 | `Assets/Scripts/Infrastructure/Player/PlayerMapper.cs`・`PlayerDiffApplier.cs` | `Level` を渡さない（3.9） | 3 |
| 変更 | `Assets/Scripts/Presentation/Home/HomeDtoFactory.cs` | `PlayerLevelCatalog` で Lv を出す（3.9） | 3 |
| 変更 | `Assets/Scripts/Presentation/Title/StartupLogs.cs` | `level=` を消す（3.9） | 3 |
| 作成 | `Assets/Tests/EditMode/Support/TestPlayerLevels.cs` | 3.10 | 3 |
| 変更 | `Assets/Tests/EditMode/` の 12 ファイル（5章の表）と、owned が足したテストのうち `new PlayerEntity(` を書くもの | 5章 | 3 |
| 作成（Unity が作る） | `shared/Uta.Shared.Domain/Player.meta`・`Player/PlayerLevelCatalog.cs.meta`、`Assets/Scripts/Infrastructure/MasterData/PlayerLevelCatalogFactory.cs.meta`、`Assets/Tests/EditMode/Support/TestPlayerLevels.cs.meta` | Editor が取り込んだときに作る。手で書かず、作られたものをコミットする | 3 |

（この計画の PR で直した docs: `uta_docs/wiki/architecture/screen-design.md` 7.5、`uta_docs/wiki/architecture/server/produce-server.md` 4.1、`uta_docs/plans/2026-11-03-client-screens-formation.md`・`2026-11-03-client-screens-lesson.md` の読み替えの節、`uta_docs/plans/2026-11-07-client-screens-index.md` 1章の順番の行）

## 3. 型とシグネチャ

### 3.1 `PlayerLevelCatalog`（shared。C# 9）

```csharp
using System;
using System.Collections.Generic;

namespace Uta.Shared.Domain.Player
{
    /// <summary>プレイヤー Lv ごとの経験値の累計の下限の表（produce-server.md 4.1）。マスタ PlayerLevelExp から各側の Factory が 1 度だけ作る。</summary>
    public sealed class PlayerLevelCatalog
    {
        private readonly int[] fromExps;

        private PlayerLevelCatalog(int[] fromExps)
        {
            this.fromExps = fromExps;
        }

        /// <summary>最大 Lv（fromExps の個数）。</summary>
        public int MaxLevel => fromExps.Length;

        /// <summary>経験値の累計の上限（最後の fromExps。最大 Lv の行は FromExp = ToExp）。</summary>
        public int MaxExp => fromExps[fromExps.Length - 1];

        /// <param name="fromExps">fromExps[i] は Lv i+1 の FromExp。写して持つ。</param>
        /// <exception cref="ArgumentNullException">fromExps が null</exception>
        /// <exception cref="ArgumentException">空、fromExps[0] が 0 でない、狭義の単調増加でない</exception>
        public static PlayerLevelCatalog Create(IReadOnlyList<int> fromExps);

        /// <summary>fromExps[i] &lt;= totalExp を満たす最大の i に 1 を足す。MaxExp を超えていても MaxLevel。</summary>
        /// <exception cref="ArgumentOutOfRangeException">totalExp が負</exception>
        public int GetLevel(int totalExp);
    }
}
```

- `Create` の手順: `fromExps == null` → `ArgumentNullException(nameof(fromExps))`。`Count == 0` → `ArgumentException("fromExps が空", nameof(fromExps))`。`fromExps[0] != 0` → `ArgumentException("Lv 1 の FromExp が 0 でない", nameof(fromExps))`。`i` = 1〜`Count − 1` で `fromExps[i] <= fromExps[i − 1]` → `ArgumentException($"Lv {i + 1} の FromExp が前の Lv 以下", nameof(fromExps))`。通れば `new int[Count]` に写して `new PlayerLevelCatalog(copy)`
- `GetLevel` の手順: `totalExp < 0` → `ArgumentOutOfRangeException(nameof(totalExp))`。二分探索（`fromExps[i] <= totalExp` を満たす最大の `i`）で `i + 1` を返す。`Array.BinarySearch(fromExps, totalExp)` を使い、戻り値 `r` が 0 以上なら `r + 1`、負なら `~r`（挿入位置。`fromExps[0]` は 0 で `totalExp` は 0 以上なので `~r` は 1 以上）を返す
- LINQ は使わない（`shared/` は `System.Linq` のままでよいが、要らない）。値が変わる static の状態を持たない（CLAUDE.md）

### 3.2 マスタ `PlayerLevelExp`

```csharp
[MemoryTable("playerLevelExp"), MessagePackObject(true)]
public class PlayerLevelExp : IValidatable<PlayerLevelExp>
{
    [PrimaryKey]
    public int Level { get; init; }

    /// <summary>この Lv である経験値の累計の下限（両端を含む）。</summary>
    public int FromExp { get; init; }

    /// <summary>この Lv である経験値の累計の上限（両端を含む）。最大 Lv の行は FromExp と同じで、累計の上限。コードは読まない（検証と読みやすさのための列。Lv は FromExp だけで決まる）。</summary>
    public int ToExp { get; init; }

    void IValidatable<PlayerLevelExp>.Validate(IValidator<PlayerLevelExp> validator)
    {
        validator.Validate(x => x.Level >= 1, "playerLevelExp: Level が 1 未満");
        validator.Validate(x => x.FromExp >= 0, "playerLevelExp: FromExp が 0 未満");
        validator.Validate(x => x.FromExp <= x.ToExp, "playerLevelExp: FromExp が ToExp より大きい");
    }
}
```

CSV（`masterdata/csv/playerLevelExp.csv`。LF、BOM なし。今のファイルと同じ）: 1〜4 行目は

```
PlayerLevelExp,,
出典: uta_docs/wiki/systems/produce.md 7章,,
int,int,int
Level,FromExp,ToExp
```

5 行目から Lv 1〜100 の 100 行。Lv L の `FromExp` = `20 × L × (L − 1)`、L < 100 の `ToExp` = `20 × L × (L + 1) − 1`、Lv 100 は `198000,198000`。リポジトリのルートで次を実行して作る（Git Bash）:

```bash
{ printf 'PlayerLevelExp,,\n出典: uta_docs/wiki/systems/produce.md 7章,,\nint,int,int\nLevel,FromExp,ToExp\n'; for L in $(seq 1 100); do if [ "$L" -lt 100 ]; then echo "$L,$((20*L*(L-1))),$((20*L*(L+1)-1))"; else echo "100,198000,198000"; fi; done; } > masterdata/csv/playerLevelExp.csv
```

先頭の 3 行は `1,0,39`・`2,40,119`・`3,120,239`、99 行目（ファイルの 103 行目）は `99,194040,197999`、最後の行は `100,198000,198000`。

### 3.3 マイグレーション `0008_drop_player_level.sql`（LF、BOM なし）

```sql
-- プレイヤーは経験値の累計だけを持つ（produce-server.md 4.1）。今の「今のレベルの中の経験値」は捨てる（リリース前）
ALTER TABLE players DROP COLUMN level;
UPDATE players SET exp = 0;
```

`server/Uta.Server/Uta.Server.csproj` の `<EmbeddedResource Include="Migrations\*.sql" />` で取り込まれるので、csproj は変えない。

### 3.4 `PlayerExpRules`（server。C# 14）

```csharp
namespace Uta.Server.Domain.Player;

public static class PlayerExpRules
{
    /// <summary>経験値の累計に gain を足し、maxExp で止める。totalExp が既に maxExp 以上なら totalExp のまま（上限をマスタで下げても減らさない）。</summary>
    /// <exception cref="ArgumentOutOfRangeException">gain が負</exception>
    public static int AddExp(int totalExp, int gain, int maxExp)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(gain);
        if (totalExp >= maxExp)
        {
            return totalExp;
        }

        return Math.Min(totalExp + gain, maxExp);
    }
}
```

### 3.5 サーバーのプレイヤー

- `PlayerEntity`: `public sealed record PlayerEntity(int Exp, int Gold, int SupportPoints, int GachaCurrency);`（`Exp` は累計）
- `IPlayerRepository.GrantProduceRewardAsync(TxContext tx, Ulid playerId, ProduceReward reward, int exp)`。summary は「プロデュースの報酬を加え、経験値の累計を上書きする（1 文）。」
- `PlayerRepository`:
  - `FindAsync` の SQL: `SELECT exp AS Exp, gold AS Gold, support_points AS SupportPoints, gacha_currency AS GachaCurrency FROM players WHERE player_id = @playerId`。`new PlayerEntity(row.Exp, row.Gold, row.SupportPoints, row.GachaCurrency)`
  - `GrantProduceRewardAsync` の SQL: `UPDATE players SET gacha_currency = gacha_currency + @gachaCurrency, gold = gold + @gold, support_points = support_points + @supportPoints, exp = @exp WHERE player_id = @playerId`（パラメーターから `level` を消す）
  - private の `Row` から `Level` を消す

### 3.6 サーバーの Factory と登録

```csharp
using Uta.Shared.Domain.Player;

namespace Uta.Server.Infrastructure;

/// <summary>マスタ PlayerLevelExp から PlayerLevelCatalog を作る（produce-server.md 4.1）。All は主キー（Level）の昇順なので並べ替えない。</summary>
public static class PlayerLevelCatalogFactory
{
    public static PlayerLevelCatalog Create(Uta.MasterData.MemoryDatabase master) =>
        PlayerLevelCatalog.Create(master.PlayerLevelExpTable.All.Select(x => x.FromExp).ToArray());
}
```

`ServerHost.AddServices` の `ProduceCatalogFactory` の登録の次の行に `services.AddSingleton(sp => PlayerLevelCatalogFactory.Create(sp.GetRequiredService<MemoryDatabase>()));` を足す。`using Uta.Shared.Domain.Player;` が要れば足す。

### 3.7 サーバーの Application・Api

- `ProduceEndResult`: 最後の 2 つを `int Exp, int PlayerExp` にする（`PlayerLevel` を消す）。`PlayerExp` は付与したあとの累計（放棄は今の累計）
- `ProduceCompletion`・`StartProduceUseCase` に `using Uta.Shared.Domain.Player;` を足す
- `ProduceCompletion`: コンストラクターに `PlayerLevelCatalog levelCatalog` を最後に足す（`ProduceCompletion(IPlayerRepository players, IProduceSessionRepository sessions, MemoryDatabase master, PlayerLevelCatalog levelCatalog)`）。`CompleteAsync` の中:
  - `requiredExps` の行と `PlayerLevelRules.AddExp` の行を消し、`var exp = PlayerExpRules.AddExp(player.Exp, reward.Exp, levelCatalog.MaxExp);`
  - `await players.GrantProduceRewardAsync(tx, playerId, reward, exp);`
  - ゲームログの `level_after` は `levelCatalog.GetLevel(exp)`、`exp_after` は `exp`
  - 戻り値の `new ProduceEndResult(...)` の最後を `reward.Exp, exp`
  - summary の「報酬・Lv・」は「報酬・経験値・」にする
- `AbandonProduceUseCase`: `new ProduceEndResult(ProduceSessionStatus.Abandoned, ExamOutcome.Nothing, 0, ProduceRank.Nothing, 0, 0, 0, 0, 0, 0, player.Exp)`
- `StartProduceUseCase`: コンストラクターの最後に `PlayerLevelCatalog levelCatalog` を足す。82 行目付近の `player.Level` を `levelCatalog.GetLevel(player.Exp)` にする
- `ProduceVersions.RuleVersion` を `2` にする（summary は今のまま）
- `PlayerDiffCollector.BuildAsync`: `var level = 0;` と `level = snapshot.Level;` を消し、`new PlayerDiffDto(hasBalance, exp, gold, supportPoints, gachaCurrency, idolDtos.ToArray(), cardDtos.ToArray(), removed)`
- `PlayerService`（34 行目付近）: `new SignInResponse(player.Exp, player.Gold, player.SupportPoints, player.GachaCurrency, result.HasActiveProduce, result.DiscardedProduce, …)`（`player.Level` を消す。後ろはそのまま）
- `ProduceResponseMapper.ToResultResponse`: 最後を `end.Exp, end.PlayerExp`（`end.PlayerLevel` を消す）
- DI: `StartProduceUseCase`・`ProduceCompletion` は `AddScoped` で登録済みなので、3.6 の登録だけで解決される（`ValidateOnBuild` が起動時に確かめる）

### 3.8 Contracts（C# 9。`[SerializationConstructor]` と整数キー）

| 型 | コンストラクターの引数（キーは 0 から宣言順） |
|---|---|
| `SignInResponse` | `int exp, int gold, int supportPoints, int gachaCurrency, bool hasActiveProduce, bool discardedProduce, OwnedIdolDto[] idols, OwnedSupportCardDto[] supportCards, string playerId`（`Exp` が `Key(0)`、`PlayerId` が `Key(8)`） |
| `PlayerDiffDto` | `bool hasBalance, int exp, int gold, int supportPoints, int gachaCurrency, OwnedIdolDto[] idols, OwnedSupportCardDto[] supportCards, string[] removedSupportCardUniqueIds`（`Exp` が `Key(1)`、`RemovedSupportCardUniqueIds` が `Key(7)`） |
| `ProduceResultResponse` | `…, int supportPoints, int exp, int playerExp`（`PlayerLevel` を消し、`PlayerExp` が `Key(10)`） |

- プロパティの宣言も同じ順で、`[Key(n)]` を 0 から振り直す
- doc コメント: `SignInResponse.Exp` は「経験値の累計（produce.md 7章）。Lv はクライアントが PlayerLevelCatalog で求める。」、`PlayerDiffDto.HasBalance` は「false なら経験値の累計と通貨（次の 4 つ）は変化なし。」、`ProduceResultResponse.PlayerExp` は「付与したあとのプレイヤーの経験値の累計。」

### 3.9 クライアント（C# 9）

- `PlayerEntity`: コンストラクター `PlayerEntity(string uniqueId, int exp, int gold, int supportPoints, int gachaCurrency, bool hasActiveProduce, bool discardedProduce)`。`Level` のプロパティを消す。`Exp` に `/// <summary>経験値の累計。Lv は PlayerLevelCatalog.GetLevel で求める（screen-design.md 7.5）。</summary>`
- `PlayerMapper.ToPlayerEntity`: `new PlayerEntity(response.PlayerId, response.Exp, response.Gold, response.SupportPoints, response.GachaCurrency, response.HasActiveProduce, response.DiscardedProduce)`
- `PlayerDiffApplier.Apply`: `new PlayerEntity(current.UniqueId, diff.Exp, diff.Gold, diff.SupportPoints, diff.GachaCurrency, current.HasActiveProduce, current.DiscardedProduce)`
- `StartupLogs`（21 行目）: `"sign-in: ok level=" + … .Level + " exp=" + …` を `"sign-in: ok exp=" + enterPlayerResult.Player.Exp + " gold=" + …`（`level=…` の部分だけを消す。後ろは変えない）
- `PlayerLevelCatalogFactory`:

  ```csharp
  using System;
  using System.Linq;
  using ZLinq;
  using Uta.MasterData;
  using Uta.Shared.Domain.Player;

  namespace Uta.Client.Infrastructure
  {
      /// <summary>マスタ PlayerLevelExp から PlayerLevelCatalog を作る（screen-design.md 7.5）。All は主キー（Level）の昇順なので並べ替えない。</summary>
      public static class PlayerLevelCatalogFactory
      {
          public static PlayerLevelCatalog Create(MemoryDatabase db)
          {
              if (db == null)
              {
                  throw new ArgumentNullException(nameof(db));
              }

              return PlayerLevelCatalog.Create(db.PlayerLevelExpTable.All.Select(x => x.FromExp).ToArray());
          }
      }
  }
  ```

- `PlayerLevelCatalogFactory` の `Select(...).ToArray()` が ZLinq で通らなければ `ToList()` にする（`List<int>` はそのまま `Create(IReadOnlyList<int>)` に渡せる）
- `BootstrapEntry.RunAsync`: `CardCatalog cardCatalog;` の次の行に `PlayerLevelCatalog playerLevelCatalog;`、`cardCatalog = CardCatalogFactory.Create(db);` の次の行に `playerLevelCatalog = PlayerLevelCatalogFactory.Create(db);`（同じ `try`。失敗はマスタの失敗のダイアログになる）。`builder.RegisterInstance<ICardCatalog>(cardCatalog);` の次の行に `builder.RegisterInstance(playerLevelCatalog);`。`using Uta.Shared.Domain.Player;` を足す。ログの `"[Uta] master: ok cards=" …` は変えない
- `HomeDtoFactory`: フィールド `private readonly PlayerLevelCatalog playerLevelCatalog;` を足し、コンストラクターを `HomeDtoFactory(IReadOnlyPlayerRepository playerRepository, FeaturedIdolPicker featuredIdolPicker, DiscardNoticeState discardNoticeState, MemoryDatabase memoryDatabase, PlayerLevelCatalog playerLevelCatalog)` にする（最後に足す）。`profileText = "プレイヤーLv " + playerLevelCatalog.GetLevel(p.Exp) + "　ガチャ通貨 " + …`（後ろは変えない）。`using Uta.Shared.Domain.Player;` を足す。登録（`builder.Register<HomeDtoFactory>(Lifetime.Singleton)`）は変えない（ルートの `PlayerLevelCatalog` が解決される）

### 3.10 テストの補助 `TestPlayerLevels`

```csharp
using Uta.Shared.Domain.Player;

namespace Uta.Client.Tests
{
    /// <summary>マスタの初期値と同じ式（Lv L の FromExp = 20 × L × (L − 1)、100 行）の PlayerLevelCatalog。Lv 12 = 2640、Lv 13 = 3120。</summary>
    public static class TestPlayerLevels
    {
        public static PlayerLevelCatalog Catalog()
        {
            var fromExps = new int[100];
            for (var i = 0; i < fromExps.Length; i++)
            {
                var level = i + 1;
                fromExps[i] = 20 * level * (level - 1);
            }

            return PlayerLevelCatalog.Create(fromExps);
        }
    }
}
```

## 4. タスク

各タスクの終わりで、そのタスクの「終わりの確かめ」が通る状態にする。生成されたファイル（`Generated/`・`masterdata.bytes`・`masterdata.manifest.json`）を手で直さない。この計画にない設計判断に出会ったら止まって報告する。.NET SDK・Unity・Docker が無い環境では、結果を推測で「通った」と書かない。

**タスク 1（shared）: `PlayerLevelCatalog`**

- 触るファイル: 2章のタスク 1 の行
- 手順: 3.1 のとおりに書く。テストは 5章
- 境界: 空・Lv 1 が 0 でない・単調増加でない・null は `Create` が例外。`totalExp` が負は例外、`MaxExp` を超えたら `MaxLevel`
- `.meta` はこのタスクでは作らない（タスク 3 で Unity が作る）
- 終わりの確かめ: `dotnet build shared/Uta.Shared.Domain` が警告 0・エラー 0、`dotnet test --project shared/Uta.Shared.Domain.Tests` が通る

**タスク 2（masterdata・server・Contracts）: 累計で持つ**

- 触るファイル: 2章のタスク 2 の行
- 手順:
  1. 3.2 の定義を書き、CSV を 3.2 のコマンドで作り直す
  2. リポジトリのルートで `powershell -NoProfile -File masterdata/gen.ps1 code-gen` → `powershell -NoProfile -File masterdata/gen.ps1 master-gen` → `powershell -NoProfile -File masterdata/gen.ps1 check`（どれも終了コード 0。実行ポリシーで読めなければ `-ExecutionPolicy Bypass` を足す）。`git status` で、`masterdata.yaml` の 2 つの出力先（`server`・`unity`）の `Generated/Produce/PlayerLevelExp.cs`・`Generated/MasterDataSchema.cs`・`masterdata.bytes`・`masterdata.manifest.json` が両方変わったことを確かめる（列が変わるので `MasterDataSchema.Hash` が変わる）
  3. 3.3 のマイグレーションを足す（番号は「この計画で決めたこと」7）
  4. 3.4〜3.8 を書く。`server/Uta.Server/Domain/Produce/PlayerLevelRules.cs` を消す
  5. 5章のサーバーのテストを直し・足す。`server/Uta.Server.Tests/Domain/PlayerLevelRulesTests.cs` を消す
  6. `server/`（`obj/` を除く）と `shared/Uta.Shared.Contracts/` に、`player.Level`・`PlayerLevelRules`・`RequiredExp`・`end.PlayerLevel`・`.PlayerLevel`（`ProduceResultResponse`・`ProduceEndResult` のもの） が残っていないこと、`server/Uta.Server/Infrastructure/PlayerRepository.cs` と `server/Uta.Server.Tests/Integration/` の `players` の SQL に `level` が残っていないことを Grep で確かめる（サポートカードの `level`（`SupportCardRepository.cs` の `player_support_cards` の SQL、`SupportTests.cs` の `SELECT level FROM player_support_cards` など）は残す）。**`shared/Uta.Shared.Domain/` の `PlayerLevel`（`ProduceInputs`・`ProduceSnapshot`・`ProduceStateComparer`・`ProduceEngine` など）と、それを写すサーバーの `ProduceStateSerializer`・`ProduceStateBlobs`（`ProduceSnapshotBlob.PlayerLevel`）・`ProducePlayTests` の `data.Snapshot.PlayerLevel`・shared のテストの `PlayerLevel` はすべて残す**（開始時の入力の写しで、この計画では変えない）
- 境界: 付与の上限（`PlayerExpRules` のテスト）。既に上限を超えた累計は減らさない。放棄は今の累計を返す
- このタスクの後、タスク 3 が終わるまで Unity のコンパイルは通らない（Contracts の形が変わるため）
- 終わりの確かめ: `dotnet build Uta.slnx` が警告 0・エラー 0。`dotnet test --project shared/Uta.Shared.Domain.Tests` と `dotnet test --project server/Uta.Server.Tests`（結合テストは Docker が要る）が通る

**タスク 3（client。Unity を開く worktree。owned の実装ブランチの上。「この計画で決めたこと」3a）: 表示とキャッシュ**

- 触るファイル: 2章のタスク 3 の行
- 前提: Unity 7（`7000.0.0a12`）の Editor がこの worktree の `client/uta` を開いていること。開けないときは、コンパイル・EditMode のテストを「確認できていない」と書いてユーザーに頼む
- 手順:
  1. 3.9・3.10 を書く
  2. 5章のクライアントのテストを直す。owned が足したファイル（`OwnedPresenterTests`・`SupportDetailPresenterTests` など）に `new PlayerEntity(` があれば、第 2 引数（level）を消すだけにする（設計判断ではないので止まらない）。`.Level` をプレイヤーに使っていれば `.Exp` に直せるかを見て、意味が変わるなら止まって報告する
  3. `unity recompile --format json`（`client/uta` で実行）でエラー 0。Editor が `shared/Uta.Shared.Domain/Player.meta`・`Player/PlayerLevelCatalog.cs.meta` と、新しい `.cs` の `.meta` を作ったことを `git status` で確かめる（作られていなければ止まって報告）
  4. EditMode のテストを CLAUDE.md「ビルド・テスト・実行」の手順（`unity command --timeout 300 eval` で `TestRunnerApi`、結果は `client/uta/Temp/uta-test-results.xml`）で回す
  5. `Level`（プレイヤーの）を使う所が `client/uta/Assets/Scripts`・`Assets/Tests` に残っていないことを Grep で確かめる（サポートカードの `Level` は残る）
- 境界: マスタの読み込みに失敗したら、`PlayerLevelCatalogFactory.Create` も同じ `try` の中なので、今のマスタの失敗のダイアログになる（足すものは無い）
- 終わりの確かめ: `unity recompile` がエラー 0。EditMode のテストが全部通る。Play でタイトル → ホームに進み、ホームの上部が「プレイヤーLv N　ガチャ通貨 …」の形で出て、N が DB の `players.exp` から求めた Lv（0008 を当てた直後は `exp` が 0 なので 1）であること。ローカルのサーバーは 0008 まで `migrate` したもの。**この Play の確認は、owned の Play の確認が済んでから行う**（「この計画で決めたこと」3b）

## 5. テスト

**shared**（`shared/Uta.Shared.Domain.Tests/Player/PlayerLevelCatalogTests.cs`。xUnit の `[Fact]`、名前空間 `Uta.Shared.Domain.Tests.Player`、`public class PlayerLevelCatalogTests`。C# 9 なので `[0, 40, 120]` は `new[] { 0, 40, 120 }` と書く。既存の `Produce/ProduceFormulasTests.cs` と同じ形）。表は `[0, 40, 120]`（マスタの値に結びつけない）:

| テスト | 確かめること |
|---|---|
| `GetLevel_Boundaries_ReturnsLevel` | 0 → 1、39 → 1、40 → 2、119 → 2、120 → 3 |
| `GetLevel_OverMaxExp_ReturnsMaxLevel` | 121 → 3、`int.MaxValue` → 3 |
| `GetLevel_Negative_Throws` | −1 → `ArgumentOutOfRangeException` |
| `MaxLevelAndMaxExp_FromTable` | `MaxLevel` = 3、`MaxExp` = 120 |
| `Create_Empty_Throws` | `new int[0]` → `ArgumentException` |
| `Create_FirstNotZero_Throws` | `[10, 40]` → `ArgumentException` |
| `Create_NotStrictlyIncreasing_Throws` | `[0, 40, 40]` → `ArgumentException` |
| `Create_Null_Throws` | `PlayerLevelCatalog.Create(null)`（オーバーロードは 1 つなのでキャストは要らない）→ `ArgumentNullException` |
| `Create_CopiesInput` | 渡した配列 `[0, 40, 120]` を作った後で `[0, 40, 50]` に書き換えても、`GetLevel(100)` は 2 |
| `Create_SingleLevel` | `[0]` → `MaxLevel` = 1、`MaxExp` = 0、`GetLevel(0)` = 1、`GetLevel(5)` = 1 |

**server**:

| ファイル | 変えること |
|---|---|
| `Domain/PlayerExpRulesTests.cs`（作成。名前空間は消す `PlayerLevelRulesTests` と同じ `namespace Uta.Server.Tests.Domain;`。`using Uta.Server.Domain.Player;` を足す） | `AddExp(0, 40, 198000)` → 40、`(197990, 60, 198000)` → 198000、`(198000, 10, 198000)` → 198000、`(200000, 10, 198000)` → 200000（減らさない）、`(5, 0, 198000)` → 5、`gain` = −1 → `ArgumentOutOfRangeException` |
| `MasterData/MasterDataTests.cs` | 199 行目の件数 99 → 100。`PlayerLevelExp_EveryRowIs40TimesLevel` を `PlayerLevelExp_EveryRowMatchesFormula` にし、全行で `FromExp == 20 * Level * (Level - 1)`、`Level < 100` は `ToExp == 20 * Level * (Level + 1) - 1`、`Level == 100` は `ToExp == 198000`（失敗の文は今の形に合わせて Lv と値を出す）。`PlayerLevelExp_AllIsInLevelOrder`: `Enumerable.Range(1, 100)`、`all.First().FromExp == 0`、`all.Last().FromExp == 198000`。その上のコメントを「`PlayerLevelCatalogFactory` は All を並べ替えずに `FromExp` の列にする。`PlayerLevelCatalog.Create` は単調増加でなければ例外なので、All が Level の昇順であることを固定する」にする。足す: `PlayerLevelCatalogFactory_Create_MaxLevel100_MaxExp198000`（`PlayerLevelCatalogFactory.Create(Db)` の `MaxLevel` = 100、`MaxExp` = 198000、`GetLevel(0)` = 1、`GetLevel(40)` = 2、`GetLevel(197999)` = 99） |
| `MasterData/MasterDataInvariantTests.cs` | 足す: `PlayerLevelExp_RangesAreContiguous`（`Level` の昇順で並べた行 `rows` について: `rows[0].FromExp == 0`、最後でない行は `FromExp < ToExp` かつ `rows[i + 1].FromExp == rows[i].ToExp + 1`、最後の行は `FromExp == ToExp`。どれも `Assert.True(…, "playerLevelExp Level {Level} の …")` の形）。既存の `OfferRarityWeights_SumIsPositive_AndPlayerLevelExpIsContiguous` は変えない |
| `Integration/CommonResponseTests.cs` | 172 行目（`SELECT level` と `diff.Level`）を消す。`exp` の行は残す |
| `Integration/PlayerSignUpSignInTests.cs` | 66 行目の `Assert.Equal(1, response.Level);` を消す（次の行の `Exp` の 0 は残す）。205 行目は `Assert.Equal(0, response.Exp);`。`Migration_AddsPlayerColumns` の配列から `'level'` を消し、期待を `4L` に。足す: `Migration_DropsPlayerLevel`（`SELECT count(*) FROM information_schema.columns WHERE table_name = 'players' AND column_name = 'level'` が `0L`） |
| `Integration/ProduceOperationTests.cs` | `PlayerValuesAsync` の戻りの組から `Level` と `SELECT level` の行を消す（`(int GachaCurrency, int Gold, int SupportPoints, int Exp)`） |
| `Integration/ProducePlayTests.cs` | `MasterRequiredExps()` を `private static PlayerLevelCatalog MasterLevelCatalog() => PlayerLevelCatalogFactory.Create(MasterDataLoader.Load());` に置き換える。90〜94 行目: 期待値は本番の関数で作らず、`reward.Exp` と直接比べる（開始の累計は 0 で上限から遠い）: DB の `exp` が `reward.Exp`、`result.PlayerExp` が `reward.Exp`（`PlayerLevelRules.AddExp` と `level` の行を消す）。Lv の確かめは次の `Complete_LevelUp_AndMaxLevel` に任せる（produce-server.md 4.1 の「Lv で確かめる」はこのテストで満たす）。`Complete_LevelUp_AndMaxLevel`: `final.Result.PlayerExp == 39 + reward.Exp`、`MasterLevelCatalog().GetLevel(final.Result.PlayerExp) == 2`。2 人目の準備は `UPDATE players SET exp = 198000 WHERE player_id = $1`、期待は `otherFinal.Result.PlayerExp == 198000` と `MasterLevelCatalog().GetLevel(otherFinal.Result.PlayerExp) == 100`。317 行目の `data.Snapshot.PlayerLevel` は変えない |
| `Integration/ProduceStartTests.cs` | 73 行目の `rule_version` の期待 1 → 2。359 行目の `Assert.Equal(1, result.PlayerLevel);` を `Assert.Equal(0, result.PlayerExp);` に |

`using` は要るものを足す（`Uta.Server.Domain.Player`・`Uta.Server.Infrastructure`・`Uta.Shared.Domain.Player`）。消えた `Uta.Server.Domain.Produce` の `using` が使われなくなったら消す（警告 0 のため）。

**client**（`client/uta/Assets/Tests/EditMode/`）。`level` の値を渡していた所は、意味が同じになる `exp` に置き換える:

| ファイル | 変えること |
|---|---|
| `AppCommonDtoSerializationTests.cs` | `new PlayerDiffDto(true, 2, 3, 4, 5, …)`（`1` を消す）。`Assert.AreEqual(1, actual.Level);` を消す |
| `CommonResponseApplierTests.cs` | `new PlayerEntity("P1", 2, 3, 4, 5, false, false)`、`new PlayerDiffDto(true, 8, 7, 6, 105, null, null, null)`、`Assert.AreEqual(9, players.Find().Level)` → `Assert.AreEqual(8, players.Find().Exp)` |
| `Fakes/FakePlayerUseCase.cs` | `new PlayerEntity("P1", 0, 0, 0, 0, false, false)` |
| `HomeDtoFactoryTests.cs` | `Create` で `new HomeDtoFactory(players, new FeaturedIdolPicker(idols, db), notice, db, TestPlayerLevels.Catalog())`。`Player(int exp = 0, int gachaCurrency = 0, …)` → `new PlayerEntity("P1", exp, 0, 0, gachaCurrency, …)`。`Player(12, 3250, true)` → `Player(2640, 3250, true)`（期待の `"プレイヤーLv 12　ガチャ通貨 3,250"` は変えない） |
| `HomeEntryTests.cs` | `new PlayerEntity("P1", 0, 0, 0, 0, false, false)` |
| `HomePresenterTests.cs` | `CreatePresenter` の `new HomeDtoFactory(…, db, TestPlayerLevels.Catalog())`。`SetPlayer(int exp = 2640, …)` → `new PlayerEntity("P1", exp, 0, 0, gachaCurrency, …)`。`SetPlayer(level: 13)` → `SetPlayer(exp: 3120)`（期待の `"プレイヤーLv 12"`・`"プレイヤーLv 13"` は変えない） |
| `MasterDataLoaderTests.cs` | `PlayerLevelExpTable` の件数 99 → 100 |
| `PlayerDiffApplierTests.cs` | `new PlayerEntity("P1", 2, 3, 4, 5, true, true)`、`new PlayerDiffDto(hasBalance, 8, 7, 6, 5 + 100, …)`、49 行目の `Assert.AreEqual(9, p.Level);` を消す（次の `Exp` の 8 は残す）、115 行目の `Assert.AreEqual(9, players.Find().Level);` → `Assert.AreEqual(8, players.Find().Exp);` |
| `PlayerMapperTests.cs` | `new SignInResponse(40, 500, 60, 700, true, false, …)`（`3` を消す）。`Assert.AreEqual(3, player.Level);` を消す |
| `PlayerRepositoryTests.cs` | `Player(int exp)` → `new PlayerEntity("P1", exp, 2, 3, 4, false, false)`、`Assert.AreEqual(2, repo.Find().Level)` → `.Exp`。40 行目は `new PlayerEntity(null, 0, 0, 0, 0, false, false)` |
| `PlayerUseCaseTests.cs` | `SignIn(int exp = 1, …)` → `new SignInResponse(exp, 3, 4, 5, false, false, idols, supportCards, "P1")`（第 1 引数の名前を `level` から `exp` に。呼び出しの数はそのまま）。`.Level` の `Assert` はすべて `.Exp`（数はそのまま: 154・155 行目の 7、179・217・238・258 行目の 1、197 行目の 2） |
| `Support/GachaFixtures.cs` | `new PlayerEntity("P1", 0, 0, 0, gachaCurrency, false, false)` |

足すテスト（`HomeDtoFactoryTests.cs`）: `Create_LevelFromExp`（`Player(39)` → `"プレイヤーLv 1　ガチャ通貨 0"`、`Player(40)` → `"プレイヤーLv 2　ガチャ通貨 0"`、`Player(198000)` → `"プレイヤーLv 100　ガチャ通貨 0"`。1 つのテストで 3 回 `Upsert` して `Create` する）。

Unity でしか確かめられないもの: クライアントのコンパイル・EditMode のテスト・Play（タスク 3）。IL2CPP のビルドは確かめない（Contracts は既存の型のキーを詰めるだけで、型を足さないため）。

## 6. 未決事項

なし

## 7. 実装メモ

（実装を始めるときに足す）
