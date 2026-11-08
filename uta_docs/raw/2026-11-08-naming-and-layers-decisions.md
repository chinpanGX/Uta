# 命名規則と層の中身: ユーザーとの相談の記録（2026-11-08）

命名規則（層ごとのクラス名の付け方）を決める相談から、マスタの参照範囲・各側の Domain の中身・クライアントの Application の置き方まで広がった。このファイルは、その場で決まったことと、まだ決まっていないことの記録。ADR と wiki（`architecture/`）への反映は、このファイルを元に別に行う。

前提にした main の状態: `3b9a1eb`（PR #36〜#39 のマージ後）。共通基盤の移行の計画 [2026-11-08-client-foundation-migration.md](../plans/2026-11-08-client-foundation-migration.md) と、共通レスポンスの計画 [2026-11-08-common-response-player-diff.md](../plans/2026-11-08-common-response-player-diff.md) を読んだうえで決めた。

## 1. 決まったこと（ユーザーの判断）

### 1.1 マスタ（`Uta.MasterData`）の参照

- **生成したマスタは、Shared.Domain 以外のどの層から参照してもよい**。理由（ユーザー）: 依存させないために詰め替えると、型変換のコストが大きく増え、コード量が増えて保守のコストが上がるため
- Shared.Domain は、構造上（生成物がサーバー用・クライアント用で別。ADR 0007）参照できないので、Shared.Domain に渡すときだけ詰め替える（今の `CardRow` → `CardCatalog.Create` の形）
- **`MemoryDatabase` はシングルトンで作り、必要なクラスに Inject する**。サーバーは今の `Program.cs` の形のまま。クライアントは読み終えてから登録する。マスタをダウンロードで更新する方式に移ったら、差し替えではなくスコープを作り直す（Claude の案。ユーザーは「シングルトンで Inject」を決めた）
- ADR に一緒に書く条件の案（Claude。まだユーザーの確認を取っていない）: マスタの行の型を DB に保存しない・DTO に入れない、クライアントの Presentation では表示にだけ使う
- 置き換える決定: ADR 0008 の「`Domain` / `Application` をマスタの形式から独立させる」、[repository-layout.md](../wiki/architecture/repository-layout.md) 3章・4章の「生成物は Infrastructure だけ」「Application へはポート越しに必要な値だけ」、アーキテクチャテスト `MasterData_IsReferencedOnlyByInfrastructure`

### 1.2 各側の Domain の中身

- **サーバーもクライアントも、Domain にエンティティと Repository の IF を置く**（ユーザーの提案）。クライアントには Domain の層（asmdef）を新しく作る
- **エンティティの名前は `<名詞>Entity`**（`PlayerEntity`、`IdolEntity`、`SupportCardEntity`）。**所持を表す `Owned` は付けない**（ユーザー）
- **Repository の IF は `I<名詞>Repository`**（`IPlayerRepository`、`IIdolRepository`、`ISupportCardRepository`）。名前はエンティティ名から `Entity` を除いたもの。エンティティごとに分ける
- **Repository の処理は取得・Upsert・Remove**。サーバーとの通信は Repository に書かない（ユーザー）。クライアントの Repository はローカル（キャッシュ・セーブデータ）。今の `PlayerCache` は `PlayerEntity` を持つ Repository になる
- Shared.Domain は変えない（ルール・状態・計算だけ。エンティティも Repository も置かない）

### 1.3 PlayerDiff の位置づけ（ユーザーが明記を求めた）

- **PlayerDiff はエンティティではない（`PlayerEntity` ≠ `PlayerDiff`）**
- PlayerDiff は、**クライアントのリポジトリへの Upsert の経路を 1 つにするための仕組み**。個々のレスポンス（`DrawResponse.GachaCurrency` など）から必要な差分を拾ってリポジトリに反映するのではなく、共通レスポンスの `Common` を、共通の処理（`PlayerCommonApplier`）がリポジトリに反映することが目的
- PlayerDiff は、変わったエンティティの変更後の値を運ぶ入れ物。反映の処理がそれをエンティティに直し、各リポジトリに Upsert・Remove する

