---
title: 実装計画: client/ の画面 2/6 ガチャ
status: draft
updated: 2026-11-03
spec: ../wiki/systems/gacha.md
---

# 実装計画: client/ の画面 2/6 ガチャ

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7 の 2 本目。ガチャの画面（ガチャ・排出率の表示・結果）と、`IGachaService` を呼ぶクライアントの層（ゲートウェイ・ユースケース）を作る。画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 9、仕様は [gacha.md](../wiki/systems/gacha.md)（GAC-01〜04）、RPC は [api-design.md](../wiki/architecture/api-design.md) 1章と `shared/Uta.Shared.Contracts/Gacha/`、サーバー側は [gacha-support-server.md](../wiki/architecture/server/gacha-support-server.md)。

- 方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章。型の土台は [1 本目の計画](2026-11-03-client-screens-foundation.md)（`ScreenNavigator`・`PageBase`・`ModalBase`・`Presenter<TView>`・`DialogService`・`IGameMaster`・`RefreshPlayerUseCase`・`HomeDestinations`）
- **前提**: 1 本目が done（ここで足す型はそれを使う）
- **決めないこと**: 交換（GAC-03。MVP の画面に入れない: [game-spec.md](../wiki/overview/game-spec.md)「画面」）、抽選履歴、ガチャの演出（MVP は演出なし: screen-wireframes.md 9）、複数のガチャの切り替え（MVP はガチャ 1 本）。交換pt は「交換pt N / M」の表示だけ出し、「交換」ボタンは作らない

## 2. 触るファイル

パスは `client/uta/Assets/` からの相対。

| 操作 | パス | 中身 |
|---|---|---|
| 作成 | `Scripts/Application/Common/PortResult.cs` | ポートの結果の型（以降の計画でも使う） |
| 作成 | `Scripts/Application/Common/ServerReasons.cs` | サーバーの業務エラーの reason の文字列定数 |
| 作成 | `Scripts/Application/Gacha/IGachaPort.cs` | ガチャの通信ポート |
| 作成 | `Scripts/Application/Gacha/GachaStatus.cs`・`DrawItem.cs`・`DrawCallResult.cs` | ポートが返す型 |
| 作成 | `Scripts/Application/Gacha/GetGachaStatusUseCase.cs`・`DrawUseCase.cs`・`DrawOutcome.cs` | ユースケース |
| 作成 | `Scripts/Application/Gacha/GachaRateCalculator.cs`・`GachaRateRow.cs` | 排出率の計算 |
| 作成 | `Scripts/Infrastructure/Api/GachaApi.cs` | `ApiServiceBase<IGachaService>` |
| 変更 | `Scripts/Infrastructure/Api/UtaApiClient.cs` | `Gacha`（`GachaApi`）を足す |
| 変更 | `Scripts/Infrastructure/Api/UtaMagicOnionClientInitializer.cs` | `typeof(IGachaService)` を足す |
| 作成 | `Scripts/Infrastructure/Gacha/GachaGateway.cs` | `IGachaPort` の実装 |
| 作成 | `Scripts/Presentation/Gacha/IGachaView.cs`・`GachaPage.cs`・`GachaPresenter.cs` | ガチャの画面 |
| 作成 | `Scripts/Presentation/Gacha/IGachaRatesView.cs`・`GachaRatesModal.cs`・`GachaRatesPresenter.cs` | 排出率のモーダル |
| 作成 | `Scripts/Presentation/Gacha/IGachaResultView.cs`・`GachaResultModal.cs`・`GachaResultPresenter.cs`・`GachaResultRow.cs` | 結果のモーダル |
| 変更 | `Scripts/Presentation/Home/HomeDestinations.cs` | `OpenGachaAsync` を実装し、コンストラクターに `ScreenNavigator` を足す |
| 変更 | `Scripts/Bootstrap/UtaBootstrap.cs` | `GachaGateway`・ユースケース・Presenter の登録 |
| 作成 | `Tests/EditMode/Support/FakeGameMaster.cs`・`FakeDialogService.cs`・`FakeScreenNavigator.cs` | `IGameMaster`・`DialogService`・`ScreenNavigator` のテスト用の実装（以降の計画でも使う） |
| 作成 | `Tests/EditMode/*.cs`（5章） | テスト |
| 変更 | `Tests/EditMode/HomePresenterTests.cs` | `HomeDestinations` の派生のコンストラクター呼び出しを直す |

