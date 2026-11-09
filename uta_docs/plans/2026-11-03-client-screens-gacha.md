---
title: 実装計画: client/ の画面 2/6 ガチャ
status: done
updated: 2026-11-09
spec: ../wiki/systems/gacha.md, ../wiki/architecture/screen-design.md
---

# 実装計画: client/ の画面 2/6 ガチャ

## ADR 0020〜0022・naming.md への読み替え（2026-11-08。この節が本文・「基盤 11d9b06 への読み替え」の節と食い違ったら、この節が正）

[ADR 0020](../wiki/decisions/0020-masterdata-referenced-outside-shared-domain.md)・[0021](../wiki/decisions/0021-domain-entities-and-repositories.md)・[0022](../wiki/decisions/0022-client-external-concretes-in-infrastructure.md)（3 本とも accepted）と [naming.md](../wiki/architecture/naming.md) に合わせる。**画面の計画に共通の読み替えは [foundation の計画](2026-11-03-client-screens-foundation.md)の R0 が正**で、この節は gacha に固有のことだけを書く。**この節の G1〜G6 に書いた型・シグネチャ・手順・テストは完成形**で、本文の 2〜5 章のうち、この節に出てくる型・ファイル・テストの記述は読まずに、この節だけで書く。本文のうち、この節が触れていないもの（3.4 の View の `Awake` の購読の書き方、Presenter の実行中フラグ・`disposed` の決まり、遷移の `playAnimation: false`、`GachaRatesPresenter` の見出しの文と順、`GachaResultPresenter` の行の文、5 章の Play・手動の確認の項目）は本文のまま。前提: foundation（PR #52）の実装が済んでいる（`ApiClient`・`ErrorDefinitions`・`DialogOpener`・`HomeDtoFactory`・`FakePlayerUseCase`・`FakeDialogOpener`・`FakeScreenNavigator`・`CallLog`・`TestMasterData`・`PlayerRepository`）。

### G0. 決めた名前（foundation の R0 に書き戻した）

`DrawResult` を 1 つにまとめることと、引いた後のガチャ通貨を応答の値ではなく player_diff を反映したリポジトリの値で出すことは、2026-11-08 にユーザーが決めた（未 push の書きかけの節 `claude/plan-readthrough-2-52b82e` の `4115e7f` に記録がある。この節はそれを foundation の実装後の形で書き直したもの）。

| 本文 | 読み替え | 理由 |
|---|---|---|
| `IGachaPort`・`GachaGateway`・`GachaApi`・`GetGachaStatusUseCase`・`DrawUseCase`（クライアント） | Application の `IGachaUseCase`、Infrastructure の具象 `GachaUseCase`、詰め替えの `GachaMapper`（static） | R0 |
| `PortResult<T>`・`ServerReasons`（と `PortResultTests`・`ServerReasonsTests`） | **作らない**。Presenter が reason で文言を分ける操作が無い（ガチャの業務エラーで画面ごとの文言を出すのは通貨不足だけで、`DrawResultKind` に写す） | R0（ユーザーの判断 2026-11-08） |
| `GachaStatus`（class） | **作らない**。`IGachaUseCase.GetExchangePointsAsync(int gachaId)` が `int?` を返す（一覧の型が要らない。サーバーの `GachaStatus` の改名はサーバーを触る計画で決める） | naming.md 2章（`Status` は enum） |
| `DrawItem` | `GachaResult`（1 枠分。サーバーの `GachaResult` と同じ役割・同じ名前） | naming.md 1・2章 |
| `DrawCallResult` と `DrawOutcome` | 1 つにまとめて **`DrawResult`**（`Kind`（`DrawResultKind`）と、成功のときの `Results`・`GachaCurrency`・`ExchangePoints`。サーバーの `DrawResult` の 3 つのメンバーと同じ名前） | R0「値も返すなら enum と値を持つ `<操作>Result`」 |
| `DrawStatus` | `DrawResultKind`（値は `Succeeded`・`NotEnoughCurrency`・`Failed`） | naming.md 2章 |
| `DrawOutcome.RefreshFailed` | 無い（取り直しの失敗は具象が握りつぶす） | R0 |
| `GachaRateRow`（Application） | `GachaItemRate`（カード 1 枚分の率） | naming.md 4章（`Row` はマスタの行） |
| `GachaRates` | `GachaRateResult`（`Groups`・`Items`） | naming.md 2章（操作の戻り値は `Result`） |
| `GachaRateGroup` | そのまま | — |
| `GachaRateCalculator`（static） | `MemoryDatabase` を受けるインスタンス。`FormatPercent` は Presentation の `GachaTexts.Percent` へ | R0、ADR 0022（文言は Presentation） |
| `GachaRateLine` | `GachaLineRowViewDto`（排出率・結果の両方のモーダルが使う） | R0 |
| `GachaLineRow`（P2 の行の View） | `GachaLineRowView`（G4 の改名の手順） | naming.md 6章 |
| `IGameMaster`・`GachaInfo`・`GachaEntryInfo`・`IdolInfo`・`SupportCardInfo`・`MasterItemKind` | 作らない。`MemoryDatabase` の `GachaTable`・`GachaTableEntryTable`・`IdolTable`・`SupportCardTable` と `Uta.MasterData.ItemKind`・`Uta.MasterData.Rarity` | R0、ADR 0020 |
| Presenter が組み立てる文字（ラインナップ・排出率の行・結果の行・確認の文）とページが組み立てる数値の書式 | `GachaDtoFactory`・`GachaRatesDtoFactory`・`GachaResultDtoFactory`（Presentation。ルートのスコープにシングルトン）。View は文字を入れるだけ | R0、ADR 0022 |
| `FakeGameMaster`・`StubPlayerPort`・`FakeGachaPort` | `TestMasterData`・`GachaFixtures.CreateMaster()`、`FakePlayerUseCase`、`FakeGachaUseCase`（Presenter のテスト）・`FakeGachaService`（具象のテスト） | R0 |
| Contracts の `GachaOutcome` → `GachaResultKind`、`DrawResultDto.Outcome` → `ResultKind`（索引の申し送り・R0 では gacha が行う） | **この計画では行わない**。改名はサーバーのコード（`server/Uta.Server/Api/GachaService.cs`・`Uta.Server.Tests` の `GachaTests.cs`・`ContractsEnumsMatchTests.cs`）も直す必要があり、この計画の実装は `server/` に触らない（進行役の指示 2026-11-08）。naming.md「今のコードとの差」の行は残し、サーバーを触る計画が行う | — |

### G1. 名前空間と `using` の決まり

- フォルダと名前空間は本文 3 章のとおり（`Application/Gacha` → `Uta.Client.Application.Gacha`、`Infrastructure/Gacha` → `Uta.Client.Infrastructure.Gacha`、`Presentation/Gacha` → `Uta.Client.Presentation.Gacha`）。
- 名前空間 `Uta.Client.<層>.Gacha`・`…Idol`・`…SupportCard`・`…Player` があるので、その中ではマスタの型 `Gacha`・`Idol`・`SupportCard` を**型名だけで書かない**（CS0118 になる）。変数は `var` で受けるか、`Uta.MasterData.Gacha` と名前空間付きで書く（naming.md 2章）
- Contracts（`Uta.Shared.Contracts.Gacha`）とマスタ（`Uta.MasterData`）は、どちらにも `ItemKind` がある。**両方を使うファイル（`GachaMapper`・`GachaUseCase`・`GachaFixtures`・`GachaMapperTests`・`GachaUseCaseTests`）は `using Uta.MasterData;` を書かず**、マスタの型を `Uta.MasterData.ItemKind`・`Uta.MasterData.MemoryDatabase` と名前空間付きで書く。マスタだけを使うファイルは `using Uta.MasterData;` を書いてよい（`ItemKind`・`Rarity` はマスタの enum）
- **変数名**（[foundation の計画](2026-11-03-client-screens-foundation.md) 8.5。2026-11-09。ユーザーの判断。この節のシグネチャ・手順の短い名前より優先する）: `Scripts/` のフィールド・コンストラクターとメソッドの引数の名前は、型名から先頭の `I` と `ReadOnly` を除いて先頭を小文字にしたもの。この節の名前は次のように読む: `view` → `gachaView`・`gachaRatesView`・`gachaResultView`（`[Inject]` 側は `gachaPage`・`gachaRatesModal`・`gachaResultModal`）、`args` → `gachaRatesArgs`・`gachaResultArgs`、`dtoFactory` → `gachaDtoFactory`・`gachaRatesDtoFactory`・`gachaResultDtoFactory`、`gacha`（`IGachaUseCase`）→ `gachaUseCase`、`gacha`（`IGachaService`）→ `gachaService`、`dialogs` → `dialogOpener`、`navigator` → `screenNavigator`、`db` → `memoryDatabase`、`players` → `playerRepository`、`player`（`IPlayerUseCase`）→ `playerUseCase`、`calculator` → `gachaRateCalculator`。`HomePresenter` に足す引数・フィールドは `screenNavigator`（既存の `homeView`・`dialogOpener` などは c7265e5 の名前のまま）。コレクション・組み込みの型・ループと短い一時のローカル変数・テストのコードは対象外
- ガチャのファイルは Shared.Domain の enum を使わない（レア度・種別はマスタの enum。表示は `rarity.ToString()` で "SSR"・"SR"・"R"・"N"）
- 書式は C# 9 の範囲・BOM 付き UTF-8・LF（CLAUDE.md）。`N0(x)` は `x.ToString("N0", CultureInfo.InvariantCulture)`

### G2. Application（`Uta.Client.Application.Gacha`）

本文 3.1・3.2 の型は全部作らない。代わりに次の 7 つ（1 ファイル 1 型。ファイル名 = 型名）。

```csharp
public enum DrawResultKind { Succeeded, NotEnoughCurrency, Failed }

public sealed class GachaResult
{
    public GachaResult(int slot, ItemKind kind, int itemId, bool isNew, int shards, int supportPoints);
    public int Slot { get; }              // 1〜10
    public ItemKind Kind { get; }         // Uta.MasterData.ItemKind（Idol・SupportCard）
    public int ItemId { get; }            // 育成アイドルなら IdolId、サポートカードなら SupportCardId
    public bool IsNew { get; }            // 応答の Outcome が New（育成アイドルは初めて得た、サポートカードは所持の行が 1 つ増えた）
    public int Shards { get; }            // 育成アイドルの重複で得たかけら（それ以外は 0）
    public int SupportPoints { get; }     // サポートカードの完凸後の重複で変換した強化pt（それ以外は 0）
}

public sealed class DrawResult
{
    public DrawResultKind Kind { get; }
    public IReadOnlyList<GachaResult> Results { get; }   // Succeeded のとき Slot の昇順。それ以外は空のリスト（Array.Empty<GachaResult>()）
    public int GachaCurrency { get; }                    // Succeeded のとき引いた後の残り（DrawResponse.GachaCurrency）。それ以外は 0
    public int ExchangePoints { get; }                   // Succeeded のとき引いた後のこのガチャの交換pt。それ以外は 0
    public static DrawResult Success(IReadOnlyList<GachaResult> results, int gachaCurrency, int exchangePoints);   // results が null なら ArgumentNullException("results")
    public static DrawResult Rejected(DrawResultKind kind);                                                       // kind が Succeeded なら ArgumentException
    // コンストラクターは private 1 本（kind, results, gachaCurrency, exchangePoints）
}

public interface IGachaUseCase
{
    /// <summary>ガチャの一覧（GetGachasAsync）を取り、gachaId の交換pt を返す。一覧に無ければ 0。通信の失敗は null（共通のダイアログは ApiCore が出している）。</summary>
    UniTask<int?> GetExchangePointsAsync(int gachaId);

    /// <summary>
    /// count は 1 か 10（それ以外は ArgumentOutOfRangeException）。費用はマスタの SingleCost／TenCost。
    /// 所持のガチャ通貨（リポジトリ）が費用より少なければ通信せず NotEnoughCurrency。
    /// サーバーの通貨不足は NotEnoughCurrency、ほかの失敗は Failed（共通のダイアログは ApiCore が出している）。
    /// 成功したら所持データを取り直し（失敗しても成功を返す）、Succeeded を返す。
    /// </summary>
    UniTask<DrawResult> DrawAsync(int gachaId, int count);
}

public sealed class GachaRateGroup
{
    public GachaRateGroup(Rarity rarity, ItemKind kind, int basisPoints);
    public Rarity Rarity { get; }      // Uta.MasterData.Rarity
    public ItemKind Kind { get; }
    public int BasisPoints { get; }    // 0.01% 単位（10000 = 100%）
}

public sealed class GachaItemRate
{
    public GachaItemRate(Rarity rarity, ItemKind kind, int itemId, string name, int basisPoints);
    public Rarity Rarity { get; }
    public ItemKind Kind { get; }
    public int ItemId { get; }
    public string Name { get; }
    public int BasisPoints { get; }
}

public sealed class GachaRateResult
{
    public GachaRateResult(IReadOnlyList<GachaRateGroup> groups, IReadOnlyList<GachaItemRate> items);
    public IReadOnlyList<GachaRateGroup> Groups { get; }   // レア別・種別の合計
    public IReadOnlyList<GachaItemRate> Items { get; }     // カードごと
}

/// <summary>排出率の計算。外部に触れない具象（ルートのスコープにシングルトン）。</summary>
public sealed class GachaRateCalculator
{
    public GachaRateCalculator(MemoryDatabase db);
    public GachaRateResult Calculate(int gachaTableId);
}
```

- `using`: `System`・`System.Collections.Generic`・`System.Linq`・`Cysharp.Threading.Tasks`（`IGachaUseCase`）・`Uta.MasterData`（G1。マスタだけを使う）
- `GachaRateCalculator.Calculate(gachaTableId)` の手順（本文 3.2 の `Calculate` の 1〜4 と同じ規則。[gacha.md](../wiki/systems/gacha.md) 3.1: 重みを合計してから切り捨てる）:
  1. `var entries = db.GachaTableEntryTable.All.Where(e => e.GachaTableId == gachaTableId).OrderBy(e => e.Order).ToList();`
  2. `total` = `entries` の `Weight` の合計（`long`）。`entries` が空か `total == 0` なら、`Groups`・`Items` とも空の `GachaRateResult` を返す
  3. 各エントリー: `ItemKind.Idol` は `itemId = e.IdolId`、`var idol = db.IdolTable.FindByIdolId(itemId);` の `Rarity`・`Name`。`ItemKind.SupportCard` は `itemId = e.SupportCardId`、`db.SupportCardTable.FindBySupportCardId(itemId)`。それ以外の `ItemKind` は `ArgumentException`。マスタに無い ID で `FindBy…` が投げる例外は捕まえない
  4. `Groups`: (レア度, 種別) ごとに重みを足し、`(int)(合計 * 10000 / total)`。エントリーが 1 つでもある組だけ（重み 0 だけの組は 0 で出す）。並びはレア度の数値の降順（SSR → SR → R → N）、同じレアは種別の数値の昇順（育成アイドル → サポートカード）
  5. `Items`: エントリーごとに `(int)(weight * 10000 / total)`（重み 0 も 0 で出す）。並びは `Groups` と同じ順のあと `ItemId` の昇順（LINQ の `OrderBy`／`ThenBy` は安定なので、同じ ID は `Order` の順のまま）

### G3. Infrastructure

`Infrastructure/Gacha/GachaMapper.cs`・`GachaUseCase.cs`（名前空間 `Uta.Client.Infrastructure.Gacha`）。`using`: `System`・`System.Collections.Generic`・`System.Linq`・`Cysharp.Threading.Tasks`・`Uta.Client.Application.Gacha`・`Uta.Client.Application.Player`・`Uta.Client.Domain.Player`・`Uta.Shared.Contracts.Gacha`・`VolTech.Client`（G1: `Uta.MasterData` は `using` しない）。

```csharp
/// <summary>DTO → Application の型。</summary>
public static class GachaMapper
{
    /// <summary>response.Gachas の中で GachaMasterId == gachaMasterId の最初の行の ExchangePoints。Gachas が null か、行が無ければ 0。</summary>
    public static int ToExchangePoints(GetGachasResponse response, int gachaMasterId);

    /// <summary>
    /// DrawResult.Success(results, response.GachaCurrency, response.ExchangePoints)。results は response.Results（null なら空）の各 dto を
    /// new GachaResult(dto.Slot, (Uta.MasterData.ItemKind)(int)dto.ItemKind, dto.ItemMasterId, dto.Outcome == GachaOutcome.New, dto.Shards, dto.SupportPoints)
    /// にして、Slot の昇順に並べ直した List。
    /// </summary>
    public static DrawResult ToDrawResult(DrawResponse response);
}

public sealed class GachaUseCase : IGachaUseCase
{
    public GachaUseCase(IGachaService gacha, IReadOnlyPlayerRepository players, IPlayerUseCase player, Uta.MasterData.MemoryDatabase db);
    public UniTask<int?> GetExchangePointsAsync(int gachaId);   // async
    public UniTask<DrawResult> DrawAsync(int gachaId, int count); // async
}
```

