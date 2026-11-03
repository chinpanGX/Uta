---
title: 実装計画: client/ の画面 2/6 ガチャ
status: draft
updated: 2026-11-03
spec: ../wiki/systems/gacha.md, ../wiki/architecture/screen-design.md
---

# 実装計画: client/ の画面 2/6 ガチャ

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7 の 2 本目。ガチャの画面（ガチャ・排出率のモーダル・結果のモーダル）と、`IGachaService` を呼ぶクライアントの層（API・ゲートウェイ・ユースケース）を作り、ホームの「ガチャ」ボタンから開けるようにする。仕様は [gacha.md](../wiki/systems/gacha.md)（GAC-01・02・04。排出率の表示は 3.1、重複の扱いは 5章）、画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 9、RPC は [api-design.md](../wiki/architecture/api-design.md) 1章と `shared/Uta.Shared.Contracts/Gacha/`、サーバー側は [gacha-support-server.md](../wiki/architecture/server/gacha-support-server.md) 1章。

- 方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章。型・遷移・テストの書き方の正は [1 本目の計画](2026-11-03-client-screens-foundation.md) の 3章（**3.3 の共通ルール**: Presenter の形・購読と `Dispose`・実行中フラグ・`IScreenNavigator` の使い方・Args・通信とエラー・文字の書式・テストの書き方と偽）。この計画は 3.3 と違うことを書かない。書いていないことは 3.3 に従う
- 関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)（層。DTO の変換は Infrastructure に閉じる）、[0009](../wiki/decisions/0009-client-stack.md)（uGUI + UnityScreenNavigator・VContainer・UniTask）
- **前提**: 1 本目（foundation）が done。この計画が使うもの: `IScreenNavigator`（基盤）、`PageBase`・`ModalBase`・`UiFactory`・`UiTheme`・`DialogService`・`UiRoot.RegisterPage`／`RegisterModal`、`IGameMaster`（`GachaInfo`・`GachaEntryInfo`・`IdolInfo`・`SupportCardInfo`・`MasterItemKind`）、`RefreshPlayerUseCase`、`HomePresenter`、テスト用の `CallLog`・`FakeScreenNavigator`・`FakeDialogService`、`UtaErrors` の業務エラーの登録（`GachaErrors` の 5 つが `ErrorAction.Stay`）
- **後続が使うもの**（この計画で作る）: `PortResult<T>`・`ServerReasons`（Application の共通）と、テスト用の `FakeGameMaster`。3 本目（所持一覧・強化）以降はこれを使うので、**3 本目はこの計画の後に進める**（並行しない）。`ServerReasons` にはこの計画で使う reason だけを置き、ほかの reason はそれを使う計画が足す
- **ホームの「ガチャ」**: foundation の `HomePresenter.OpenGachaAsync`（いまは「準備中です。」）を、ガチャのページを積む処理に置き換える。`HomePresenter` のコンストラクター（2 本）の最後に `IScreenNavigator` を足すのはこの計画（foundation 3.7）
- **決めないこと**: 交換（GAC-03。MVP の画面に入れない: [game-spec.md](../wiki/overview/game-spec.md)「画面」）。交換pt は「交換pt N / M」の表示だけで、「交換」ボタンは作らない。抽選履歴、ガチャの演出（MVP は演出なし: screen-wireframes.md 9）、複数のガチャの切り替え（MVP はガチャ 1 本。`master.Gachas[0]` だけを使う）
- **叩き台との差**（[Uta案]。結果の記録で screen-design の実装メモに書く）:
  - ガチャ名の行は、マスタの `Gacha` に名前の列が無いので、固定の「ガチャ」にする
  - ガチャの画像は出さない（MVP は文字だけ）
  - 結果はカードの絵ではなく、1 枠 1 行の文字にする
  - 結果の「NEW」は育成アイドルを初めて得たときだけ付ける。サポートカードは `Outcome = New` が「行が増えた」（2 枚目以降も New。gacha.md 5章）なので、「NEW」ではなく「所持に追加」と出す

## 2. 触るファイル

パスは `client/uta/Assets/` からの相対。新しい `.cs`・フォルダには、Unity の再コンパイルのあとに生成された `.meta` をコミットする。

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `Scripts/Application/Common/PortResult.cs` | ポートの結果の型（3 本目以降も使う） |
| 作成 | `Scripts/Application/Common/ServerReasons.cs` | サーバーの業務エラーの reason（この計画は 1 つ） |
| 作成 | `Scripts/Application/Gacha/GachaStatus.cs`・`DrawItem.cs`・`DrawCallResult.cs`・`IGachaPort.cs` | ガチャの通信ポートと、ポートが返す型 |
| 作成 | `Scripts/Application/Gacha/GetGachaStatusUseCase.cs`・`DrawStatus.cs`・`DrawOutcome.cs`・`DrawUseCase.cs` | ユースケース |
| 作成 | `Scripts/Application/Gacha/GachaRateGroup.cs`・`GachaRateRow.cs`・`GachaRates.cs`・`GachaRateCalculator.cs` | 排出率の計算 |
| 作成 | `Scripts/Infrastructure/Api/GachaApi.cs` | `ApiServiceBase<IGachaService>` |
| 変更 | `Scripts/Infrastructure/Api/UtaApiClient.cs` | `Gacha`（`GachaApi`）を足す |
| 変更 | `Scripts/Infrastructure/Api/UtaMagicOnionClientInitializer.cs` | `typeof(IGachaService)` を足す |
| 作成 | `Scripts/Infrastructure/Gacha/GachaGateway.cs` | `IGachaPort` の実装 |
| 作成 | `Scripts/Presentation/Gacha/GachaTexts.cs` | 種別の表示名 |
| 作成 | `Scripts/Presentation/Gacha/IGachaView.cs`・`GachaPage.cs`・`GachaPresenter.cs` | ガチャのページ |
| 作成 | `Scripts/Presentation/Gacha/GachaRatesArgs.cs`・`GachaRateLine.cs`・`IGachaRatesView.cs`・`GachaRatesModal.cs`・`GachaRatesPresenter.cs` | 排出率のモーダル |
| 作成 | `Scripts/Presentation/Gacha/GachaResultArgs.cs`・`IGachaResultView.cs`・`GachaResultModal.cs`・`GachaResultPresenter.cs` | 結果のモーダル |
| 変更 | `Scripts/Presentation/Home/HomePresenter.cs` | コンストラクターに `IScreenNavigator` を足し、`OpenGachaAsync` をガチャのページを積む処理にする |
| 変更 | `Scripts/Bootstrap/UtaBootstrap.cs` | 雛形の登録（`RegisterScreens`）と DI の登録 |
| 作成 | `Tests/EditMode/Fakes/FakeGameMaster.cs` | `IGameMaster` のテスト用の実装（3 本目以降も使う） |
| 作成 | `Tests/EditMode/PortResultTests.cs`・`ServerReasonsTests.cs`・`GachaRateCalculatorTests.cs`・`DrawUseCaseTests.cs`・`GetGachaStatusUseCaseTests.cs`・`GachaGatewayTests.cs`・`GachaPresenterTests.cs`・`GachaRatesPresenterTests.cs`・`GachaResultPresenterTests.cs` | テスト（5章） |
| 変更 | `Tests/EditMode/HomePresenterTests.cs` | コンストラクターの引数と、ガチャのボタンのテストを直す（5章） |
| 変更 | `uta_docs/wiki/architecture/screen-design.md`・`unity7-evaluation.md` | 結果の記録（タスク 4） |