## 3. 型とシグネチャ

C# は C# 9 の範囲（`.editorconfig`）。`Uta.Client.Application.*` の名前空間は置き場のフォルダに合わせる。

### 3.1 `Uta.Client.Application.Common`

```csharp
public sealed class PortResult<T>
{
    public bool Succeeded { get; }
    public T Value { get; }             // 成功のときだけ。失敗は default
    public string Reason { get; }       // 業務エラーの reason（ServerReasons の定数と同じ文字列）。業務エラーでなければ null
    public string ErrorText { get; }    // 失敗の説明（ログ用）。成功は null
    public bool IsBusinessError { get; }   // !Succeeded && Reason != null
    public static PortResult<T> Success(T value);
    public static PortResult<T> BusinessError(string reason, string errorText);   // reason が null・空なら ArgumentException
    public static PortResult<T> Failure(string errorText);                        // ApiCore が共通のダイアログを出し終えた失敗（通信の失敗など）
}

public static class ServerReasons
{
    public const string GachaNotEnoughCurrency = "GACHA_NOT_ENOUGH_CURRENCY";
    public const string GachaNotEnoughPoints = "GACHA_NOT_ENOUGH_POINTS";
    public const string GachaNotFound = "GACHA_NOT_FOUND";
    public const string GachaInvalidCount = "GACHA_INVALID_COUNT";
    public const string GachaCostMismatch = "GACHA_COST_MISMATCH";
    public const string SupportInvalidTargetLevel = "SUPPORT_INVALID_TARGET_LEVEL";
    public const string SupportNotEnoughPoints = "SUPPORT_NOT_ENOUGH_POINTS";
    public const string SupportMaxLimitBreak = "SUPPORT_MAX_LIMIT_BREAK";
    public const string SupportNotOwned = "SUPPORT_NOT_OWNED";
    public const string SupportInvalidMaterial = "SUPPORT_INVALID_MATERIAL";
    public const string PlayerNotEnoughGold = "PLAYER_NOT_ENOUGH_GOLD";
    public const string ProduceStaleOperation = "PRODUCE_STALE_OPERATION";
    public const string ProduceUnexpectedPhase = "PRODUCE_UNEXPECTED_PHASE";
    public const string ProduceInvalidArgument = "PRODUCE_INVALID_ARGUMENT";
    public const string ProduceInvalidFormation = "PRODUCE_INVALID_FORMATION";
    public const string ProduceNotInProgress = "PRODUCE_NOT_IN_PROGRESS";
    public const string ProduceSessionDiscarded = "PRODUCE_SESSION_DISCARDED";
    public const string ProduceAlreadyInProgress = "PRODUCE_ALREADY_IN_PROGRESS";
}
```

（値は `shared/Uta.Shared.Contracts/*/*Errors.cs` の `ErrorCode(domain "uta", reason)` の reason と同じ。`ServerReasons` の値が `Contracts` と食い違わないことはテストで確かめる: 5 章 `ServerReasonsTests`。）

### 3.2 `Uta.Client.Application.Gacha`

