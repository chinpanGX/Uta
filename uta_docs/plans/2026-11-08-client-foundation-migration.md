---
title: 実装計画: クライアントを共通基盤の新しい呼び方（11d9b06）に移す
status: in-progress
updated: 2026-11-08
spec: ../wiki/architecture/api-design.md, ../wiki/architecture/server/server-foundation.md, ../wiki/architecture/screen-design.md
---

# 実装計画: クライアントを共通基盤の新しい呼び方（11d9b06）に移す

## ADR 0020〜0022・naming.md への読み替え（案。ADR 0020〜0022 は proposed のまま、この計画で実装し、この計画の PR で 3 本まとめて accepted を判断する。この節が本文と食い違ったら、この節が正）

2026-11-08。proposed の [ADR 0020](../wiki/decisions/0020-masterdata-referenced-outside-shared-domain.md)（マスタの参照）・[ADR 0021](../wiki/decisions/0021-domain-entities-and-repositories.md)（Domain のエンティティとリポジトリ）・[ADR 0022](../wiki/decisions/0022-client-external-concretes-in-infrastructure.md)（Infrastructure の具象と UseCase の IF）と [naming.md](../wiki/architecture/naming.md) の形に読み替える。**この計画は、ADR 0022 の「accepted にする条件」を確かめる計画でもある**（ユーザーの判断 2026-11-08）: Player（サインアップ・サインイン）を Application の `IPlayerUseCase` と Infrastructure の具象 `PlayerUseCase` に移し、偽の `IPlayerService` で今の `EnterPlayerUseCaseTests` の場合分けを手順のテストとして書く（R5）。**ADR の状態**（ユーザーの判断 2026-11-08）: 3 本とも proposed のままこの計画を実装し、この計画の PR で 3 本まとめて accepted にするかを判断する（ADR 0022 の「accepted の前に進めてよいこと」は読み替えの節を書くことまでなので、この計画の実装はそこから外れる。ADR 0020・0021 には accepted の前に実装してよいかの記述が無い。どちらも ADR 側の穴として親のセッションに報告した）。**採らないと判断したら、同じ PR で、ADR に由来する部分を本文の形に戻す**: R1 の asmdef の追加と生成物の移動（生成物は Infrastructure に戻す）、R2（エンティティ・リポジトリ → `PlayerCache`・`PlayerSnapshot`・`OwnedIdol`・`OwnedSupportCard`）、R3〜R5 の UseCase の IF・具象・偽・テスト（→ `IPlayerPort`・`IDeviceAuthPort`・`PlayerGateway`・`DeviceAuthGateway`・`EnterPlayerUseCase`・`SignInUseCase` と本文 6 章のテスト）、R4 の登録。**戻さないもの（naming.md に由来する改名など）の一覧の正は [ADR 0022](../wiki/decisions/0022-client-external-concretes-in-infrastructure.md) の「採らないと判断したら」**（ユーザーの判断 2026-11-08）。画面の計画にも当てはまる共通の読み替えは [foundation の計画](2026-11-03-client-screens-foundation.md) の同名の節の R0 の対応表が正。「基盤 11d9b06 への読み替え」の節（タスク 4 で画面の計画に足す）とこの節の優先は、この節 > 11d9b06 の節 > 本文（11d9b06 の節の `UtaApiClient`・ゲートウェイは、この節の `ApiClient`・Infrastructure の具象と読む）。

ADR の中で未決だったものの決め方（この計画で決めた。2026-11-08。ユーザーの判断）: クライアントの UseCase の具象の名前は IF と対にした `<機能>UseCase`（`PlayerUseCase`・`DeviceAuthUseCase`。naming.md の未決事項）。リポジトリの取得のメソッド名は `Find`（1 件。無ければ null）と `FindAll`（全件）。**サーバーのリポジトリ（`FindAsync`・`ListAsync`）も同じ役割の名前に揃える（`FindAsync`・`FindAllAsync`）**ことをユーザーが求めた。サーバーの改名は ADR 0021 のサーバー側を実装する計画で行う（この計画ではしない。naming.md への追記は親のセッション）。

### R1. 層と asmdef（ADR 0020・0021）

- asmdef を 2 つ足す:
  - `Uta.Client.MasterData`（`Assets/Scripts/MasterData/Uta.Client.MasterData.asmdef`）: 今の `Infrastructure/MasterData/Generated/`・`MasterDataAssembly.cs`・`IsExternalInit.cs` を `Assets/Scripts/MasterData/` に移す（`git mv`。`.meta` ごと。生成物は動かすだけで再生成しない）。`rootNamespace` は `Uta.MasterData`、`references` は空、`overrideReferences: true`、`precompiledReferences` は `MasterMemory.dll`・`MessagePack.dll`（`MasterMemory.Annotations.dll` が要るかはコンパイルで確かめる。要れば足す）、`autoReferenced: false`。`masterdata/masterdata.yaml` の client の `source` を `../client/uta/Assets/Scripts/MasterData/Generated` に変える
  - `Uta.Client.Domain`（`Assets/Scripts/Domain/Uta.Client.Domain.asmdef`）: `rootNamespace` は `Uta.Client.Domain`、`references` は `Uta.Shared.Domain`・`Uta.Client.MasterData`、`overrideReferences: true`、`precompiledReferences` は `Uta.Client.MasterData` と同じ、`autoReferenced: false`
- 参照を足す: `Uta.Client.Application`・`Uta.Client.Infrastructure`・`Uta.Client.Presentation`・`Uta.Client.Bootstrap`・テストの asmdef の `references` の最後に `"Uta.Client.Domain"`, `"Uta.Client.MasterData"`。`overrideReferences: true` の asmdef（Application・Presentation。テストは既にある）は `precompiledReferences` に `"MasterMemory.dll"`, `"MessagePack.dll"` を足す。`Infrastructure` の `MasterMemory.Addons.Core` の参照は今のまま（`MasterDataLoader` が使う）
- `MasterCardCatalog`（Infrastructure）は `CardCatalogFactory` に改名する（naming.md 7章。テストも `CardCatalogFactoryTests`。置き場は `Infrastructure/MasterData/` のまま）
- テストの asmdef を `Uta.Client.Tests.EditMode`（ファイル名も。`rootNamespace` は `Uta.Client.Tests`）に改名し、既存のテストの名前空間 `Uta.Client.Infrastructure.Tests` を `Uta.Client.Tests` に直す（naming.md 8章）

### R2. Domain（`Uta.Client.Domain`。ADR 0021）

`Domain/Player/` → 名前空間 `Uta.Client.Domain.Player`。1 ファイル 1 型。C# 9（`client/` の `.editorconfig`）。

```csharp
public sealed class PlayerEntity   // 今の PlayerSnapshot から所持の一覧を除いたもの
{
    public PlayerEntity(int level, int exp, int gold, int supportPoints, int gachaCurrency, bool hasActiveProduce, bool discardedProduce);
    public int Level { get; } public int Exp { get; } public int Gold { get; } public int SupportPoints { get; } public int GachaCurrency { get; }
    public bool HasActiveProduce { get; } public bool DiscardedProduce { get; }
}
public sealed class IdolEntity   // 今の OwnedIdol と同じ
{
    public IdolEntity(int idolId, int training, int awakening, int shards);
    public int IdolId { get; } public int Training { get; } public int Awakening { get; } public int Shards { get; }
}
public sealed class SupportCardEntity   // 今の OwnedSupportCard と同じ
{
    public SupportCardEntity(string supportCardUid, int cardId, int level, int limitBreak);   // supportCardUid が null なら ArgumentNullException
    public string SupportCardUid { get; } public int CardId { get; } public int Level { get; } public int LimitBreak { get; }
}

public interface IReadOnlyPlayerRepository { PlayerEntity Find(); }                                    // 無ければ null
public interface IPlayerRepository : IReadOnlyPlayerRepository { void Upsert(PlayerEntity player); }  // null なら ArgumentNullException

public interface IReadOnlyIdolRepository
{
    IReadOnlyList<IdolEntity> FindAll();   // IdolId の昇順。0 件は空
    IdolEntity Find(int idolId);           // 無ければ null
}
public interface IIdolRepository : IReadOnlyIdolRepository
{
    void Upsert(IdolEntity idol);                        // IdolId が同じものを置き換え、無ければ足す。null なら ArgumentNullException
    void ReplaceAll(IReadOnlyList<IdolEntity> idols);    // 全部入れ替える。null なら ArgumentNullException
}

public interface IReadOnlySupportCardRepository
{
    IReadOnlyList<SupportCardEntity> FindAll();   // 入れた順（サインインの応答の順。Upsert で足したものは末尾）
    SupportCardEntity Find(string uid);           // 無ければ null
}
public interface ISupportCardRepository : IReadOnlySupportCardRepository
{
    void Upsert(SupportCardEntity card);     // SupportCardUid が同じものを置き換え、無ければ末尾に足す
    void Remove(string uid);                 // 無ければ何もしない
    void ReplaceAll(IReadOnlyList<SupportCardEntity> cards);
}
```