- `GetExchangePointsAsync`: `var result = await gacha.GetGachasAsync().ToResult();` → `result.IsError` なら `null`、それ以外は `GachaMapper.ToExchangePoints(result.Value, gachaId)`
- `DrawAsync` の手順（`async`。1 の例外は `await` したときに出る）:
  1. `count` が 1・10 以外なら `throw new ArgumentOutOfRangeException(nameof(count));`
  2. `var master = db.GachaTable.FindByGachaId(gachaId);`（無い ID の例外は捕まえない）。`var cost = count == 1 ? master.SingleCost : master.TenCost;`
  3. `var p = players.Find();` が null か `p.GachaCurrency < cost` なら、通信せず `DrawResult.Rejected(DrawResultKind.NotEnoughCurrency)`（リポジトリが古くて多めに見えていても、サーバーが同じ業務エラーで断る）
  4. `var result = await gacha.WithHandledCodes(GachaErrors.NotEnoughCurrency).DrawAsync(new DrawRequest(gachaId, count, cost)).ToResult();`（`PlayerUseCase` の `SignInAsync` と同じ書き方。`handledCodes` は通貨不足だけ。ほかの 4 つは `ErrorDefinitions` に `Stay` で登録済みなので、共通のダイアログが出て `Failed` になる: 本文 3.3）
  5. `result.IsError` のとき: `result.ErrorCode == GachaErrors.NotEnoughCurrency` なら `Rejected(NotEnoughCurrency)`、それ以外は `Rejected(Failed)`。取り直しはしない
  6. 成功: `var drawn = GachaMapper.ToDrawResult(result.Value);`（詰め替えを先に）→ `try { await player.RefreshAsync(); } catch (InvalidOperationException) { }`（握りつぶす。取り直しの通信の失敗のダイアログは ApiCore が出している。R0）→ `return drawn;`
- **Common の player_diff**: `DrawResponse` は `Common` を持ち、基盤が呼び出し側に返す前に `CommonResponseApplier` → `PlayerDiffApplier` がリポジトリへ反映する（[api-design.md](../wiki/architecture/api-design.md) 1.3・[screen-design.md](../wiki/architecture/screen-design.md) 7.5）。`RefreshAsync` は安全網。具象は player_diff を自分で当てない
- `ApiClient`（`Infrastructure/Api/ApiClient.cs`）: `public IGachaService Gacha { get; }` を `Player` の次に足す。private コンストラクターの引数の最後に `IGachaService gacha` を足して `Gacha = gacha;`。`Create` の `var playerClient = core.CreateClient<IPlayerService>();` の次に `var gachaClient = core.CreateClient<IGachaService>();` を足し、`new ApiClient(..., playerClient, gachaClient)`。`using Uta.Shared.Contracts.Gacha;` を足す
- `MagicOnionClientInitializer`: `[MagicOnionClientGeneration(typeof(IDeviceService), typeof(IPlayerService), typeof(IGachaService))]`、`using Uta.Shared.Contracts.Gacha;`
- 本文 3.3 の `GachaApi`・`GachaGateway`・`drawHandled`・「`ApiResult` はテストから作れない」は無効（偽の `IGachaService` が `FakeRpcErrors.ApiFailureResult<T>` で失敗を作れるので、`GachaUseCase` の分岐もテストする）

### G4. Presentation（`Uta.Client.Presentation.Gacha`）

**行の View の改名**（最初に行う）: `git mv client/uta/Assets/Scripts/Presentation/Gacha/GachaLineRow.cs client/uta/Assets/Scripts/Presentation/Gacha/GachaLineRowView.cs` と同じ名前の `.meta` も `git mv`（GUID を保つので、Prefab の参照は切れない。Prefab は作り直さない）。クラス名を `GachaLineRowView` にし、`GachaLineRow` を参照する 6 か所（`GachaRatesModal.cs`・`GachaResultModal.cs`・`Editor/Prefabs/Screens/Gacha/GachaRatesModalBuilder.cs`・`GachaResultModalBuilder.cs`・`GachaRowBuilders.cs`・`Tests/EditMode/GachaPrefabTests.cs`。Grep `GachaLineRow\b` で探す）を直す。`screen-design.md` の `GachaLineRow` の記述（267 行付近）も `GachaLineRowView` に直す（タスク 4）。

View（P2 の骨格に足す。フィールドは足さない・名前を変えない。`Awake` の購読の書き方は本文 3.4 のまま）:

```csharp
[DisallowMultipleComponent]
public sealed class GachaLineRowView : MonoBehaviour
{
    // フィールドは P2 の骨格のまま（text）
    public void SetText(string value);   // text.text = value ?? ""
}

/// <summary>ガチャの 1 行（排出率・結果のモーダル）の表示の値。</summary>
public sealed class GachaLineRowViewDto
{
    public GachaLineRowViewDto(string text, bool isHeading);
    public string Text { get; }
    public bool IsHeading { get; }   // 排出率のモーダルだけが見る（結果のモーダルは常に false）
}

public interface IGachaView
{
    void SetCurrency(string text);                        // currencyText.text = text
    void SetExchange(string text);                        // exchangeText.text = text
    void SetCosts(string singleLabel, string tenLabel);   // singleButton.SetLabel(singleLabel); tenButton.SetLabel(tenLabel);
    void SetLineup(string text);                          // lineupText.text = text
    event Action SingleClicked; event Action TenClicked; event Action RatesClicked; event Action BackClicked;
}

public interface IGachaRatesView
{
    void SetLines(IReadOnlyList<GachaLineRowViewDto> lines);   // 本文 3.4 の GachaRatesModal.SetLines（IsHeading で headingRowPrefab／lineRowPrefab、名前は "Line" + i）
    event Action CloseClicked;
}

public interface IGachaResultView
{
    void SetLines(IReadOnlyList<GachaLineRowViewDto> lines);   // 本文 3.4 の GachaResultModal.SetLines（いつも rowPrefab。lines[i].Text を SetText）
    event Action CloseClicked; event Action RetryClicked;
}
```

- `GachaPage : PageBase, IGachaView`・`GachaRatesModal : ModalBase, IGachaRatesView`・`GachaResultModal : ModalBase, IGachaResultView`。`GachaPage` は数値の書式を持たない（`System.Globalization` は要らない）
- `UiObjects.AddRow` の型引数は `GachaLineRowView`

表示の値と文字（ルートのスコープにシングルトン。状態を持たず、リポジトリに書かず、通信・遷移・View に触れない。`using Uta.MasterData;` を書いてよい（G1）。マスタの `Gacha` は `Uta.MasterData.Gacha` か `var`）:

```csharp
public static class GachaTexts   // 本文の internal を public に（テストから呼ぶ）
{
    /// <summary>Idol → "育成アイドル"、SupportCard → "サポートカード"、それ以外 → ArgumentOutOfRangeException。</summary>
    public static string Kind(ItemKind kind);
    /// <summary>本文の GachaRateCalculator.FormatPercent と同じ。basisPoints / 100 + "." + (basisPoints % 100) を 2 桁 + "%"。例 0 → "0.00%"、5 → "0.05%"、1234 → "12.34%"、10000 → "100.00%"。負なら ArgumentOutOfRangeException。</summary>
    public static string Percent(int basisPoints);
}

public sealed class GachaViewDto
{
    public GachaViewDto(int gachaId, string currencyText, string exchangeText, string singleLabel, string tenLabel, string lineupText);
    public int GachaId { get; } public string CurrencyText { get; } public string ExchangeText { get; }
    public string SingleLabel { get; } public string TenLabel { get; } public string LineupText { get; }
}

public sealed class GachaDtoFactory
{
    public GachaDtoFactory(IReadOnlyPlayerRepository players, MemoryDatabase db);
    /// <summary>
    /// ガチャはマスタの GachaTable の GachaId の最小の 1 本（MVP はガチャ 1 本: 本文 1章「決めないこと」）。
    /// currency = players.Find() の GachaCurrency（null なら 0。player_diff と取り直しが反映済みの値）。
    /// CurrencyText = "ガチャ通貨 " + N0(currency)。ExchangeText = "交換pt " + (exchangePoints.HasValue ? N0(exchangePoints.Value) : "-") + " / " + N0(CeilingPoints)。
    /// SingleLabel = "単発 " + N0(SingleCost)、TenLabel = "10連 " + N0(TenCost)。
    /// LineupText = 通常枠（NormalTableId の GachaTableEntry）のうちレア度が Rarity.SSR のものを、種別の数値の昇順 → ItemId の昇順に並べ、同じ (種別, ID) は 1 つにして、
    ///   1 行ずつ "SSR " + GachaTexts.Kind(kind) + ": " + name を "\n" でつないだもの（SSR が無ければ ""）。名前・レア度は IdolTable／SupportCardTable の FindBy…。
    /// </summary>
    public GachaViewDto Create(int? exchangePoints);
    /// <summary>"ガチャ通貨 " + N0(cost) + " を使って " + count + " 回引きます。（所持 " + N0(currency) + "）"。cost は Create と同じガチャの count == 1 ? SingleCost : TenCost、currency は Create と同じ。</summary>
    public string CreateDrawConfirmText(int count);
}

public sealed class GachaRatesDtoFactory
{
    public GachaRatesDtoFactory(MemoryDatabase db, GachaRateCalculator calculator);
    /// <summary>
    /// var gacha = db.GachaTable.FindByGachaId(gachaId)。本文 3.4 の GachaRatesPresenter.InitializeAsync の 1〜8 の順の行（見出しは isHeading = true）:
    /// 合計の行 = rarity + " " + GachaTexts.Kind(kind) + " " + GachaTexts.Percent(bp)、カードごとの行 = rarity + " " + GachaTexts.Kind(kind) + " " + name + " " + GachaTexts.Percent(bp)。
    /// 通常枠は calculator.Calculate(gacha.NormalTableId)、確定枠は calculator.Calculate(gacha.GuaranteedTableId)。
    /// </summary>
    public IReadOnlyList<GachaLineRowViewDto> Create(int gachaId);
}

public sealed class GachaResultDtoFactory
{
    public GachaResultDtoFactory(MemoryDatabase db);
    /// <summary>
    /// results の各 r を本文 3.4 の GachaResultPresenter の行の文（item → r、name・rarity は IdolTable／SupportCardTable の FindBy…）にした、isHeading = false の行。
    /// results が null なら ArgumentNullException。
    /// </summary>
    public IReadOnlyList<GachaLineRowViewDto> Create(IReadOnlyList<GachaResult> results);
}
```

Args・Presenter（実行中フラグ・`disposed`・購読・`Dispose` の決まりは本文 3.4 の前置きのまま。`using UnityScreenNavigator;`・`using VContainer;`・`using Cysharp.Threading.Tasks;`）:

```csharp
public sealed class GachaRatesArgs { public GachaRatesArgs(int gachaId); public int GachaId { get; } }   // 本文のまま

public sealed class GachaResultArgs
{
    public GachaResultArgs(IReadOnlyList<GachaResult> results, Func<UniTask> retry);   // results が null なら ArgumentNullException。retry は null を許す
    public IReadOnlyList<GachaResult> Results { get; }
    public Func<UniTask> Retry { get; }
}

[AssetAddress(nameof(GachaPage))]
public sealed class GachaPresenter : IPresenter
{
    [Inject]
    public GachaPresenter(GachaPage view, GachaDtoFactory dtoFactory, IGachaUseCase gacha, DialogOpener dialogs, IScreenNavigator navigator)
        : this((IGachaView)view, dtoFactory, gacha, dialogs, navigator) { }
    public GachaPresenter(IGachaView view, GachaDtoFactory dtoFactory, IGachaUseCase gacha, DialogOpener dialogs, IScreenNavigator navigator);
    public UniTask InitializeAsync(); public void Dispose();
    public UniTask DrawAsync(int count); public UniTask OpenRatesAsync(); public UniTask BackAsync();
}

[AssetAddress(nameof(GachaRatesModal))]
public sealed class GachaRatesPresenter : IScreenWithArgs<GachaRatesArgs>
{
    [Inject]
    public GachaRatesPresenter(GachaRatesModal view, GachaRatesArgs args, GachaRatesDtoFactory dtoFactory, IScreenNavigator navigator)
        : this((IGachaRatesView)view, args, dtoFactory, navigator) { }
    public GachaRatesPresenter(IGachaRatesView view, GachaRatesArgs args, GachaRatesDtoFactory dtoFactory, IScreenNavigator navigator);
    public UniTask InitializeAsync(); public void Dispose(); public UniTask CloseAsync();
}

[AssetAddress(nameof(GachaResultModal))]
public sealed class GachaResultPresenter : IScreenWithArgs<GachaResultArgs>
{
    [Inject]
    public GachaResultPresenter(GachaResultModal view, GachaResultArgs args, GachaResultDtoFactory dtoFactory, IScreenNavigator navigator)
        : this((IGachaResultView)view, args, dtoFactory, navigator) { }
    public GachaResultPresenter(IGachaResultView view, GachaResultArgs args, GachaResultDtoFactory dtoFactory, IScreenNavigator navigator);
    public UniTask InitializeAsync(); public void Dispose(); public UniTask CloseAsync(); public UniTask RetryAsync();
}
```

- **`GachaPresenter`**（本文の手順を次に読み替える）:
  - フィールド: `int gachaId`、`int? exchangePoints`、`bool running`、`bool disposed`（本文の `GachaInfo gacha`・`int currency` は無い。通貨は毎回 `GachaDtoFactory` がリポジトリから読む: player_diff と取り直しで、引いた後のリポジトリは応答の残りと同じ値になる。本文の決めたこと 6 の「通貨は `DrawResponse` の値で出す」はこれに読み替える）
  - private `Show(GachaViewDto dto)`: この順に `view.SetCurrency(dto.CurrencyText); view.SetCosts(dto.SingleLabel, dto.TenLabel); view.SetLineup(dto.LineupText); view.SetExchange(dto.ExchangeText);`
  - `InitializeAsync`（`async`）: 1. 4 つのイベントを購読（本文のまま）→ 2. `gachaId = dtoFactory.Create(null).GachaId;` → 3. `exchangePoints = await gacha.GetExchangePointsAsync(gachaId); if (disposed) return;` → 4. `Show(dtoFactory.Create(exchangePoints));`
  - `DrawAsync(count)`（実行中フラグの中）: 1. `var answer = await dialogs.ConfirmAsync(dtoFactory.CreateDrawConfirmText(count), "引く", "やめる"); if (disposed) return;` `DialogResult.Negative` なら終わる → 2. `var result = await gacha.DrawAsync(gachaId, count); if (disposed) return;` → 3. `Succeeded`: `exchangePoints = result.ExchangePoints; Show(dtoFactory.Create(exchangePoints)); await navigator.PushModalAsync<GachaResultPresenter, GachaResultArgs>(new GachaResultArgs(result.Results, () => DrawAsync(count)), playAnimation: false);` → `NotEnoughCurrency`: `await dialogs.ShowInfoAsync("ガチャ通貨が足りません。");` → `Failed`: 何もしない。`catch` は書かない
  - `OpenRatesAsync`: `PushModalAsync<GachaRatesPresenter, GachaRatesArgs>(new GachaRatesArgs(gachaId), playAnimation: false)`。`BackAsync`・`Dispose` は本文のまま
- **`GachaRatesPresenter.InitializeAsync`**: `view.CloseClicked += OnCloseClicked; view.SetLines(dtoFactory.Create(args.GachaId));` `UniTask.CompletedTask` を返す。`CloseAsync`・`Dispose` は本文のまま
- **`GachaResultPresenter.InitializeAsync`**: 2 つを購読し、`view.SetLines(dtoFactory.Create(args.Results));`。`CloseAsync`・`RetryAsync`・`Dispose` は本文のまま
- **`HomePresenter`**（本文 3.5 を読み替える）: 2 本のコンストラクターの最後に `IScreenNavigator navigator` を足す（`[Inject] HomePresenter(HomePage view, HomeDtoFactory dtoFactory, IPlayerUseCase player, DialogOpener dialogs, IScreenNavigator navigator) : this((IHomeView)view, view.CreateStage(), dtoFactory, player, dialogs, navigator)`、`HomePresenter(IHomeView view, ICharacterStage stage, HomeDtoFactory dtoFactory, IPlayerUseCase player, DialogOpener dialogs, IScreenNavigator navigator)`）。`OpenGachaAsync` を次にする（`OpenProduceAsync`・`OpenOwnedAsync` は `ShowNotReadyAsync` のまま）:
  ```csharp
  public async UniTask OpenGachaAsync()
  {
      if (running) { return; }
      running = true;
      try { await navigator.PushPageAsync<GachaPresenter>(playAnimation: false); }
      finally { running = false; }
  }
  ```
  `using Uta.Client.Presentation.Gacha;` を足す（`using UnityScreenNavigator;` はある）

### G5. Bootstrap（本文 3.5 の `RegisterUseCases` の 3 行を読み替える）

`BootstrapEntry.RegisterUseCases` の `builder.Register<IPlayerService>(…)` の次の行に `builder.Register<IGachaService>(resolver => resolver.Resolve<ApiClient>().Gacha, Lifetime.Singleton);`、最後（`PlayerUseCase` の次）にこの順で 5 行:

```csharp
builder.Register<GachaRateCalculator>(Lifetime.Singleton);
builder.Register<GachaDtoFactory>(Lifetime.Singleton);
builder.Register<GachaRatesDtoFactory>(Lifetime.Singleton);
builder.Register<GachaResultDtoFactory>(Lifetime.Singleton);
builder.Register<GachaUseCase>(Lifetime.Singleton).As<IGachaUseCase>();
```