```csharp
public sealed class GachaStatus
{
    public GachaStatus(int gachaId, int exchangePoints);
    public int GachaId { get; } public int ExchangePoints { get; }
}
public sealed class DrawItem
{
    public DrawItem(int slot, MasterItemKind kind, int itemId, bool isNew, int shards, int supportPoints);
    public int Slot { get; } public MasterItemKind Kind { get; } public int ItemId { get; }
    public bool IsNew { get; }          // GachaOutcome.New
    public int Shards { get; }          // 重複した育成アイドルのかけら（それ以外は 0）
    public int SupportPoints { get; }   // 重複したサポートカードの強化pt（それ以外は 0）
}
public sealed class DrawCallResult
{
    public DrawCallResult(IReadOnlyList<DrawItem> items, int gachaCurrency, int exchangePoints);
    public IReadOnlyList<DrawItem> Items { get; } public int GachaCurrency { get; } public int ExchangePoints { get; }
}

public interface IGachaPort
{
    UniTask<PortResult<IReadOnlyList<GachaStatus>>> GetGachasAsync();
    UniTask<PortResult<DrawCallResult>> DrawAsync(int gachaId, int count, int expectedCost);
}

public sealed class GetGachaStatusUseCase
{
    public GetGachaStatusUseCase(IGachaPort port);
    /// <summary>gachaId の交換pt を返す。サーバーの一覧に無ければ 0。通信の失敗は InvalidOperationException。</summary>
    public async UniTask<int> ExecuteAsync(int gachaId);
}

public enum DrawStatus { Succeeded, NotEnoughCurrency, CostMismatch, Failed }
public sealed class DrawOutcome
{
    public DrawStatus Status { get; }
    public DrawCallResult Result { get; }   // Succeeded のときだけ
    public static DrawOutcome Success(DrawCallResult result);
    public static DrawOutcome Rejected(DrawStatus status);   // Succeeded 以外
}

public sealed class DrawUseCase
{
    public DrawUseCase(IGachaPort port, RefreshPlayerUseCase refresh, PlayerCache cache);
    /// <summary>count は 1 か 10（それ以外は ArgumentOutOfRangeException）。費用は count == 1 ? gacha.SingleCost : gacha.TenCost。</summary>
    public async UniTask<DrawOutcome> ExecuteAsync(GachaInfo gacha, int count);
}

public sealed class GachaRateRow
{
    public GachaRateRow(Rarity rarity, MasterItemKind kind, int itemId, string name, int basisPoints);
    public Rarity Rarity { get; } public MasterItemKind Kind { get; } public int ItemId { get; } public string Name { get; }
    public int BasisPoints { get; }   // 0.01% 単位（10000 = 100%）
}
public static class GachaRateCalculator
{
    /// <summary>entries の重みの合計を分母に、1 行ずつ (long)weight * 10000 / total（切り捨て）。レア度の降順（SSR → N）、同じレアは種別（育成アイドル → サポートカード）、ID の昇順で並べる。entries が空・重みの合計が 0 なら空のリスト。マスタに無い ID は KeyNotFoundException。</summary>
    public static IReadOnlyList<GachaRateRow> Calculate(IReadOnlyList<GachaEntryInfo> entries, IGameMaster master);
    /// <summary>"12.34%" の形（basisPoints / 100 + "." + (basisPoints % 100) を 2 桁）。</summary>
    public static string FormatPercent(int basisPoints);
}
```

`DrawUseCase.ExecuteAsync` の手順:
1. `count` が 1・10 以外なら `ArgumentOutOfRangeException`
2. `cost` を決める。`cache.Current.GachaCurrency < cost` なら通信せず `DrawOutcome.Rejected(DrawStatus.NotEnoughCurrency)`
3. `port.DrawAsync(gacha.GachaId, count, cost)`
4. 失敗のとき: `IsBusinessError` で `Reason` が `GachaNotEnoughCurrency` → `NotEnoughCurrency`、`GachaCostMismatch` → `CostMismatch`、それ以外（`GachaNotFound`・`GachaInvalidCount` を含む）→ `Failed`。業務エラーでない失敗も `Failed`
5. 成功のとき: `await refresh.ExecuteAsync();`（サインインで所持品を取り直す。失敗は例外のまま上へ）→ `DrawOutcome.Success(result)`