- `FindAll` は呼ぶたびに新しい `List<T>` を返す（呼び手が持ち続けても、あとの Upsert で変わらない）
- `IPlayerRepository` に `Remove`・`ReplaceAll` は無い（1 人分。ADR 0021「使うものだけ」）
- 実装は Infrastructure の `Infrastructure/Player/PlayerRepository.cs`・`IdolRepository.cs`・`SupportCardRepository.cs`（名前空間 `Uta.Client.Infrastructure.Player`。メモリ: フィールド 1 つ、または `List<T>` 1 つ）。DI は 3 つとも `builder.Register<PlayerRepository>(Lifetime.Singleton).As<IPlayerRepository>().As<IReadOnlyPlayerRepository>();` の形（同じインスタンスを 2 つの IF で登録する）
- `PlayerCache`・`PlayerSnapshot`・`OwnedIdol`・`OwnedSupportCard` は削除する（`.meta` ごと）。`PlayerCache` に foundation が足す予定の UI の状態（知らせ済みの印）は foundation の読み替えの `DiscardNoticeState`

### R3. Application（ADR 0022）

`Application/Auth/IDeviceAuthUseCase.cs`、`Application/Player/IPlayerUseCase.cs`・`EnterPlayerResult.cs`（既存を直す）。`Application/Auth/AccountState.cs` は削除する（`IDeviceAuthPort` が無くなると Application で使う側が無い。`DeviceAuthUseCase` は基盤の `AccountStatus`（`VolTech.Client`）を直接見る。naming.md の `AccountState` → `AccountStatus` の改名は、基盤の同名の型とぶつかるので行わない: 親のセッションへの報告に挙げた）。

```csharp
public interface IDeviceAuthUseCase   // 今の SignInUseCase + IDeviceAuthPort。Contracts に相手が無い機能（ADR 0022）
{
    /// <summary>端末の認証（今の SignInUseCase.ExecuteAsync + DeviceAuthGateway.GetAccountStateAsync／EnsureSignedInAsync）。
    /// provider.GetAccountStatusAsync() が Corrupted なら InvalidOperationException("account corrupted")、知らない値なら InvalidOperationException("unknown account status: " + status)。
    /// provider.GetTokenAsync() を待ち、状態が NotRegistered だったなら true（初めて登録した）。</summary>
    UniTask<bool> SignInAsync();
    UniTask ReissueTokenAsync();
    // 診断（ConnectivityCheck）が使う 3 つ。使い手が TitleDiagnostics の 1 つだけなので、画面の IF と分けない
    UniTask<bool> CanReceiveServerErrorAsync();
    UniTask InvalidateServerSideTokenAsync();
    int TokenInvalidationCount { get; }
}

public interface IPlayerUseCase
{
    /// <summary>
    /// 今の EnterPlayerUseCase.ExecuteAsync の手順そのまま: afterRegistration ならサインアップ → サインイン。サインインが PLAYER_NOT_CREATED なら
    /// （まだサインアップしていなければ）サインアップしてサインインし直す。サインアップの後も未作成なら InvalidOperationException("sign-in: player not created after sign-up")。
    /// 失敗は InvalidOperationException（"sign-up failed: " + 失敗の文字列、"sign-in failed: " + 失敗の文字列）。
    /// 成功した応答は 3 つのリポジトリに書いて（下の Apply）から返す。
    /// </summary>
    UniTask<EnterPlayerResult> EnterAsync(bool afterRegistration);
    /// <summary>
    /// サインインで所持データを取り直す（foundation の計画の RefreshPlayerUseCase の役）。成功で 3 つのリポジトリを置き換える。
    /// 失敗は InvalidOperationException（"sign-in failed: " + 失敗の文字列。未作成は "sign-in failed: player not created"）。リポジトリは変えない。
    /// </summary>
    UniTask RefreshAsync();
}

public sealed class EnterPlayerResult
{
    // PlayerSnapshot をやめ、ログ（foundation の StartupLogs.SignIn）が使う値だけ持つ
    public EnterPlayerResult(bool signedUp, bool created, PlayerEntity player, int idolCount, int supportCardCount);
}
```

- `PlayerSignInResult`・`PlayerSignUpResult`・`PlayerSignInOutcome`・`IPlayerPort`・`IDeviceAuthPort`・`EnterPlayerUseCase`・`SignInUseCase` は削除する（サインインの 3 分岐は `PlayerUseCase` の private に閉じる。naming.md の `PlayerSignInOutcome` → `PlayerSignInResultKind` の改名は、型が無くなるので要らない）
- foundation の計画の `RefreshPlayerUseCase` は作らない（`IPlayerUseCase.RefreshAsync` がその役）
- `UseCaseResult<T>`（naming.md 2章）はこの計画では作らない（Player は例外で返す。Presenter が reason で文言を分ける操作が出たときだけ、その計画が作る: foundation の読み替えの節 R0）

### R4. Infrastructure（ADR 0022）

```csharp
// Infrastructure/Auth/DeviceAuthUseCase.cs（今の DeviceAuthGateway の中身。コンストラクターは ApiClient を受けるまま）
public sealed class DeviceAuthUseCase : IDeviceAuthUseCase

// Infrastructure/Player/PlayerUseCase.cs
public sealed class PlayerUseCase : IPlayerUseCase
{
    public PlayerUseCase(IPlayerService player, IPlayerRepository players, IIdolRepository idols, ISupportCardRepository supportCards);
}

// Infrastructure/Player/PlayerMapper.cs（DTO → エンティティ。static の純粋な関数。naming.md 7章の Mapper。ADR 0022 のテストの規約「詰め替えは static に分ける」）
public static class PlayerMapper
{
    public static PlayerEntity ToPlayerEntity(SignInResponse response);                        // 7 つのスカラー
    public static IdolEntity ToIdolEntity(OwnedIdolDto dto);
    public static SupportCardEntity ToSupportCardEntity(OwnedSupportCardDto dto);
    public static List<IdolEntity> ToIdolEntities(OwnedIdolDto[] dtos);                        // null は空
    public static List<SupportCardEntity> ToSupportCardEntities(OwnedSupportCardDto[] dtos);  // null は空
}
```

- `PlayerUseCase` の private の手順: `SignUpAsync()` は `await player.SignUpAsync().ToResult()`、`SignInAsync()` は `await player.WithHandledCodes(PlayerErrors.NotCreated).SignInAsync().ToResult()`（本文 4 章の呼び方。`result.ErrorCode == PlayerErrors.NotCreated` を未作成とする。失敗の文字列は `result.Error.ToString()`）。成功した `SignInResponse` は `Apply(response)`: `players.Upsert(PlayerMapper.ToPlayerEntity(r))` → `idols.ReplaceAll(PlayerMapper.ToIdolEntities(r.Idols))` → `supportCards.ReplaceAll(PlayerMapper.ToSupportCardEntities(r.SupportCards))` の順で、間で `await` しない（ADR 0021 の前提）。`EnterAsync` の戻り値は `new EnterPlayerResult(signedUp, created, PlayerMapper.ToPlayerEntity(r), r.Idols?.Length ?? 0, r.SupportCards?.Length ?? 0)`
- `PlayerGateway`・`DeviceAuthGateway` は削除する（`PlayerGateway.ToSnapshot` は `PlayerMapper` に移る）。本文 3 章の `PlayerGateway.cs`（変更）と 4 章の `PlayerGateway` のコードは `PlayerUseCase` のものとして読み、**本文どおりに `PlayerGateway` を一度直してから消す、という手順は踏まない**（R6 の順）
- `MasterCardCatalog.LoadFromStreamingAssetsAsync`（I/O を伴う）は、改名先の `CardCatalogFactory` に残さない（naming.md 7章: Factory は I/O をしない）。`BootstrapEntry.RunAsync` は `var db = await MasterDataLoader.LoadFromStreamingAssetsAsync(); var cardCatalog = CardCatalogFactory.Create(db);` にし（`MasterDataLoader` は既にある。`Infrastructure/MasterDataLoader.cs`）、`db` をそのまま `RegisterInstance` する。`MasterCardCatalogTests` に `LoadFromStreamingAssetsAsync` のテストがあれば `MasterDataLoaderTests` の読み方（`Load(File.ReadAllBytes(...))`）に写し、`CardCatalogFactoryTests` は `Create(db)` だけを見る
- #24 の仮の画面 `HomePresenter`（`Presentation/Home/HomePresenter.cs`。`PlayerCache.Current` を表示に使っている。foundation が置き換える）は、`PlayerCache` の引数と `cache.Current` を読む行を消し、表示を固定の文（`"ホーム（仮）"` など、今の文のうちプレイヤーの値を含まない部分）にする。Presenter にリポジトリを渡さない（ADR 0022「Presenter が受けるのは Application の公開の型と DtoFactory だけ」。DtoFactory は foundation が作る）。ボタンの遷移（`Produce` へ）は変えない
- 改名（naming.md「今のコードとの差」のうち、この計画が触るファイル）: `UtaApiClient` → `ApiClient`、`UtaErrors` → `ErrorDefinitions`、`UtaMagicOnionClientInitializer` → `MagicOnionClientInitializer`、テストの `UtaErrorsTests` → `ErrorDefinitionsTests`（`AccountState` は改名せず削除する: R3）。`ApiClient`・`MagicOnionClientInitializer` が基盤・MagicOnion の型とぶつかって `using` で解決できなければ止まって報告する（naming.md）。`ErrorDefinitions` は基盤の `ErrorDefinition`（1 件の定義）と 1 文字違い: 使うのは `new ErrorPolicyProvider(ErrorDefinitions.Create())` の 1 か所だけ
- `BootstrapEntry` の登録（本文 4 章の `IPlayerService` の登録に続けて）。今の `PlayerCache`・`SignInUseCase`・`EnterPlayerUseCase`・`PlayerGateway`・`DeviceAuthGateway` の登録は消す。`MemoryDatabase` は `builder.RegisterInstance(db);` で登録する（ADR 0020。`ICardCatalog` の登録は今のまま）:
  ```csharp
  builder.Register<PlayerRepository>(Lifetime.Singleton).As<IPlayerRepository>().As<IReadOnlyPlayerRepository>();
  builder.Register<IdolRepository>(Lifetime.Singleton).As<IIdolRepository>().As<IReadOnlyIdolRepository>();
  builder.Register<SupportCardRepository>(Lifetime.Singleton).As<ISupportCardRepository>().As<IReadOnlySupportCardRepository>();
  builder.Register<DeviceAuthUseCase>(Lifetime.Singleton).As<IDeviceAuthUseCase>();
  builder.Register<PlayerUseCase>(Lifetime.Singleton).As<IPlayerUseCase>();
  ```