### 1.4 クライアントの Application と Infrastructure

- **Presentation が通信を意識しないように、外部（通信・保存）に触れる具象は Infrastructure に置く**（ユーザーの提案）。Infrastructure の具象は `I<機能>Service`（Contracts）を直接呼び、DTO を詰め替える。Gateway の IF（今の `IPlayerPort`）は要らなくなる
  - これが成り立つ根拠: 基盤 11d9b06 で `PlayerApi`（`ApiServiceBase`）が無くなり、`IPlayerService` を直接呼ぶ形になった。テストは偽の `IPlayerService` と基盤の `FakeRpcErrors.ApiFailureResult<T>` で失敗も作れる（移行計画 4章・5章）
- **外部に触れない具象（試験の順位の見込み、効果の予測など、Shared.Domain を呼ぶだけの処理）は Application に置いてよい**（案 A。ユーザーの判断）。線引きは「外部に触れるか」の 1 つ
- **UseCase の IF は機能ごとに 1 つ**（`IGachaUseCase` に `DrawAsync`・`ExchangeAsync`。Contracts の `I<機能>Service` と対）。Application に置く
- 表示の整形（`DisplayText`）は Presentation のまま（Claude の案）

### 1.5 命名

- サーバーとクライアントで命名を揃える（ユーザー）
- クライアントのテストのアセンブリは `Uta.Client.Tests.EditMode`（今は `Uta.Client.Infrastructure.Tests.EditMode` で、Application・Presentation のテストも入っていて名前と中身がずれている）
- 今のコードで揃っている名前（`<操作>Request`/`Response`、`<名詞>Dto`、`<機能>Errors`、`I<機能>Service`、`<名詞>Kind`、`<機能>Engine`/`Formulas`/`Rules`、`<機能>State`、`<名詞>Snapshot`、`<名詞>Validator`、`<名詞>Mapper`、`<画面>View`/`Presenter`/`LifetimeScope`）は、そのまま規約にする案（Claude。反対はなかった）

## 2. まだ決まっていないこと

- 結果の enum の名前: `Outcome` に統一し、enum なのに `Result` の 3 つ（`UseCheckResult`、`LessonApplyResult`、`ProduceApplyResult`）を改名するか
- サーバーの `Application/Produce/ProduceView`（クライアントの View と語がかぶる）を改名するか
- クラス名から `Uta` 接頭辞を外すか（`UtaServiceAttribute`、`UtaDatabase`、`UtaApiClient`、`UtaErrors`、`UtaMagicOnionClientInitializer`。名前空間が `Uta.*` なので外し、役割の名前にする案）
- マスタの IF（`I<名詞>Catalog` / `Master<名詞>Catalog`）: 1.1 でマスタを直接参照してよくなったので、Catalog の IF を残すか消すか
- 命名規則を書く場所（新しいページ `architecture/naming.md` の案）
- ADR の分け方（マスタの参照と、各側の Domain・Application の中身を、別の ADR にする案）

## 3. 影響（直す必要があるもの）

- main の ready の計画のうち、クライアントの画面の計画（foundation・formation・gacha・owned・week）は、Application に UseCase と Gateway を置く形で書かれている。実装する前に、新しい形への「読み替え」の節を足す（移行計画のタスク 4 と同じやり方）
- 共通レスポンスの計画: Application の `PlayerDiffData` と `PlayerCache.Apply` は要らなくなり、`PlayerCommonApplier` がエンティティに直して各リポジトリに Upsert・Remove する形になる
- サーバー: `OwnedIdol`・`OwnedSupportCard`・`PlayerSnapshot`・`ProduceSessionData` などを Domain のエンティティに移し、`I<名詞>Repository` を Domain に移す。`I<名詞>Catalog` と詰め替え先の型（`GachaDefinition` など）は 1.1 の結果しだい