### 3.3 `Uta.Client.Infrastructure`

```csharp
// Api/GachaApi.cs
public sealed class GachaApi : ApiServiceBase<IGachaService>
{
    public GachaApi(ApiCore core, IGachaService client) : base(core, client) { }
    public UniTask<ApiResult<GetGachasResponse>> GetGachasAsync(IEnumerable<ErrorCode> handledCodes = null)
        => InvokeAsync(nameof(IGachaService.GetGachasAsync), 0, static (c, _) => c.GetGachasAsync(), handledCodes);
    public UniTask<ApiResult<DrawResponse>> DrawAsync(DrawRequest request, IEnumerable<ErrorCode> handledCodes = null)
        => InvokeAsync(nameof(IGachaService.DrawAsync), request, static (c, r) => c.DrawAsync(r), handledCodes);
}

// Gacha/GachaGateway.cs
public sealed class GachaGateway : IGachaPort
{
    public GachaGateway(UtaApiClient api);
    public async UniTask<PortResult<IReadOnlyList<GachaStatus>>> GetGachasAsync();
    public async UniTask<PortResult<DrawCallResult>> DrawAsync(int gachaId, int count, int expectedCost);
    public static DrawCallResult ToDrawCallResult(DrawResponse response);   // テスト用に public static
}
```

- `UtaApiClient`: コンストラクターとプロパティに `public GachaApi Gacha { get; }` を足し、`Create` で `ServiceClientFactory.Create<IGachaService>(callInvoker)` を作って `new GachaApi(core, gachaClient)` を渡す（`PlayerApi` と同じ並び）
- `UtaMagicOnionClientInitializer`: `[MagicOnionClientGeneration(typeof(IDeviceService), typeof(IPlayerService), typeof(IGachaService))]`（`using Uta.Shared.Contracts.Gacha;`）。生成は Unity の再コンパイルで行われる
- `GachaGateway.DrawAsync`: `handledCodes` に `GachaErrors.NotEnoughCurrency`・`GachaErrors.CostMismatch` を渡す（その 2 つはダイアログなしで失敗として返り、`BusinessError` にする。ほかのコードはダイアログが出て `Failure`）。`result.IsSuccess` なら `PortResult.Success(ToDrawCallResult(result.Value))`。失敗で `result.Error.Code.Domain == "uta"` かつ `Reason` が上の 2 つなら `PortResult.BusinessError(Reason, result.Error.ToString())`、それ以外は `PortResult.Failure(result.Error.ToString())`
- `GachaGateway.GetGachasAsync`: `handledCodes` なし。成功は `GachaStatus` の配列（`response.Gachas` が `null` のときは空）、失敗は `Failure`
- `ToDrawCallResult`: `DrawResultDto` ごとに `new DrawItem(dto.Slot, (MasterItemKind)(int)dto.ItemKind, dto.ItemId, dto.Outcome == GachaOutcome.New, dto.Shards, dto.SupportPoints)`。`response.Results` が `null` なら空。順序は `Slot` の昇順に並べ直す

### 3.4 `Uta.Client.Presentation.Gacha`