asmdef は変えない（Presentation・Bootstrap・テストの参照は foundation 3.1 で足してある。`Uta.Client.Application` は `Uta.Shared.Domain`・`UniTask` だけで足りる）。

## 3. 型とシグネチャ

C# 9 の範囲・書式・名前空間の決め方は foundation 3章の冒頭のとおり。名前空間: `Application/Common` → `Uta.Client.Application.Common`、`Application/Gacha` → `Uta.Client.Application.Gacha`、`Infrastructure/Api` → `Uta.Client.Infrastructure.Api`、`Infrastructure/Gacha` → `Uta.Client.Infrastructure.Gacha`、`Presentation/Gacha` → `Uta.Client.Presentation.Gacha`。型の名前は `Uta.Shared.Domain` と衝突しないことを確かめてある（2026-11-03 に grep）。

### 3.1 `Uta.Client.Application.Common`

```csharp
public sealed class PortResult<T>
{
    public bool Succeeded { get; }
    public T Value { get; }               // 成功のときだけ。失敗は default
    public string Reason { get; }         // 業務エラーの reason（ServerReasons の定数と同じ文字列）。業務エラーでなければ null
    public string ErrorText { get; }      // 失敗の説明（ログ用）。成功は null。失敗で null を渡されたら "" にする
    public bool IsBusinessError { get; }  // !Succeeded && Reason != null

    public static PortResult<T> Success(T value);                                  // value は null でもよい
    public static PortResult<T> BusinessError(string reason, string errorText);    // reason が null か "" なら ArgumentException
    public static PortResult<T> Failure(string errorText);                         // ApiCore が共通のダイアログを出し終えた失敗（通信の失敗、handledCodes に入れなかった業務エラー）
}

public static class ServerReasons
{
    public const string GachaNotEnoughCurrency = "GACHA_NOT_ENOUGH_CURRENCY";
}
```

- コンストラクターは private 1 本（`succeeded, value, reason, errorText`）。
- `ServerReasons` の値は `Uta.Shared.Contracts` の `ErrorCode` の `Reason` と同じ文字列（`Application` は `Contracts` を参照できないので写す: [repository-layout.md](../wiki/architecture/repository-layout.md) 3章）。食い違わないことは `ServerReasonsTests` で確かめる。**ほかの reason は、それを使う計画が定数とテストの行を足す**（3 本目の `SupportErrors` など）。`PLAYER_NOT_CREATED` は `PlayerGateway` が `PlayerErrors.NotCreated` で直接判定するので（Infrastructure の中で閉じる）、ここに置かない。

### 3.2 `Uta.Client.Application.Gacha`

```csharp
public sealed class GachaStatus
{
    public GachaStatus(int gachaId, int exchangePoints);
    public int GachaId { get; }
    public int ExchangePoints { get; }
}

public sealed class DrawItem
{
    public DrawItem(int slot, MasterItemKind kind, int itemId, bool isNew, int shards, int supportPoints);
    public int Slot { get; }              // 1〜10
    public MasterItemKind Kind { get; }
    public int ItemId { get; }            // 育成アイドルなら IdolId、サポートカードなら SupportCardId
    public bool IsNew { get; }            // GachaOutcome.New（育成アイドルは初めて得た、サポートカードは所持の行が 1 つ増えた）
    public int Shards { get; }            // 育成アイドルの重複で得たかけら（それ以外は 0）
    public int SupportPoints { get; }     // サポートカードの完凸後の重複で変換した強化pt（それ以外は 0）
}

public sealed class DrawCallResult
{
    public DrawCallResult(IReadOnlyList<DrawItem> items, int gachaCurrency, int exchangePoints);
    public IReadOnlyList<DrawItem> Items { get; }   // Slot の昇順
    public int GachaCurrency { get; }               // 引いた後の残り（DrawResponse.GachaCurrency）
    public int ExchangePoints { get; }              // 引いた後のこのガチャの交換pt
}

public interface IGachaPort
{
    UniTask<PortResult<IReadOnlyList<GachaStatus>>> GetGachasAsync();
    UniTask<PortResult<DrawCallResult>> DrawAsync(int gachaId, int count, int expectedCost);
}

public sealed class GetGachaStatusUseCase
{
    public GetGachaStatusUseCase(IGachaPort port);
    /// <summary>
    /// port.GetGachasAsync を呼ぶ。成功なら gachaId の行の ExchangePoints（一覧に無ければ 0）。
    /// 失敗なら null（共通のダイアログは ApiCore が出している）。例外は投げない（port が投げたものはそのまま上へ）。
    /// </summary>
    public UniTask<int?> ExecuteAsync(int gachaId);
}

public enum DrawStatus { Succeeded, NotEnoughCurrency, Failed }

public sealed class DrawOutcome
{
    public DrawStatus Status { get; }
    public DrawCallResult Result { get; }   // Succeeded のときだけ。それ以外は null
    public bool RefreshFailed { get; }      // Succeeded で、取り直し（RefreshPlayerUseCase）が失敗した。PlayerCache は古いまま
    public static DrawOutcome Success(DrawCallResult result, bool refreshFailed);   // result が null なら ArgumentNullException
    public static DrawOutcome Rejected(DrawStatus status);                         // status が Succeeded なら ArgumentException
}

public sealed class DrawUseCase
{
    public DrawUseCase(IGachaPort port, RefreshPlayerUseCase refresh, PlayerCache cache);
    /// <summary>count は 1 か 10（それ以外は ArgumentOutOfRangeException）。手順は下。</summary>
    public UniTask<DrawOutcome> ExecuteAsync(GachaInfo gacha, int count);
}
```

`DrawUseCase.ExecuteAsync` の手順:
1. `count` が 1・10 以外なら `ArgumentOutOfRangeException(nameof(count))`
2. `var cost = count == 1 ? gacha.SingleCost : gacha.TenCost;`。`cache.Current.GachaCurrency < cost` なら通信せず `DrawOutcome.Rejected(DrawStatus.NotEnoughCurrency)` を返す（キャッシュが古くて多めに見えていても、サーバーが同じ業務エラーで断る）
3. `var result = await port.DrawAsync(gacha.GachaId, count, cost);`
4. 失敗のとき: `result.IsBusinessError && result.Reason == ServerReasons.GachaNotEnoughCurrency` なら `Rejected(NotEnoughCurrency)`、それ以外は全部 `Rejected(Failed)`（共通のダイアログは ApiCore が出している）。取り直しはしない
5. 成功のとき: `try { await refresh.ExecuteAsync(); refreshFailed = false; } catch (Exception) { refreshFailed = true; }`（握りつぶす。取り直しの通信の失敗のダイアログは ApiCore が出している。foundation 3.3.5）→ `DrawOutcome.Success(result.Value, refreshFailed)`