- #24 の仮の画面 `TitlePresenter`・`ConnectivityCheck`（foundation が置き換えるまで残る）は、`SignInUseCase`・`EnterPlayerUseCase` の代わりに `IDeviceAuthUseCase`・`IPlayerUseCase` を受ける形に直す（手順・ログの行は変えない）。読み替え: `signIn.ExecuteAsync()` → `deviceAuth.SignInAsync()`、`enterPlayer.ExecuteAsync(registered)` → `player.EnterAsync(registered)`、`signIn.ReissueAsync()`／`CanReceiveServerErrorAsync()`／`InvalidateServerSideTokenAsync()`／`TokenInvalidationCount` → `deviceAuth` の同名のメンバー（`ReissueAsync` は `ReissueTokenAsync`）、ログの `entered.Snapshot.Level`・`Exp`・`Gold`・`SupportPoints`・`GachaCurrency`・`HasActiveProduce`・`DiscardedProduce` → `entered.Player.〜`、`entered.Snapshot.Idols.Count`・`SupportCards.Count` → `entered.IdolCount`・`entered.SupportCardCount`。`entered.SignedUp`・`Created` はそのまま

### R5. テスト（ADR 0022 の規約。**accepted の条件を確かめる**）

- `Tests/EditMode/Fakes/FakePlayerService.cs`（public。`IPlayerService` の偽。本文 5 章タスク 2 の private の偽をここに移す）: `Queue<Func<UnaryResult<SignUpResponse>>> SignUpResults`・`Queue<Func<UnaryResult<SignInResponse>>> SignInResults`（メソッドは `Dequeue()()` をそのまま返す。空なら `InvalidOperationException("FakePlayerService: no result")`）、呼び出しの記録 `List<string> Calls`（`"SignUp"`・`"SignIn"`）。積み方の補助（同じクラスの static）: `Ok<T>(T value)` → `() => new UnaryResult<T>(value)`、`Fail<T>(ErrorCode code)` → `() => FakeRpcErrors.ApiFailureResult<T>(code)`（基盤の `UnaryResult<T>` に `T` を受けるコンストラクターが無ければ `new UnaryResult<T>(UniTask.FromResult(value).AsTask())`。コンパイルで確かめる）。`IService<IPlayerService>` の 5 メンバーの実装は本文タスク 2 のとおり（`WithHeaders` は `this`）
- **止まる条件**: 基盤の `FakeRpcErrors`（VolTech-MagicOnion の Unity パッケージの `Tests/EditMode`）がテストの asmdef から参照できない（テスト用のアセンブリが `testables` に無い、型が `internal`、パッケージに無い）とき。手元で実在を確かめられていない（`Library/PackageCache` が無い）。使えなければ、同じ形の失敗の `UnaryResult<T>`（`RpcException` を投げる `Task<T>` を包む）を `FakePlayerService` の中に作れるかを調べて止まって報告する（ADR 0022 の条件に直結するので、勝手に別の方法へ変えない）
- `FakeDeviceService` は作らない（`DeviceAuthUseCase` は基盤の `DeviceTokenProvider` 越しで、偽の `IDeviceService` では手順を再現できない。今と同じく EditMode でテストせず、タスク 5 の診断で確かめる）
- `PlayerUseCaseTests.cs`（`EnterPlayerUseCaseTests` を置き換える。**ADR 0022 の条件のテスト**）: 既存の `EnterPlayerUseCaseTests` の各ケースを、偽の `IPlayerPort` の代わりに `FakePlayerService` で同じ場合分けにする（成功は `Ok(new SignInResponse(...))`／`Ok(new SignUpResponse(created))`、未作成は `Fail<SignInResponse>(PlayerErrors.NotCreated)`、ほかの失敗は `Fail<T>(PlayerErrors.NotEnoughGold)`（`PlayerErrors` にある別のコードなら何でもよい））。少なくとも: 登録直後（サインアップ → サインイン。`Calls` が `"SignUp"`, `"SignIn"`）、保存ありの起動（`"SignIn"` だけ）、未作成 → サインアップ → サインイン（`"SignIn"`, `"SignUp"`, `"SignIn"`）、サインアップ後も未作成で例外、サインアップ失敗で例外、サインイン失敗で例外、`Enter_Success_ReplacesRepositories`（3 つのリポジトリの中身が応答と一致）、`Refresh_Success_ReplacesRepositories`、`Refresh_Failed_ThrowsAndKeepsRepositories`。例外のメッセージは `result.Error.ToString()` を含むので完全一致で比べず、`StringAssert.StartsWith("sign-in failed: ", e.Message)`（サインアップは `"sign-up failed: "`）で比べる。リポジトリは本物（R2 のメモリ実装）。`SignInResponse`・`SignUpResponse` の組み立ては、テストのファイルの中の `private static` の補助（`SignIn(int level = 1, OwnedIdolDto[] idols = null, ...)`。引数の並びは `shared/Uta.Shared.Contracts/Player/` のコンストラクターのとおり）に 1 か所で書く
- `PlayerMapperTests.cs`: `PlayerGatewayTests` の `ToSnapshot` の 2 本を `ToPlayerEntity`・`ToIdolEntities`・`ToSupportCardEntities` に写す（null の配列は空）。本文 6 章の `PlayerGatewayTests` の 4 本は `PlayerUseCaseTests` の経路に含める（`SignIn_ReturnsNotCreated_WhenPlayerNotCreated` は「未作成 → サインアップ」の経路）
- `PlayerRepositoryTests.cs`・`IdolRepositoryTests.cs`・`SupportCardRepositoryTests.cs`: `Upsert` の置き換えと追加、`Remove`（無ければ何もしない）、`ReplaceAll`、`Find` の null、`FindAll` が新しいリストを返すこと、`FindAll` の順
- 手順のテストの重さ（`IService<T>` のメンバー・基盤のエラーの表し方・DTO の形への結合）が受け入れられるかは、この PR でユーザーが判断する（ADR 0022）。書けない・重すぎると判断したら、この節を使わず本文の形で実装し、ADR 0022 を案A に戻す提案を PR の本文に書く

### R6. タスクの読み替え