```csharp
public sealed class GachaResultRow
{
    public GachaResultRow(string label, string rarityText, bool isNew, string note);
    public string Label { get; } public string RarityText { get; } public bool IsNew { get; } public string Note { get; }
}

public interface IGachaView
{
    void SetCurrency(int gachaCurrency);
    void SetExchange(int points, int ceilingPoints);   // "交換pt {points} / {ceilingPoints}"（交換のボタンは無い）
    void SetCosts(int singleCost, int tenCost);        // ボタンの文字 "単発 {singleCost}" "10連 {tenCost}"
    void SetLineup(IReadOnlyList<string> lines);       // 目玉の表示（SSR の一覧）
    event Action SingleClicked;
    event Action TenClicked;
    event Action RatesClicked;
    event Action BackClicked;
}
public sealed class GachaPage : PageBase, IGachaView { ... }

public sealed class GachaPresenter : Presenter<IGachaView>
{
    public GachaPresenter(IGameMaster master, PlayerCache cache, GetGachaStatusUseCase status, DrawUseCase draw, DialogService dialogs, ScreenNavigator navigator);
}

public interface IGachaRatesView
{
    void SetLines(IReadOnlyList<string> lines);
    event Action CloseClicked;
}
public sealed class GachaRatesModal : ModalBase, IGachaRatesView { ... }
public sealed class GachaRatesPresenter : Presenter<IGachaRatesView>
{
    public GachaRatesPresenter(IGameMaster master, ScreenNavigator navigator);
    public int GachaId { get; set; }   // configure で設定する
}

public interface IGachaResultView
{
    void SetRows(IReadOnlyList<GachaResultRow> rows);
    event Action CloseClicked;
    event Action RetryClicked;
}
public sealed class GachaResultModal : ModalBase, IGachaResultView { ... }
public sealed class GachaResultPresenter : Presenter<IGachaResultView>
{
    public GachaResultPresenter(IGameMaster master, ScreenNavigator navigator);
    public IReadOnlyList<DrawItem> Items { get; set; }      // configure で設定する
    public Func<UniTask> Retry { get; set; }                // 「もう一度」で、モーダルを閉じたあとに呼ぶ
}
```

**`GachaPresenter` の動き**
- `OnBind`: `var gacha = master.Gachas[0]`（`Gachas` が空なら `dialogs.ShowInfoAsync("ガチャがありません。")` して `navigator.PopPageAsync()`）。`SetCurrency(cache.Current.GachaCurrency)`、`SetCosts(gacha.SingleCost, gacha.TenCost)`、`SetLineup`: `gacha.NormalEntries` のうち `Rarity.SSR` のものを `"SSR 育成アイドル: <名前>"` / `"SSR サポートカード: <名前>"`（`MasterItemKind.Idol` は `master.GetIdol(id)`、`SupportCard` は `master.GetSupportCard(id)`）にして ID 順。`SetExchange(0, gacha.CeilingPoints)` を出してから、`status.ExecuteAsync(gacha.GachaId)` の結果で `SetExchange(points, gacha.CeilingPoints)` を更新（例外のときは 0 のまま、ダイアログは出さない）
- 単発・10 連: 確認（`dialogs.ConfirmAsync($"ガチャ通貨 {cost} を使って {count} 回引きます。（残り {cache.Current.GachaCurrency}）")`）→ `Positive` なら `draw.ExecuteAsync(gacha, count)`
  - `Succeeded`: `SetCurrency(cache.Current.GachaCurrency)`、`SetExchange(outcome.Result.ExchangePoints, gacha.CeilingPoints)`、`navigator.PushModalAsync<GachaResultModal, GachaResultPresenter>(p => { p.Items = outcome.Result.Items; p.Retry = () => DrawAsync(gacha, count); })`
  - `NotEnoughCurrency`: `dialogs.ShowInfoAsync("ガチャ通貨が足りません。")`
  - `CostMismatch`: `dialogs.ShowInfoAsync("ガチャの内容が変わりました。アプリを起動し直してください。")`
  - `Failed`: 何も出さない（共通のダイアログが出ている）
  - 例外: `dialogs.ShowInfoAsync("通信に失敗しました: " + e.Message)`
  - 連打を避けるため、実行中は次の要求を無視する（フラグ `drawing`）
- 排出率: `navigator.PushModalAsync<GachaRatesModal, GachaRatesPresenter>(p => p.GachaId = gacha.GachaId)`
- 戻る: `navigator.PopPageAsync()`