`using Uta.Client.Application.Gacha;`・`using Uta.Client.Infrastructure.Gacha;`・`using Uta.Client.Presentation.Gacha;`・`using Uta.Shared.Contracts.Gacha;` を足す（`BootstrapEntry` は `ItemKind` を使わないので曖昧にならない）。

### G6. テスト（本文 5 章の表を置き換える。置き場 `Tests/EditMode/`、名前空間 `Uta.Client.Tests`）

偽と補助（`Tests/EditMode/Fakes/` は public、1 ファイル 1 型）:

- `Fakes/FakeGachaUseCase.cs`（`IGachaUseCase`）: `FakeGachaUseCase(CallLog log = null)`。`Queue<Func<UniTask<int?>>> ExchangePointsResults`（空なら `UniTask.FromResult<int?>(0)`）、`Queue<Func<UniTask<DrawResult>>> DrawResults`（空なら `InvalidOperationException("FakeGachaUseCase: no draw result")`）、`List<string> DrawCalls`（`DrawAsync` のたびに `gachaId + "," + count`）。`log` があれば `DrawAsync` の入口で `"Draw:" + count` を記録（`GetExchangePointsAsync` は記録しない）。`Dequeue` した `Func` を呼んでその `UniTask` を返す
- `Fakes/FakeGachaService.cs`（`IGachaService`。`FakePlayerService` と同じ形）: `Queue<Func<UnaryResult<GetGachasResponse>>> GetGachasResults`、`Queue<Func<UnaryResult<DrawResponse>>> DrawResults`、`List<string> Calls`（`"GetGachas"`、`"Draw:" + r.GachaMasterId + "," + r.Count + "," + r.ExpectedCost`）、static の `Ok<T>`・`Fail<T>(ErrorCode)`（`FakeRpcErrors.ApiFailureResult<T>`）。`ExchangeAsync` と `WithHost` は `NotSupportedException`、`WithOptions`・`WithHeaders`・`WithDeadline`・`WithCancellationToken` は `this`。キューが空なら `InvalidOperationException("FakeGachaService: no result")`
- `Support/GachaFixtures.cs`（本文 5.1 を置き換える。`public static class`。G1 のとおり `using Uta.MasterData;` を書かず、`using Uta.Shared.Contracts.Gacha;`・`using Uta.Client.Domain.Player;`）:
  ```csharp
  public const int GachaId = 1, NormalTableId = 10, GuaranteedTableId = 11, SingleCost = 250, TenCost = 2500, CeilingPoints = 200, Currency = 3250;
  /// <summary>new SupportCard { SupportCardId, Name, Rarity, Type = SupportType.Singing, Plan = Plan.Free }（Uta.MasterData の型）。</summary>
  public static Uta.MasterData.SupportCard SupportCard(int supportCardId, string name, Uta.MasterData.Rarity rarity);
  /// <summary>new GachaTableEntry { GachaTableId = tableId, Order = order, ItemKind = Idol, IdolId = idolId, Weight = weight }。</summary>
  public static Uta.MasterData.GachaTableEntry IdolEntry(int tableId, int order, int idolId, int weight);
  /// <summary>同じく ItemKind = SupportCard、SupportCardId = supportCardId。</summary>
  public static Uta.MasterData.GachaTableEntry SupportCardEntry(int tableId, int order, int supportCardId, int weight);
  /// <summary>TestMasterData.Build で、idols・supportCards・entries と、ガチャ 1 行 new Gacha { GachaId, SingleCost, TenCost, NormalTableId, GuaranteedTableId, CeilingPoints } を Append した MemoryDatabase（Append は表ごとに 1 回）。</summary>
  public static Uta.MasterData.MemoryDatabase Build(IEnumerable<Uta.MasterData.Idol> idols, IEnumerable<Uta.MasterData.SupportCard> supportCards, IEnumerable<Uta.MasterData.GachaTableEntry> entries);
  /// <summary>新しい List: TestMasterData.Idol(1, "アイドルA", SSR)・TestMasterData.Idol(3, "アイドルC", R)。</summary>
  public static List<Uta.MasterData.Idol> Idols();
  /// <summary>新しい List: SupportCard(2, "サポートB", SSR)・SupportCard(4, "サポートD", SR)。</summary>
  public static List<Uta.MasterData.SupportCard> SupportCards();
  /// <summary>新しい List（通常枠。表 10）: IdolEntry(10, 1, 1, 1)・SupportCardEntry(10, 2, 2, 1)・IdolEntry(10, 3, 3, 8)。</summary>
  public static List<Uta.MasterData.GachaTableEntry> NormalEntries();
  /// <summary>新しい List（確定枠。表 11）: SupportCardEntry(11, 1, 4, 3)・IdolEntry(11, 2, 1, 1)。</summary>
  public static List<Uta.MasterData.GachaTableEntry> GuaranteedEntries();
  /// <summary>共通の前提のマスタ: Build(Idols(), SupportCards(), NormalEntries().Concat(GuaranteedEntries()))。</summary>
  public static Uta.MasterData.MemoryDatabase CreateMaster();
  /// <summary>new PlayerEntity("P1", 1, 0, 0, 0, gachaCurrency, false, false)。</summary>
  public static PlayerEntity Player(int gachaCurrency);
  /// <summary>new DrawResultDto(slot, ItemKind.Idol, idolId, outcome, shards, 0, "")（Contracts の ItemKind）。</summary>
  public static DrawResultDto IdolResult(int slot, int idolId, GachaOutcome outcome, int shards);
  /// <summary>new DrawResultDto(slot, ItemKind.SupportCard, supportCardId, outcome, 0, supportPoints, "uid" + slot)。</summary>
  public static DrawResultDto SupportCardResult(int slot, int supportCardId, GachaOutcome outcome, int supportPoints);
  ```
  - 本体のマスタの型・enum は全部 `Uta.MasterData.` 付きで書く（`Uta.MasterData.Rarity.SSR`・`Uta.MasterData.ItemKind.Idol`・`Uta.MasterData.SupportType.Singing`・`Uta.MasterData.Plan.Free`。Contracts の `ItemKind` と曖昧になるため）。`using System.Collections.Generic;`・`using System.Linq;` を書く
  - **マスタを変えるテストの組み立て**: 共通の前提を少し変えるテストは `GachaFixtures.Build(idols, supportCards, entries)` に、`Idols()` などで作った List に足したもの・差し替えたものを渡す（下の表に、テストごとに渡すものを書いた）
  ガチャのテストはマスタの行と DTO を直接 `new` せず、これと `TestMasterData` を使う。`GachaResult`・`DrawResult`（Application の型で、引数がどれも期待に出る値）は直接 `new` してよい
- 共通の前提: マスタは `GachaFixtures.CreateMaster()`、リポジトリは本物の `PlayerRepository` に `GachaFixtures.Player(GachaFixtures.Currency)` を `Upsert`、偽の View は本文 5 章のとおり（記録の書式は `"<メソッド名>:<引数を , でつないだもの>"`。`SetLines` は `"SetLines:" + 行数` を記録して `LastLines` に入れる）

| ファイル | テスト | 内容 |
|---|---|---|
| `GachaRateCalculatorTests.cs` | `Calculate_ItemsToBasisPoints` | `CreateMaster()` の `Calculate(10)` → `Items` の `BasisPoints` が 1000・1000・8000 |
| | `Calculate_GroupsSumWeightsBeforeTruncating` | `Build` に渡すもの: `Idols()` に `TestMasterData.Idol(5, "アイドルE", Uta.MasterData.Rarity.SSR)` を足した List、`SupportCards()`、`new[] { IdolEntry(20, 1, 1, 1), IdolEntry(20, 2, 5, 1), IdolEntry(20, 3, 3, 5) }` → `Calculate(20)` の (SSR, Idol) の `Groups` が 2857 |
| | `Calculate_SortedByRarityDescThenKindThenId` | `CreateMaster()` → `Calculate(10)` の `Groups` が (SSR, Idol)・(SSR, SupportCard)・(R, Idol)、`Calculate(11)` の `Items` の `ItemId` が 1 → 4 |
| | `Calculate_ZeroWeightItem_Included` | `Build(Idols(), SupportCards(), new[] { IdolEntry(20, 1, 1, 0), IdolEntry(20, 2, 3, 4) })` → `Calculate(20)` の `Items` が 2 行で、Idol 1 の `BasisPoints == 0` |
| | `Calculate_EmptyOrZeroTotal_ReturnsEmpty` | `CreateMaster()` の `Calculate(99)`（無い表）と、`Build(Idols(), SupportCards(), new[] { IdolEntry(20, 1, 1, 0) })` の `Calculate(20)` → `Groups`・`Items` とも空 |
| | `Calculate_UnknownId_Throws` | `Build(Idols(), SupportCards(), new[] { IdolEntry(20, 1, 99, 1) })` の `Calculate(20)` → `Assert.Catch(() => ...)`（例外の型は問わない） |
| `GachaTextsTests.cs` | `Percent_Formats` | 0 → "0.00%"、5 → "0.05%"、1234 → "12.34%"、10000 → "100.00%"。-1 は `ArgumentOutOfRangeException` |
| `GachaMapperTests.cs` | `ToDrawResult_MapsOutcomeAndSortsBySlot` | `new DrawResponse(new[] { SupportCardResult(2, 2, Duplicate, 10), IdolResult(1, 1, New, 0) }, 750, 47)` → `Kind == Succeeded`、`Results` が Slot 1・2、`IsNew` が true・false、`Kind` が Idol・SupportCard、`ItemId` 1・2、`SupportPoints` 0・10、`GachaCurrency == 750`、`ExchangePoints == 47` |
| | `ToDrawResult_NullResults_ReturnsEmpty` | `Results.Count == 0` |
| | `ToExchangePoints_FindsRowOrZero` | `[(2, 5), (1, 37)]` で 1 → 37、3 → 0、`Gachas == null` → 0 |
| `GachaUseCaseTests.cs`（`FakeGachaService`・本物の `PlayerRepository`・`FakePlayerUseCase`・`CreateMaster()`。成功の応答は `FakeGachaService.Ok(new DrawResponse(new[] { GachaFixtures.IdolResult(1, 1, GachaOutcome.New, 0) }, 3000, 1))`） | `Draw_Success_RefreshesAndReturnsResult` | 成功の応答 → `Succeeded`、`Results.Count == 1`、`GachaCurrency == 3000`、`ExchangePoints == 1`、`player.RefreshCount == 1` |
| | `Draw_RefreshFails_StillSucceeds` | `player.RefreshResults` に `() => throw new InvalidOperationException("sign-in failed: x")` → `Succeeded` |
| | `Draw_NotEnoughCurrencyInRepository_DoesNotCall` | 所持 100 → `NotEnoughCurrency`、`service.Calls` が空 |
| | `Draw_NoPlayer_ReturnsNotEnoughCurrency` | 空のリポジトリ → `NotEnoughCurrency`、`Calls` が空 |
| | `Draw_ServerNotEnoughCurrency_DoesNotRefresh` | `Fail<DrawResponse>(GachaErrors.NotEnoughCurrency)` → `NotEnoughCurrency`、`RefreshCount == 0` |
| | `Draw_OtherError_ReturnsFailed` | `Fail<DrawResponse>(GachaErrors.CostMismatch)` → `Failed`、`RefreshCount == 0` |
| | `Draw_InvalidCount_Throws` | `try { await useCase.DrawAsync(1, 2); Assert.Fail(); } catch (ArgumentOutOfRangeException) { }`、`Calls` が空 |
| | `Draw_PassesExpectedCost` | 単発と 10 連 → `Calls` が `"Draw:1,1,250"`・`"Draw:1,10,2500"` |
| | `GetExchangePoints_FoundNotFoundAndFailure` | `[(1, 37)]` で 1 → 37、2 → 0、`Fail<GetGachasResponse>(GachaErrors.NotFound)` → null |
| `GachaDtoFactoryTests.cs` | `Create_FormatsTexts` | `Create(37)` → `GachaId == 1`、"ガチャ通貨 3,250"、"交換pt 37 / 200"、"単発 250"、"10連 2,500"、"SSR 育成アイドル: アイドルA\nSSR サポートカード: サポートB" |
| | `Create_NullPoints_ShowsDash` | "交換pt - / 200" |
| | `Create_LineupDistinctAndNoSsr` | `Build(Idols(), SupportCards(), entries)` で、entries = `NormalEntries()` に `IdolEntry(10, 4, 1, 1)`（同じ ID）を足した List → `LineupText` は共通の前提と同じ 2 行。entries = `new[] { IdolEntry(10, 1, 3, 1) }`（SSR の無い通常枠）→ "" |
| | `Create_NoPlayer_ZeroCurrency` | 空のリポジトリ → "ガチャ通貨 0" |
| | `CreateDrawConfirmText_UsesCostAndCurrency` | 10 → "ガチャ通貨 2,500 を使って 10 回引きます。（所持 3,250）"、1 → "ガチャ通貨 250 を使って 1 回引きます。（所持 3,250）" |
| `GachaRatesDtoFactoryTests.cs` | `Create_BuildsGroupsThenItems` | 本文 5 章の `InitializeAsync_BuildsGroupsThenRows` の 14 行（`Text` と `IsHeading`）と同じ |
| `GachaResultDtoFactoryTests.cs` | `Create_BuildsLines` | 本文 5 章の `InitializeAsync_BuildsLines` の 4 件 → 4 行の `Text` が本文の 4 つの文、`IsHeading` が全部 false |
| `GachaPresenterTests.cs`（`FakeGachaUseCase(log)`・`FakeDialogOpener(log)`・`FakeScreenNavigator(log)`・本物の `GachaDtoFactory`） | `InitializeAsync_ShowsCurrencyCostsLineupAndExchange` | `ExchangePointsResults` に 37 → `log` が `"SetCurrency:ガチャ通貨 3,250"`, `"SetCosts:単発 250,10連 2,500"`, `"SetLineup:SSR 育成アイドル: アイドルA\nSSR サポートカード: サポートB"`, `"SetExchange:交換pt 37 / 200"` |
| | `InitializeAsync_StatusFails_ShowsDash` | `ExchangePointsResults` に null → 最後が `"SetExchange:交換pt - / 200"` |
| | `DrawAsync_Confirmed_PushesResultModal` | `DrawResults` に `() => { players.Upsert(GachaFixtures.Player(750)); return UniTask.FromResult(DrawResult.Success(2 件, 750, 47)); }`（player_diff の代わり）→ `InitializeAsync` のあとの `log` が `"Confirm:ガチャ通貨 2,500 を使って 10 回引きます。（所持 3,250）"`, `"Draw:10"`, `"SetCurrency:ガチャ通貨 750"`, `"SetCosts:単発 250,10連 2,500"`, `"SetLineup:…"`（初期化と同じ）, `"SetExchange:交換pt 47 / 200"`, `"PushModal:GachaResultPresenter"`。`navigator.Args[0]` が `GachaResultArgs` で `Results.Count == 2`、`Retry != null` |
| | `DrawAsync_Declined_DoesNotDraw` | `ConfirmResults` に `Negative` → `"Draw:"` も `"PushModal:"` も無い |
| | `DrawAsync_NotEnoughCurrency_ShowsInfo` | `Rejected(NotEnoughCurrency)` → 最後が `"Info:ガチャ通貨が足りません。"`、`"PushModal:"` が無い |
| | `DrawAsync_Failed_ShowsNothing` | `Rejected(Failed)` → `"Confirm:…"`, `"Draw:10"` のあとに何も無い |
| | `DrawAsync_WhileRunning_Ignored` | `DrawResults` に `() => gate.Task`（`UniTaskCompletionSource<DrawResult>`）→ `var first = presenter.DrawAsync(1);` → `await presenter.DrawAsync(1);` → `gate.TrySetResult(DrawResult.Rejected(DrawResultKind.Failed))` → `await first` → `"Draw:1"` が 1 回 |
| | `DrawAsync_DisposedDuringDraw_TouchesNothing` | 同じ源で `first` → `presenter.Dispose()` → `TrySetResult(Success(1 件, 3000, 1))` → `await first` → `"Draw:1"` のあとに記録が無い |
| | `ResultRetry_DrawsAgain` | `DrawResults` に 1 回目（`Player(750)` を Upsert して `Success(2 件, 750, 47)`）と 2 回目（`Success(2 件, 750, 48)`）。`DrawAsync(10)` のあと `await ((GachaResultArgs)navigator.Args[0]).Retry();` → `"Confirm:"` で始まる行が 2 回（2 回目は `"Confirm:ガチャ通貨 2,500 を使って 10 回引きます。（所持 750）"`）、`"Draw:10"` が 2 回、`"PushModal:GachaResultPresenter"` が 2 回、`"Info:"` が無い |
| | `OpenRatesAsync_PushesRatesModal` | `"PushModal:GachaRatesPresenter"`、`Args[0]` が `GachaRatesArgs` で `GachaId == 1` |
| | `BackAsync_PopsPage` | `"PopPage:GachaPresenter"` |
| | `Dispose_UnsubscribesEvents` | `Dispose()` のあと `RaiseBack()` → `"PopPage:"` が無い |
| `GachaRatesPresenterTests.cs` | `InitializeAsync_SetsLines` | `"SetLines:14"`、`LastLines[0].IsHeading`・`LastLines[0].Text == "通常枠（単発・10連の1〜9枠目）"` |
| | `CloseAsync_PopsModal` | `"PopModal:GachaRatesPresenter"` |
| `GachaResultPresenterTests.cs` | `InitializeAsync_SetsLines` | 2 件 → `"SetLines:2"` |
| | `RetryAsync_PopsModalThenCallsRetry`・`RetryAsync_NullRetry_OnlyPops`・`CloseAsync_PopsModal` | 本文 5 章のまま |
| `HomePresenterTests.cs`（変更） | 組み立て | `CreatePresenter` の `new HomePresenter(...)` の最後に `navigator`（同じ `log` の `FakeScreenNavigator`） |
| | `GachaClicked_ShowsNotReady` → `GachaClicked_PushesGachaPage` | `InitializeAsync` のあと `RaiseGachaClicked()` → 初期化の後の記録が `"PushPage:GachaPresenter"` だけ |
| | `OpenGachaAsync_WhileRunning_Ignored`（あれば） → `OpenProduceAsync_WhileRunning_Ignored` | 呼ぶメソッドを `OpenProduceAsync` にする。無ければ足さない |
| | `OpenGachaAsync_PushesGachaPage`（足す） | `await presenter.OpenGachaAsync();` → 初期化の後の `log` が `"PushPage:GachaPresenter"` だけ |