```csharp
public sealed class GachaRateGroup
{
    public GachaRateGroup(Rarity rarity, MasterItemKind kind, int basisPoints);
    public Rarity Rarity { get; }
    public MasterItemKind Kind { get; }
    public int BasisPoints { get; }   // 0.01% 単位（10000 = 100%）
}

public sealed class GachaRateRow
{
    public GachaRateRow(Rarity rarity, MasterItemKind kind, int itemId, string name, int basisPoints);
    public Rarity Rarity { get; }
    public MasterItemKind Kind { get; }
    public int ItemId { get; }
    public string Name { get; }
    public int BasisPoints { get; }
}

public sealed class GachaRates
{
    public GachaRates(IReadOnlyList<GachaRateGroup> groups, IReadOnlyList<GachaRateRow> rows);
    public IReadOnlyList<GachaRateGroup> Groups { get; }   // レア別・種別の合計
    public IReadOnlyList<GachaRateRow> Rows { get; }       // カードごと
}

public static class GachaRateCalculator
{
    /// <summary>手順は下。</summary>
    public static GachaRates Calculate(IReadOnlyList<GachaEntryInfo> entries, IGameMaster master);

    /// <summary>basisPoints / 100 + "." + (basisPoints % 100) を 2 桁 + "%"。例 5 → "0.05%"、1234 → "12.34%"、10000 → "100.00%"。負なら ArgumentOutOfRangeException。</summary>
    public static string FormatPercent(int basisPoints);
}
```

`GachaRateCalculator.Calculate` の手順（[gacha.md](../wiki/systems/gacha.md) 3.1: レア別・種別の % は**重みを合計してから**計算し、小数第 3 位以下を切り捨てる）:
1. `total` = `entries` の `Weight` の合計（`long`）。`entries` が空か `total == 0` なら、`Groups`・`Rows` とも空の `GachaRates` を返す
2. 各エントリーのレア度と名前を引く: `ItemKind == Idol` は `master.GetIdol(ItemId)`（`Rarity`・`Name`）、`SupportCard` は `master.GetSupportCard(ItemId)`。それ以外の `ItemKind` は `ArgumentException`。マスタに無い ID は `GetIdol` などが投げる `KeyNotFoundException` をそのまま上へ
3. `Groups`: (レア度, 種別) ごとに重みを足し、`(int)(合計 * 10000 / total)`。エントリーが 1 つでもある組だけを出す（重み 0 のエントリーだけの組は 0 で出す）。並びはレア度の降順（`Rarity` の数値の降順: SSR → SR → R → N）、同じレアは種別の昇順（`MasterItemKind` の数値: 育成アイドル → サポートカード）
4. `Rows`: エントリーごとに `(int)(weight * 10000 / total)`（重み 0 の行も 0 で出す）。並びは `Groups` と同じ順のあと `ItemId` の昇順（同じ ID が 2 行あれば `entries` の順のまま。LINQ の `OrderBy`／`ThenBy` は安定）

### 3.3 `Uta.Client.Infrastructure`

```csharp
// Api/GachaApi.cs（PlayerApi と同じ形）
public sealed class GachaApi : ApiServiceBase<IGachaService>
{
    public GachaApi(ApiCore core, IGachaService client) : base(core, client) { }
    public UniTask<ApiResult<GetGachasResponse>> GetGachasAsync()
        => InvokeAsync(nameof(IGachaService.GetGachasAsync), 0, static (c, _) => c.GetGachasAsync());
    public UniTask<ApiResult<DrawResponse>> DrawAsync(DrawRequest request, IEnumerable<ErrorCode> handledCodes = null)
        => InvokeAsync(nameof(IGachaService.DrawAsync), request, static (c, r) => c.DrawAsync(r), handledCodes);
}

// Gacha/GachaGateway.cs
public sealed class GachaGateway : IGachaPort
{
    public GachaGateway(UtaApiClient api);   // gacha = api.Gacha
    public UniTask<PortResult<IReadOnlyList<GachaStatus>>> GetGachasAsync();
    public UniTask<PortResult<DrawCallResult>> DrawAsync(int gachaId, int count, int expectedCost);
    public static IReadOnlyList<GachaStatus> ToGachaStatuses(GetGachasResponse response);   // テストのため public static
    public static DrawCallResult ToDrawCallResult(DrawResponse response);                    // 同上
}
```

- `UtaApiClient`: `public GachaApi Gacha { get; }` を足す。private のコンストラクターの最後に `GachaApi gacha` を足して `Gacha = gacha;`。`Create`（foundation 3.8 で引数が増えた版）で、`playerClient` の次の行に `var gachaClient = ServiceClientFactory.Create<IGachaService>(callInvoker);` を足し、`new UtaApiClient(..., new PlayerApi(core, playerClient), new GachaApi(core, gachaClient))` にする。`using Uta.Shared.Contracts.Gacha;` を足す
- `UtaMagicOnionClientInitializer`: `[MagicOnionClientGeneration(typeof(IDeviceService), typeof(IPlayerService), typeof(IGachaService))]`（`using Uta.Shared.Contracts.Gacha;`）。生成は Unity の再コンパイルで行われる
- `GachaGateway` のフィールド: `private readonly ErrorCode[] drawHandled = { GachaErrors.NotEnoughCurrency };`（`PlayerGateway` の `notCreated` と同じ書き方）
- `GachaGateway.DrawAsync`: `gacha.DrawAsync(new DrawRequest(gachaId, count, expectedCost), drawHandled)`。`result.IsSuccess` なら `PortResult<DrawCallResult>.Success(ToDrawCallResult(result.Value))`。失敗で `result.Error.Code.Equals(GachaErrors.NotEnoughCurrency)` なら `BusinessError(result.Error.Code.Reason, result.Error.ToString())`、それ以外は `Failure(result.Error.ToString())`
  - `handledCodes` に入れるのは通貨不足だけ。`GachaErrors` のほかの 4 つ（`NotFound`・`InvalidCount`・`CostMismatch`・`NotEnoughPoints`）は foundation の `UtaErrors` に `Stay` で登録されているので、ApiCore の共通ダイアログが出て、画面に残り、`Failure` になる。`CostMismatch` はマスタとサーバーの食い違い（クライアントの不具合のときだけ起きる: gacha-support-server.md 1章）なので、画面ごとの文言を出さない
- `GachaGateway.GetGachasAsync`: `gacha.GetGachasAsync()`。成功なら `Success(ToGachaStatuses(result.Value))`、失敗なら `Failure(result.Error.ToString())`
- `ToGachaStatuses`: `response.Gachas` が null なら空のリスト。それ以外は `new GachaStatus(dto.GachaId, dto.ExchangePoints)` を同じ順で
- `ToDrawCallResult`: `response.Results` が null なら `Items` は空。それ以外は `DrawResultDto` ごとに `new DrawItem(dto.Slot, (MasterItemKind)(int)dto.ItemKind, dto.ItemId, dto.Outcome == GachaOutcome.New, dto.Shards, dto.SupportPoints)` を作り、`Slot` の昇順に並べ直す。`GachaCurrency`・`ExchangePoints` は応答の値
- `ApiResult` の成功・失敗はテストから作れない（`internal`。unity7-evaluation.md 3.3）ので、`GetGachasAsync`・`DrawAsync` の分岐は EditMode でテストしない。変換（`ToGachaStatuses`・`ToDrawCallResult`）だけをテストする

### 3.4 `Uta.Client.Presentation.Gacha`