**`GachaRatesPresenter.OnBind`**: `var gacha = master.GetGacha(GachaId)`。`lines` を作る: 見出し `"通常枠"`、`GachaRateCalculator.Calculate(gacha.NormalEntries, master)` の各行を `"{rarity} {種別} {name} {FormatPercent}"`（種別は `Idol` → "育成アイドル"、`SupportCard` → "サポートカード"）、見出し `"確定枠（10連の最後の1枠）"`、`GuaranteedEntries` の各行を同じ形式。`SetLines(lines)`。`CloseClicked` で `navigator.PopModalAsync()`。

**`GachaResultPresenter.OnBind`**: `Items` の各 `DrawItem` を `GachaResultRow` にする: `label` = 名前（`master.GetIdol`/`GetSupportCard`）、`rarityText` = `Rarity` の文字列、`isNew` = `item.IsNew`、`note` = 重複のとき: 育成アイドルは `"かけら +{Shards}"`、サポートカードは `"サポート強化pt +{SupportPoints}"`、新規は `""`。`CloseClicked` → `navigator.PopModalAsync()`。`RetryClicked` → `navigator.PopModalAsync()` のあと `Retry` を呼ぶ（`Retry` が `null` なら閉じるだけ）。

**画面のレイアウト**（`BuildUi`。見た目は仮）
- `GachaPage`: `CreateVertical` の下に、ヘッダー行（`CreateHorizontal`: 「＜」ボタン（BackClicked）と通貨の Text）、「ガチャ」のタイトル Text、ラインナップの Text（`SetLineup` の行を改行でつなぐ）、交換pt の Text、「排出率」ボタン、下に「単発 N」「10連 M」ボタンを横に 2 つ
- `GachaRatesModal`: `Panel` に `CreateScrollContent`（高さは `LayoutElement.preferredHeight = 1200`）、行ごとに `CreateText(UiTheme.FontSmall)`、下に「閉じる」
- `GachaResultModal`: 行ごとに `"{NEW} {rarityText} {label}  {note}"` の Text（`isNew` のとき先頭に `"NEW "`）を `CreateScrollContent` に並べる（10 連でも 1 画面に収まる大きさ。`preferredHeight = 1200`）、下に「もう一度」「閉じる」を横に 2 つ

`HomeDestinations` は `public HomeDestinations(DialogService dialogs, ScreenNavigator navigator)` にし、`OpenGachaAsync` を `await navigator.PushPageAsync<GachaPage, GachaPresenter>();` にする（戻り値は捨てる）。

## 4. タスク

各タスクの終わりに `unity recompile --format json`（`client/uta`）でコンパイルエラー 0、5 章のそのタスク分のテストが通る状態にする。Editor を開けないときは、通ったと書かず、確かめていないと報告する。

### タスク 1: Application の型と排出率

触る: `Application/Common/*`、`Application/Gacha/*`（`DrawUseCase`・`GetGachaStatusUseCase` 以外）、`Tests/EditMode/Support/FakeGameMaster.cs`。
手順: 3.1・3.2 のとおり作る。`FakeGameMaster : IGameMaster` は、`List<IdolInfo> IdolList`、`List<SupportCardInfo> SupportCardList`、`List<GachaInfo> GachaList`、`List<ScenarioInfo> ScenarioList` を public フィールドで持ち、`Idols` などはそれを返す。`GetIdol` などはリストから探し、無ければ `KeyNotFoundException`。`GetSupportMaxLevel` / `GetSupportMaxLimitBreak` / `GetSupportLevelUpCost` は、public フィールドの `Func<Rarity, int, int> MaxLevel`・`Func<Rarity, int> MaxLimitBreak`・`Func<Rarity, int, int, SupportLevelUpCost> LevelUpCost` を呼ぶ（`null` なら `NotImplementedException`）。
`FakeDialogService : DialogService`（`base(null)`）: `public DialogResult ConfirmResult = DialogResult.Positive;`、`public int ChooseResult;`、`public readonly List<string> Messages`。3 つのメソッドは `Messages` に message を足して、`UniTask.CompletedTask` / `ConfirmResult` / `ChooseResult` を返す。
`FakeScreenNavigator : ScreenNavigator`（`base(null, null)`）: `public readonly List<string> Calls`、`public Delegate LastConfigure`。`PushPageAsync<TPage, TPresenter>` は `Calls.Add("PushPage:" + typeof(TPage).Name)`、`LastConfigure = configure` として `UniTask.FromResult(default(TPresenter))`。`ReplacePageAsync` は `"ReplacePage:"`、`PushModalAsync` は `"PushModal:"`、`PopPageAsync` は `"PopPage"`、`PopToRootPageAsync` は `"PopToRoot"`、`PopModalAsync` は `"PopModal"` を足す。`public void ApplyConfigure<TPresenter>(TPresenter presenter)` は `LastConfigure` を `Action<TPresenter>` にして（null なら何もしない）呼ぶ。
境界: `GachaRateCalculator.Calculate` は重みが 0 の行も出す（率 0）。`FormatPercent(5)` は `"0.05%"`、`FormatPercent(10000)` は `"100.00%"`。
確かめ方: `GachaRateCalculatorTests`・`ServerReasonsTests`。