### G7. タスクの読み替え

- **タスク 1**（Application と排出率・補助）: G2 の 7 型、`Fakes/FakeGachaUseCase.cs`、`Support/GachaFixtures.cs`、`GachaRateCalculatorTests`。前提の確認は foundation の型（`DialogOpener`・`UiObjects`・`FakePlayerUseCase`・`TestMasterData`・`PlayerRepository`）があること。本文の `FakeGameMaster`・`PortResult`・`ServerReasons` とそのテストは作らない。`TestMasterData.Build` での `Append`（`Gacha`・`GachaTableEntry`・`SupportCard` の表）の実在の形はコンパイルで確かめ、違えば止まって報告する
- **タスク 2**（Infrastructure・DI）: G3 の `GachaMapper`・`GachaUseCase`・`ApiClient`・`MagicOnionClientInitializer`、G5 の登録（`GachaUseCase` と `GachaRateCalculator` の 2 行と `IGachaService` の行。DtoFactory の 3 行はタスク 3a で型ができてから足す）、`Fakes/FakeGachaService.cs`、`GachaMapperTests`・`GachaUseCaseTests`。止まる条件・確かめ方は本文のまま
- **タスク 3a**（ページとホーム）: G4 の改名（最初に）、`GachaLineRowViewDto`・`GachaTexts`・`IGachaView`・`GachaViewDto`・`GachaDtoFactory`・`GachaPresenter`・`GachaPage` の実装、`GachaRatesArgs`・`IGachaRatesView`・`GachaResultArgs`・`IGachaResultView`、仮の `GachaRatesPresenter`・`GachaResultPresenter`（本文の手順 3 の形で、`[Inject]` のコンストラクターの引数は G4 のもの: `(GachaRatesModal view, GachaRatesArgs args, GachaRatesDtoFactory dtoFactory, IScreenNavigator navigator)`。そのため **`GachaRatesDtoFactory`・`GachaResultDtoFactory` もタスク 3a で G4 の完成形として作る**）、`HomePresenter`、G5 の DtoFactory の 3 行、`GachaTextsTests`・`GachaDtoFactoryTests`・`GachaRatesDtoFactoryTests`・`GachaResultDtoFactoryTests`・`GachaPresenterTests`・`HomePresenterTests`。Play の手順 7 は本文のまま（式の型名は `Uta.Client.Presentation.Gacha.GachaPage`）
- **タスク 3b**（モーダル）: `GachaLineRowView.SetText`、`GachaRatesModal`・`GachaResultModal` の実装、`GachaRatesPresenter`・`GachaResultPresenter` の本体、`GachaRatesPresenterTests`・`GachaResultPresenterTests`。Play の手順 4 は本文のまま、式の `GachaLineRow` は `GachaLineRowView`。**Play の手順 4 の 3（単発を引く）で、次も確かめる**:
  - **player_diff の反映**: 引く前と後に `unity command eval` で `VContainer` のルートのリポジトリは引けないので、ガチャのページの通貨の表示で見る: `unity command eval 'return UnityEngine.GameObject.Find("CurrencyText").GetComponent<TMPro.TMP_Text>().text;'`（子の名前は P2 3.3 の表で確かめる）が、引いた後に費用の 250 だけ減っている
  - **`CommonResponseApplier.ApplyAsync` のスレッド**（共通レスポンスの計画からの申し送り）: Play の前に `CommonResponseApplier.ApplyAsync` の先頭へ一時的に `UnityEngine.Debug.Log("[Uta] common-apply thread=" + System.Threading.Thread.CurrentThread.ManagedThreadId);` を足し、引いたあと `unity command console get --tail 50 --format json` でその値を読み、`unity command eval 'return System.Threading.Thread.CurrentThread.ManagedThreadId;'`（メインスレッドで動く）と同じであることを確かめ、**行を消してから**コミットする。違えば止まって報告する（ADR 0021 の前提が崩れるため、勝手に直さない）。`Common` が null の応答では呼ばれないので、ログが出なければ「確かめられなかった」と報告する
- 本文 4 章の「止まる条件」の `G1`〜`G3` は、この節の G1〜G9 と別物（本文のリスクの番号）。報告では「止まる条件 G2」のように書く
- `GachaDtoFactory` のガチャは `db.GachaTable.All.OrderBy(x => x.GachaId).First()`。マスタにガチャが無いときの例外は捕まえない（マスタの検証で起きない）
- **タスク 4**（記録）: **実装担当に渡さず、進行役（メインのセッション）が `uta-implement` の 6「ズレを wiki に反映する」と一緒に行う**（実装の結果・Play の結果で文面が決まるため）。本文のタスク 4 は無効で、次を行う:
  1. `screen-design.md`: 2 章の表の「ガチャ（結果）」を「モーダル」に。267 行付近の `GachaLineRow` を `GachaLineRowView` に。7.5 の「所持データのキャッシュ」の最後に「ガチャの画面は、引いた後のガチャ通貨をリポジトリ（player_diff を反映済み）から出す [2026-11-08]」を足す。「実装メモ」に、決めたこと 13 の叩き台との差 4 つと実装のズレを書く
  2. `unity7-evaluation.md`: 2 章の表に Play で確かめたもの（`IGachaService` の `GetGachasAsync`・`DrawAsync` を `ApiCore` 経由で呼べた、モーダルを積んで閉じられた、`Common` の player_diff が反映された）を 1 行。3.3 の `ApplyAsync` のスレッドの行（66 行）に実行の結果を足す。3.3 の `AppCommonDto` の IL2CPP の行（67 行）と「モーダル」の行（76 行）は、「IL2CPP での送受信は順 7 の 6 本目（lesson）の通しのビルドで確かめる」「2 本目で確かめた」に直す
  3. `naming.md`: 「今のコードとの差」の `GachaLineRow` の行を消す。`<名詞>Status` の行と `PortResult<T>` の行から gacha の例（`DrawStatus`）を消す（`PortResult<T>` の行は「gacha では作らなかった」と書き足す）。9 章の `GachaRateCalculator（計画）`・`DialogOpener（計画）` の「（計画）」を消す
  4. `api-design.md` 1.1 の `DrawAsync` の行の後ろに「クライアントは Infrastructure の `GachaUseCase`（`IGachaUseCase`）が呼ぶ」を足す
  5. 索引 `2026-11-07-client-screens-index.md` の 1 章の順 5 の行を `done` にし、「何を作るか」を `IGachaUseCase`・`GachaUseCase`・DtoFactory の形に直す
  6. `python -I .claude/skills/linting-wiki/lint_wiki.py` がエラー 0。計画を `status: done`
- G2 の型は 8 つ（`DrawResultKind`・`GachaResult`・`DrawResult`・`IGachaUseCase`・`GachaRateGroup`・`GachaItemRate`・`GachaRateResult`・`GachaRateCalculator`）
- `GachaPresenterTests` は、どのテストも先に `await presenter.InitializeAsync();`（`ExchangePointsResults` が空なら交換pt 0）を呼び、`InitializeAsync` の記録のあとを確かめる（`log.Entries.Clear()` してからでよい）。「2 件」の結果は `new GachaResult(1, Uta.MasterData.ItemKind.Idol, 1, true, 0, 0)` と `new GachaResult(2, Uta.MasterData.ItemKind.SupportCard, 2, true, 0, 0)`、`DrawAsync` の `count` は 10（`WhileRunning`・`Disposed` は 1）
- 改名の Grep は `.cs` だけ（`--include=*.cs`）。Prefab の名前 `GachaRateRow` などは変えない
- 本文 5 章の偽の View の「リストは要素を `/` でつなぐ」は無い（G4 の View は文字を受ける。`SetLines` は行数を記録）
- `GachaDtoFactory` のラインナップの `ItemKind` が Idol・SupportCard 以外なら、`GachaTexts.Kind` の `ArgumentOutOfRangeException` のまま（捕まえない）

### G8. 触るファイル（本文 2 章の表を置き換える）

パスは `client/uta/Assets/` からの相対（`uta_docs/` で始まるものはリポジトリのルートから）。新しい `.cs` には、再コンパイルで生成された `.meta` をコミットする。Prefab・Addressables の設定・`server/`・`shared/` は触らない。

| タスク | 操作 | パス |
|---|---|---|
| 1 | 作成 | `Scripts/Application/Gacha/DrawResultKind.cs`・`GachaResult.cs`・`DrawResult.cs`・`IGachaUseCase.cs`・`GachaRateGroup.cs`・`GachaItemRate.cs`・`GachaRateResult.cs`・`GachaRateCalculator.cs` |
| 1 | 作成 | `Tests/EditMode/Fakes/FakeGachaUseCase.cs`、`Tests/EditMode/Support/GachaFixtures.cs`、`Tests/EditMode/GachaRateCalculatorTests.cs` |
| 2 | 作成 | `Scripts/Infrastructure/Gacha/GachaMapper.cs`・`GachaUseCase.cs`、`Tests/EditMode/Fakes/FakeGachaService.cs`、`Tests/EditMode/GachaMapperTests.cs`・`GachaUseCaseTests.cs` |
| 2 | 変更 | `Scripts/Infrastructure/Api/ApiClient.cs`・`MagicOnionClientInitializer.cs`、`Scripts/Bootstrap/BootstrapEntry.cs` |
| 3a | 改名（`git mv`。`.meta` も） | `Scripts/Presentation/Gacha/GachaLineRow.cs` → `GachaLineRowView.cs` |
| 3a | 変更（改名に合わせる） | `Scripts/Presentation/Gacha/GachaRatesModal.cs`・`GachaResultModal.cs`、`Scripts/Editor/Prefabs/Screens/Gacha/GachaRatesModalBuilder.cs`・`GachaResultModalBuilder.cs`・`GachaRowBuilders.cs`、`Tests/EditMode/GachaPrefabTests.cs` |
| 3a | 作成 | `Scripts/Presentation/Gacha/GachaLineRowViewDto.cs`・`GachaTexts.cs`・`IGachaView.cs`・`GachaViewDto.cs`・`GachaDtoFactory.cs`・`GachaPresenter.cs`・`GachaRatesArgs.cs`・`IGachaRatesView.cs`・`GachaRatesDtoFactory.cs`・`GachaRatesPresenter.cs`（仮）・`GachaResultArgs.cs`・`IGachaResultView.cs`・`GachaResultDtoFactory.cs`・`GachaResultPresenter.cs`（仮） |
| 3a | 変更 | `Scripts/Presentation/Gacha/GachaPage.cs`、`Scripts/Presentation/Home/HomePresenter.cs`、`Scripts/Bootstrap/BootstrapEntry.cs`（DtoFactory の 3 行） |
| 3a | 作成・変更 | `Tests/EditMode/GachaTextsTests.cs`・`GachaDtoFactoryTests.cs`・`GachaRatesDtoFactoryTests.cs`・`GachaResultDtoFactoryTests.cs`・`GachaPresenterTests.cs`（作成）、`Tests/EditMode/HomePresenterTests.cs`（変更） |
| 3b | 変更 | `Scripts/Presentation/Gacha/GachaLineRowView.cs`・`GachaRatesModal.cs`・`GachaResultModal.cs`・`GachaRatesPresenter.cs`・`GachaResultPresenter.cs`（仮を置き換える）。Play の確認のあいだだけ `Scripts/Infrastructure/Api/CommonResponseApplier.cs`（ログの 1 行。コミットしない） |
| 3b | 作成 | `Tests/EditMode/GachaRatesPresenterTests.cs`・`GachaResultPresenterTests.cs` |
| 4 | 変更 | `uta_docs/wiki/architecture/screen-design.md`・`unity7-evaluation.md`・`naming.md`・`api-design.md`、この計画の frontmatter |

本文 2 章の表と、本文 1 章の「後続が使うもの」（`PortResult<T>`・`ServerReasons`・`FakeGameMaster`）は無効。後続が使うのは偽の形（`FakeGachaUseCase`・`FakeGachaService`。owned 以降は同じ形で自分の偽を作る）だけ。

### G9. 本文のうち有効なもの（ここに無い本文の記述は無効。G0〜G8 が正）

- 「この計画で決めたこと」1・2・3（型名は G0 で読む）・7・8・9（コンストラクターは G4）・13・14
- 1 章の前提 2・3 と「決めないこと」。前提 1 は foundation の型があることだけを確かめる（`IGameMaster`・`PlayerCache`・`RefreshPlayerUseCase`・`StubPlayerPort` は無いのが正しい。確かめるのは G7 のタスク 1 の型）
- 3.4 の「View（P2 の骨格に足す）」の P2 のフィールドの表と `Awake` の購読の書き方、`SetLines` の行の作り方（`GachaLineRow` は `GachaLineRowView`）
- 3.4 の Presenter の前置き（購読・`Dispose`・実行中フラグ・`disposed`・`playAnimation: false`・`using`）、`GachaPresenter` の `BackAsync`、`GachaRatesPresenter` の見出しの文と順（`GachaRatesDtoFactory` が使う）、`GachaResultPresenter` の行の文と `note`（`GachaResultDtoFactory` が使う）・`CloseAsync`・`RetryAsync`
- 4 章のタスクの「止まる条件」「境界」「Play の手順」と、各タスクの終わりの確かめ方（G7 の読み替えのうえで）
- 5 章の冒頭の偽の View の書き方（`private sealed class`、`CallLog` の共有、`[UnityTest]` + `UniTask.ToCoroutine`。**名前空間は `Uta.Client.Tests`**）、Play・手動の確認の項目
- 無効: 3.1〜3.3・3.5、5.1（名前空間 `Uta.Client.Infrastructure.Tests`・`internal` の `GachaFixtures`）、5.2 の表（G6 の表が正。`GachaResultPresenterTests` の 3 本は G6 の表が「本文のまま」と言うので本文の行を使う）

## 基盤 11d9b06 への読み替え（2026-11-08。[クライアントの移行計画](2026-11-08-client-foundation-migration.md)。この節が本文と食い違ったら、この節が正）

- `XxxApi`（`ApiServiceBase<…>` のラッパー。`GachaApi`・`SupportApi`・`ProduceApi`）は**作らない**。`UtaApiClient` が `public IGachaService Gacha { get; }`（`ISupportService Support`・`IProduceService Produce`）を持ち、`Create` の中で `core.CreateClient<I…Service>()` で作る。本文の「`ServiceClientFactory.Create<I…Service>(callInvoker)` で作って `new XxxApi(core, client)`」は「`core.CreateClient<I…Service>()`」に読み替える。private コンストラクターの引数の型は `XxxApi` ではなく `I…Service`
- ゲートウェイのコンストラクターは `I…Service` を受ける（`UtaApiClient` を受けない）。`BootstrapEntry` に `builder.Register<I…Service>(resolver => resolver.Resolve<UtaApiClient>().Xxx, Lifetime.Singleton);` を足す（`PlayerGateway` と同じ）
- 呼び方: `xxx.XxxAsync(req, handledCodes)` → `await xxx.WithHandledCodes(codes).XxxAsync(req).ToResult()`（`handledCodes` の無い RPC は `WithHandledCodes` を付けない）。`result.IsSuccess` → `!result.IsError`。`result.Error.Code.Equals(X)` → `result.ErrorCode == X`。`result.Error.ToString()`・`result.Error.Code.Reason` は今のまま使える
- 本文のファイル表の `Api/XxxApi.cs`（作成）の行は無効（作らない）。テストの偽物は、`PlayerGatewayTests` と同じく偽の `I…Service`（`IService<T>` の 5 メンバーを実装し、`WithHeaders` は `this` を返す）
- `[MagicOnionClientGeneration]` の型の並びに `typeof(I…Service)` を足す指示は今のまま有効（`Uta.Shared.Contracts` の同じアセンブリなので、足し忘れても `IPlayerService` で足りるが、本文どおり足す）
- DTO の ID の名前（`OwnedIdolDto.IdolId` など）は、[移行計画](2026-11-08-client-foundation-migration.md)の R9 の表に読み替える（2026-11-08）
- クライアントのエンティティ・リポジトリの形（`IdolEntity.IdolId` → `MasterId`、`SupportCardEntity.SupportCardUid`・`CardId` → `UniqueId`・`MasterId`、`PlayerEntity` の `UniqueId`、コンストラクターの先頭の `uniqueId`、`Find(string uniqueId)`、名前空間 `Uta.Client.Domain.Idol`・`…SupportCard`・`Uta.Client.Infrastructure.Idol`・`…SupportCard`）は、[移行計画](2026-11-08-client-foundation-migration.md)の R8 に読み替える（2026-11-08）