- タスク 2 は本文のタスク 2（API 層の移行）と R1〜R5 を 1 つにまとめる。**順は次のとおり**（各段の終わりで `unity recompile --format json` がエラー 0。(a)・(b) の時点では本文の旧い API のコンパイルエラー（`ApiServiceBase` など）はまだ残っていてよいが、新しく出るエラーが無いこと）:
  - (a) asmdef の追加と生成物の移動・改名（R1。`Uta.MasterData` の名前空間は変わらないので `Infrastructure` の `using Uta.MasterData;` はそのまま通る）
  - (b) Domain の型と Infrastructure のリポジトリとそのテスト（R2）
  - (c) 本文のタスク 2（`ServiceClientFactory`・`ApiClient`・`PlayerApi` の削除・テストの asmdef の DLL）と R3〜R5 を同時に行う。**`PlayerGateway` は本文のとおりに直さず、`PlayerUseCase` として書く**。ADR 0022「具象を移す前にテストを書く」は、この段の中で `IPlayerUseCase`・`PlayerUseCase`（中身は `NotImplementedException`）・`FakePlayerService`・`PlayerUseCaseTests` を先に書いてテストが失敗することを確かめ、そのあと `PlayerUseCase` の中身を `EnterPlayerUseCase`・`PlayerGateway` から移す、という順で満たす。この段の終わりで EditMode のテストが全部通る
  - 触るファイル（本文 3 章の表に足す・読み替える）: 作成 `Assets/Scripts/MasterData/Uta.Client.MasterData.asmdef`・`Assets/Scripts/Domain/Uta.Client.Domain.asmdef`・`Domain/Player/*.cs`（9 ファイル）・`Infrastructure/Player/PlayerRepository.cs`・`IdolRepository.cs`・`SupportCardRepository.cs`・`PlayerUseCase.cs`・`PlayerMapper.cs`・`Infrastructure/Auth/DeviceAuthUseCase.cs`・`Application/Auth/IDeviceAuthUseCase.cs`・`Application/Player/IPlayerUseCase.cs`・`Tests/EditMode/Fakes/FakePlayerService.cs`・`Tests/EditMode/PlayerUseCaseTests.cs`・`PlayerMapperTests.cs`・`PlayerRepositoryTests.cs`・`IdolRepositoryTests.cs`・`SupportCardRepositoryTests.cs`。移動 `Infrastructure/MasterData/Generated/`・`MasterDataAssembly.cs`・`IsExternalInit.cs` → `Assets/Scripts/MasterData/`。改名 `UtaApiClient.cs`・`UtaErrors.cs`・`UtaMagicOnionClientInitializer.cs`・`MasterCardCatalog.cs`・`Tests/EditMode/UtaErrorsTests.cs`・`MasterCardCatalogTests.cs`・`Uta.Client.Infrastructure.Tests.EditMode.asmdef`。変更 `Application/Player/EnterPlayerResult.cs`・5 つの asmdef・`Bootstrap/BootstrapEntry.cs`・`Presentation/Title/TitlePresenter.cs`・`ConnectivityCheck.cs`・`Presentation/Home/HomePresenter.cs`・`masterdata/masterdata.yaml`。削除 `PlayerCache.cs`・`PlayerSnapshot.cs`・`OwnedIdol.cs`・`OwnedSupportCard.cs`・`PlayerSignInResult.cs`・`PlayerSignUpResult.cs`・`PlayerSignInOutcome.cs`・`IPlayerPort.cs`・`EnterPlayerUseCase.cs`・`Application/Auth/IDeviceAuthPort.cs`・`SignInUseCase.cs`・`AccountState.cs`・`Infrastructure/Player/PlayerGateway.cs`・`Infrastructure/Auth/DeviceAuthGateway.cs`・`Infrastructure/Api/PlayerApi.cs`・`Tests/EditMode/EnterPlayerUseCaseTests.cs`・`PlayerGatewayTests.cs`（`.meta` ごと）
- タスク 3（wiki）に足す: `api-design.md` 1 章「クライアントの実装」を `PlayerUseCase`（`IPlayerService` を直接呼び、`PlayerMapper` で詰め替え、リポジトリに書く）の形に。ADR 0020〜0022 の「accepted にしたら直すもの」のうち、この計画の範囲を直す: `repository-layout.md` 2 章の木・3 章の asmdef の表と「`Uta.MasterData` を参照してよいのは Infrastructure だけ」「生成したマスタの置き場」・4 章の表（Domain・Application・Infrastructure の行の禁止の列と責務）・5 章の `IsExternalInit` の置き場・6.1「Client.Domain は今は作らない」、`masterdata-pipeline.md` のクライアントの生成物の置き場と「`MemoryDatabase` 自体は DI に登録しない」（245 行付近）、`screen-design.md` 6 章（「`Application` のユースケース越しに」→ UseCase の IF 越しに）・7.4（ユースケースの行「通信は `I<機能>Port` 越し」、組み立ての行「ゲートウェイ」）・7.5（「所持データのキャッシュ」の行を `IPlayerRepository`・`IPlayerUseCase.RefreshAsync` の形に書き直す。「マスタ」の行を ADR 0020 の形に）・8 章 204 行付近（ルートのスコープの登録: `PlayerCache` → リポジトリ）・105・169 行付近の `PlayerCache`、`lesson-domain-types.md` 124 行付近の `PlayerCache`、`glossary.md` の Client.Domain の項（`Uta.Client.Domain` を作ったこと）・エンティティ・リポジトリの項を足し、「ポート」の項は「クライアントでは使わない（Application の UseCase の IF と Infrastructure の具象。ADR 0022）。サーバーは ADR 0021 のサーバー側を実装するまで、Application が定義するリポジトリの IF を指す」と書き分ける（Server.Domain の項は、サーバーのコードを変える ADR 0021 のサーバー側の計画で直す）、`.claude/skills/requesting-code-review/layers/client.md` の「ポートで定義しているか」「マスタの生成物が Infrastructure に留まっているか」（ADR 0020・0022 の線引きに）と、足す項目「DtoFactory が `IReadOnly…` だけを受けているか」（ADR 0021）「Presenter が業務の操作と読み取りのために受けるのは Application の公開の型と DtoFactory だけか」（ADR 0022）、`screen-design.md` に ADR 0022「影響・リスク」のテストの規約（`Fake<機能>Service`・`Fake<機能>UseCase`・詰め替えの static の `Mapper`）を 7.4 の下に節として足す。改名・削除した型（`UtaApiClient`・`UtaErrors`・`UtaMagicOnionClientInitializer`・`MasterCardCatalog`・`PlayerGateway`・`PlayerCache`・`PlayerSnapshot`・`EnterPlayerUseCase`・`SignInUseCase`・`IPlayerPort`・`IDeviceAuthPort`）は `uta_docs/wiki/architecture`・`uta_docs/wiki/systems`・`glossary.md` を Grep し、**クライアントについて書いている行だけ**を同じ変更で直す（naming.md「今のコードとの差」の規則。サーバーに残る同名の型（`MasterCardCatalog`・`PlayerSnapshot` のサーバー側）の記述と、`decisions/` の ADR の本文は直さない。ADR・naming.md は親のセッションが直す）。naming.md の「今のコードとの差」から直した行を消す。`screen-design.md` 7.8 の `IGameMaster` は foundation の計画が直す（foundation の読み替えの節 R5）。ADR 0020〜0022 の `status` は、ユーザーが PR で判断してから変える（実装担当は変えない）
- タスク 4 の「基盤 11d9b06 への読み替え」の節は本文どおり足す（この節とは別。両方が要る）。索引への行は、本文の「順 0 は P1 より前に main へ入れる」と共通レスポンスの「順 4 の前でも後でもよい」を、どちらも「**P1 の実装ブランチを切る前に main へ入れる**」と書き、共通レスポンスも「MVP の後の計画」の箇条書きではなく 1 章の表の行（順 0 の次）にする（[索引の読み替えの節](2026-11-07-client-screens-index.md)に合わせる。共通レスポンスの計画の 0 章「foundation（順 4）より前」も同じ意味に読む）
- タスク 5 の確認の手順は変わらない（`TitleDiagnostics` はまだ無いので #24 の診断の経路。ログの行は同じ）
- 6 章の「既存の `EnterPlayerUseCaseTests`・`UtaErrorsTests`・`MasterCardCatalogTests` は変えず」は、`PlayerUseCaseTests`・`ErrorDefinitionsTests`・`CardCatalogFactoryTests` に置き換わる・改名されると読む

### R7. 確かめること・前提