### タスク 2: ユースケース・API・ゲートウェイ

触る: `Application/Gacha/IGachaPort.cs`・`GetGachaStatusUseCase.cs`・`DrawUseCase.cs`・`DrawOutcome.cs`、`Infrastructure/Api/GachaApi.cs`・`UtaApiClient.cs`・`UtaMagicOnionClientInitializer.cs`、`Infrastructure/Gacha/GachaGateway.cs`。
手順: 3.2・3.3 のとおり作る。
確かめ方: `DrawUseCaseTests`・`GetGachaStatusUseCaseTests`・`GachaGatewayTests`（`ToDrawCallResult` のみ）。`unity recompile` のあと、`[MagicOnionClientGeneration]` が `IGachaService` の生成物（`UtaMagicOnionClientInitializer` の `partial` の生成コード）を作ったことを `unity command console get --level error` で確認する（エラーが無いこと）。

### タスク 3: 画面

触る: `Presentation/Gacha/*`、`Presentation/Home/HomeDestinations.cs`、`Bootstrap/UtaBootstrap.cs`、`Tests/EditMode/HomePresenterTests.cs`。
手順: 3.4 のとおり作る。`UtaBootstrap` に `Register<GachaGateway>(Singleton).As<IGachaPort>()`、`Register<GetGachaStatusUseCase>(Singleton)`、`Register<DrawUseCase>(Singleton)`、`Register<GachaPresenter>(Transient)`、`Register<GachaRatesPresenter>(Transient)`、`Register<GachaResultPresenter>(Transient)` を足す（`HomeDestinations` のコンストラクターは VContainer が `ScreenNavigator` を注入する）。`HomePresenterTests` の派生は `base(null, null)` にする。
確かめ方: `GachaPresenterTests`・`GachaRatesPresenterTests`・`GachaResultPresenterTests`。Play モード（下）。

### タスク 4: 結果の記録

`screen-design.md` の「実装メモ」にズレを書く（なければ「なし」）。確かめた項目と未確認の項目を `unity7-evaluation.md` に足す（`IGachaService` の DTO（配列の入れ子）が Windows の IL2CPP で通ったかは、3 本目以降のビルドで確かめる）。この計画を `done` にする。

## 5. テスト

置き場: `Tests/EditMode/`。