```csharp
internal static class GachaTexts
{
    /// <summary>Idol → "育成アイドル"、SupportCard → "サポートカード"、それ以外 → ArgumentOutOfRangeException。</summary>
    public static string Kind(MasterItemKind kind);
}

public interface IGachaView
{
    void SetCurrency(int gachaCurrency);
    void SetExchange(int? points, int ceilingPoints);   // points が null は「取れなかった」
    void SetCosts(int singleCost, int tenCost);
    void SetLineup(IReadOnlyList<string> lines);
    event Action SingleClicked;
    event Action TenClicked;
    event Action RatesClicked;
    event Action BackClicked;
}

public sealed class GachaPage : PageBase, IGachaView { ... }   // レイアウトは下

[AssetAddress(nameof(GachaPage))]
public sealed class GachaPresenter : IPresenter
{
    [Inject]
    public GachaPresenter(GachaPage view, IGameMaster master, PlayerCache cache, GetGachaStatusUseCase status, DrawUseCase draw, DialogService dialogs, IScreenNavigator navigator)
        : this((IGachaView)view, master, cache, status, draw, dialogs, navigator) { }
    public GachaPresenter(IGachaView view, IGameMaster master, PlayerCache cache, GetGachaStatusUseCase status, DrawUseCase draw, DialogService dialogs, IScreenNavigator navigator);

    public UniTask InitializeAsync();          // async
    public void Dispose();
    public UniTask DrawAsync(int count);       // 単発は 1、10 連は 10。結果のモーダルの「もう一度」からも呼ばれる
    public UniTask OpenRatesAsync();
    public UniTask BackAsync();
}

public sealed class GachaRatesArgs
{
    public GachaRatesArgs(int gachaId);
    public int GachaId { get; }
}

public sealed class GachaRateLine
{
    public GachaRateLine(string text, bool isHeading);
    public string Text { get; }
    public bool IsHeading { get; }
}

public interface IGachaRatesView
{
    void SetLines(IReadOnlyList<GachaRateLine> lines);
    event Action CloseClicked;
}

public sealed class GachaRatesModal : ModalBase, IGachaRatesView { ... }

[AssetAddress(nameof(GachaRatesModal))]
public sealed class GachaRatesPresenter : IScreenWithArgs<GachaRatesArgs>
{
    [Inject]
    public GachaRatesPresenter(GachaRatesModal view, GachaRatesArgs args, IGameMaster master, IScreenNavigator navigator)
        : this((IGachaRatesView)view, args, master, navigator) { }
    public GachaRatesPresenter(IGachaRatesView view, GachaRatesArgs args, IGameMaster master, IScreenNavigator navigator);

    public UniTask InitializeAsync();
    public void Dispose();
    public UniTask CloseAsync();
}

public sealed class GachaResultArgs
{
    public GachaResultArgs(IReadOnlyList<DrawItem> items, Func<UniTask> retry);   // items が null なら ArgumentNullException。retry は null を許す（「もう一度」は閉じるだけになる）
    public IReadOnlyList<DrawItem> Items { get; }
    public Func<UniTask> Retry { get; }
}

public interface IGachaResultView
{
    void SetLines(IReadOnlyList<string> lines);
    event Action CloseClicked;
    event Action RetryClicked;
}

public sealed class GachaResultModal : ModalBase, IGachaResultView { ... }

[AssetAddress(nameof(GachaResultModal))]
public sealed class GachaResultPresenter : IScreenWithArgs<GachaResultArgs>
{
    [Inject]
    public GachaResultPresenter(GachaResultModal view, GachaResultArgs args, IGameMaster master, IScreenNavigator navigator)
        : this((IGachaResultView)view, args, master, navigator) { }
    public GachaResultPresenter(IGachaResultView view, GachaResultArgs args, IGameMaster master, IScreenNavigator navigator);

    public UniTask InitializeAsync();
    public void Dispose();
    public UniTask CloseAsync();
    public UniTask RetryAsync();
}
```

どの Presenter も foundation 3.3.2 の決まりに従う: 購読は `InitializeAsync` で、`Dispose` で全部解く。イベントの受け口（`OnXxxClicked`）は同期で `XxxAsync().Forget()` を呼ぶだけ。公開の `XxxAsync`（`DrawAsync`・`OpenRatesAsync`・`BackAsync`・`CloseAsync`・`RetryAsync`）は、Presenter に 1 つの `private bool running;` を使い、先頭で `if (running) return;`、`running = true;`、本体を `try`、`finally` で `running = false;`。遷移はすべて `await` し、`playAnimation: false` を名前付きで渡す。数値の 3 桁区切りは `ToString("N0", CultureInfo.InvariantCulture)`（以下 `N0(x)` と書く）。レア度の文字は `rarity.ToString()`（"SSR"・"SR"・"R"・"N"）。

**`GachaPresenter`**
- フィールド: `GachaInfo gacha`（`InitializeAsync` で決める）、`int currency`（いま画面に出しているガチャ通貨）、`bool running`
- `InitializeAsync`（`async`）:
  1. `view` の 4 つのイベントを購読する（`SingleClicked` → `DrawAsync(1).Forget()`、`TenClicked` → `DrawAsync(10).Forget()`、`RatesClicked` → `OpenRatesAsync().Forget()`、`BackClicked` → `BackAsync().Forget()`）
  2. `gacha = master.Gachas[0];`（MVP はガチャ 1 本。マスタの検証を通っているので空の場合は扱わない）
  3. `currency = cache.Current.GachaCurrency; view.SetCurrency(currency);`
  4. `view.SetCosts(gacha.SingleCost, gacha.TenCost);`
  5. `view.SetLineup(lines)`: `gacha.NormalEntries` のうち、レア度（`GetIdol`／`GetSupportCard` の `Rarity`）が `Rarity.SSR` のものを、種別の昇順（育成アイドル → サポートカード）→ `ItemId` の昇順に並べ、1 行ずつ `$"SSR {GachaTexts.Kind(kind)}: {name}"`。同じ (種別, ID) が 2 回あれば 1 行にする。SSR が無ければ空のリスト
  6. `var points = await status.ExecuteAsync(gacha.GachaId); view.SetExchange(points, gacha.CeilingPoints);`（失敗は null のまま渡す。ダイアログは ApiCore が出している。`InitializeAsync` が終わるまでページは出ない）
- `DrawAsync(int count)`（実行中フラグの中）:
  1. `var cost = count == 1 ? gacha.SingleCost : gacha.TenCost;`
  2. `var answer = await dialogs.ConfirmAsync($"ガチャ通貨 {N0(cost)} を使って {count} 回引きます。（所持 {N0(currency)}）", "引く", "やめる");` `Negative` なら終わる
  3. `var outcome = await draw.ExecuteAsync(gacha, count);`
  4. `Succeeded`: `currency = outcome.Result.GachaCurrency; view.SetCurrency(currency); view.SetExchange(outcome.Result.ExchangePoints, gacha.CeilingPoints);` → `await navigator.PushModalAsync<GachaResultPresenter, GachaResultArgs>(new GachaResultArgs(outcome.Result.Items, () => DrawAsync(count)), playAnimation: false);`。`RefreshFailed` でも同じ（通貨は応答の値で出すので、キャッシュが古くても画面は正しい。追加のダイアログは出さない。ホームの表示は次に取り直すまで古いまま: 3.3.5）
  5. `NotEnoughCurrency`: `await dialogs.ShowInfoAsync("ガチャ通貨が足りません。");`
  6. `Failed`: 何もしない（ApiCore が共通のダイアログを出している）
  - `catch` は書かない（ユースケースは通信の失敗を結果で返す。想定外の例外は `Forget` の未処理の例外としてログに出る）。`finally` で `running = false`
  - 「もう一度」: 結果のモーダルが閉じたあと `Retry` を呼ぶので、そのときこの Presenter の `running` は false に戻っている（`PushModalAsync` が返った時点で `finally` を通る）。2 回目も確認ダイアログから始まる