- リポジトリの読み書きはメインスレッドだけ（ADR 0021 の前提）。`PlayerUseCase` は `await … .ToResult()` のあとに書く。基盤の `UnaryResult` の継続がメインスレッドに戻るかは確かめていないので、**タスク 5 の診断で確かめる**: `PlayerUseCase.Apply` の先頭に `Debug.Log("[Uta] player apply thread=" + System.Threading.Thread.CurrentThread.ManagedThreadId)` を一時的に入れ、`Uta.exe -batchmode -nographics -uta-quit-after-check` のログで `1`（Unity のメインスレッド）であることを見てから、そのログを消してコミットする。`1` でなければ止まって報告する（リポジトリの同期の前提が崩れるので、勝手に `UniTask.SwitchToMainThread` などを足さない）。基盤が `ICommonResponseApplier` を呼ぶスレッドは[共通レスポンスの計画](2026-11-08-common-response-player-diff.md)の読み替えの節と gacha の計画で確かめる
- `MemoryDatabase` を Inject する形（ADR 0020）は、`BootstrapEntry` が `MasterDataLoader.LoadFromStreamingAssetsAsync()` で読み終えてから `LifetimeScope.Create` する今の順のままで満たせる（R4）。`Uta.Client.MasterData` の `precompiledReferences` に要る DLL は、この計画のコンパイルで確かめる（ADR 0020）

### R8. Entity の ID と機能ごとのフォルダ（2026-11-08。PR #44 のコードの確認でユーザーが求めた。この節が R1〜R7 より優先）

**決まっていること**（ユーザーの判断）: Entity は、サーバーが採番する ULID を `UniqueId`（`string`）、マスタの int の ID を `MasterId`（`int`）として持つ。フォルダと名前空間は、Domain・Infrastructure とも機能ごと（`Player`・`Idol`・`SupportCard`）に分ける。この変更も #44 に入れる。

**サーバー**（アイドルの行に ULID が無いので足す。Player の ID は今ある `player_id`）:
- `server/Uta.Server/Migrations/0007_add_player_idol_uid.sql`（作成。埋め込みリソースの登録は既存の 0001〜0006 と同じ形）: `player_idols` に `idol_uid char(26)` を足し、既存の行を ULID の形の値（`created_at` のミリ秒を先頭 10 文字、乱数 16 文字。Crockford の base32 `0123456789ABCDEFGHJKMNPQRSTVWXYZ`）で埋めてから `NOT NULL` と `UNIQUE` にする。埋めるのは `pg_temp` の一時関数（`CREATE FUNCTION pg_temp.uta_ulid(ts timestamptz) RETURNS text … LANGUAGE plpgsql`）。主キー `(player_id, idol_id)` は変えない
- `Application/Idol/OwnedIdol.cs`: `OwnedIdol(Ulid Uid, int IdolId, int Training, int Awakening, int Shards)`（`OwnedSupportCard(Ulid Uid, …)` と同じ形）
- `Infrastructure/IdolRepository.cs`: `TryInsertAsync` は `idol_uid` に `Ulid.NewUlid().ToString()` を入れる（IF のシグネチャは変えない。`ON CONFLICT DO NOTHING` で入らなかったときは捨てる）。`ListAsync` は `idol_uid` も読む（`Ulid.Parse`。サポートカードの `ListAsync` の読み方に合わせる）
- `shared/Uta.Shared.Contracts/Player/OwnedIdolDto.cs`: 末尾に `[Key(4)] public string IdolUid { get; }`、コンストラクターの最後の引数 `string idolUid`。`SignInResponse.cs`: 末尾に `[Key(9)] public string PlayerId { get; }`、コンストラクターの最後の引数 `string playerId`（キーは足すだけで、既存の番号は変えない）
- `Api/PlayerService.cs`: `new OwnedIdolDto(x.IdolId, x.Training, x.Awakening, x.Shards, x.Uid.ToString())`、`SignInResponse` の最後に `playerId.ToString()`
- テスト（`server/Uta.Server.Tests`）: サインインの結合テストで、`PlayerId` が 26 文字でサーバーの `player_id` と一致すること、各アイドルの `IdolUid` が 26 文字で互いに違うことを足す。`player_idols.idol_uid` が `NOT NULL`・`UNIQUE` であること。ほかの結合テストは、コンパイルが通るように直すだけ

**クライアント**（`Uta.Client.Domain`・`Uta.Client.Infrastructure`）:
- フォルダと名前空間: `Domain/Player/`（`PlayerEntity`・`IPlayerRepository`・`IReadOnlyPlayerRepository`。`Uta.Client.Domain.Player`）、`Domain/Idol/`（`IdolEntity`・`IIdolRepository`・`IReadOnlyIdolRepository`。`Uta.Client.Domain.Idol`）、`Domain/SupportCard/`（`SupportCardEntity`・`ISupportCardRepository`・`IReadOnlySupportCardRepository`。`Uta.Client.Domain.SupportCard`）。`Infrastructure/Idol/IdolRepository.cs`（`Uta.Client.Infrastructure.Idol`）・`Infrastructure/SupportCard/SupportCardRepository.cs`（`Uta.Client.Infrastructure.SupportCard`）。`PlayerRepository`・`PlayerUseCase`・`PlayerMapper` は `Infrastructure/Player/` のまま（`PlayerMapper` はサインインの応答の詰め替えなので Player に置く）。移動は `git mv`（`.meta` ごと）。名前空間 `…Idol` と生成したマスタの型 `Uta.MasterData.Idol` がぶつかってコンパイルできないところは、型の側を `Uta.MasterData.Idol` と名前空間付きで書く
- Entity:
  - `PlayerEntity(string uniqueId, int level, int exp, int gold, int supportPoints, int gachaCurrency, bool hasActiveProduce, bool discardedProduce)`。`UniqueId` を足す
  - `IdolEntity(string uniqueId, int masterId, int training, int awakening, int shards)`。`IdolId` → `MasterId`
  - `SupportCardEntity(string uniqueId, int masterId, int level, int limitBreak)`。`SupportCardUid` → `UniqueId`、`CardId` → `MasterId`
  - 3 つとも `uniqueId` が null なら `ArgumentNullException`
- リポジトリ: 1 件を引く・置き換える・消すのは `UniqueId` で行う。`IReadOnlyIdolRepository.Find(string uniqueId)`（`Find(int idolId)` はやめる）、`FindAll` は `MasterId` の昇順。`IIdolRepository.Upsert` は `UniqueId` が同じものを置き換える。`ISupportCardRepository` は引数名だけ `uniqueId` に（`Find`・`Remove`）。`IPlayerRepository` は変えない
- `PlayerMapper`: `ToPlayerEntity` は `response.PlayerId`、`ToIdolEntity` は `dto.IdolUid`・`dto.IdolId`、`ToSupportCardEntity` は `dto.SupportCardUid`・`dto.CardId`
- テスト（`client/uta/Assets/Tests/EditMode`）: 今のテストを新しいシグネチャに合わせる。`IdolRepositoryTests` の置き換え・`Find` は `UniqueId` で、`FindAll` の順は `MasterId`。`PlayerMapperTests` で `UniqueId`・`MasterId` の写しを確かめる。`PlayerUseCaseTests` の応答の組み立て（`private static` の補助）に ID を足す。テストのファイルは移さない

**wiki**: `api-design.md`（`SignInResponse`・`OwnedIdolDto` の項目）、サーバーの表の説明（`player_idols`）、`screen-design.md`・`glossary.md` のエンティティの項（`UniqueId`・`MasterId`）、`repository-layout.md` 2 章の木、`naming.md`（`UniqueId`・`MasterId` の命名を足す）を直す。

**確かめ方**: `dotnet build Uta.slnx`（警告 0・エラー 0）、`dotnet test --project server/Uta.Server.Tests`（Docker）、Unity の `unity recompile`（エラー 0）と EditMode のテスト（非同期）、Editor の Play でアカウント無しから `sign-in: ok`（ローカルの DB にマイグレーション 0007 を流してから）。

### R9. Contracts とサーバーの型の ID の名前を揃える（2026-11-08。ユーザーの判断。R8 より後に決めた。食い違えばこの節が正）

**規則**: リクエスト・レスポンスの名前はクライアントとサーバーで統一する。DB の列名は今のまま（C# では Dapper の `AS` で写す）。
1. DTO（またはサーバーの型）がそのものを表すときは `UniqueId`（サーバーが採番する ULID。`string`、サーバーの型では `Ulid`）・`MasterId`（マスタの `int`）
2. ほかのものを指すときは `<名詞>UniqueId`・`<名詞>MasterId`（配列は `…UniqueIds`）
3. `PlayerId` はそのまま。プロデュースの中で振る番号（`InstanceId`・`CardInstanceId`）は ULID でもマスタの ID でもないので変えない
4. MessagePack の `[Key(n)]` の番号は変えない（プロパティ名とコンストラクターの引数名だけを変える）

