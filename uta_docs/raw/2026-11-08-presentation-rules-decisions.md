# Presentation 層のルール: ユーザーとの相談の記録（2026-11-08）

[2026-11-08-naming-and-layers-decisions.md](2026-11-08-naming-and-layers-decisions.md) の続き。同じ日の相談で、クライアントの Presentation 層のルール（表示用の型、その組み立て、リポジトリの読み取り）を決めた。ADR と wiki（`architecture/screen-design.md`）への反映は、2 つの記録を元に別に行う。

## 1. 今のルールの置き場（相談の時点）

| 場所 | 中身 | 状態 |
|---|---|---|
| [repository-layout.md](../wiki/architecture/repository-layout.md) 4章 | 依存のルール（Presentation は Application・Domain だけ。Infrastructure・Contracts は禁止） | 決定（ADR 0008） |
| [screen-design.md](../wiki/architecture/screen-design.md) 1・5・6・7章 | View と Presenter の役割、通信とエラー、Shared.Domain との関係、置き場、命名、マスタ | ほとんど Uta案 |
| [2026-11-03-client-screens-foundation.md](../plans/2026-11-03-client-screens-foundation.md) 3.3 | 画面を作る共通ルール（遷移、Presenter の形、View の形、Args、テスト、DI） | 実装計画（ready）。wiki ではない |
| `.claude/skills/requesting-code-review/layers/client.md` | レビューのチェックリスト | 上の 3 つのまとめ |

細かいルールが wiki ではなく実装計画にあるので、命名規則のページを書くときに screen-design.md へ移す（Claude の案）。

## 2. 決まったこと（ユーザーの判断）

### 2.1 Args と Dto

- **前の画面から受け取る情報は `<画面>Args`**（今のまま。foundation の計画 3.3.4）。前の画面の Presenter が Push のときに渡し、次の画面の Presenter が `IScreenWithArgs<TArgs>` で受ける。中身は ID などの加工していない値
- **View に渡す表示用の型は `ViewData` から Dto に変える。名前は「受け取る View の名前 + `Dto`」**（案 B）
  - 行の View に渡すもの: `OwnedRowView` ← `OwnedRowViewDto`
  - 画面に渡すもの: `IWeekView` ← `WeekViewDto`（`I` を除く）。画面の中の行の一覧は `WeekActionRowViewDto` など
  - 理由: `<名詞>Dto` にすると、Contracts の `WeekActionDto`・`ClassOptionDto` と同じ名前の型ができる（テストのアセンブリは両方を参照する）。`View` が付く名前を「Presentation の View と、それに渡す値」に限り、Contracts の `<名詞>Dto` と名前で見分ける

### 2.2 DtoFactory

- **Dto は Factory が作る**（ユーザーの提案）。画面ごとに `<画面>DtoFactory` を 1 つ、`Presentation/<機能>/` に置く。中身の多い少ないにかかわらず作り、Presenter は文言・書式を持たない（Claude の案）
- **Factory は Domain まで参照してよい**（ユーザー）。参照してよいもの: Domain（エンティティ、読み取り専用のリポジトリの IF）、Application（読み取りの型、外部に触れない計算）、マスタ、`DisplayText`
- **Factory はリポジトリの Upsert・Remove をしない**（ユーザー）。通信・遷移・View に触ることもしない（Claude の案）
- Factory はインスタンスにし、リポジトリをコンストラクタで受ける。状態を持たないので、ルートのスコープにシングルトンで登録する（Claude の案。Factory がエンティティを取得するため）
- 画面をまたぐ文言は `DisplayText` に置いたまま、Factory から呼ぶ。名前を揃えるため `CardSummaryBuilder` は `CardSummaryFactory` に改名する（Claude の案）
- foundation の計画 3.3.3 の「文言の組み立ては Presenter でもページでもよい」はやめる。View は値を入れるだけ

### 2.3 読み取り専用のリポジトリの IF

- **Factory が書き込めないように、リポジトリの IF を読み取りと書き込みに分ける**（ユーザーの提案。形は Claude の案）。.NET の `IReadOnlyList<T>` / `IList<T>` と同じ形:
  - `IReadOnly<名詞>Repository`: 取得だけ
  - `I<名詞>Repository : IReadOnly<名詞>Repository`: Upsert・Remove を足す
- 実装は 1 つのクラスで、DI には両方の IF で登録する（VContainer の `.As<I…>().As<IReadOnly…>()`）。同じインスタンスなので、読み取り側も常に最新の値を見る
- 書き込むのは、UseCase の具象と `PlayerCommonApplier`（共通レスポンスの反映）だけ
- サーバーも、書き込まない使い手には読み取り専用の IF を渡す、という名前の規則を同じにする（今は該当する使い手がない）

### 2.4 View の名前（今のまま。確認しただけ）

| 種類 | 名前 | 例 |
|---|---|---|
| 画面（Prefab のルート） | `<画面>Page` / `<画面>Modal` | `HomePage`、`GachaResultModal` |
| Presenter が見る画面の IF | `I<画面>View` | `IHomeView` |
| 部品・行 | `<名詞>View` / `<名詞>RowView` | `ButtonView`、`DeckCardRowView` |

`Home/HomeView.cs`・`Title/TitleView.cs`・`Produce/ProduceView.cs`・`Startup/StartupView.cs` はこの表から外れる（Prefab の画面を作る前の View が残っているとみられる）。使われているかを確かめ、片付けの対象にする。

## 3. 今のルールと、この日の決定がぶつかるところ

| 今のルール | この日の決定 |
|---|---|
| マスタは `IGameMaster` で受け、`Uta.MasterData` を Application・Presentation に出さない（screen-design 7.5） | マスタは Shared.Domain 以外から参照してよい（1 本目の記録 1.1）。Presentation では表示にだけ使う条件の案 |
| UseCase は `I<機能>Port` 越しに通信する（screen-design 7.4） | 外部に触れる具象は Infrastructure、Application には機能ごとの `I<機能>UseCase`（1 本目の記録 1.4） |
| Presenter が `PlayerCache`（Application の具象）を受ける（foundation の計画 3.3.2） | Domain の `IPlayerRepository`（`PlayerEntity`）。表示のための読み取りは DtoFactory が `IReadOnlyPlayerRepository` で行う |
| 表示用の型は `<名詞>ViewData`、組み立ては Presenter（owned・week・lesson の計画） | `<View 名>Dto` を `<画面>DtoFactory` が作る |
| Presenter は Shared.Domain を直接呼ばない（screen-design 6章） | 変えない。外部に触れない計算は Application の具象（1 本目の記録 1.4）か、DtoFactory から呼ぶ |

ready のクライアントの画面の計画（foundation・formation・gacha・owned・week・lesson）は、どれも「読み替え」の対象になる。