## この計画で決めたこと

ユーザーは、この節と次の「ユーザーの確認が要ること」だけを読めば判断できるように書く。

1. **ガチャの画面は 3 つ**（ガチャのページ・排出率のモーダル・結果のモーダル）。どれも **`Home` シーンの中で、ホームのページの上に積む**（シーンを変えない）。ホームの「ガチャ」でガチャのページを積み、「戻る」で自分のページを閉じてホームに戻る。ホームは `DidPopEnter` で所持データの表示をやり直す（foundation 3.7 のまま）
2. **C# で UI を組まない**。Prefab とその階層・View のフィールドは P2（[2026-11-07-client-prefabs-screens-a.md](2026-11-07-client-prefabs-screens-a.md)）の 3.2・3.3 が正。この計画は、P2 が作った骨格（`GachaPage`・`GachaRatesModal`・`GachaResultModal`・`GachaLineRow`。フィールドだけ）に、`IGachaView`・`IGachaRatesView`・`IGachaResultView` の実装（フィールドに値を入れる・`Awake` で `onClick` を購読してイベントを起こす）と、行の `GachaLineRow.SetText` を足すだけ。フィールドは足さない・名前を変えない
3. **一覧の行は行の Prefab から作る**: 排出率は `GachaRateLine.IsHeading` で `headingRowPrefab`（見出し）と `lineRowPrefab`（行）を選び、結果は `rowPrefab`。`UiObjects.ClearChildren(listContent)` のあと `UiObjects.AddRow` で作る（foundation 3.2）
4. **文字の組み立ての置き場**: 一覧の行の文（ラインナップ・排出率・結果）は Presenter が作る。通貨・交換pt・費用の数値の書式（`"ガチャ通貨 3,250"` など）はページが作る
5. **通信の層**: `IGachaPort`（Application）と `GachaGateway`（Infrastructure）。結果の型 `PortResult<T>` と reason の写し `ServerReasons` を Application の共通に置く（3 本目以降も使う）。画面ごとの文言で出す業務エラーは通貨不足だけ（`handledCodes` に入れる）。ほかのガチャの業務エラーは foundation が `UtaErrors` に `Stay` で登録済みなので、共通のダイアログが出て画面に残る
6. **抽選が成功したあとの取り直し（`RefreshPlayerUseCase`）の失敗は、成功として扱う**: 結果のモーダルを出し、通貨は `DrawResponse` の値で出す。追加のダイアログは出さない（foundation 3.3.5）
7. **排出率は、レア別・種別（育成アイドル／サポートカード）の合計の % を先に出し、そのあとカードごとの %**（[gacha.md](../wiki/systems/gacha.md) 3.1。重みを合計してから切り捨てる）
8. **結果はモーダル**。「もう一度」は Args の `Func<UniTask>`（ガチャのページの `DrawAsync`）で呼び戻す。モーダルを閉じてから、確認ダイアログからやり直す
9. **ホームの「ガチャ」**: foundation の `HomePresenter` のコンストラクター（2 本）の最後に `IScreenNavigator` を足し、`OpenGachaAsync` をガチャのページを積む処理にする。ほかの 2 つ（プロデュース・アイドル）は「準備中です。」のまま
10. **DI**: ゲートウェイとユースケースを `BootstrapEntry.RegisterUseCases`（ルートのスコープ）に 3 行足す。Presenter・ページは登録しない（基盤の `ScreenNavigator` が Push のたびに子スコープで解決する）。Addressables への登録は P2 の生成スクリプトが済ませている
11. テスト用の `FakeGameMaster`（`IGameMaster` の偽）はこの計画で作る（3 本目以降も使う）。所持データの取り直しの偽は foundation の `StubPlayerPort` を使う
12. **テストのダミー値は共通のヘルパー `GachaFixtures`（`Tests/EditMode/Support/`）に 1 か所で決める**: `IdolInfo`・`SupportCardInfo`・`GachaInfo`・`PlayerSnapshot`・`DrawResultDto` の、名前とレア度（DTO は枠・種別・ID・結果・かけら・強化pt）以外の引数の値はここだけに書く。ガチャのテストはこれを使い、これらの型を直接 `new` しない（5章）
13. **叩き台（[screen-wireframes.md](../wiki/overview/screen-wireframes.md) 9）との差を、次の 4 つにしてよいか**（[Uta案]）（ユーザーが推奨どおり了承した。2026-11-07）
   - ガチャ名の行は固定の「ガチャ」（マスタの `Gacha` に名前の列が無い）
   - ガチャの画像は出さない（MVP は文字だけ）
   - 結果はカードの絵のグリッドではなく、1 枠 1 行の文字
   - 「NEW」は育成アイドルを初めて得たときだけ。サポートカードは 2 枚目以降も「行が増えた」（`Outcome = New`。gacha.md 5章）なので、「NEW」ではなく「所持に追加」と出す
14. **結果の「もう一度」も、引く前の確認ダイアログ（消費量と所持）を出してよいか**。確認を省く案もあるが、通貨を使う操作なので毎回確かめる（ユーザーが推奨どおり了承した。2026-11-07）

## ユーザーの確認が要ること

なし（全件、2026-11-07 にユーザーが推奨どおり了承した。「この計画で決めたこと」の末尾に移した）。

## 1. 目的と前提

[game-spec.md](../wiki/overview/game-spec.md) の順 7 の C# の 2 本目。ガチャの画面（ガチャのページ・排出率のモーダル・結果のモーダル）の `IXxxView` の実装と Presenter、`IGachaService` を呼ぶクライアントの層（API・ゲートウェイ・ユースケース）を作り、ホームの「ガチャ」ボタンから開けるようにする。仕様は [gacha.md](../wiki/systems/gacha.md)（GAC-01・02・04。排出率の表示は 3.1、重複の扱いは 5章）、画面の叩き台は [screen-wireframes.md](../wiki/overview/screen-wireframes.md) 9、RPC は [api-design.md](../wiki/architecture/api-design.md) 1章と `shared/Uta.Shared.Contracts/Gacha/`、サーバー側は [gacha-support-server.md](../wiki/architecture/server/gacha-support-server.md) 1章。

- 方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章（Prefab・Addressables・TextMeshPro）と 8章（シーン構成）。C# の公開 API と書き方の正は [foundation の計画](2026-11-03-client-screens-foundation.md) の 3章（**3.3 の共通ルール**: 遷移・Presenter の形・View の形・Args・通信とエラー・文字の書式・テストの書き方と偽・DI の置き場）。この計画は 3.3 と違うことを書かない。書いていないことは 3.3 に従う
- Prefab・階層・View のフィールドの正は [P2 の計画](2026-11-07-client-prefabs-screens-a.md) の 3.2（階層）と **3.3（子のパス → View のフィールド → `IXxxView` のメンバー）**
- 関係する ADR: [0008](../wiki/decisions/0008-shared-domain-and-onion.md)（層。DTO の変換は Infrastructure に閉じる）、[0009](../wiki/decisions/0009-client-stack.md)（uGUI + UnityScreenNavigator・VContainer・UniTask）
- 計画の並び（[索引](2026-11-07-client-screens-index.md)）: 計画 0 → P1 → P2 → foundation → **この計画** → owned → P3 → formation → week → lesson
- **前提 1**: foundation が done。この計画が使うもの: `IScreenNavigator`（基盤。各シーンのスコープ）、`DialogService`・`DialogResult`、`UiObjects`、部品の View のメソッド（`ButtonView.Button`・`SetLabel`、`HeaderView.BackButton`）、`IGameMaster`（`GachaInfo`・`GachaEntryInfo`・`IdolInfo`・`SupportCardInfo`・`MasterItemKind`）、`PlayerCache`・`RefreshPlayerUseCase`、`HomePresenter`（3.7）、`BootstrapEntry.RegisterUseCases`、`UtaApiClient.Create`（3.8 で引数が増えた版）、`UtaErrors` の業務エラーの登録（`GachaErrors` の 5 つが `ErrorAction.Stay`）、テスト用の `CallLog`・`FakeScreenNavigator`・`FakeDialogService`・`StubPlayerPort`。始める前に `Scripts/Presentation/Common/UiObjects.cs`・`DialogService.cs` と `Tests/EditMode/Fakes/StubPlayerPort.cs` があることを確かめ、無ければ止まって報告する
- **前提 2**: P2 が done で、`Scripts/Presentation/Gacha/` に `GachaPage.cs`・`GachaRatesModal.cs`・`GachaResultModal.cs`・`GachaLineRow.cs`（P2 3.1 のフィールドだけの骨格）、`Assets/Prefabs/Screens/Gacha/` に `GachaPage.prefab`・`GachaRatesModal.prefab`・`GachaResultModal.prefab`（Addressables の `Screens` に、アドレス = ファイル名）、`Assets/Prefabs/Parts/` に `GachaRateHeadingRow.prefab`・`GachaRateRow.prefab`・`GachaResultRow.prefab` があること。始める前に確かめ、フィールドの名前・型が 3.4 の表と違えば止まって報告する（P2 が正。この計画を直してから続ける）
- **前提 3**: Unity 7（`7000.0.0a12`）の Editor が、この worktree の `client/uta` を開いていること（`unity status --until-ready`）。**Editor を開けない環境では、コンパイル・テスト・Play のどれも確かめられない**。そのときは「確認できていない」と書いてユーザーに実行を頼む（推測で「通った」と書かない）
- **後続が使うもの**（この計画で作る）: `PortResult<T>`・`ServerReasons`（Application の共通）と、テスト用の `FakeGameMaster`。3 本目（所持一覧・強化）以降はこれを使うので、**3 本目はこの計画の後に進める**（並行しない）。`ServerReasons` にはこの計画で使う reason だけを置き、ほかの reason はそれを使う計画が足す
- **決めないこと**: 交換（GAC-03。MVP の画面に入れない: [game-spec.md](../wiki/overview/game-spec.md)「画面」）。交換pt は「交換pt N / M」の表示だけで、「交換」ボタンは作らない。抽選履歴、ガチャの演出（MVP は演出なし: screen-wireframes.md 9）、複数のガチャの切り替え（MVP はガチャ 1 本。`master.Gachas[0]` だけを使う）。Prefab の階層・見た目の値（P2）

## 2. 触るファイル

パスは、`client/uta/Assets/` で始まらないものはリポジトリのルートからの相対、それ以外は `client/uta/Assets/` からの相対。新しい `.cs`・フォルダには、Unity の再コンパイルのあとに生成された `.meta` をコミットする。Prefab（`.prefab`）と Addressables の設定は触らない（P2 の生成物）。

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
| 作成 | `Scripts/Presentation/Gacha/IGachaView.cs`・`GachaPresenter.cs` | ガチャのページの IF と Presenter |
| 変更 | `Scripts/Presentation/Gacha/GachaPage.cs` | P2 の骨格に `IGachaView` の実装を足す |
| 作成 | `Scripts/Presentation/Gacha/GachaRatesArgs.cs`・`GachaRateLine.cs`・`IGachaRatesView.cs`・`GachaRatesPresenter.cs` | 排出率のモーダルの Args・行・IF・Presenter |
| 変更 | `Scripts/Presentation/Gacha/GachaRatesModal.cs` | P2 の骨格に `IGachaRatesView` の実装を足す |
| 作成 | `Scripts/Presentation/Gacha/GachaResultArgs.cs`・`IGachaResultView.cs`・`GachaResultPresenter.cs` | 結果のモーダルの Args・IF・Presenter |
| 変更 | `Scripts/Presentation/Gacha/GachaResultModal.cs` | P2 の骨格に `IGachaResultView` の実装を足す |
| 変更 | `Scripts/Presentation/Gacha/GachaLineRow.cs` | P2 の骨格に `SetText` を足す |
| 変更 | `Scripts/Presentation/Home/HomePresenter.cs` | コンストラクターに `IScreenNavigator` を足し、`OpenGachaAsync` をガチャのページを積む処理にする |
| 変更 | `Scripts/Bootstrap/BootstrapEntry.cs` | `RegisterUseCases` に 3 行足す |
| 作成 | `Tests/EditMode/Fakes/FakeGameMaster.cs` | `IGameMaster` のテスト用の実装（3 本目以降も使う） |
| 作成 | `Tests/EditMode/Support/GachaFixtures.cs` | ガチャのテストのダミー値（5.1） |
| 作成 | `Tests/EditMode/PortResultTests.cs`・`ServerReasonsTests.cs`・`GachaRateCalculatorTests.cs`・`DrawUseCaseTests.cs`・`GetGachaStatusUseCaseTests.cs`・`GachaGatewayTests.cs`・`GachaPresenterTests.cs`・`GachaRatesPresenterTests.cs`・`GachaResultPresenterTests.cs` | テスト（5章） |
| 変更 | `Tests/EditMode/HomePresenterTests.cs` | コンストラクターの引数と、ガチャのボタンのテストを直す（5章） |
| 変更 | `uta_docs/wiki/architecture/screen-design.md`・`unity7-evaluation.md` | 結果の記録（タスク 4） |

asmdef は変えない（`Uta.Client.Presentation`・テストの参照は P1・foundation 3.1 で足してある。`Uta.Client.Application` は `Uta.Shared.Domain`・`UniTask` だけで足りる）。

## 3. 型とシグネチャ

C# 9 の範囲・書式・名前空間の決め方は foundation 3章の冒頭のとおり。名前空間: `Application/Common` → `Uta.Client.Application.Common`、`Application/Gacha` → `Uta.Client.Application.Gacha`、`Infrastructure/Api` → `Uta.Client.Infrastructure.Api`、`Infrastructure/Gacha` → `Uta.Client.Infrastructure.Gacha`、`Presentation/Gacha` → `Uta.Client.Presentation.Gacha`。型の名前は `Uta.Shared.Domain` と衝突しないことを確かめてある（2026-11-03 に grep）。Application の `GachaRateRow`（型）と P2 の行の Prefab `GachaRateRow.prefab`（アセット。型ではない）は名前が同じだが、C# の型としては衝突しない。

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

- `UtaApiClient`: `public GachaApi Gacha { get; }` を足す。private のコンストラクターの最後に `GachaApi gacha` を足して `Gacha = gacha;`。`Create`（foundation 3.8 で `IApiActivityHandler` が増えた版）で、`playerClient` の次の行に `var gachaClient = ServiceClientFactory.Create<IGachaService>(callInvoker);` を足し、最後の `new UtaApiClient(..., new PlayerApi(core, playerClient))` を `new UtaApiClient(..., new PlayerApi(core, playerClient), new GachaApi(core, gachaClient))` にする。`using Uta.Shared.Contracts.Gacha;` を足す
- `UtaMagicOnionClientInitializer`: `[MagicOnionClientGeneration(typeof(IDeviceService), typeof(IPlayerService), typeof(IGachaService))]`（`using Uta.Shared.Contracts.Gacha;`）。生成は Unity の再コンパイルで行われる
- `GachaGateway` のフィールド: `private readonly ErrorCode[] drawHandled = { GachaErrors.NotEnoughCurrency };`（`PlayerGateway` の `notCreated` と同じ書き方）
- `GachaGateway.DrawAsync`: `gacha.DrawAsync(new DrawRequest(gachaId, count, expectedCost), drawHandled)`。`result.IsSuccess` なら `PortResult<DrawCallResult>.Success(ToDrawCallResult(result.Value))`。失敗で `result.Error.Code.Equals(GachaErrors.NotEnoughCurrency)` なら `BusinessError(result.Error.Code.Reason, result.Error.ToString())`、それ以外は `Failure(result.Error.ToString())`
  - `handledCodes` に入れるのは通貨不足だけ。`GachaErrors` のほかの 4 つ（`NotFound`・`InvalidCount`・`CostMismatch`・`NotEnoughPoints`）は foundation の `UtaErrors` に `Stay` で登録されているので、ApiCore の共通ダイアログが出て、画面に残り、`Failure` になる。`CostMismatch` はマスタとサーバーの食い違い（クライアントの不具合のときだけ起きる: gacha-support-server.md 1章）なので、画面ごとの文言を出さない
- `GachaGateway.GetGachasAsync`: `gacha.GetGachasAsync()`。成功なら `Success(ToGachaStatuses(result.Value))`、失敗なら `Failure(result.Error.ToString())`
- `ToGachaStatuses`: `response.Gachas` が null なら空のリスト。それ以外は `new GachaStatus(dto.GachaId, dto.ExchangePoints)` を同じ順で
- `ToDrawCallResult`: `response.Results` が null なら `Items` は空。それ以外は `DrawResultDto` ごとに `new DrawItem(dto.Slot, (MasterItemKind)(int)dto.ItemKind, dto.ItemId, dto.Outcome == GachaOutcome.New, dto.Shards, dto.SupportPoints)` を作り、`Slot` の昇順に並べ直す。`GachaCurrency`・`ExchangePoints` は応答の値
- `ApiResult` の成功・失敗はテストから作れない（`internal`。unity7-evaluation.md 3.3）ので、`GetGachasAsync`・`DrawAsync` の分岐は EditMode でテストしない。変換（`ToGachaStatuses`・`ToDrawCallResult`）だけをテストする

### 3.4 `Uta.Client.Presentation.Gacha`

#### View（P2 の骨格に足す）