**改名の一覧**（`shared/Uta.Shared.Contracts`）:
| 型 | 今 | 新 |
|---|---|---|
| `OwnedIdolDto` | `IdolId`・`IdolUid` | `MasterId`・`UniqueId` |
| `OwnedSupportCardDto` | `SupportCardUid`・`CardId` | `UniqueId`・`MasterId` |
| `DrawResultDto` | `ItemId`・`SupportCardUid` | `ItemMasterId`・`SupportCardUniqueId` |
| `DrawRequest`・`ExchangeRequest`・`GachaStatusDto` | `GachaId` | `GachaMasterId` |
| `LevelUpRequest` | `SupportCardUid` | `SupportCardUniqueId` |
| `LimitBreakRequest` | `SupportCardUid`・`MaterialUid` | `SupportCardUniqueId`・`MaterialUniqueId` |
| `StartProduceRequest` | `IdolId`・`SupportCardUids`・`RentalCandidateId`・`ScenarioId` | `IdolMasterId`・`SupportCardUniqueIds`・`RentalCandidateMasterId`・`ScenarioMasterId` |
| `ProduceStateResponse` | `ScenarioId`・`IdolId`・`CardOffers` | `ScenarioMasterId`・`IdolMasterId`・`CardChoiceMasterIds`（`CardOffers` はレビューの後にユーザーが決めた。「カードの選択肢」のカードのマスタ ID の配列） |
| `DeckCardDto` | `CardId` | `CardMasterId` |

コンストラクターの引数名も同じ規則（camelCase）。IService の RPC の引数に同じ ID があれば同じ規則で直す。

**サーバー**: DTO を作る・読む側を直す。サーバーの型のうち所持品を表すもの `OwnedIdol(Ulid Uid, int IdolId, …)` → `OwnedIdol(Ulid UniqueId, int MasterId, …)`、`OwnedSupportCard(Ulid Uid, int CardId, …)` → `OwnedSupportCard(Ulid UniqueId, int MasterId, …)`（Dapper の `AS` も合わせる）。それ以外のサーバーの内部の型・UseCase の引数・ローカル変数・DB の列は**変えない**（この節の範囲は Contracts と所持品の 2 つの型。サーバーの内部の命名は ADR 0021 のサーバー側の計画で揃える）。テストはコンパイルが通るように直し、振る舞いは変えない。

**クライアント**: `PlayerMapper` と EditMode のテスト（`FakePlayerService`・`PlayerUseCaseTests`・`PlayerMapperTests` の応答の組み立て）を新しい名前に合わせる。

**wiki と計画**: `api-design.md` ほか wiki の DTO の項目名、`naming.md` に規則 1〜4、ready の画面の計画（foundation・formation・gacha・owned・week・lesson と共通レスポンス）は本文を書き換えず、「基盤 11d9b06 への読み替え」の節（無ければ先頭の読み替えの節）に「DTO の ID の名前は移行計画の R9 の表に読み替える」の 1 行を足す。

**確かめ方**: `dotnet build Uta.slnx`（警告 0・エラー 0）、`dotnet test --project server/Uta.Server.Tests`（件数が R8 の後の 294 と同じ）、`unity recompile`（エラー 0）、EditMode のテスト（非同期。103 件）。

## 0. 決まっていること

ユーザーが推奨どおりに了承した（2026-11-08）。

1. **共通レスポンス項目（player_diff）は採る**が、範囲が広いので後続の [2026-11-08-common-response-player-diff.md](2026-11-08-common-response-player-diff.md) で行う。この計画では `.WithCommonResponseApplier` を付けない。
2. **順 7 の画面計画のうち、旧い呼び方を書いている 4 本（foundation・formation・gacha・owned）は、各計画の先頭に「基盤 11d9b06 への読み替え」の節を足して直す**（本文は書き換えない。節が本文と食い違ったら節が正）。
3. **実装は Unity を開く worktree で行う**。この計画を書いた worktree（`Uta-wt/2`）では Unity を開いておらず、`packages-lock.json` の解決もコンパイルもできなかった。
4. **順番**: この計画 → 共通レスポンスの計画 の順。順 7 の `main` への取り込みは、この計画を **P1 より前**に `main` へ入れる（P1 の積み PR の前提を変えないため）。索引 [2026-11-07-client-screens-index.md](2026-11-07-client-screens-index.md) の 1章に「順 0」として足す（タスク 4）。

## 1. 目的と前提

PR #34 で共通基盤（VolTech-MagicOnion）の固定を `11d9b06` に上げた。基盤は互換性を壊す変更を入れた（基盤の `CHANGELOG.md`「互換性が壊れる変更」）ので、今の Uta のクライアントは Unity でコンパイルできない（`ApiServiceBase<T>` が無い、`ApiResult<T>.IsSuccess` が無い、`ApiCore.CreateBuilder` の引数が違う）。サーバーは `dotnet build Uta.slnx` が警告 0・エラー 0、`dotnet test --project server/Uta.Server.Tests` が 293 件成功で、影響は無い（Uta は `[SkipAuth]`・`[NoSideEffects]` を共有インターフェースのメソッドにだけ付けている）。

この計画は、クライアントを基盤の新しい呼び方に移して通る状態に戻し、Monitor（Editor の通信ウィンドウ）が使えることを確かめる。

- 基盤の使い方の正: VolTech-MagicOnion の `docs/guide/client.md`。署名は `Unity/com.chinpangx.voltech.magiconion/Runtime/Client/`（`ApiCore.cs`・`ApiClientExtensions.cs`・`ApiResult.cs`）
- 関係する ADR: [0002](../wiki/decisions/0002-magiconion.md)、[0009](../wiki/decisions/0009-client-stack.md)、[0019](../wiki/decisions/0019-foundation-packages.md)
- **この計画で決めないこと**: 共通レスポンス項目（後続の計画）、`IApiActivityHandler`（foundation の計画）、`IApiLogger`、Tracing、実機での Monitor の表示（基盤にまだ無い）、画面の実装そのもの

## 2. 新旧の対応

| 今（旧） | 新 |
|---|---|
| `ApiServiceBase<IPlayerService>` のラッパー `PlayerApi` | 無くす。`core.CreateClient<IPlayerService>()` で作った共有インターフェースを直接呼ぶ |
| `InvokeAsync(nameof(...), req, (c, r) => c.XxxAsync(r), handledCodes)` | `await client.WithHandledCodes(codes).XxxAsync(req).ToResult()`（コードを渡さないときは `WithHandledCodes` を付けない） |
| `result.IsSuccess` | `!result.IsError` |
| `result.Error.Code.Equals(X)` | `result.ErrorCode == X`（`ErrorCode?` と `ErrorCode` の `==`） |
| `result.Error.ToString()`（失敗の文字列） | **そのまま `result.Error.ToString()`**（`ApiResult<T>.Error` は public のまま。コードとステータスが文字列に入る。`ErrorMessage` はメッセージだけなので使わない） |
| `ApiCore.CreateBuilder(options, tokenProvider, policy, handler)` | `ApiCore.CreateBuilder(callInvoker, serializerProvider, clientFactoryProvider, options, tokenProvider, policy, handler)` |

`ToResult()` は `ApiResultExtensions`、`WithHandledCodes` は `ApiClientExtensions`（どちらも `using VolTech.Client;`）。`IsError` が真のとき `Value` を読むと例外になるので、必ず先に `IsError` を見る。`WithErrorNavigator(null)` は基盤が受ける（`ApiCoreBuilder.cs`）ので、分岐は要らない。

## 3. 触るファイル

| 操作 | パス | 中身 |
|---|---|---|
| 削除 | `client/uta/Assets/Scripts/Infrastructure/Api/PlayerApi.cs`（と `.meta`） | ラッパーをやめる |
| 変更 | `client/uta/Assets/Scripts/Infrastructure/Api/ServiceClientFactory.cs` | `Create<T>` は `IDeviceService` 用に残す（`DeviceTokenProvider` は `ApiCore` の前に要るため）。シリアライザーの組み立てを `SerializerProvider`・`ClientFactoryProvider` に切り出す |
| 変更 | `client/uta/Assets/Scripts/Infrastructure/Api/UtaApiClient.cs` | `Player` を `IPlayerService` にする。`CreateBuilder` の新しい引数 |
| 変更 | `client/uta/Assets/Scripts/Infrastructure/Player/PlayerGateway.cs` | コンストラクターを `IPlayerService` にし、新しい呼び方で読む |
| 変更 | `client/uta/Assets/Scripts/Bootstrap/BootstrapEntry.cs` | `using Uta.Shared.Contracts.Player;` を足し、`IPlayerService` を VContainer に登録する（81 行目付近） |
| 変更 | `client/uta/Assets/Scripts/Bootstrap/Uta.Client.Bootstrap.asmdef` | `references` に `"Uta.Shared.Contracts"` を足す（`Uta.Shared.Domain` の次） |
| 変更 | `client/uta/Assets/Tests/EditMode/Uta.Client.Infrastructure.Tests.EditMode.asmdef` | `precompiledReferences` に `MagicOnion.Abstractions.dll`・`MagicOnion.Client.dll`・`Grpc.Core.Api.dll` を足す |
| 変更 | `client/uta/Assets/Tests/EditMode/PlayerGatewayTests.cs` | 偽の `IPlayerService` で 4 本を足す（既存の `ToSnapshot` の 2 本は残す） |
| 変更 | `client/uta/Packages/packages-lock.json` | `unity command package resolve` の結果（手で書かない） |
| 変更 | `uta_docs/wiki/architecture/api-design.md`（58 行目）、`unity7-evaluation.md`（31・61・63 行目） | `ApiServiceBase`・`PlayerApi` の記述を新しい呼び方に |
| 変更 | `uta_docs/plans/2026-11-03-client-screens-foundation.md`・`-formation.md`・`-gacha.md`・`-owned.md` | 先頭に「基盤 11d9b06 への読み替え」の節（タスク 4 の文面） |
| 変更 | `uta_docs/plans/2026-11-07-client-screens-index.md` | 1章の表に「順 0」の行を足す |