- `OpenRatesAsync`（実行中フラグの中）: `await navigator.PushModalAsync<GachaRatesPresenter, GachaRatesArgs>(new GachaRatesArgs(gacha.GachaId), playAnimation: false);`
- `BackAsync`（実行中フラグの中）: `await navigator.PopPageAsync(this, playAnimation: false);`
- `Dispose`: 4 つの購読を解く

**`GachaRatesPresenter`**
- `InitializeAsync`: `view.CloseClicked += OnCloseClicked;`。`var gacha = master.GetGacha(args.GachaId);`。次の順に `GachaRateLine` のリストを作り、`view.SetLines(lines)`。`UniTask.CompletedTask` を返す
  1. 見出し `"通常枠（単発・10連の1〜9枠目）"`
  2. `var normal = GachaRateCalculator.Calculate(gacha.NormalEntries, master);` の `Groups` を 1 行ずつ `$"{rarity} {GachaTexts.Kind(kind)} {FormatPercent(bp)}"`（見出しでない行）
  3. 見出し `"通常枠の内訳"`
  4. `normal.Rows` を 1 行ずつ `$"{rarity} {GachaTexts.Kind(kind)} {name} {FormatPercent(bp)}"`
  5. 見出し `"確定枠（10連の10枠目）"`
  6. `GuaranteedEntries` の `Groups` を 2 と同じ形で
  7. 見出し `"確定枠の内訳"`
  8. `GuaranteedEntries` の `Rows` を 4 と同じ形で
- `CloseAsync`（実行中フラグの中）: `await navigator.PopModalAsync(this, playAnimation: false);`
- `Dispose`: 購読を解く

**`GachaResultPresenter`**
- `InitializeAsync`: `CloseClicked`・`RetryClicked` を購読する。`args.Items` を 1 つずつ 1 行の文字列にして `view.SetLines(lines)`。`UniTask.CompletedTask` を返す
  - 行: `$"{item.Slot}. {rarity} {GachaTexts.Kind(item.Kind)} {name}　{note}"`（`name` の後は全角の空白 1 つ）。`rarity`・`name` は `GetIdol`／`GetSupportCard`
  - `note`: 育成アイドルで `IsNew` → `"NEW"`、育成アイドルで `!IsNew` → `$"かけら +{N0(Shards)}"`、サポートカードで `IsNew` → `"所持に追加"`、サポートカードで `!IsNew` → `$"サポート強化pt +{N0(SupportPoints)}"`
- `CloseAsync`（実行中フラグの中）: `await navigator.PopModalAsync(this, playAnimation: false);`
- `RetryAsync`（実行中フラグの中）: `await navigator.PopModalAsync(this, playAnimation: false);` のあと、`args.Retry` が null でなければ `await args.Retry();`
- `Dispose`: 2 つの購読を解く

**画面のレイアウト**（`BuildUi`。色・寸法は `UiTheme`。見た目は仮だが、構成・文字・大きさはこのとおりにする）。どの View も、公開メソッド（`Set…`）の先頭で `EnsureBuilt()` を呼び、ボタンの `onClick.AddListener(() => XxxClicked?.Invoke())` でイベントを起こす（foundation 3.3.3）。

- `GachaPage.BuildUi(body)`: `var root = UiFactory.CreateVertical(body, "Root");` の子に上から
  1. "Header"（`CreateHorizontal(root, "Header")`、`SetHeight(header, UiTheme.ButtonHeight)`。`HorizontalLayoutGroup.childForceExpandWidth = false` に変える）の子に、"Back"（`CreateButton(header, "Back", "＜")`。`LayoutElement` の `preferredWidth = 160`・`flexibleWidth = 0`）と "Currency"（`CreateText(header, "Currency", "", UiTheme.FontBody, TextAnchor.MiddleRight)`。`LayoutElement` の `flexibleWidth = 1`）
  2. "Title"（`CreateText(root, "Title", "ガチャ", UiTheme.FontTitle, TextAnchor.MiddleCenter)`、`SetHeight(100)`）
  3. "Lineup"（`CreateText(root, "Lineup", "", UiTheme.FontBody, TextAnchor.MiddleCenter)`、`SetFlexibleHeight(1)`）
  4. "Exchange"（`CreateText(root, "Exchange", "", UiTheme.FontBody, TextAnchor.MiddleCenter)`、`SetHeight(70)`）
  5. "Rates"（`CreateButton(root, "Rates", "排出率")`）→ `RatesClicked`
  6. "Draw"（`CreateHorizontal(root, "Draw")`、`SetHeight(UiTheme.ButtonHeight)`）の子に左から "Single"（`CreateButton("単発")`）→ `SingleClicked`、"Ten"（`CreateButton("10連")`）→ `TenClicked`
  - "Back" → `BackClicked`
  - `SetCurrency(n)`: Currency の text = `"ガチャ通貨 " + N0(n)`
  - `SetExchange(points, ceiling)`: Exchange の text = `"交換pt " + (points.HasValue ? N0(points.Value) : "-") + " / " + N0(ceiling)`
  - `SetCosts(single, ten)`: `UiFactory.SetLabel(singleButton, "単発 " + N0(single))`、`SetLabel(tenButton, "10連 " + N0(ten))`
  - `SetLineup(lines)`: Lineup の text = `string.Join("\n", lines)`（空なら ""）
- `GachaRatesModal.BuildUi(panel)`: 上から "Title"（`CreateText(panel, "Title", "排出率", UiTheme.FontTitle, TextAnchor.MiddleCenter)`、`SetHeight(90)`）、`content = UiFactory.CreateScrollContent(panel, "Scroll")` と `UiFactory.SetHeight(content.parent.parent, 1200f)`（`CreateScrollContent` は Content を返す。高さを付けるのはその親の親の "Scroll"。パネルは `ContentSizeFitter` なので `flexibleHeight` では高さが決まらない）、"Close"（`CreateButton(panel, "Close", "閉じる")`）→ `CloseClicked`
  - `SetLines(lines)`: `UiFactory.ClearChildren(content)` のあと、i 番目を `CreateText(content, "Line" + i, line.Text, line.IsHeading ? UiTheme.FontBody : UiTheme.FontSmall, TextAnchor.MiddleLeft, line.IsHeading ? UiTheme.Accent : (Color?)null)`
- `GachaResultModal.BuildUi(panel)`: 上から "Title"（`CreateText(panel, "Title", "結果", UiTheme.FontTitle, TextAnchor.MiddleCenter)`、`SetHeight(90)`）、`content = CreateScrollContent(panel, "Scroll")` と `SetHeight(content.parent.parent, 900f)`、"Buttons"（`CreateHorizontal(panel, "Buttons")`、`SetHeight(UiTheme.ButtonHeight)`）の子に左から "Retry"（`CreateButton("もう一度")`）→ `RetryClicked`、"Close"（`CreateButton("閉じる")`）→ `CloseClicked`
  - `SetLines(lines)`: `ClearChildren(content)` のあと、i 番目を `CreateText(content, "Line" + i, lines[i], UiTheme.FontBody, TextAnchor.MiddleLeft)`

### 3.5 ホームと Bootstrap