View の形は foundation 3.3.3 のとおり: **`Awake` で各ボタンの `Button.onClick` を購読して C# の `event` を起こす**（解除は書かない）。`Set…` は P2 のフィールドに値を入れるだけ。フィールドは P2 3.1 のまま（足さない・名前を変えない）。どの子のどの部品かは P2 3.3 の表を見る。文字の大きさ・色は Prefab が決める。

使う P2 のフィールド（P2 3.3 の表の写し。食い違えば P2 が正）:

| View | フィールド（型） | 使うメンバー |
|---|---|---|
| `GachaPage` | `header`（`HeaderView`） | `BackClicked`（`header.BackButton.Button`） |
| | `currencyText`（`TMP_Text`） | `SetCurrency` |
| | `lineupText`（`TMP_Text`） | `SetLineup` |
| | `exchangeText`（`TMP_Text`） | `SetExchange` |
| | `ratesButton`（`ButtonView`） | `RatesClicked` |
| | `singleButton`・`tenButton`（`ButtonView`） | `SingleClicked`・`TenClicked`・`SetCosts` |
| `GachaRatesModal` | `listContent`（`RectTransform`） | `SetLines` |
| | `headingRowPrefab`・`lineRowPrefab`（`GachaLineRow`。アセット） | `SetLines` |
| | `closeButton`（`ButtonView`） | `CloseClicked` |
| `GachaResultModal` | `listContent`（`RectTransform`） | `SetLines` |
| | `rowPrefab`（`GachaLineRow`。アセット） | `SetLines` |
| | `retryButton`・`closeButton`（`ButtonView`） | `RetryClicked`・`CloseClicked` |
| `GachaLineRow` | `text`（`TMP_Text`） | `SetText` |

```csharp
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

public sealed class GachaPage : PageBase, IGachaView
{
    // フィールドは P2 の骨格のまま（header・currencyText・lineupText・exchangeText・ratesButton・singleButton・tenButton）
    public event Action SingleClicked;
    public event Action TenClicked;
    public event Action RatesClicked;
    public event Action BackClicked;
    private void Awake();   // 下
    // Set… は下
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

public sealed class GachaRatesModal : ModalBase, IGachaRatesView
{
    // フィールドは P2 の骨格のまま（listContent・headingRowPrefab・lineRowPrefab・closeButton）
    public event Action CloseClicked;
    private void Awake();
}

public interface IGachaResultView
{
    void SetLines(IReadOnlyList<string> lines);
    event Action CloseClicked;
    event Action RetryClicked;
}

public sealed class GachaResultModal : ModalBase, IGachaResultView
{
    // フィールドは P2 の骨格のまま（listContent・rowPrefab・retryButton・closeButton）
    public event Action CloseClicked;
    public event Action RetryClicked;
    private void Awake();
}

[DisallowMultipleComponent]
public sealed class GachaLineRow : MonoBehaviour
{
    // フィールドは P2 の骨格のまま（text）
    /// <summary>text.text = value。null は ""。</summary>
    public void SetText(string value);
}
```

ページ・モーダルの中身（`N0(x)` は `x.ToString("N0", CultureInfo.InvariantCulture)`。foundation 3.3.6）:

- `GachaPage.Awake`: `header.BackButton.Button.onClick.AddListener(() => BackClicked?.Invoke());`、`ratesButton.Button.onClick` → `RatesClicked`、`singleButton.Button.onClick` → `SingleClicked`、`tenButton.Button.onClick` → `TenClicked`（同じ書き方）
  - `SetCurrency(n)`: `currencyText.text = "ガチャ通貨 " + N0(n)`
  - `SetExchange(points, ceiling)`: `exchangeText.text = "交換pt " + (points.HasValue ? N0(points.Value) : "-") + " / " + N0(ceiling)`
  - `SetCosts(single, ten)`: `singleButton.SetLabel("単発 " + N0(single))`、`tenButton.SetLabel("10連 " + N0(ten))`
  - `SetLineup(lines)`: `lineupText.text = string.Join("\n", lines)`（空なら ""）
- `GachaRatesModal.Awake`: `closeButton.Button.onClick` → `CloseClicked`
  - `SetLines(lines)`: `UiObjects.ClearChildren(listContent);` のあと、i = 0 から順に `var row = UiObjects.AddRow(lines[i].IsHeading ? headingRowPrefab : lineRowPrefab, listContent, "Line" + i); row.SetText(lines[i].Text);`
- `GachaResultModal.Awake`: `retryButton.Button.onClick` → `RetryClicked`、`closeButton.Button.onClick` → `CloseClicked`
  - `SetLines(lines)`: `UiObjects.ClearChildren(listContent);` のあと、i = 0 から順に `UiObjects.AddRow(rowPrefab, listContent, "Line" + i).SetText(lines[i]);`
- `using`: `System`・`System.Collections.Generic`・`System.Globalization`（`GachaPage` だけ）・`UnityEngine`・`TMPro`（P2 の骨格にあるもの）・`Uta.Client.Presentation.Common`・`Uta.Client.Presentation.Common.Parts` のうち使うもの

#### Presenter・Args・文字