`2026-11-03-client-signup-signin.md`（done）は履歴なので直さない。`UtaMagicOnionClientInitializer.cs`・`CountingTokenProvider.cs`・`UtaErrors.cs` は変えない。`client-prefabs-common.md`・`week`・`lesson` に旧い呼び方は無い（Grep 済み）。

## 4. 型とシグネチャ

```csharp
// ServiceClientFactory.cs（internal static）
public static IMagicOnionSerializerProvider SerializerProvider { get; }   // 今の Create<T> の中の options / serializerProvider を static readonly に持つ
public static IMagicOnionClientFactoryProvider ClientFactoryProvider => UtaMagicOnionClientInitializer.ClientFactoryProvider;
public static TService Create<TService>(CallInvoker callInvoker) where TService : IService<TService>;   // 中で SerializerProvider を使う。IDeviceService 用

// UtaApiClient.cs
public IPlayerService Player { get; }      // core.CreateClient<IPlayerService>()
// Create(...) の引数は今のまま。中の組み立て:
var core = ApiCore.CreateBuilder(
        callInvoker, ServiceClientFactory.SerializerProvider, ServiceClientFactory.ClientFactoryProvider,
        options, counting, new ErrorPolicyProvider(UtaErrors.Create()), errorHandler)
    .WithErrorNavigator(errorNavigator)
    .Build();
var playerClient = core.CreateClient<IPlayerService>();
```

```csharp
// PlayerGateway.cs
private readonly IPlayerService player;
public PlayerGateway(IPlayerService player) { this.player = player; }   // UtaApiClient を受けない

public async UniTask<PlayerSignUpResult> SignUpAsync()
{
    var result = await player.SignUpAsync().ToResult();
    return result.IsError ? PlayerSignUpResult.Failure(result.Error.ToString()) : PlayerSignUpResult.Success(result.Value.Created);
}

public async UniTask<PlayerSignInResult> SignInAsync()
{
    var result = await player.WithHandledCodes(PlayerErrors.NotCreated).SignInAsync().ToResult();
    if (!result.IsError) { return PlayerSignInResult.Success(ToSnapshot(result.Value)); }
    if (result.ErrorCode == PlayerErrors.NotCreated) { return PlayerSignInResult.NotCreated(); }
    return PlayerSignInResult.Failure(result.Error.ToString());
}
```

`notCreated` フィールドは消す。`ToSnapshot` は今のまま。

```csharp
// BootstrapEntry.cs: UtaApiClient の登録の次の行に足す
builder.Register<IPlayerService>(resolver => resolver.Resolve<UtaApiClient>().Player, Lifetime.Singleton);
// PlayerGateway の登録（既存の builder.Register<PlayerGateway>(...).As<IPlayerPort>()）は、コンストラクターの引数が IPlayerService になるだけで、そのまま動く
```

## 5. タスク

**タスク 1（Unity worktree）: パッケージの解決**

- 手順: 実装する worktree の `client/uta` で `unity command package resolve` を実行し、`packages-lock.json` の `com.chinpangx.voltech.magiconion` が `11d9b06` になったことを確かめて commit する
- 終わりの確かめ: `unity recompile --format json` が `ApiServiceBase`・`IsSuccess`・`CreateBuilder` のコンパイルエラーを返す（タスク 2 の直す対象の確認）

**タスク 2: Api 層・ゲートウェイ・テストを移す**（`ServiceClientFactory.cs`・`UtaApiClient.cs`・`PlayerApi.cs` の削除・`PlayerGateway.cs`・`BootstrapEntry.cs`・テストの asmdef・`PlayerGatewayTests.cs`）

- 手順: 4 章のとおり。壊れたまま終わらないよう、1 つのタスクでコンパイルが通るところまで直す
- 偽の `IPlayerService`（テスト側の private クラス）: `IPlayerService` の 2 メソッド（`SignUpAsync`・`SignInAsync`）に加え、`IService<IPlayerService>` の 5 メンバー（`WithOptions`・`WithHeaders`・`WithDeadline`・`WithCancellationToken`・`WithHost`）を実装する。**`WithHeaders` は `this` を返す**（`WithHandledCodes` の実体が `client.WithHeaders(...)` を呼ぶため。戻りを新しい偽物にすると、登録した返り値が引き継がれない）。ほかの `With…` も `this` を返し、`WithHost` だけ `NotSupportedException`。`SignUpAsync`・`SignInAsync` は登録した `Func<UniTask<…>>` を `new UnaryResult<T>(task.AsTask())` で包んで返す。見本は基盤の `Tests/EditMode/DeviceTokenProviderTests.cs` の `FakeDeviceService`（353 行目付近）。失敗は基盤の `FakeRpcErrors.ApiFailureResult<T>` で作った `UnaryResult<T>` を返す（偽物のメソッドから同期で例外を投げない）
- 境界: `UtaApiClient.Dispose` は変えない。asmdef の DLL 名がリストアされたパッケージと違ってコンパイルできなければ、止まって報告する
- 終わりの確かめ: `unity recompile --format json` がエラー 0。EditMode のテスト（6 章）が通る

**タスク 3（docs のみ）: wiki を直す**

- `api-design.md` 58 行目: 「`PlayerApi`（`ApiServiceBase<IPlayerService>`）が `ApiCore` 経由で呼び」を「`UtaApiClient.Player`（`ApiCore.CreateClient<IPlayerService>()` が作る共有インターフェース）を `PlayerGateway` が `WithHandledCodes(...).SignInAsync().ToResult()` の形で呼び」に
- `unity7-evaluation.md` 31 行目: 表の記録に「（2026-11-08 に呼び方を `ApiCore.CreateClient` へ移したため、**この新しい呼び方では未確認**。タスク 5 で確かめる）」と足す。61 行目の取り消し線の項目はそのまま残し、直後に「→ 新しい呼び方（`RpcSettings`／`ApiCallFilter`）での確認はタスク 5」と足す。63 行目の「`ApiResult` の失敗を `Failure` に直す経路」は、偽の `IPlayerService` の EditMode のテストで確かめられるようになった旨に直す

**タスク 4（docs のみ）: 画面の計画に「読み替え」の節を足す**

foundation・formation・gacha・owned の 4 本の、タイトル（`# 実装計画: …`）の直後に、次の節をそのまま足す。

```
## 基盤 11d9b06 への読み替え（2026-11-08。[クライアントの移行計画](2026-11-08-client-foundation-migration.md)。この節が本文と食い違ったら、この節が正）

- `XxxApi`（`ApiServiceBase<…>` のラッパー。`GachaApi`・`SupportApi`・`ProduceApi`）は**作らない**。`UtaApiClient` が `public IGachaService Gacha { get; }`（`ISupportService Support`・`IProduceService Produce`）を持ち、`Create` の中で `core.CreateClient<I…Service>()` で作る。本文の「`ServiceClientFactory.Create<I…Service>(callInvoker)` で作って `new XxxApi(core, client)`」は「`core.CreateClient<I…Service>()`」に読み替える。private コンストラクターの引数の型は `XxxApi` ではなく `I…Service`
- ゲートウェイのコンストラクターは `I…Service` を受ける（`UtaApiClient` を受けない）。`BootstrapEntry` に `builder.Register<I…Service>(resolver => resolver.Resolve<UtaApiClient>().Xxx, Lifetime.Singleton);` を足す（`PlayerGateway` と同じ）
- 呼び方: `xxx.XxxAsync(req, handledCodes)` → `await xxx.WithHandledCodes(codes).XxxAsync(req).ToResult()`（`handledCodes` の無い RPC は `WithHandledCodes` を付けない）。`result.IsSuccess` → `!result.IsError`。`result.Error.Code.Equals(X)` → `result.ErrorCode == X`。`result.Error.ToString()`・`result.Error.Code.Reason` は今のまま使える
- 本文のファイル表の `Api/XxxApi.cs`（作成）の行は無効（作らない）。テストの偽物は、`PlayerGatewayTests` と同じく偽の `I…Service`（`IService<T>` の 5 メンバーを実装し、`WithHeaders` は `this` を返す）
- `[MagicOnionClientGeneration]` の型の並びに `typeof(I…Service)` を足す指示は今のまま有効（`Uta.Shared.Contracts` の同じアセンブリなので、足し忘れても `IPlayerService` で足りるが、本文どおり足す）
```