- `HomePresenter`（foundation 3.7）: 2 本のコンストラクターの最後に `IScreenNavigator navigator` を足す（`[Inject]` の側は `: this((IHomeView)view, cache, dialogs, navigator)`）。`OpenGachaAsync` の本体（実行中フラグの `try` の中）を `await navigator.PushPageAsync<GachaPresenter>(playAnimation: false);` に置き換える（戻り値は使わない）。`using UnityScreenNavigator;`・`using Uta.Client.Presentation.Gacha;` を足す。ほかの 3 つのメソッドは変えない（「準備中です。」のまま）
- `UtaBootstrap.RegisterScreens` に `uiRoot.RegisterPage<GachaPage>(); uiRoot.RegisterModal<GachaRatesModal>(); uiRoot.RegisterModal<GachaResultModal>();` を足す
- `UtaBootstrap` の DI（foundation 3.8 の「通常起動・診断モードで共通」の `RefreshPlayerUseCase` の次）に `builder.Register<GachaGateway>(Lifetime.Singleton).As<IGachaPort>(); builder.Register<GetGachaStatusUseCase>(Lifetime.Singleton); builder.Register<DrawUseCase>(Lifetime.Singleton);` を足す。Presenter は登録しない（foundation 3.3.2）

## 4. タスク

各タスクの終わりに、`unity recompile --format json`（`client/uta` で実行。コンパイルエラー 0）と、そのタスクまでの EditMode のテストを通す。EditMode のテストの回し方は foundation 4章の冒頭（`unity test --mode EditMode`。foundation のタスク 1 で動かなかったなら Editor の Test Runner の EditMode）。**Editor を開けない環境では、通ったとは書かず、確かめていないと報告する**。

### タスク 1: Application の共通の型・ガチャの型・排出率、テスト用の `FakeGameMaster`

触る: `Application/Common/*`、`Application/Gacha/` の `GachaStatus`・`DrawItem`・`DrawCallResult`・`IGachaPort`・`GachaRateGroup`・`GachaRateRow`・`GachaRates`・`GachaRateCalculator`、`Tests/EditMode/Fakes/FakeGameMaster.cs`、`Tests/EditMode/PortResultTests.cs`・`ServerReasonsTests.cs`・`GachaRateCalculatorTests.cs`。

手順:
1. 3.1・3.2 のとおり作る
2. `FakeGameMaster : IGameMaster`（名前空間 `Uta.Client.Infrastructure.Tests`）を作る。public フィールド: `List<IdolInfo> IdolList`、`List<SupportCardInfo> SupportCardList`、`List<GachaInfo> GachaList`、`List<ScenarioInfo> ScenarioList`（4 つとも `new List<…>()` で初期化）、`Dictionary<int, List<SupportAbilityInfo>> Abilities`、`Dictionary<int, CardTextInfo> CardTexts`（どちらも空で初期化）、`Func<Rarity, int, int> MaxLevel`、`Func<Rarity, int> MaxLimitBreak`、`Func<Rarity, int, int, SupportLevelUpCost> LevelUpCost`（3 つとも null）
   - `Idols`・`SupportCards`・`Gachas`・`Scenarios` はリストをそのまま返す（並べ替えない。テストが順に入れる）
   - `GetIdol`・`GetSupportCard`・`GetGacha`・`GetScenario` はリストから ID で探し、無ければ `KeyNotFoundException`
   - `GetSupportAbilities(id)`: `SupportCardList` に無ければ `KeyNotFoundException`。`Abilities` にあればそのリスト、無ければ空のリスト
   - `GetCardText(id)`: `CardTexts` に無ければ `KeyNotFoundException`
   - `GetSupportMaxLevel`・`GetSupportMaxLimitBreak`・`GetSupportLevelUpCost` は対応する `Func` を呼ぶ。null なら `NotImplementedException`
3. 5章の `PortResultTests`・`ServerReasonsTests`・`GachaRateCalculatorTests` を作る

境界: 重み 0 のエントリーも 0 で出す。`entries` が空・合計 0 なら空。マスタに無い ID は `KeyNotFoundException`。

確かめ方: コンパイルエラー 0。上の 3 つのテストが通る。

### タスク 2: ユースケース・API・ゲートウェイ

触る: `Application/Gacha/GetGachaStatusUseCase.cs`・`DrawStatus.cs`・`DrawOutcome.cs`・`DrawUseCase.cs`、`Infrastructure/Api/GachaApi.cs`・`UtaApiClient.cs`・`UtaMagicOnionClientInitializer.cs`、`Infrastructure/Gacha/GachaGateway.cs`、`Tests/EditMode/DrawUseCaseTests.cs`・`GetGachaStatusUseCaseTests.cs`・`GachaGatewayTests.cs`。

手順: 3.2・3.3 のとおり作る・直す。5章のテストを作る。

境界: `count` が 1・10 以外は `ArgumentOutOfRangeException`。キャッシュの通貨が足りなければ通信しない。取り直しの失敗は成功として返す（`RefreshFailed = true`）。

確かめ方: コンパイルエラー 0。上の 3 つのテストが通る。`unity recompile` のあと `unity command console get --tail 50 --level error --format json` でエラーが無いこと（MagicOnion の生成物は Source Generator が作るので、ファイルとしては残らない。`IGachaService` のクライアントが生成されなければ `ServiceClientFactory.Create<IGachaService>` の解決で失敗するので、タスク 3 の Play モードで「ガチャ」を開いて確かめる）。

### タスク 3: 画面とホーム・Bootstrap

触る: `Presentation/Gacha/*`、`Presentation/Home/HomePresenter.cs`、`Bootstrap/UtaBootstrap.cs`、`Tests/EditMode/GachaPresenterTests.cs`・`GachaRatesPresenterTests.cs`・`GachaResultPresenterTests.cs`・`HomePresenterTests.cs`。

手順:
1. 3.4 のとおり Gacha の型を作る
2. 3.5 のとおり `HomePresenter`・`UtaBootstrap` を直す
3. 5章のテストを作り、`HomePresenterTests` を直す
4. Play モードで確かめる（サーバーを起動した状態。CLAUDE.md「ローカルのサーバー」）: `unity command editor.playmode start` → ホームが出たら手で「ガチャ」を押す → `unity command console get --tail 50 --level error --format json` でエラーが無いこと → 5章の手動の確認 → `unity command editor.playmode stop`。モーダル（`ModalBase`・USN の `DefaultModalBackdrop`）が出るのはこの計画が最初なので、排出率と結果のモーダルが出て閉じられることを必ず見る

境界: 結果のモーダルの「もう一度」は、モーダルを閉じてから確認ダイアログを出す。ボタンの連打は実行中フラグで止まる。

確かめ方: コンパイルエラー 0。上の 4 つのテストが通る。Play モードの確認（5章）。

### タスク 4: 結果の記録

1. [screen-design.md](../wiki/architecture/screen-design.md):
   - 2章の表の「ガチャ（結果）」の行の「ページ／モーダル」を「モーダル」にする
   - 7.3 の「所持データのキャッシュ」の項の最後に「RPC が成功したあとの取り直しが失敗しても、RPC の結果は捨てずに出す（ガチャの結果のモーダルを出し、通貨は `DrawResponse` の値で出す）。キャッシュは次に取り直すまで古いまま」を足す（foundation のタスク 6 が同じ意味の文を足していれば足さない）
   - 「実装メモ」に、1章の「叩き台との差」の 4 つと、実装で設計から変えたこと（なければ「順 7 の 2 本目: 叩き台との差のほかは、なし」）を書く