```csharp
internal static class GachaTexts
{
    /// <summary>Idol → "育成アイドル"、SupportCard → "サポートカード"、それ以外 → ArgumentOutOfRangeException。</summary>
    public static string Kind(MasterItemKind kind);
}

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

どの Presenter も foundation 3.3.2 の決まりに従う: 購読は `InitializeAsync` で、`Dispose` で全部解く。`Dispose` で `disposed = true`。イベントの受け口（`OnXxxClicked`）は同期で `XxxAsync().Forget()` を呼ぶだけ。公開の `XxxAsync`（`DrawAsync`・`OpenRatesAsync`・`BackAsync`・`CloseAsync`・`RetryAsync`）は、Presenter に 1 つの `private bool running;` を使い、先頭で `if (running) return;`、`running = true;`、本体を `try`、`finally` で `running = false;`。`await` のあとは View・ナビゲーターに触る前に `if (disposed) return;`。遷移はすべて `await` し、`playAnimation: false` を名前付きで渡す。レア度の文字は `rarity.ToString()`（"SSR"・"SR"・"R"・"N"）。`ISceneNavigator` は使わない（3.3.10 の別名は要らない）。`using UnityScreenNavigator;`・`using VContainer;`・`using Cysharp.Threading.Tasks;` を書く。

**`GachaPresenter`**
- フィールド: `GachaInfo gacha`（`InitializeAsync` で決める）、`int currency`（いま画面に出しているガチャ通貨）、`bool running`、`bool disposed`
- `InitializeAsync`（`async`）:
  1. `view` の 4 つのイベントを購読する（`SingleClicked` → `DrawAsync(1).Forget()`、`TenClicked` → `DrawAsync(10).Forget()`、`RatesClicked` → `OpenRatesAsync().Forget()`、`BackClicked` → `BackAsync().Forget()`。受け口は private の同期メソッド `OnSingleClicked` などにして、`Dispose` で同じものを `-=` する）
  2. `gacha = master.Gachas[0];`（MVP はガチャ 1 本。マスタの検証を通っているので空の場合は扱わない）
  3. `currency = cache.Current.GachaCurrency; view.SetCurrency(currency);`
  4. `view.SetCosts(gacha.SingleCost, gacha.TenCost);`
  5. `view.SetLineup(lines)`: `gacha.NormalEntries` のうち、レア度（`GetIdol`／`GetSupportCard` の `Rarity`）が `Rarity.SSR` のものを、種別の昇順（育成アイドル → サポートカード）→ `ItemId` の昇順に並べ、1 行ずつ `$"SSR {GachaTexts.Kind(kind)}: {name}"`。同じ (種別, ID) が 2 回あれば 1 行にする。SSR が無ければ空のリスト
  6. `var points = await status.ExecuteAsync(gacha.GachaId); if (disposed) return; view.SetExchange(points, gacha.CeilingPoints);`（失敗は null のまま渡す。ダイアログは ApiCore が出している。`InitializeAsync` が終わるまでページは出ない）
- `DrawAsync(int count)`（実行中フラグの中）:
  1. `var cost = count == 1 ? gacha.SingleCost : gacha.TenCost;`
  2. `var answer = await dialogs.ConfirmAsync($"ガチャ通貨 {N0(cost)} を使って {count} 回引きます。（所持 {N0(currency)}）", "引く", "やめる");` `if (disposed) return;` `Negative` なら終わる
  3. `var outcome = await draw.ExecuteAsync(gacha, count); if (disposed) return;`
  4. `Succeeded`: `currency = outcome.Result.GachaCurrency; view.SetCurrency(currency); view.SetExchange(outcome.Result.ExchangePoints, gacha.CeilingPoints);` → `await navigator.PushModalAsync<GachaResultPresenter, GachaResultArgs>(new GachaResultArgs(outcome.Result.Items, () => DrawAsync(count)), playAnimation: false);`。`RefreshFailed` でも同じ（通貨は応答の値で出すので、キャッシュが古くても画面は正しい。追加のダイアログは出さない。ホームに戻ったときの表示は `PlayerCache` なので、次に取り直すまで古いまま: foundation 3.3.5）
  5. `NotEnoughCurrency`: `await dialogs.ShowInfoAsync("ガチャ通貨が足りません。");`
  6. `Failed`: 何もしない（ApiCore が共通のダイアログを出している）
  - `catch` は書かない（ユースケースは通信の失敗を結果で返す。想定外の例外は `Forget` の未処理の例外としてログに出る）。`finally` で `running = false`
  - 「もう一度」: 結果のモーダルが閉じたあと `Retry` を呼ぶので、そのときこの Presenter の `running` は false に戻っている（`PushModalAsync` はモーダルが積まれた時点で返り、`finally` を通る）。2 回目も確認ダイアログから始まる
- `OpenRatesAsync`（実行中フラグの中）: `await navigator.PushModalAsync<GachaRatesPresenter, GachaRatesArgs>(new GachaRatesArgs(gacha.GachaId), playAnimation: false);`
- `BackAsync`（実行中フラグの中）: `await navigator.PopPageAsync(this, playAnimation: false);`（下のホームのページは `HomePresenter.DidPopEnter` で表示をやり直す）
- `Dispose`: `disposed = true`、4 つの購読を解く

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
- `Dispose`: `disposed = true`、購読を解く

**`GachaResultPresenter`**
- `InitializeAsync`: `CloseClicked`・`RetryClicked` を購読する。`args.Items` を 1 つずつ 1 行の文字列にして `view.SetLines(lines)`。`UniTask.CompletedTask` を返す
  - 行: `$"{item.Slot}. {rarity} {GachaTexts.Kind(item.Kind)} {name}　{note}"`（`name` の後は全角の空白 1 つ）。`rarity`・`name` は `GetIdol`／`GetSupportCard`
  - `note`: 育成アイドルで `IsNew` → `"NEW"`、育成アイドルで `!IsNew` → `$"かけら +{N0(Shards)}"`、サポートカードで `IsNew` → `"所持に追加"`、サポートカードで `!IsNew` → `$"サポート強化pt +{N0(SupportPoints)}"`
- `CloseAsync`（実行中フラグの中）: `await navigator.PopModalAsync(this, playAnimation: false);`
- `RetryAsync`（実行中フラグの中）: `await navigator.PopModalAsync(this, playAnimation: false);` のあと、`args.Retry` が null でなければ `await args.Retry();`。**Pop のあとは `disposed` を見ない**（この Presenter は閉じて破棄されているが、`Retry` はガチャのページの Presenter の処理で、この Presenter の View に触らない）
- `Dispose`: `disposed = true`、2 つの購読を解く

### 3.5 ホームと Bootstrap

- `HomePresenter`（foundation 3.7）: 2 本のコンストラクターの最後に `IScreenNavigator navigator` を足す（`[Inject]` の側は `HomePresenter(HomePage view, PlayerCache cache, DialogService dialogs, IGameMaster master, IScreenNavigator navigator) : this((IHomeView)view, view.CreateStage(master), cache, dialogs, master, navigator)`、テスト用は `HomePresenter(IHomeView view, ICharacterStage stage, PlayerCache cache, DialogService dialogs, IGameMaster master, IScreenNavigator navigator)`。foundation 3.7。2026-11-07 にホームのキャラ表示のステージが足された）。フィールド `private readonly IScreenNavigator navigator;`。`OpenGachaAsync` の本体（実行中フラグの `try` の中）の `await dialogs.ShowInfoAsync("準備中です。");` を `await navigator.PushPageAsync<GachaPresenter>(playAnimation: false);` に置き換える（戻り値は使わない。`stack` は既定の true）。`using Uta.Client.Presentation.Gacha;` を足す（`using UnityScreenNavigator;` は foundation の時点である）。ほかの 2 つのメソッド（`OpenProduceAsync`・`OpenOwnedAsync`）は変えない（「準備中です。」のまま）。ガチャのページは `Home` シーンのスコープの `IScreenNavigator` で積まれ、`Home` の `UiRoot` の `PageContainer` に入る
- `BootstrapEntry.RegisterUseCases`（foundation 3.8）の最後（`RefreshPlayerUseCase` の次）に、この順で 3 行足す:
  ```csharp
  builder.Register<GachaGateway>(Lifetime.Singleton).As<IGachaPort>();
  builder.Register<GetGachaStatusUseCase>(Lifetime.Singleton);
  builder.Register<DrawUseCase>(Lifetime.Singleton);
  ```
  `using Uta.Client.Application.Gacha;`・`using Uta.Client.Infrastructure.Gacha;` を足す。Presenter・ページは登録しない（foundation 3.3.9）。`UtaApiClient` はルートに登録済みなので、`GachaGateway` のコンストラクターに渡る

## 4. タスク

各タスクの終わりに、`unity recompile --format json`（`client/uta` で実行。コンパイルエラー 0）と、EditMode のテストで既存とそのタスクまでのテストが全部通る状態にする（回し方は P1 の決めたこと 12: Editor を開いたまま `TestRunnerApi` を eval で回す。コマンドは CLAUDE.md「ビルド・テスト・実行」・unity7-evaluation 4章）。**eval で回せなければ、Editor の Test Runner（EditMode）を手動で回してもらい、確認できていないと報告する。止まらない**（P1 の決めたこと 12）。**Editor を開けない環境では、通ったとは書かず、確かめていないと報告する**。コミットの前に `unity command eval 'Uta.Client.Editor.Prefabs.FontAssets.ClearDynamicData(); return "ok";'`（P1 の `FontAssets`。Play で日本語を出すとフォントのアセットに字が足されるため）。

### タスク 1: Application の共通の型・ガチャの型・排出率、テスト用の `FakeGameMaster`

前提の確認: 1章の前提 1（foundation の型と偽）。無ければ止まって報告する。

触る: `Application/Common/*`、`Application/Gacha/` の `GachaStatus`・`DrawItem`・`DrawCallResult`・`IGachaPort`・`GachaRateGroup`・`GachaRateRow`・`GachaRates`・`GachaRateCalculator`、`Tests/EditMode/Fakes/FakeGameMaster.cs`、`Tests/EditMode/Support/GachaFixtures.cs`、`Tests/EditMode/PortResultTests.cs`・`ServerReasonsTests.cs`・`GachaRateCalculatorTests.cs`。

手順:
1. 3.1・3.2 のとおり作る
2. `FakeGameMaster : IGameMaster`（名前空間 `Uta.Client.Infrastructure.Tests`）を作る。public フィールド: `List<IdolInfo> IdolList`、`List<SupportCardInfo> SupportCardList`、`List<GachaInfo> GachaList`、`List<ScenarioInfo> ScenarioList`（4 つとも `new List<…>()` で初期化）、`Dictionary<int, List<SupportAbilityInfo>> Abilities`、`Dictionary<int, CardTextInfo> CardTexts`（どちらも空で初期化）、`Func<Rarity, int, int> MaxLevel`、`Func<Rarity, int> MaxLimitBreak`、`Func<Rarity, int, int, SupportLevelUpCost> LevelUpCost`（3 つとも null）
   - `Idols`・`SupportCards`・`Gachas`・`Scenarios` はリストをそのまま返す（並べ替えない。テストが順に入れる）
   - `GetIdol`・`GetSupportCard`・`GetGacha`・`GetScenario` はリストから ID で探し、無ければ `KeyNotFoundException`
   - `GetSupportAbilities(id)`: `SupportCardList` に無ければ `KeyNotFoundException`。`Abilities` にあればそのリスト、無ければ空のリスト
   - `GetCardText(id)`: `CardTexts` に無ければ `KeyNotFoundException`
   - `GetSupportMaxLevel`・`GetSupportMaxLimitBreak`・`GetSupportLevelUpCost` は対応する `Func` を呼ぶ。null なら `NotImplementedException`
3. 5.1 のとおり `GachaFixtures` を作る
4. 5章の `PortResultTests`・`ServerReasonsTests`・`GachaRateCalculatorTests` を作る

境界: 重み 0 のエントリーも 0 で出す。`entries` が空・合計 0 なら空。マスタに無い ID は `KeyNotFoundException`。

確かめ方: コンパイルエラー 0。上の 3 つのテストと既存のテストが通る。

### タスク 2: ユースケース・API・ゲートウェイ・DI

触る: `Application/Gacha/GetGachaStatusUseCase.cs`・`DrawStatus.cs`・`DrawOutcome.cs`・`DrawUseCase.cs`、`Infrastructure/Api/GachaApi.cs`・`UtaApiClient.cs`・`UtaMagicOnionClientInitializer.cs`、`Infrastructure/Gacha/GachaGateway.cs`、`Bootstrap/BootstrapEntry.cs`（3.5 の `RegisterUseCases`）、`Tests/EditMode/DrawUseCaseTests.cs`・`GetGachaStatusUseCaseTests.cs`・`GachaGatewayTests.cs`。

手順: 3.2・3.3・3.5（`BootstrapEntry`）のとおり作る・直す。5章のテストを作る。

境界: `count` が 1・10 以外は `ArgumentOutOfRangeException`。キャッシュの通貨が足りなければ通信しない。取り直しの失敗は成功として返す（`RefreshFailed = true`）。

確かめ方: コンパイルエラー 0。上の 3 つのテストと既存のテストが通る。`unity recompile` のあと `unity command console get --tail 50 --level error --format json` でエラーが無いこと（MagicOnion の生成物は Source Generator が作るので、ファイルとしては残らない。`IGachaService` のクライアントが生成されなければ `ServiceClientFactory.Create<IGachaService>` の解決で失敗するので、タスク 3a の Play で「ガチャ」を開いて確かめる）。

### タスク 3a: ガチャのページとホームの接続

前提の確認: 1章の前提 2（P2 の骨格と Prefab。フィールドが 3.4 の表と同じ）。違えば止まって報告する。

触る: `Presentation/Gacha/` の `GachaTexts.cs`・`IGachaView.cs`・`GachaPresenter.cs`（作成）、`GachaPage.cs`（P2 の骨格に足す）、`GachaRatesArgs.cs`・`GachaRateLine.cs`・`IGachaRatesView.cs`・`GachaResultArgs.cs`・`IGachaResultView.cs`（作成。3.4 のとおりの完成形）、`GachaRatesPresenter.cs`・`GachaResultPresenter.cs`（作成。**仮**）、`Presentation/Home/HomePresenter.cs`、`Tests/EditMode/GachaPresenterTests.cs`・`HomePresenterTests.cs`。

手順:
1. 3.4 の `GachaTexts`・`IGachaView`・`GachaRatesArgs`・`GachaRateLine`・`IGachaRatesView`・`GachaResultArgs`・`IGachaResultView` を作る
2. `GachaPage` に 3.4 の「View」の `GachaPage` の実装を足す（P2 のフィールドは変えない）
3. `GachaPresenter` が積むモーダルの型を使うので、**3.4 の属性・インターフェースだけを持つ仮の `GachaRatesPresenter`・`GachaResultPresenter`** を作る（`[AssetAddress(nameof(GachaRatesModal))]`／`[AssetAddress(nameof(GachaResultModal))]`、`IScreenWithArgs<GachaRatesArgs>`／`IScreenWithArgs<GachaResultArgs>`、`InitializeAsync`・`CloseAsync`・`RetryAsync` は `UniTask.CompletedTask`、`Dispose` は空）。**コンストラクターは `[Inject]` 側の 1 本（具象の View を受ける: `[Inject] public GachaRatesPresenter(GachaRatesModal view, GachaRatesArgs args, IGameMaster master, IScreenNavigator navigator) { }`／`[Inject] public GachaResultPresenter(GachaResultModal view, GachaResultArgs args, IGameMaster master, IScreenNavigator navigator) { }`）だけで、本体は空**にする。3a の時点では `GachaRatesModal`・`GachaResultModal` がまだ `IGachaRatesView`／`IGachaResultView` を実装していないので、`: this((IGachaRatesView)view, ...)` は CS0030 になるため。**インターフェースを受けるコンストラクターと、`[Inject]` 側からの `: this(...)` はタスク 3b で足す**。中身はタスク 3b
4. 3.4 のとおり `GachaPresenter` を作る
5. 3.5 のとおり `HomePresenter` を直す
6. 5章の `GachaPresenterTests` を作り、`HomePresenterTests` を直す。`unity recompile` → EditMode のテスト（P2 の `GachaPrefabTests`・P1 の `PrefabContractTests` も通り続けること）
7. Play で確かめる（ローカルのサーバーを起動し、初期付与済みのプレイヤーで。CLAUDE.md「ローカルのサーバー」）。式の中の `Q` は `UnityEngine.Object.FindFirstObjectByType`、`B(name)` は `UnityEngine.GameObject.Find(name).GetComponent<UnityEngine.UI.Button>().onClick.Invoke()` の略で、実際には略さずに書く。遷移・`Destroy` は次のフレーム以降に反映されるので、期待と違えば 1 秒おいてもう一度評価する
   1. `unity command editor.playmode start` → foundation のタスク 4 の手順 5 の 2・3 と同じ操作でホームまで進める（`Home:1:True`）
   2. `unity command eval 'B("NavGachaButton"); return "ok";'` → `unity command eval 'return Q<Uta.Client.Presentation.Common.UiRoot>().Pages.Pages.Count + ":" + (Q<Uta.Client.Presentation.Gacha.GachaPage>() != null);'` が `2:True`。`unity command console get --tail 50 --level error --format json` にエラーが無い（`IGachaService` の `GetGachasAsync` が通った）
   3. `unity command eval 'B("BackButton"); return "ok";'` → 2 の式が `1:False`（ガチャのページが閉じた）。`unity command console get --tail 50 --level error --format json` にエラーが無い
   4. `unity command editor.playmode stop`

止まる条件（当たったら、その時点のコンソールの出力を添えて止まって報告する。別の手段へ変えない）:
- G2: 子スコープで `GachaPresenter` が解決できない（VContainer の例外）
- G3: `IGachaService` のクライアントが生成されていない（`ServiceClientFactory.Create<IGachaService>` の例外）

境界: ボタンの連打は実行中フラグで止まる。通信中・遷移の後に破棄された Presenter は View に触らない。

確かめ方: コンパイルエラー 0。`GachaPresenterTests`・`HomePresenterTests` と既存のテストが通る。手順 7 の値が期待どおり。

### タスク 3b: 排出率・結果のモーダル

触る: `Presentation/Gacha/` の `GachaRatesModal.cs`・`GachaResultModal.cs`・`GachaLineRow.cs`（P2 の骨格に足す）、`GachaRatesPresenter.cs`・`GachaResultPresenter.cs`（仮を置き換える）、`Tests/EditMode/GachaRatesPresenterTests.cs`・`GachaResultPresenterTests.cs`。

手順:
1. `GachaLineRow`・`GachaRatesModal`・`GachaResultModal` に 3.4 の「View」の実装を足す（P2 のフィールドは変えない）
2. 3.4 のとおり `GachaRatesPresenter`・`GachaResultPresenter` を書く（タスク 3a の仮を置き換える。`[Inject]` 側のコンストラクターのシグネチャは変えず、`: this(...)` とインターフェースを受けるコンストラクターをここで足す。手順 1 で View がインターフェースを実装するので、この順で CS0030 にならない）
3. 5章の `GachaRatesPresenterTests`・`GachaResultPresenterTests` を作る。`unity recompile` → EditMode のテスト（`GachaPresenterTests`・P2 の `GachaPrefabTests`・P1 の `PrefabContractTests` も通り続けること）
4. Play で確かめる（式の略し方・待ち方はタスク 3a の手順 7 と同じ）
   1. タスク 3a の手順 7 の 1・2 でガチャのページまで進める
   2. `unity command eval 'B("RatesButton"); return "ok";'` → `unity command eval 'var m = Q<Uta.Client.Presentation.Gacha.GachaRatesModal>(); return (m != null) + ":" + (m == null ? 0 : m.GetComponentsInChildren<Uta.Client.Presentation.Gacha.GachaLineRow>().Length);'` が `True:` のあとに 1 以上の数（見出し 4 行 + 率の行）→ `unity command eval 'B("CloseButton"); return "ok";'` → `unity command eval 'return (Q<Uta.Client.Presentation.Gacha.GachaRatesModal>() == null).ToString();'` が `True`
   3. `unity command eval 'B("SingleButton"); return (UnityEngine.GameObject.Find("Dialog") != null).ToString();'` が `True`（確認ダイアログ）→ `unity command eval 'B("Button0"); return "ok";'`（「引く」）→ `unity command eval 'var m = Q<Uta.Client.Presentation.Gacha.GachaResultModal>(); return (m != null) + ":" + (m == null ? 0 : m.GetComponentsInChildren<Uta.Client.Presentation.Gacha.GachaLineRow>().Length);'` が `True:1` → `unity command eval 'B("CloseButton"); return "ok";'` → 結果のモーダルが無くなる（2 と同じ式を `GachaResultModal` で `True`）
   4. タスク 3a の手順 7 の 3・4（ページを閉じて Play を止める）。見た目と残りの操作は 5章の手動の確認で見る

止まる条件（当たったら、その時点のコンソールの出力を添えて止まって報告する。別の手段へ変えない）:
- G1: 手順 4 の 2 または 3 でモーダルが積まれない（`ModalContainer`・USN の `DefaultModalBackdrop`・Addressables の読み込みのどれかで例外。モーダルを出すのはこの計画が最初）
- G2: 子スコープで `GachaRatesPresenter`・`GachaResultPresenter` が解決できない（VContainer の例外。Args の具象型が子スコープに入らない、など）

境界: 結果のモーダルの「もう一度」は、モーダルを閉じてから確認ダイアログを出す。

確かめ方: コンパイルエラー 0。`GachaRatesPresenterTests`・`GachaResultPresenterTests` と既存のテストが通る。手順 4 の値が期待どおり。

### タスク 4: 結果の記録

1. [screen-design.md](../wiki/architecture/screen-design.md)（frontmatter の `updated` を実施日に）:
   - 2章の表の「ガチャ（結果）」の行の「ページ／モーダル」を「モーダル」にする
   - 7.5 の「所持データのキャッシュ」の項の最後に「RPC が成功したあとの取り直しが失敗しても、RPC の結果は捨てずに出す（ガチャの結果のモーダルを出し、通貨は `DrawResponse` の値で出す）。キャッシュは次に取り直すまで古いまま」を足す（foundation のタスク 6 が同じ意味の文を足していれば足さない）
   - 「実装メモ」に、「この計画で決めたこと」13 の叩き台との差 4 つと、実装で設計から変えたこと（なければ「順 7 の 2 本目: 叩き台との差のほかは、なし」）を書く
2. [unity7-evaluation.md](../wiki/architecture/unity7-evaluation.md):
   - 2章の表に、Editor の Play で確かめたものを 1 行足す（`IGachaService` の `GetGachasAsync`・`DrawAsync` を `ApiCore` 経由で呼び、`DrawResultDto[]` を受け取れた。基盤の `ScreenNavigator` で Addressables のモーダル（`ModalBase` + `DefaultModalBackdrop`）を積んで閉じられた。確かめた日付）。確かめられなかったものは 3.3 に書く
   - 3.3 に「Windows の IL2CPP のプレイヤーでの `IGachaService`（`DrawResponse`・`DrawResultDto[]`）の送受信は未確認（順 7 の 6 本目の通しのビルドで確かめる）」を足す。foundation が 3.3 に「モーダルは 2 本目で確かめる」と書いていれば、確かめた結果に直す
3. `python -I .claude/skills/linting-wiki/lint_wiki.py` を回し、エラーが 0 であること
4. 全部のタスクが終わったら、この計画の `status` を `done` にする

## 5. テスト

置き場: `Tests/EditMode/`。名前空間は `Uta.Client.Infrastructure.Tests`。書き方は foundation 3.3.7（偽の View はテストのファイルの中の `private sealed class`、`CallLog` を `FakeScreenNavigator`・`FakeDialogService`・偽の View・`FakeGachaPort` で共有し、`log.Entries` の並びで確かめる。非同期は `[UnityTest]` + `UniTask.ToCoroutine` で Presenter の公開の `UniTask` メソッドを `await` する）。View（`GachaPage`・`GachaRatesModal`・`GachaResultModal`・`GachaLineRow`）の EditMode のテストは書かない（フィールドと子の対応は P2 の `GachaPrefabTests`、参照の欠けは P1 の `PrefabContractTests` が見る。表示は Play で見る。foundation 3.3.7）。

テストのファイルの中に書く偽（どれも `private sealed class`。返す `UniTask` は、指定が無ければ完了済み）:
- `FakeGachaPort : IGachaPort`: コンストラクター `FakeGachaPort(CallLog log)`（`log` は null を許す）。public フィールド `PortResult<IReadOnlyList<GachaStatus>> GachasResult`（既定は `Success` の空のリスト）、`PortResult<DrawCallResult> DrawResult`、`UniTaskCompletionSource DrawGate`（null でなければ `DrawAsync` はこれを待ってから返す）、`List<string> DrawCalls`（`DrawAsync` が呼ばれるたびに `$"{gachaId},{count},{expectedCost}"` を足す）。`log` が null でなければ `DrawAsync` の入口で `"Draw:" + count` も記録する
- 取り直しの偽は foundation の `StubPlayerPort`（`Fakes/`）を使い、`new RefreshPlayerUseCase(stubPlayerPort, cache)` で作る。取り直しを呼んだかは、`SignInResults` に入れた件数が減ったかで見る（`StubPlayerPort` は数を数えないため）
- 偽の View: 記録の書式は foundation 3.3.7（`"<メソッド名>:<引数を , でつないだもの>"`）。`int?` の null は `null`、リストは要素を `/` でつなぐ（`SetLineup`・`IGachaResultView.SetLines`）。`IGachaRatesView.SetLines` は `"SetLines:" + 行数` を記録し、渡されたリストを public フィールド `LastLines` に入れる。イベントを起こす `RaiseSingle()`・`RaiseTen()`・`RaiseRates()`・`RaiseBack()`・`RaiseClose()`・`RaiseRetry()` を持つ

### 5.1 `GachaFixtures`（テストのダミー値）

`Tests/EditMode/Support/GachaFixtures.cs`（名前空間 `Uta.Client.Infrastructure.Tests`。タスク 1 で作る）。**名前とレア度（DTO は枠・種別・ID・結果・かけら・強化pt）以外の引数の値は、ここだけで決める**。ガチャのテスト（`GachaRateCalculatorTests`・`DrawUseCaseTests`・`GetGachaStatusUseCaseTests`・`GachaGatewayTests`・`GachaPresenterTests`・`GachaRatesPresenterTests`・`GachaResultPresenterTests`。`PortResultTests`・`ServerReasonsTests` はマスタも DTO も使わない）は、`IdolInfo`・`SupportCardInfo`・`GachaEntryInfo`・`GachaInfo`・`PlayerSnapshot`・`DrawResultDto` を直接 `new` せず、これを呼ぶ。

```csharp
using System.Collections.Generic;
using Uta.Client.Application.Master;
using Uta.Client.Application.Player;
using Uta.Shared.Contracts.Gacha;
using Uta.Shared.Domain.Lesson;    // Rarity・Plan・ParamKind
using Uta.Shared.Domain.Produce;   // SupportType

namespace Uta.Client.Infrastructure.Tests
{
    internal static class GachaFixtures
    {
        public const int GachaId = 1;
        public const int SingleCost = 250;
        public const int TenCost = 2500;
        public const int CeilingPoints = 200;
        public const int Currency = 3250;   // PlayerCache に入れておく既定の所持

        /// <summary>new IdolInfo(idolId, name, rarity, Plan.Sense, 30, 50, 50, 50, ParamKind.Singing, ParamKind.Rhythm, ParamKind.Expression)。</summary>
        public static IdolInfo Idol(int idolId, string name, Rarity rarity);

        /// <summary>new SupportCardInfo(supportCardId, name, rarity, SupportType.Singing, Plan.Free)。</summary>
        public static SupportCardInfo SupportCard(int supportCardId, string name, Rarity rarity);

        /// <summary>new GachaEntryInfo(MasterItemKind.Idol, idolId, weight)。</summary>
        public static GachaEntryInfo IdolEntry(int idolId, int weight);

        /// <summary>new GachaEntryInfo(MasterItemKind.SupportCard, supportCardId, weight)。</summary>
        public static GachaEntryInfo SupportCardEntry(int supportCardId, int weight);

        /// <summary>new GachaInfo(GachaId, SingleCost, TenCost, CeilingPoints, normalEntries, guaranteedEntries)。</summary>
        public static GachaInfo Gacha(IReadOnlyList<GachaEntryInfo> normalEntries, IReadOnlyList<GachaEntryInfo> guaranteedEntries);

        /// <summary>
        /// 下の「共通の前提」のマスタを入れた新しい FakeGameMaster（呼ぶたびに新しいインスタンス。テストが足してよい）。
        /// IdolList: Idol(1, "アイドルA", SSR)・Idol(3, "アイドルC", R)。SupportCardList: SupportCard(2, "サポートB", SSR)・SupportCard(4, "サポートD", SR)。
        /// GachaList: Gacha(通常枠 = IdolEntry(1, 1)・SupportCardEntry(2, 1)・IdolEntry(3, 8)、確定枠 = SupportCardEntry(4, 3)・IdolEntry(1, 1))。
        /// </summary>
        public static FakeGameMaster CreateMaster();

        /// <summary>new PlayerSnapshot(1, 0, 0, 0, gachaCurrency, false, false, new OwnedIdol[0], new OwnedSupportCard[0])。</summary>
        public static PlayerSnapshot Snapshot(int gachaCurrency);

        /// <summary>new DrawResultDto(slot, ItemKind.Idol, idolId, outcome, shards, 0, null)。</summary>
        public static DrawResultDto IdolResult(int slot, int idolId, GachaOutcome outcome, int shards);

        /// <summary>new DrawResultDto(slot, ItemKind.SupportCard, supportCardId, outcome, 0, supportPoints, "uid" + slot)。</summary>
        public static DrawResultDto SupportCardResult(int slot, int supportCardId, GachaOutcome outcome, int supportPoints);
    }
}
```

- 本体は 1 行の式（`=>`）で、`<summary>` に書いた `new` のとおり。値（Plan.Sense・30・50 など）はテストの期待に出てこない（ガチャの画面は名前・レア度・種別しか出さない）ので、変えてもテストは変わらない
- `DrawItem`・`DrawCallResult`・`GachaStatus`（Application の型で、引数がどれも期待に出る値）は、テストが直接 `new` してよい

### 5.2 共通の前提と表

マスタ（`FakeGameMaster`）の共通の前提（ガチャのテストで使う）: `GachaFixtures.CreateMaster()`。中身は育成アイドル 1「アイドルA」SSR、3「アイドルC」R、サポートカード 2「サポートB」SSR、4「サポートD」SR。ガチャ 1: `SingleCost = 250`、`TenCost = 2500`、`CeilingPoints = 200`、通常枠 = (Idol 1, 重み 1)・(SupportCard 2, 1)・(Idol 3, 8)、確定枠 = (SupportCard 4, 3)・(Idol 1, 1)。`PlayerCache` には `GachaFixtures.Snapshot(GachaFixtures.Currency)`（`GachaCurrency = 3250`）を `Set` しておく。**`StubPlayerPort.Snapshot`（取り直しの成功で `PlayerCache` に入る値）の既定は `GachaCurrency = 0`**（foundation 3.3.8）。取り直しのあとにもう一度引くテストは、これを差し替える（`ResultRetry_DrawsAgain`）。

| ファイル | テスト | 内容 |
|---|---|---|
| `PortResultTests.cs` | `Success_HoldsValue` | `Succeeded`、`Value`、`Reason == null`、`IsBusinessError == false` |
| | `BusinessError_SetsReason` | `!Succeeded`、`Reason == "R"`、`IsBusinessError`、`ErrorText == "t"` |
| | `BusinessError_NullOrEmptyReason_Throws` | null と "" のどちらも `ArgumentException` |
| | `Failure_IsNotBusinessError` | `!Succeeded`、`Reason == null`、`!IsBusinessError`。`Failure(null)` の `ErrorText == ""` |
| `ServerReasonsTests.cs` | `Reasons_MatchContractsErrorCodes` | `ServerReasons.GachaNotEnoughCurrency == GachaErrors.NotEnoughCurrency.Reason`（後続の計画は、足した定数の行をここに足す） |
| `GachaRateCalculatorTests.cs` | `Calculate_RowsToBasisPoints` | 共通の通常枠 → `Rows` が 1000・1000・8000 |
| | `Calculate_GroupsSumWeightsBeforeTruncating` | `IdolList` に `GachaFixtures.Idol(5, "アイドルE", Rarity.SSR)` を足し、エントリー (Idol 1, 1)・(Idol 5, 1)・(Idol 3, 5)（合計 7）→ SSR 育成アイドルの `Groups` が 2857（行ごとの 1428 + 1428 = 2856 ではない） |
| | `Calculate_SortedByRarityDescThenKindThenId` | 共通の通常枠と確定枠 → `Groups` が SSR 育成アイドル・SSR サポートカード・R 育成アイドル の順、確定枠の `Rows` が Idol 1 → SupportCard 4 の順 |
| | `Calculate_ZeroWeightRow_Included` | 重み 0 の行が `BasisPoints == 0` で出る |
| | `Calculate_EmptyOrZeroTotal_ReturnsEmpty` | 空のリストと、重みが全部 0 → `Groups`・`Rows` とも空 |
| | `Calculate_UnknownId_Throws` | `KeyNotFoundException` |
| | `FormatPercent_Formats` | 0 → "0.00%"、5 → "0.05%"、1234 → "12.34%"、10000 → "100.00%" |
| `DrawUseCaseTests.cs` | `Execute_Success_RefreshesPlayerAndReturnsResult` | `DrawResult` が成功、`StubPlayerPort.SignInResults` に新しいスナップショットの `Success` を 1 つ → `Succeeded`、`RefreshFailed == false`、`PlayerCache.Current` が差し替わる、`SignInResults.Count == 0` |
| | `Execute_Success_RefreshFails_ReturnsSuccessWithRefreshFailed` | `SignInResults` に `PlayerSignInResult.Failure("x")` → `Succeeded`、`RefreshFailed == true`、`Result` は応答のまま、`PlayerCache.Current` は元のまま |
| | `Execute_NotEnoughCurrencyInCache_DoesNotCallPort` | 通貨 100 → `NotEnoughCurrency`、`DrawCalls` が空 |
| | `Execute_BusinessErrorNotEnoughCurrency_ReturnsNotEnoughCurrency` | `BusinessError(ServerReasons.GachaNotEnoughCurrency, "e")`、`SignInResults` に `Success` を 1 つ → `NotEnoughCurrency`、`SignInResults.Count == 1`（取り直していない） |
| | `Execute_OtherBusinessError_ReturnsFailed` | `BusinessError("OTHER", "e")` → `Failed` |
| | `Execute_Failure_ReturnsFailed` | `Failure("e")`、`SignInResults` に `Success` を 1 つ → `Failed`、`SignInResults.Count == 1` |
| | `Execute_InvalidCount_Throws` | 2 → `ArgumentOutOfRangeException` |
| | `Execute_PassesExpectedCost` | 単発 → `DrawCalls[0] == "1,1,250"`、10 連 → `"1,10,2500"` |
| `GetGachaStatusUseCaseTests.cs` | `Execute_Found_ReturnsPoints` | 一覧に (1, 37) → 37 |
| | `Execute_NotInList_ReturnsZero` | 一覧に (2, 37) だけ → 0 |
| | `Execute_Failure_ReturnsNull` | `Failure("e")` → null |
| `GachaGatewayTests.cs` | `ToDrawCallResult_MapsOutcomeAndSortsBySlot` | `new DrawResponse(new[] { GachaFixtures.SupportCardResult(2, 2, GachaOutcome.Duplicate, 10), GachaFixtures.IdolResult(1, 1, GachaOutcome.New, 0) }, 750, 47)`（Slot 2・Slot 1 の順の配列） → `Items` が Slot 1・2 の順、`IsNew` が true・false、`Kind`・`SupportPoints`・`GachaCurrency`・`ExchangePoints` が写る |
| | `ToDrawCallResult_NullResults_ReturnsEmpty` | `Items.Count == 0` |
| | `ToGachaStatuses_MapsAndHandlesNull` | 2 行が同じ順で写る。`Gachas == null` は空 |
| `GachaPresenterTests.cs` | `InitializeAsync_ShowsCurrencyCostsLineupAndExchange` | `GachasResult` に (1, 37) → `log` が `"SetCurrency:3250"`, `"SetCosts:250,2500"`, `"SetLineup:SSR 育成アイドル: アイドルA/SSR サポートカード: サポートB"`, `"SetExchange:37,200"` |
| | `InitializeAsync_StatusFails_ShowsNullExchange` | `GachasResult` が `Failure` → 最後が `"SetExchange:null,200"` |
| | `DrawAsync_Confirmed_PushesResultModal` | `DrawResult` が成功（通貨 750、交換pt 47、2 件）、`StubPlayerPort` は既定（成功）→ `InitializeAsync` のあとの `log` が `"Confirm:ガチャ通貨 2,500 を使って 10 回引きます。（所持 3,250）"`, `"Draw:10"`, `"SetCurrency:750"`, `"SetExchange:47,200"`, `"PushModal:GachaResultPresenter"`。`navigator.Args[0]` が `GachaResultArgs` で `Items.Count == 2`、`Retry != null` |
| | `DrawAsync_RefreshFailed_StillPushesResultModal` | `SignInResults` に `Failure("x")` → 同じく `"SetCurrency:750"`・`"PushModal:GachaResultPresenter"` があり、`"Info:"` で始まる行が無い |
| | `DrawAsync_Declined_DoesNotDraw` | `ConfirmResults` に `Negative` → `"Draw:"` も `"PushModal:"` も無い |
| | `DrawAsync_NotEnoughCurrency_ShowsInfo` | `DrawResult` が `BusinessError(ServerReasons.GachaNotEnoughCurrency, "e")` → 最後が `"Info:ガチャ通貨が足りません。"`、`"PushModal:"` が無い |
| | `DrawAsync_Failed_ShowsNothing` | `DrawResult` が `Failure("e")` → `"Confirm:ガチャ通貨 2,500 を使って 10 回引きます。（所持 3,250）"`, `"Draw:10"` のあとに何も無い |
| | `DrawAsync_WhileRunning_Ignored` | `DrawGate` に未完了の源 → `var first = presenter.DrawAsync(1);` → `await presenter.DrawAsync(1);`（すぐ返る）→ 源を完了 → `await first` → `"Draw:1"` が 1 回 |
| | `DrawAsync_DisposedDuringDraw_TouchesNothing` | `DrawGate` に未完了の源 → `var first = presenter.DrawAsync(1);` → `presenter.Dispose()` → 源を完了 → `await first` → `"Draw:1"` のあとに記録が無い（`"SetCurrency:"`・`"PushModal:"` が無い） |
| | `ResultRetry_DrawsAgain` | 前提: **`stubPlayerPort.Snapshot = GachaFixtures.Snapshot(3250);`**（10 連の費用 2500 以上。既定の 0 のままだと、1 回目の取り直しで `PlayerCache` の通貨が 0 になり、2 回目は `DrawUseCase` が通信せずに `NotEnoughCurrency` を返す）。`DrawAsync_Confirmed` と同じあと、`await ((GachaResultArgs)navigator.Args[0]).Retry();` → `"Confirm:"` で始まる行が 2 回（2 回目は `"Confirm:ガチャ通貨 2,500 を使って 10 回引きます。（所持 750）"`。`currency` は応答の値）、`"Draw:10"` が 2 回、`"PushModal:GachaResultPresenter"` が 2 回、`"Info:"` で始まる行が無い |
| | `OpenRatesAsync_PushesRatesModal` | `"PushModal:GachaRatesPresenter"`、`navigator.Args[0]` が `GachaRatesArgs` で `GachaId == 1` |
| | `BackAsync_PopsPage` | `"PopPage:GachaPresenter"` |
| | `Dispose_UnsubscribesEvents` | `Dispose()` のあと偽の View の `RaiseBack()` → `"PopPage:"` が無い |
| `GachaRatesPresenterTests.cs` | `InitializeAsync_BuildsGroupsThenRows` | `LastLines` の `Text` と `IsHeading` が順に: 見出し "通常枠（単発・10連の1〜9枠目）"、"SSR 育成アイドル 10.00%"、"SSR サポートカード 10.00%"、"R 育成アイドル 80.00%"、見出し "通常枠の内訳"、"SSR 育成アイドル アイドルA 10.00%"、"SSR サポートカード サポートB 10.00%"、"R 育成アイドル アイドルC 80.00%"、見出し "確定枠（10連の10枠目）"、"SSR 育成アイドル 25.00%"、"SR サポートカード 75.00%"、見出し "確定枠の内訳"、"SSR 育成アイドル アイドルA 25.00%"、"SR サポートカード サポートD 75.00%" |
| | `CloseAsync_PopsModal` | `"PopModal:GachaRatesPresenter"` |
| `GachaResultPresenterTests.cs` | `InitializeAsync_BuildsLines` | Items: (1, Idol 1, New)・(2, Idol 3, Duplicate, Shards 5)・(3, SupportCard 2, New)・(4, SupportCard 4, Duplicate, SupportPoints 1000) → `"SetLines:1. SSR 育成アイドル アイドルA　NEW/2. R 育成アイドル アイドルC　かけら +5/3. SSR サポートカード サポートB　所持に追加/4. SR サポートカード サポートD　サポート強化pt +1,000"` |
| | `RetryAsync_PopsModalThenCallsRetry` | `Retry` は `() => { log.Add("Retry"); return UniTask.CompletedTask; }` → `"PopModal:GachaResultPresenter"`, `"Retry"` の順 |
| | `RetryAsync_NullRetry_OnlyPops` | `"PopModal:GachaResultPresenter"` だけ |
| | `CloseAsync_PopsModal` | `"PopModal:GachaResultPresenter"`、`"Retry"` が無い |
| `HomePresenterTests.cs`（変更） | コンストラクター | 全部のテストで `new HomePresenter(view, stage, cache, dialogs, master, navigator)`（`stage`・`master` は foundation のまま（`FakeCharacterStage`・`StubIdolMaster`）、`navigator` は同じ `CallLog` の `FakeScreenNavigator`） |
| | `GachaClicked_ShowsNotReady` を `GachaClicked_PushesGachaPage` に差し替える | `InitializeAsync` のあと `RaiseGachaClicked()` → `InitializeAsync` の後の記録が `"PushPage:GachaPresenter"` だけ。ほかのボタンのテスト（`ProduceClicked_`・`OwnedClicked_ShowsNotReady`）とステージのテストは変えない（foundation の決めたこと 16） |
| | `OpenGachaAsync_WhileRunning_Ignored` を `OpenProduceAsync_WhileRunning_Ignored` に直す | 中身は同じで、呼ぶメソッドを `OpenProduceAsync` にする（`OpenGachaAsync` はダイアログを出さなくなったため） |
| | `OpenGachaAsync_PushesGachaPage`（足す） | `await presenter.OpenGachaAsync();` → `log` が `"PushPage:GachaPresenter"` だけ |

Play・手動（Editor、サーバー起動済み・初期付与済みのプレイヤー。タスク 3a の手順 7・タスク 3b の手順 4 の自動の確認のあと、ユーザーに見てもらう）:
- ホーム →「ガチャ」でガチャのページ。ヘッダーの「ガチャ」、「ガチャ通貨 N」、ラインナップ（SSR）、「交換pt N / 200」、「単発 250」「10連 2,500」（値はマスタのまま）が日本語で出る。ヘッダーの「戻る」でホームに戻る
- 「排出率」で通常枠・確定枠の、レア別・種別の合計とカードごとの率が出る（見出しの行は強調の色）。スクロールできる。「閉じる」で閉じる
- 「単発」→ 確認 →「引く」→ 結果のモーダル（1 行）。通貨が減る。「閉じる」→ ホームに戻ると減った通貨が出ている
- 「10連」→ 結果 10 行（はみ出す分はスクロール）。初めての育成アイドルに「NEW」、重複に「かけら +N」が出る。「もう一度」でモーダルが閉じ、確認ダイアログが再び出る
- 通貨が足りないとき（`server/` の DB でプレイヤーのガチャ通貨を減らしてから起動し直す。手段が無ければ確かめていないと報告する）「ガチャ通貨が足りません。」
- ガチャのページでサーバーを止めて「単発」→ 共通のエラーダイアログ（結果のモーダルは出ない）
- Windows の IL2CPP のビルドでの確認（`DrawResponse`・`DrawResultDto[]` の送受信）は、この計画では行わない。6 本目の通しのビルドで確かめる。確かめるまでは未確認と書く（タスク 4）

## 6. 未決事項

いまは ready。ユーザーの確認は全件了承済み（2026-11-07）。

- なし

## 7. 実装メモ

| タスク | 状態 | コミット | 計画とのズレ・確認できていないこと |
|---|---|---|---|
| 1 | 完了 | `10df9ce` | 止めた実装担当の書きかけを引き継いだ。BOM を付けた。EditMode 215 件通過 |
| 2 | 完了 | `d773807` | なし。EditMode 227 件通過 |
| 3a | 完了 | `786ffbd` | テストを 2 本足した（`GachaTextsTests` の `Kind_Texts`、`GachaResultDtoFactoryTests` の `Create_NullResults_Throws`）。EditMode 250 件通過。Play の手順 7 は、Bash から `EditorApplication.Step()` の eval を繰り返して Editor の Pipeline に接続できなくなり、タスク 3b のときに確かめた（期待どおり） |
| 3b | 完了 | `8b22946` | モーダルの Presenter は `disposed` を持たない（読まれないので CS0414 を避けた）。Play: 排出率 35 行、単発の結果 1 行、通貨 3,000 → 2,750（player_diff）、`ApplyAsync` のスレッドはメイン（2 で一致）。計画の式 `Find("Dialog")` は実物が `Dialog(Clone)` で当たらない（`DialogView` の有無で見た）。10 連・「もう一度」・見た目・サーバー停止時のダイアログ・IL2CPP は未確認。Play 中にローカルの DB の `gacha_currency` が 2500 → 0 に変わっていた（原因不明。別のセッションの可能性） |
| 4 | 完了 | `c30be79` | 進行役が行った（G7）。lint エラー 0 |
| レビュー 1 | 完了 | `90cd341` | client: Critical 0・Important 1（引数名が型名の規則に合わない。enum の引数も同じ規則にした）・Minor 5（M1 行の Prefab の `m_EditorClassIdentifier`、M2 実装メモのハッシュ、M3 例外の投げ方、M4 テスト 2 本、M5 名前付きのタプル）を全部直した。EditMode 261 件通過。報告は `uta_docs/reviews/.raw/2026-11-09-client-screens-gacha/code-client-1.md`（Git に入れない） |