加えて各計画に、次の 1 行を足す。
- foundation: 「`UtaApiClient.Create` の `IApiActivityHandler`（3.8）は、移行計画の `Create`（引数は今のまま）に足す。`ApiCoreBuilder.AddActivityHandler` を使う」
- formation: 「314 行目の `UtaApiClient` の変更は `ProduceApi` ではなく `IProduceService Produce`（上の節）」

索引: 1章の表の先頭に行を足す: `| 0 | [クライアントの基盤移行](2026-11-08-client-foundation-migration.md) | 共通基盤 11d9b06 の新しい呼び方への移行（`PlayerApi` の廃止ほか） | なし | `main` | ready |` と、「順 0 は P1 より前に main へ入れる」の 1 行。続けて共通レスポンスの計画は「MVP の後の計画」の箇条書きに足す（順 4 の前でも後でもよい。foundation の `UtaApiClient.Create` を変えるので、**順 4 の前**に main へ入れる）

**タスク 5（Unity。Windows の IL2CPP）: 実機相当の確認**

- 準備: 基盤の固定で `devices`・`last_responses`・`game_logs` の `player_id` が `uuid` から `char(26)` に変わり、既存のマイグレーションが書き換わっている。**手元のローカル DB は作り直す**（`docker compose --env-file server/.env -f server/compose.yaml down -v` → `up -d --wait` → `dotnet run --project server/Uta.Server -- migrate`）
- 手順: CLAUDE.md の「ビルド・テスト・実行」の Windows の IL2CPP のビルドで `Uta.exe` を作り、ローカルのサーバー（`docker compose --env-file server/.env -f server/compose.yaml up -d --wait` → `dotnet run --project server/Uta.Server -- migrate` → `dotnet run --project server/Uta.Server`）に対して、次の順に `Uta.exe -batchmode -nographics -uta-quit-after-check` を実行する。アカウントのファイルは `%USERPROFILE%\AppData\LocalLow\<会社名>\<製品名>\` の `ServerEndpoint.AccountFileName(接続先 URL)` が返す名前（実装担当は `Uta.exe` のログか `ServerEndpoint.cs` で名前を確かめる）
  1. ①アカウント無し: 上のアカウントのファイルを消して実行 → `device: registered` → `sign-up: created` → `sign-in: ok`
  2. ②そのままもう一度実行 → `sign-up: skipped` → `sign-in: ok`
  3. ③`players` の行を消して実行: `docker compose --env-file server/.env -f server/compose.yaml exec postgres psql -U postgres -d uta -c "TRUNCATE players CASCADE"` の後で実行 → `SignInAsync` が `PLAYER_NOT_CREATED` を `WithHandledCodes` で受け、`SignUpAsync` → `SignInAsync` で復帰（ローカルの確認用 DB なので `CASCADE` で所持品も消えてよい。外部キーの関係で失敗したら、そのエラーを添えて止まって報告）
  4. ④トークンの無効化: ①〜③のどの実行でも、`ConnectivityCheck` が `reauth retry: ok (invalidations=1)` をログに出す（既存の診断。出なければ止まって報告）
  結果は `unity7-evaluation.md` の 31 行目を書き換えて記録する
- Monitor: Editor の Play で起動し、**Window > VolTech MagicOnion > Monitor** に `RegisterAsync`・`SignUpAsync`・`SignInAsync` が行になることを確かめる（コード変更は要らない。`ApiChannel.CreateCallInvoker` を通っているため）

## 6. テスト

`PlayerGatewayTests`（EditMode。偽の `IPlayerService`）に足す 4 本:
- `SignIn_ReturnsSuccess_WhenOk`
- `SignIn_ReturnsNotCreated_WhenPlayerNotCreated`（`FakeRpcErrors.ApiFailureResult<SignInResponse>` で `PlayerErrors.NotCreated` を返す）
- `SignIn_ReturnsFailure_WhenOtherError`
- `SignUp_ReturnsFailure_WhenError`

偽物は `ApiCore` を通らないので、`WithHandledCodes` が `handledCodes` に載せたコードを `ApiCore` が尊重するところ（共通ハンドラーに回さないこと）は、このテストでは確かめない。基盤のテストと、タスク 5 の ③ で確かめる。既存の `EnterPlayerUseCaseTests`・`UtaErrorsTests`・`MasterDataLoaderTests`・`MasterCardCatalogTests` は変えず、通ること。サーバー: `dotnet build Uta.slnx`（警告 0・エラー 0）と `dotnet test --project server/Uta.Server.Tests` は変えない（この計画で触らない）。

## 7. 未決事項

なし（0 章の了承を反映した）。

## 8. 実装メモ

| タスク | 状態 | コミット | 計画とのズレ・確認できていないこと |
|---|---|---|---|
| 2(a) | 完了 | `752fb9e` | Uta.Client.MasterData・Domain の precompiledReferences に `MasterMemory.Annotations.dll`・`MessagePack.Annotations.dll` も要った（R1 の 2 つでは足りない）。後ろのアセンブリは旧い API のエラーで (c) までコンパイルされない |
| 2(b) | 完了 | `3a9e78e` | なし（テストの実行は (c)） |
| 2(c) | 完了 | `a497b11` | `FakeRpcErrors` はパッケージの `Runtime/Client/Testing`（計画は `Tests/EditMode`）。`new UnaryResult<T>(value)` で通った。`PlayerUseCaseTests` は `[UnityTest]` + `UniTask.ToCoroutine`（11 件。計画のケースに `Enter_NotCreatedTwice_Throws`・`Refresh_NotCreated_ThrowsAndKeepsRepositories` を足した）。`MasterCardCatalogTests` の読み込みのテストは `MasterDataLoaderTests` に同じものがあり、写さず消した。テストを先に書いて失敗することは、この段では実測できなかった（Editor が固まった）。レビューの後に、中身を一時的に `NotImplementedException` にして実測した（下の「レビュー」の行）。EditMode は非同期の実行で 102 件成功。unity7-evaluation.md 4 章の `runSynchronously = true` のコマンドは `[UnityTest]` を走らせない（13 件が total=0） |
| 3・4 | 完了 | （wiki のコミット） | 索引の積み PR の base（foundation・P3・formation の行）と計画 0 の状態の食い違い、共通レスポンスの計画 16 行目の古い順番の記述は、意味が変わるので直していない |
| 5 | 一部 | （記録のコミット） | **IL2CPP のビルドはしない**（ユーザーの判断 2026-11-08。exe は必須ではない）。代わりに Editor の Play で ①②③ を確かめた（通った）。R7 のスレッドは `ManagedThreadId=2` で、Editor のメインスレッドの ID も 2 だった（計画の「1」は Unity のメインスレッドの意味。メインスレッドで動いている）。④再送・Monitor は未確認。ローカルの DB は `down -v` で作り直した（main のチェックアウトの `server/.env` のパスワードと user-secrets が違ったので、この worktree の `.env` を user-secrets に合わせた） |
| レビュー | 完了 | `99fd2c2`・`d4d00a7` | client 1 回目: Important 2（`Enter_Failed_ThrowsAndKeepsRepositories` が移っていない／テストを先に書いて失敗することの実測が無い）を直した。実測: `PlayerUseCase` を一時的に `NotImplementedException` にすると `PlayerUseCaseTests` 12 件すべて失敗、ほか 91 件は成功。Minor M1〜M5 も直した（`Apply` は詰め替えを先に済ませてから書く、など）。M6（`Uta.Client.Domain`・`MasterData` の asmdef を `noEngineReferences: true` にできるか）は任意のため見送り。shared: Minor 1（ADR 0022 の「採らないと判断したら」に yaml の `source` を戻すことが無い。ADR は親のセッションが直す） |
| R8 | 完了 | （R8 のコミット 3 つ） | ユーザーの判断で追加。マイグレーション 0007 の埋め戻しはローカルの DB の 2 行で確かめた（時刻の部分が `created_at` と一致）。サーバー 294 件・EditMode 103 件成功 |
| R9 | 完了 | `84e799f` | ユーザーの判断で追加。表どおり（差分なし）。クライアントのテストは位置引数で DTO を組んでいて変更不要だった。week・lesson の計画には読み替えの節が無かったので、同じ見出しの節を新しく作って 1 行だけ入れた |
