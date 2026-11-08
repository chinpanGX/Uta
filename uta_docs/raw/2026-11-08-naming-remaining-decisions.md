# 命名規則の残りの決定: ユーザーとの相談の記録（2026-11-08）

[2026-11-08-naming-and-layers-decisions.md](2026-11-08-naming-and-layers-decisions.md) の 2章「まだ決まっていないこと」を、同じ日に決めた記録。[2026-11-08-presentation-rules-decisions.md](2026-11-08-presentation-rules-decisions.md) も合わせて読む。

## 1. 要求は Request、結果は Result（ユーザー）

- **`Outcome` はやめ、結果は `Result` に揃える**。結果の値（record・class）は `<操作>Result`、結果の種類（enum）は `<操作>ResultKind`（Claude の案。`PlayerSignInResult` と `PlayerSignInOutcome` のように両方ある型で名前がぶつかるため、`<名詞>Kind` の規則に合わせる）

  | 今 | 新 | 場所 |
  |---|---|---|
  | `GachaOutcome` | `GachaResultKind` | Contracts（[api-design.md](../wiki/architecture/api-design.md) 1.1 の正も直す） |
  | `ExamOutcome` | `ExamResultKind` | Shared.Domain |
  | `PlayerSignInOutcome` | `PlayerSignInResultKind` | クライアント Application |
  | `GrantOutcome` | `GrantResultKind` | サーバー Domain |
  | `UseCheckResult`・`LessonApplyResult`・`ProduceApplyResult`（enum） | `UseCheckResultKind`・`LessonApplyResultKind`・`ProduceApplyResultKind` | Shared.Domain |

  - プロパティ名の `Outcome`（`DrawResultDto.Outcome` など）も `ResultKind` にする。MessagePack はキーの番号で読み書きするので、通信には影響しない
  - DB の列名（`exam_outcome` など）は変えない（マイグレーションが要るため。対象は C# の名前だけ）
- **`Request` は通信と UseCase の入力に限る**。通信の出力は今のまま `<操作>Response`、内部の結果は `<操作>Result`
- **Shared.Domain の `LessonCommand`・`ProduceCommand`（エンジンへの操作の入力）は `Command` のまま残す**（ユーザー。サーバーの `ProduceCommandNormalizer` も同じ）

## 2. サーバーの `ProduceView` を改名する（Claude の案。ユーザーは「クライアントと語がかぶるのが気になる」として改名を求めた）

| 今 | 新 |
|---|---|
| `ProduceView` | `ProduceStateResult`（`ProduceStateResponse` と対） |
| `ProduceViewBuilder` | `ProduceStateResultFactory`（作る役は Factory に揃える） |
| `ClassOptionView` | `ClassOptionSummary`（同じファイルの `ProduceEndSummary` に揃える） |

- **`View` はクライアントの Presentation だけで使う**

## 3. クラス名から `Uta` 接頭辞を外す（ユーザー。新しい名前は Claude の案）

| 今 | 新 |
|---|---|
| `UtaServiceAttribute`（`[UtaService]`） | `GameServiceFilterAttribute`（`[GameServiceFilter]`） |
| `UtaDatabase` | `DatabaseMigrator` |
| `UtaApiClient` | `ApiClient` |
| `UtaErrors` | `ErrorDefinitions` |
| `UtaMagicOnionClientInitializer` | `MagicOnionClientInitializer` |

- `ApiClient`・`MagicOnionClientInitializer` は、基盤・MagicOnion に同じ名前の型があるかを手元で確かめられなかった。実装のときにコンパイルで確かめ、ぶつかったら止まって報告する（計画に書く）

## 4. マスタの Catalog の IF は要らない（ユーザー: 要否で決める）

調べたこと:

- サーバーのテストに Catalog の IF の偽物は無い。UseCase のテストは本物のマスタを使う結合テスト。`Master<名詞>Catalog` のテストは詰め替えそのものを確かめるものだけ（`server/Uta.Server.Tests/MasterData/`）
- クライアントの `IGameMaster` はまだコードが無い（計画 [2026-11-03-client-screens-foundation.md](../plans/2026-11-03-client-screens-foundation.md) 3.4 にあるだけ）

決めたこと:

- **やめる**: サーバーの `I<名詞>Catalog`・`Master<名詞>Catalog`・詰め替え先の型（`GachaDefinition` など）と、その詰め替えのテスト。クライアントの `IGameMaster` は作らない。UseCase とサーバーの Domain は `MemoryDatabase` を直接使う
- **残す**: Shared.Domain の `ICardCatalog`・`CardCatalog`・`ProduceCatalog`（Shared.Domain はマスタを参照できないため）。組み立て役の `MasterCardCatalog`・`MasterProduceCatalog` は `CardCatalogLoader`・`ProduceCatalogLoader` に改名する

## 5. 命名規則を書く場所（ユーザー）

- **wiki の `architecture/naming.md`** に書き、CLAUDE.md の「C# のコードを書く」の行からリンクする。コードを書くときだけ読むので、CLAUDE.md の本文に書くよりトークンを抑えられる
- raw に置かない理由: raw は「一度置いたら書き換えない一次資料」なので、直していく規約に向かない

## 6. ADR は読みやすさを重視して分ける（ユーザー）

1 つの ADR に 1 つの決定（Claude の案）:

| ADR | 決定 |
|---|---|
| 0020 | 生成したマスタは Shared.Domain 以外のどの層から参照してもよい。`MemoryDatabase` はシングルトンで Inject する |
| 0021 | 各側の Domain に、エンティティとリポジトリの IF（読み取り専用と書き込み）を置く |
| 0022 | クライアントでは、外部に触れる具象を Infrastructure に置き、Application には機能ごとの UseCase の IF と、外部に触れない具象を置く |

命名の規則（`naming.md`）と Presentation の細かいルール（`screen-design.md`）は ADR にしない（方針ではなく設計の細部のため）。