2. [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md):
   - 2章の表に、Editor の Play モードで確かめたものを 1 行足す（`IGachaService` の `GetGachasAsync`・`DrawAsync` を `ApiCore` 経由で呼び、`DrawResultDto[]` を受け取れた。USN のモーダル（`ModalBase` + `DefaultModalBackdrop`）を積んで閉じられた。確かめた日付）。確かめられなかったものは 3.3 に書く
   - 3.3 に「Windows の IL2CPP のプレイヤーでの `IGachaService`（`DrawResponse`・`DrawResultDto[]`）の送受信は未確認（順 7 の 6 本目の通しのビルドで確かめる）」を足す。foundation が 3.3 に「モーダルは 2 本目で確かめる」と書いていれば、確かめた結果に直す
3. `python -I .claude/skills/linting-wiki/lint_wiki.py` を回し、エラーが 0 であること
4. 全部のタスクが終わったら、この計画の `status` を `done` にする

## 5. テスト

置き場: `Tests/EditMode/`。名前空間は `Uta.Client.Infrastructure.Tests`。書き方は foundation 3.3.7（偽の View はテストのファイルの中の `private sealed class`、`CallLog` を `FakeScreenNavigator`・`FakeDialogService`・偽の View で共有し、`log.Entries` の並びで確かめる。非同期は `[UnityTest]` + `UniTask.ToCoroutine` で Presenter の公開の `UniTask` メソッドを `await` する）。

テストのファイルの中に書く偽（どれも `private sealed class`。返す `UniTask` は、指定が無ければ完了済み）:
- `FakeGachaPort : IGachaPort`: public フィールド `PortResult<IReadOnlyList<GachaStatus>> GachasResult`（既定は `Success` の空のリスト）、`PortResult<DrawCallResult> DrawResult`、`UniTaskCompletionSource DrawGate`（null でなければ `DrawAsync` はこれを待ってから返す）、`List<string> DrawCalls`（`DrawAsync` が呼ばれるたびに `$"{gachaId},{count},{expectedCost}"` を足す）。`CallLog` を受けたときは `DrawAsync` で `"Draw:" + count` も記録する
- `FakePlayerPort : IPlayerPort`（`EnterPlayerUseCaseTests` と同じ作りを、使うファイルごとに書く）: `SignInResult`（`PlayerSignInResult`）を返す。`SignInCount` を数える
- 偽の View: 記録の書式は foundation 3.3.7（`"<メソッド名>:<引数を , でつないだもの>"`）。`int?` の null は `null`、リストは要素を `/` でつなぐ（`SetLineup`・`IGachaResultView.SetLines`）。`IGachaRatesView.SetLines` は `"SetLines:" + 行数` を記録し、渡されたリストを public フィールド `LastLines` に入れる

マスタ（`FakeGameMaster`）の共通の前提（ガチャのテストで使う）: 育成アイドル 1「アイドルA」SSR、3「アイドルC」R、サポートカード 2「サポートB」SSR、4「サポートD」SR。ガチャ 1: `SingleCost = 250`、`TenCost = 2500`、`CeilingPoints = 200`、通常枠 = (Idol 1, 重み 1)・(SupportCard 2, 1)・(Idol 3, 8)、確定枠 = (SupportCard 4, 3)・(Idol 1, 1)。`PlayerCache` には `GachaCurrency = 3250` の `PlayerSnapshot` を入れる。