| ファイル | テスト | 内容 |
|---|---|---|
| `ServerReasonsTests.cs` | `Reasons_MatchContractsErrorCodes` | `ServerReasons.GachaNotEnoughCurrency == GachaErrors.NotEnoughCurrency.Reason` など、`GachaErrors`・`SupportErrors`・`PlayerErrors.NotEnoughGold`・`ProduceErrors` の 18 個すべてと一致（`Uta.Shared.Contracts` を参照するテスト asmdef に既にある） |
| `GachaRateCalculatorTests.cs` | `Calculate_WeightsToBasisPoints` | 重み 1・1・2 の 3 行 → 2500・2500・5000 |
| | `Calculate_SortedByRarityDescThenKindThenId` | 並び順 |
| | `Calculate_EmptyOrZeroTotal_ReturnsEmpty` | |
| | `Calculate_UnknownId_Throws` | `KeyNotFoundException` |
| | `FormatPercent_*` | 5 → "0.05%"、10000 → "100.00%"、1234 → "12.34%" |
| `DrawUseCaseTests.cs` | `Execute_Success_RefreshesPlayerAndReturnsResult` | 偽の `IGachaPort` と偽の `IPlayerPort`。成功のあと `PlayerCache.Current` が差し替わる |
| | `Execute_NotEnoughCurrencyInCache_DoesNotCallPort` | 通信しない |
| | `Execute_BusinessErrorNotEnoughCurrency_ReturnsNotEnoughCurrency` | |
| | `Execute_BusinessErrorCostMismatch_ReturnsCostMismatch` | |
| | `Execute_OtherFailure_ReturnsFailed` | |
| | `Execute_InvalidCount_Throws` | 2 → `ArgumentOutOfRangeException` |
| | `Execute_PassesExpectedCost` | 単発は `SingleCost`、10 連は `TenCost` を `expectedCost` に渡す |
| `GetGachaStatusUseCaseTests.cs` | `Execute_Found_ReturnsPoints` / `Execute_NotInList_ReturnsZero` / `Execute_Failure_Throws` | |
| `GachaGatewayTests.cs` | `ToDrawCallResult_MapsOutcomeAndSortsBySlot` | New → `IsNew`、Duplicate → `!IsNew`、スロット昇順、`Results == null` は空 |
| `GachaPresenterTests.cs` | `Bind_ShowsCostsLineupAndCurrency` | 偽の `IGachaView`。`SetCosts`・`SetLineup`（SSR だけ）・`SetCurrency` |
| | `TenClicked_Confirmed_PushesResultModal` | `DialogService` と `ScreenNavigator` の派生（1 本目で公開メソッドが `virtual`。コンストラクターに `null` を渡せる）を `Tests/EditMode/Support/` に作る（`FakeDialogService`: 確認の戻り値を設定でき、呼ばれたメッセージを記録する。`FakeScreenNavigator`: 呼ばれたメソッドと `configure` を記録し、`configure` は `new TPresenter` ではなく**記録だけ**する） |
| | `TenClicked_Declined_DoesNotDraw` | |
| | `DrawNotEnoughCurrency_ShowsInfo` | |
| `GachaRatesPresenterTests.cs` | `Bind_BuildsHeadingsAndRows` | 見出し 2 つと各行の文字列 |
| `GachaResultPresenterTests.cs` | `Bind_BuildsRowsWithNewAndDuplicateNotes` | |
| | `RetryClicked_PopsModalThenCallsRetry` | |

Play モード・手動（Editor、サーバー起動済み・初期付与済みのプレイヤー）:
- ホーム →「ガチャ」でガチャの画面。通貨・交換pt・ラインナップ・単発/10 連の費用が出る
- 「排出率」で通常枠と確定枠の率が出る。率の合計がおよそ 100%
- 「単発」→ 確認 → 結果のモーダル。通貨が減り、ホームに戻ると減った通貨が出ている
- 「10連」→ 結果 10 件。新規と重複（かけら・強化pt）が出る。「もう一度」で確認ダイアログが再び出る
- 通貨が足りないとき「ガチャ通貨が足りません。」
- サーバーを止めて引く → 共通のエラーダイアログ（結果のモーダルは出ない）

Windows の IL2CPP のビルドでの確認（`DrawResponse`・`DrawResultDto[]` の送受信）は 3〜4 本目のビルドと一緒に行ってよい。確かめるまでは未確認と書く。

## 6. 未決事項

なし