| ファイル | テスト | 内容 |
|---|---|---|
| `PortResultTests.cs` | `Success_HoldsValue` | `Succeeded`、`Value`、`Reason == null`、`IsBusinessError == false` |
| | `BusinessError_SetsReason` | `!Succeeded`、`Reason == "R"`、`IsBusinessError`、`ErrorText == "t"` |
| | `BusinessError_NullOrEmptyReason_Throws` | null と "" のどちらも `ArgumentException` |
| | `Failure_IsNotBusinessError` | `!Succeeded`、`Reason == null`、`!IsBusinessError`。`Failure(null)` の `ErrorText == ""` |
| `ServerReasonsTests.cs` | `Reasons_MatchContractsErrorCodes` | `ServerReasons.GachaNotEnoughCurrency == GachaErrors.NotEnoughCurrency.Reason`（後続の計画は、足した定数の行をここに足す） |
| `GachaRateCalculatorTests.cs` | `Calculate_RowsToBasisPoints` | 共通の通常枠 → `Rows` が 1000・1000・8000 |
| | `Calculate_GroupsSumWeightsBeforeTruncating` | `IdolList` に 5「アイドルE」SSR を足し、エントリー (Idol 1, 1)・(Idol 5, 1)・(Idol 3, 5)（合計 7）→ SSR 育成アイドルの `Groups` が 2857（行ごとの 1428 + 1428 = 2856 ではない） |
| | `Calculate_SortedByRarityDescThenKindThenId` | 共通の通常枠と確定枠 → `Groups` が SSR 育成アイドル・SSR サポートカード・R 育成アイドル の順、確定枠の `Rows` が Idol 1 → SupportCard 4 の順 |
| | `Calculate_ZeroWeightRow_Included` | 重み 0 の行が `BasisPoints == 0` で出る |
| | `Calculate_EmptyOrZeroTotal_ReturnsEmpty` | 空のリストと、重みが全部 0 → `Groups`・`Rows` とも空 |
| | `Calculate_UnknownId_Throws` | `KeyNotFoundException` |
| | `FormatPercent_Formats` | 0 → "0.00%"、5 → "0.05%"、1234 → "12.34%"、10000 → "100.00%" |
| `DrawUseCaseTests.cs` | `Execute_Success_RefreshesPlayerAndReturnsResult` | `DrawResult` が成功、`FakePlayerPort` が新しいスナップショットの成功 → `Succeeded`、`RefreshFailed == false`、`PlayerCache.Current` が差し替わる |
| | `Execute_Success_RefreshFails_ReturnsSuccessWithRefreshFailed` | `FakePlayerPort` が `PlayerSignInResult.Failure("x")` → `Succeeded`、`RefreshFailed == true`、`Result` は応答のまま、`PlayerCache.Current` は元のまま |
| | `Execute_NotEnoughCurrencyInCache_DoesNotCallPort` | 通貨 100 → `NotEnoughCurrency`、`DrawCalls` が空 |
| | `Execute_BusinessErrorNotEnoughCurrency_ReturnsNotEnoughCurrency` | `BusinessError(ServerReasons.GachaNotEnoughCurrency, "e")` → `NotEnoughCurrency`、`SignInCount == 0` |
| | `Execute_OtherBusinessError_ReturnsFailed` | `BusinessError("OTHER", "e")` → `Failed` |
| | `Execute_Failure_ReturnsFailed` | `Failure("e")` → `Failed`、`SignInCount == 0` |
| | `Execute_InvalidCount_Throws` | 2 → `ArgumentOutOfRangeException` |
| | `Execute_PassesExpectedCost` | 単発 → `DrawCalls[0] == "1,1,250"`、10 連 → `"1,10,2500"` |
| `GetGachaStatusUseCaseTests.cs` | `Execute_Found_ReturnsPoints` | 一覧に (1, 37) → 37 |
| | `Execute_NotInList_ReturnsZero` | 一覧に (2, 37) だけ → 0 |
| | `Execute_Failure_ReturnsNull` | `Failure("e")` → null |
| `GachaGatewayTests.cs` | `ToDrawCallResult_MapsOutcomeAndSortsBySlot` | Slot 2（SupportCard、Duplicate、SupportPoints 10）・Slot 1（Idol、New）の順の配列 → `Items` が Slot 1・2 の順、`IsNew` が true・false、`Kind`・`SupportPoints`・`GachaCurrency`・`ExchangePoints` が写る |
| | `ToDrawCallResult_NullResults_ReturnsEmpty` | `Items.Count == 0` |
| | `ToGachaStatuses_MapsAndHandlesNull` | 2 行が同じ順で写る。`Gachas == null` は空 |
| `GachaPresenterTests.cs` | `InitializeAsync_ShowsCurrencyCostsLineupAndExchange` | `GachasResult` に (1, 37) → `log` が `"SetCurrency:3250"`, `"SetCosts:250,2500"`, `"SetLineup:SSR 育成アイドル: アイドルA/SSR サポートカード: サポートB"`, `"SetExchange:37,200"` |
| | `InitializeAsync_StatusFails_ShowsNullExchange` | `GachasResult` が `Failure` → 最後が `"SetExchange:null,200"` |
| | `DrawAsync_Confirmed_PushesResultModal` | `DrawResult` が成功（通貨 750、交換pt 47、2 件）、`FakePlayerPort` は成功 → `InitializeAsync` のあとの `log` が `"Confirm:ガチャ通貨 2,500 を使って 10 回引きます。（所持 3,250）"`, `"Draw:10"`, `"SetCurrency:750"`, `"SetExchange:47,200"`, `"PushModal:GachaResultPresenter"`。`navigator.Args[0]` が `GachaResultArgs` で `Items.Count == 2`、`Retry != null` |
| | `DrawAsync_RefreshFailed_StillPushesResultModal` | `FakePlayerPort` が失敗 → 同じく `"SetCurrency:750"`・`"PushModal:GachaResultPresenter"` があり、`"Info:"` で始まる行が無い |
| | `DrawAsync_Declined_DoesNotDraw` | `ConfirmResults` に `Negative` → `"Draw:"` も `"PushModal:"` も無い |
| | `DrawAsync_NotEnoughCurrency_ShowsInfo` | `DrawResult` が `BusinessError(ServerReasons.GachaNotEnoughCurrency, "e")` → 最後が `"Info:ガチャ通貨が足りません。"`、`"PushModal:"` が無い |
| | `DrawAsync_Failed_ShowsNothing` | `DrawResult` が `Failure("e")` → `"Confirm:…"`, `"Draw:10"` のあとに何も無い |
| | `DrawAsync_WhileRunning_Ignored` | `DrawGate` に未完了の源 → `var first = presenter.DrawAsync(1);` → `await presenter.DrawAsync(1);`（すぐ返る）→ 源を完了 → `await first` → `"Draw:1"` が 1 回 |
| | `ResultRetry_DrawsAgain` | `DrawAsync_Confirmed` と同じあと、`await ((GachaResultArgs)navigator.Args[0]).Retry();` → `"Confirm:"` で始まる行が 2 回、`"PushModal:GachaResultPresenter"` が 2 回 |
| | `OpenRatesAsync_PushesRatesModal` | `"PushModal:GachaRatesPresenter"`、`navigator.Args[0]` が `GachaRatesArgs` で `GachaId == 1` |
| | `BackAsync_PopsPage` | `"PopPage:GachaPresenter"` |
| | `Dispose_UnsubscribesEvents` | `Dispose()` のあと偽の View の `RaiseBack()` → `"PopPage:"` が無い |
| `GachaRatesPresenterTests.cs` | `InitializeAsync_BuildsGroupsThenRows` | `LastLines` の `Text` と `IsHeading` が順に: 見出し "通常枠（単発・10連の1〜9枠目）"、"SSR 育成アイドル 10.00%"、"SSR サポートカード 10.00%"、"R 育成アイドル 80.00%"、見出し "通常枠の内訳"、"SSR 育成アイドル アイドルA 10.00%"、"SSR サポートカード サポートB 10.00%"、"R 育成アイドル アイドルC 80.00%"、見出し "確定枠（10連の10枠目）"、"SSR 育成アイドル 25.00%"、"SR サポートカード 75.00%"、見出し "確定枠の内訳"、"SSR 育成アイドル アイドルA 25.00%"、"SR サポートカード サポートD 75.00%" |
| | `CloseAsync_PopsModal` | `"PopModal:GachaRatesPresenter"` |
| `GachaResultPresenterTests.cs` | `InitializeAsync_BuildsLines` | Items: (1, Idol 1, New)・(2, Idol 3, Duplicate, Shards 5)・(3, SupportCard 2, New)・(4, SupportCard 4, Duplicate, SupportPoints 1000) → `"SetLines:1. SSR 育成アイドル アイドルA　NEW/2. R 育成アイドル アイドルC　かけら +5/3. SSR サポートカード サポートB　所持に追加/4. SR サポートカード サポートD　サポート強化pt +1,000"` |
| | `RetryAsync_PopsModalThenCallsRetry` | `Retry` は `() => { log.Add("Retry"); return UniTask.CompletedTask; }` → `"PopModal:GachaResultPresenter"`, `"Retry"` の順 |
| | `RetryAsync_NullRetry_OnlyPops` | `"PopModal:GachaResultPresenter"` だけ |
| | `CloseAsync_PopsModal` | `"PopModal:GachaResultPresenter"`、`"Retry"` が無い |
| `HomePresenterTests.cs`（変更） | コンストラクター | 全部のテストで `new HomePresenter(view, cache, dialogs, navigator)`（`navigator` は同じ `CallLog` の `FakeScreenNavigator`） |
| | `Clicks_ShowNotReady` を直す | 4 つのイベントを順に起こす → `"Info:準備中です。"` が 3 回と `"PushPage:GachaPresenter"` が 1 回 |
| | `OpenGachaAsync_WhileRunning_Ignored` を `OpenProduceAsync_WhileRunning_Ignored` に直す | 中身は同じで、呼ぶメソッドを `OpenProduceAsync` にする（`OpenGachaAsync` はダイアログを出さなくなったため） |
| | `OpenGachaAsync_PushesGachaPage`（足す） | `await presenter.OpenGachaAsync();` → `log` が `"PushPage:GachaPresenter"` |

Play モード・手動（Editor、サーバー起動済み・初期付与済みのプレイヤー）:
- ホーム →「ガチャ」でガチャのページ。「ガチャ通貨 N」、ラインナップ（SSR）、「交換pt N / 200」、「単発 250」「10連 2,500」（値はマスタのまま）が出る。「＜」でホームに戻る
- 「排出率」で通常枠・確定枠の、レア別・種別の合計とカードごとの率が出る。スクロールできる。「閉じる」で閉じる
- 「単発」→ 確認 →「引く」→ 結果のモーダル（1 行）。通貨が減る。「閉じる」→ ホームに戻ると減った通貨が出ている
- 「10連」→ 結果 10 行。初めての育成アイドルに「NEW」、重複に「かけら +N」が出る。「もう一度」でモーダルが閉じ、確認ダイアログが再び出る
- 通貨が足りないとき（`server/` の DB でプレイヤーのガチャ通貨を減らしてから起動し直す。手段が無ければ確かめていないと報告する）「ガチャ通貨が足りません。」
- ガチャのページでサーバーを止めて「単発」→ 共通のエラーダイアログ（結果のモーダルは出ない）
- Windows の IL2CPP のビルドでの確認（`DrawResponse`・`DrawResultDto[]` の送受信）は、この計画では行わない。6 本目の通しのビルドで確かめる。確かめるまでは未確認と書く（タスク 4）

## 6. 未決事項

なし
