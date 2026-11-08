---
title: 実装計画: client/ の画面 索引
status: ready
updated: 2026-11-08
spec: ../wiki/overview/game-spec.md
---

# 実装計画: client/ の画面 索引

## ADR 0020〜0022・naming.md への読み替え（案。ADR 0020〜0022 は proposed のまま順 0 で実装し、順 0 の PR で accepted を判断する。この節が本文と食い違ったら、この節が正）

2026-11-08。proposed の [ADR 0020](../wiki/decisions/0020-masterdata-referenced-outside-shared-domain.md)・[0021](../wiki/decisions/0021-domain-entities-and-repositories.md)・[0022](../wiki/decisions/0022-client-external-concretes-in-infrastructure.md) と [naming.md](../wiki/architecture/naming.md) に合わせ、ready の計画に同名の「読み替え」の節を足した。本文は書き換えていない。

- 節を足した計画: [基盤移行](2026-11-08-client-foundation-migration.md)・[共通レスポンス](2026-11-08-common-response-player-diff.md)・[foundation](2026-11-03-client-screens-foundation.md)・[formation](2026-11-03-client-screens-formation.md)（2026-11-08）。gacha・owned・week・lesson は次のセッションが足す。**画面の計画に共通の読み替え（対応表）は foundation の節の R0 が正**。各計画の節は、その計画に固有の読み替えだけを書く
- **節の優先**: 同じ計画に「ADR 0020〜0022 の読み替え」の節と「基盤 11d9b06 への読み替え」の節（順 0 のタスク 4 が足す）が並ぶときは、ADR の節 > 11d9b06 の節 > 本文。11d9b06 の節の `UtaApiClient`・`XxxGateway`・`PlayerGatewayTests` は、ADR の節の `ApiClient`・Infrastructure の具象 `<機能>UseCase`・`Fake<機能>Service` と読む。`ApiClient` が `I<機能>Service` を持ち `BootstrapEntry` が `I<機能>Service` を登録する形は 11d9b06 の節のまま有効
- **ADR の状態と順番**（ユーザーの判断 2026-11-08）: ADR 0020〜0022 は proposed のまま順 0（基盤移行。**ADR 0022 の accepted の条件を確かめる計画**: Player を `IPlayerUseCase` と Infrastructure の具象に移し、`Uta.Client.Domain`・`Uta.Client.MasterData` の asmdef と `Uta` 接頭辞の改名もここで行う）を実装し、順 0 の PR で 3 本まとめて accepted を判断する。採らないと決めたら、順 0 の同じ PR で ADR に由来する部分を本文の形に戻し（戻さないものの一覧の正は ADR 0022 の「採らないと判断したら」）、各計画の「ADR 0020〜0022 の読み替え」の節は使わない（「基盤 11d9b06 への読み替え」の節は ADR の採否と関係なく有効で、そのときの優先は 11d9b06 の節 > 本文）。順は 順 0 → 共通レスポンス → 順 1〜10。**ADR を採るときは、gacha・owned・week・lesson は読み替えの節が足されるまで実装に入らない**（1 章の表では ready のままだが、ADR 0022「accepted にしたら直すもの」のとおり、節を足してから実装する。採らないときはこの縛りは無い）。**ADR の採否と関係なく効く読み替え**: 順 0 で済む naming.md の改名（`UtaApiClient` → `ApiClient`、`UtaErrors` → `ErrorDefinitions`、`UtaMagicOnionClientInitializer` → `MagicOnionClientInitializer`、`MasterCardCatalog` → `CardCatalogFactory`、テストの asmdef `Uta.Client.Infrastructure.Tests.EditMode` → `Uta.Client.Tests.EditMode`、名前空間 `Uta.Client.Infrastructure.Tests` → `Uta.Client.Tests`）は、共通レスポンス・foundation〜lesson の本文と 11d9b06 の節に出る旧い名前を新しい名前で読む（採らないときも同じ）。移行計画のタスク 4 が足す索引の行は、共通レスポンスも 1 章の表の行（順 0 の次。「MVP の後の計画」の箇条書きではなく）にした。**順 0 と共通レスポンスは、P1〜P3 の後に `main` へ入る**（2026-11-08 の時点で P1〜P3 は done で `main` にマージ済み。当初の「P1 の実装ブランチを切る前に `main` へ入れる」は満たせなくなった）。**どちらも順 4（foundation）の実装ブランチを切る前に `main` へ入れる**（共通レスポンスの計画の 0 章「foundation（順 4）より前」と同じ意味）
- **前提**: 順 1（計画 0）・P1〜P3 は層の構成に触れないので読み替えの節を足さないが、次の 1 点だけ読み替える: P1・P2・P3 の本文に出るテストの asmdef の名前 `Uta.Client.Infrastructure.Tests.EditMode` と名前空間 `Uta.Client.Infrastructure.Tests`（Grep で探す。prefabs-common 90・101・113・786 行付近、screens-a 581 行付近、screens-b 68 行付近）は、順 0 の改名後の `Uta.Client.Tests.EditMode`・`Uta.Client.Tests` と読む（P1〜P3 の読み替えはこの 1 点だけ）。順 4 以降の C# の計画は、順 0 と共通レスポンスの読み替えが実装済みであることを前提にする
- 1 章の表の「何を作るか」の読み替え: 順 4 の `IGameMaster` → 作らない（`MemoryDatabase` を直接読む。代わりに `DiscardNoticeState`・`FeaturedIdolPicker`・`HomeDtoFactory`）。順 5 の `IGachaPort`・`PortResult<T>`・`FakeGameMaster` → `IGachaUseCase`・`UseCaseResult<T>`（要るときだけ）・（マスタの偽は作らず `TestMasterData`）。順 6 の `FormationValidator.IsPlanCompatible`・`DisplayText` はそのまま。順 8 の `IProducePort`・`ProduceSession` → `IProduceUseCase`・`ProduceSessionState`。順 9・10 の `ProduceFlow`・`FakeProduceFlow` はそのまま（コンストラクターの引数の型が変わる）
- 2 章の各計画の要約のうち、`IGachaPort`・`GachaGateway`・`PortResult<T>`・`FakeGameMaster`・`IProducePort`・`ProduceSession`・`CardSummaryBuilder`・`DialogService` の語は、各計画の読み替えの節の名前で読む
- **次のセッション（gacha・owned・week・lesson の節を書く）への申し送り**（各計画の節に散らばっているものをまとめた。細部は foundation の R0 と formation の R6）:
  - gacha: **`UseCaseResult<T>`・`ServerReasons` は、Presenter が reason で文言を分ける操作が無ければ作らない**（ユーザーの判断 2026-11-08。gacha・owned の本文はどの操作も enum に写しているので、作らない見込み。作らないなら `PortResultTests`・`ServerReasonsTests` も無い。foundation R0 の行と同じ）。`GachaStatus`（class）の改名。`GachaLineRow` → `GachaLineRowView`、`GachaRateLine` → `GachaLineRowViewDto`。`GachaRateRow` の名前（`Row` はマスタの行の意味なので使わない）。`GachaApi` は 11d9b06 の節で無くなる。`DrawStatus`（enum）→ `DrawResultKind`。class の `DrawOutcome`・`DrawCallResult`・`DrawItem` は、サーバーの同じ役割の型（`DrawResult`・`GachaResult`）と名前を揃えて決める（foundation R0）。`PlayerCommonApplier.ApplyAsync` がメインスレッドで呼ばれるかを Play で確かめる（共通レスポンスの計画の節。違えば止まる）。Contracts の `GachaOutcome` → `GachaResultKind` と、プロパティ `DrawResultDto.Outcome` → `ResultKind` の改名（naming.md）はこの計画で行う
  - owned: `OwnedListBuilder` → `OwnedListFactory`、`SupportLevelUpCostCalculator`、`OwnedSelectionRequest` の改名（`OwnedSelection` は結果の型と同名なので使えない）、`ForSupportCard(int idolId, IReadOnlyCollection<string> excludedUids)`（formation R4）、`SupportStatus`（enum）→ `SupportResultKind`、class の `SupportOutcome`・`SupportLevelUpResult`・`SupportLimitBreakResult` はサーバーの同じ役割の型（`LevelUpResult`・`LimitBreakResult`）と揃えて決める、新しい enum（`OwnedSortKey`・`OwnedTab`・`LevelUpStep`）は `<名詞>Kind`、`SupportSnapshotEdits` は作らない、`OwnedRowViewData` → `OwnedRowViewDto`
  - week・lesson: `SkillCardRowDtoFactory` と `SkillCardRowViewDto` の形（`CardSummaryBuilder`・`CardSummary` は作らない。画面ごとの ID・enabled は画面の Dto に: foundation R0）、`IProduceUseCase` の操作ごとのメソッドと `MarkExamResultShown()`・`ClearSession()`（formation R2・R6。lesson の Presenter は `session.ExamResultShown = true`・`session.Clear()` を直接書かず、この 2 つを呼ぶ）、`ProduceSession` → `ProduceSessionState`・`ProduceProgress` → `ProduceStateResult`・`ProduceOutcome` → `ProduceEndResult`・`ProduceEndingKind` → `ProduceEndKind`、`WeekProgress` の引数 `ScenarioInfo` → マスタの `Scenario`・`ScenarioWeek` の行、week のテストの `ServerReasons.ProduceInvalidArgument`・`ProduceStaleOperation` は無い（`FakeProduceUseCase` に `ProduceStepResultKind.Rejected`・`Recovered` を積む）、例外のテストの文字列は `"FakeProduceUseCase: no state result"`、Shared.Domain の `ExamOutcome` → `ExamResultKind` の改名は formation が行う（formation R1）、`<名詞>ViewData` → Dto の規則（foundation R0）
  - 次のセッションが決めた名前（上の「決める」とあるもの）は、決めたら foundation の R0 の該当の行に書き戻す（R0 が共通の対応表として完結するように）
- 6 章（Unity Editor が要るタスク）は変わらない

[game-spec.md](../wiki/overview/game-spec.md)「実装計画の単位と順」の順 7（client/ の画面）は、10 本の計画に分けて実装する。このページは、10 本の順番と関係、各計画の「決めたこと」と「ユーザーの確認」（全件が 2026-11-07 にユーザーの推奨どおりの了承で解決した。5章）の要約、実装の前提と流れをまとめる。細部の正は各計画（要約と食い違ったら各計画が正）。方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章（Prefab・Addressables・TextMeshPro）と 8章（シーン構成）、計画の進め方は [README](README.md)「計画のブランチと PR（流れ）」。

## 1. 順番と関係

Prefab の計画（P1〜P3）は、画面の Prefab と View の骨格（`[SerializeField]` のフィールドだけ）を Editor のスクリプトで生成する。C# の計画（foundation〜lesson）は、その骨格に `IXxxView` の実装と Presenter・ユースケースを足す。フィールド名と Prefab の子の対応は Prefab の計画の表が正。

| 順 | 計画 | 何を作るか | 前の計画 | 積み PR の base | 状態 |
|---|---|---|---|---|---|
| 0 | [クライアントの基盤移行](2026-11-08-client-foundation-migration.md) | 共通基盤 11d9b06 の新しい呼び方への移行（`PlayerApi` の廃止ほか） | なし | `main` | done |
| 0 の次 | [共通レスポンス項目（player_diff）](2026-11-08-common-response-player-diff.md) | サーバーの RPC のレスポンスに共通レスポンス項目（player_diff）を載せ、クライアントのリポジトリに反映する | 順 0 | 順 0 の実装ブランチ（順 0 が `main` に入っていれば `main`） | ready |
| 1 | [計画 0 マスタ スキルカードの名前と説明文](2026-11-03-masterdata-card-text.md) | `Card.Name`・`CardFace.Description` の列と 36 枚の値（masterdata・サーバーのテスト） | なし | `main` | done |
| 2 | [P1 共通部品・Addressables・TextMeshPro](2026-11-07-client-prefabs-common.md) | スパイク、TMP Settings、共通部品の Prefab 10 個（`UiRoot` を含む）、生成の補助 `PrefabBuildUtil`、`PrefabContractTests`、`BuildCommands` の `BuildPlayerContent` | なし（計画 0 の型は使わない）。PR #28・#29 と計画 0 の実装 PR が main にマージ済み | `main`（PR #28・#29 と計画 0 がマージ済みのとき。3章・確認 1） | done |
| 3 | [P2 Title・Home・ガチャ・所持一覧の Prefab](2026-11-07-client-prefabs-screens-a.md) | 画面の Prefab 7 個・行の Prefab 3 個・View の骨格 8 型、画面の補助 `ScreenLayout` | P1 | P1 の実装ブランチ | done |
| 4 | [foundation 土台・タイトル・ホーム](2026-11-03-client-screens-foundation.md) | ダイアログ・ブロッカー・`PageStack`・シーンのスコープへの登録、`IGameMaster`、業務エラー、タイトル・ホーム、`Bootstrap`・`Title`・`Home` シーンの `UiRoot` | 計画 0・P1・P2 | P2 の実装ブランチ（計画 0 は main にある前提） | ready |
| 5 | [gacha ガチャ](2026-11-03-client-screens-gacha.md) | ガチャのページ・排出率・結果のモーダル、`IGachaPort`、`PortResult<T>`・`ServerReasons`・`FakeGameMaster` | foundation・P2 | foundation の実装ブランチ | ready |
| 6 | [owned 所持一覧・サポートカードの強化](2026-11-03-client-screens-owned.md) | 所持一覧（選択モード付き）・サポートカードの詳細と強化、`DisplayText`、`FormationValidator.IsPlanCompatible`（shared/） | gacha | gacha の実装ブランチ | ready |
| 7 | [P3 編成・プロデュースの Prefab](2026-11-07-client-prefabs-screens-b.md) | 画面の Prefab 10 個・行の Prefab 5 個・View の骨格 | P1・P2（C# の型には依存しない） | owned の実装ブランチ | done |
| 8 | [formation 編成・プロデュースの開始と再開](2026-11-03-client-screens-formation.md) | 編成のページ、`IProducePort`・`ProduceSession`・`ProduceFlow`・`ProduceMenu`、`Produce` シーンの作り直し（`UiRoot`）、`LessonDto.UsableHand`（server・shared） | owned・P3 | P3 の実装ブランチ | ready |
| 9 | [week 週の行動選択・授業・支給・スキルカードの選択肢](2026-11-03-client-screens-week.md) | 週・授業・支給のページ、カードの選択肢のモーダル、`ProduceFlow` の `WeekSelect`・`CardOffer`、`FakeProduceFlow` | formation | formation の実装ブランチ | ready |
| 10 | [lesson レッスン・試験・試験結果・プロデュース結果](2026-11-03-client-screens-lesson.md) | レッスンのページ・山札などの一覧・試験結果・プロデュース結果、`ProduceFlow` の残りの局面。完了条件 6 の通し | week | week の実装ブランチ | ready |

- 並行しない。後の計画は前の計画の型・偽・Prefab を使う（各計画の 1章の前提）
- **順 0（基盤移行）と共通レスポンスは、P1〜P3 の後に `main` へ入る**（P1〜P3 は先に done になり `main` にマージされた。当初の「順 0 は P1 より前に `main` へ入れる」は満たせなくなった）。**順 0 → 共通レスポンスの順に、どちらも順 4（foundation）の実装ブランチを切る前に `main` へ入れる**（foundation も `ApiClient.Create` を変え、順 0 と共通レスポンスの読み替えが実装済みであることを前提にするため）
- **計画 0 と P1 は `main` 向け。計画 0 を先に main へマージしてから P1 を始める**（確認 1）。積み PR の連鎖 main → P1 → P2 → foundation に計画 0 の変更は入らないので、計画 0 が main に無いと foundation の前提 2（`Card.Name`・`CardFace.Description`）で止まる
- 「積み PR の base」は実装の PR（4章）の base。前の計画の実装 PR が main に入ったら、後の PR の base を `main` に付け替える（CLAUDE.md「Git」。付け替えとマージはユーザーが行う）
- 状態は各計画の frontmatter の `status`。計画が `done` になったら、この表も `done` にする
- **MVP の後の計画（順 7 の範囲外）**: ホームのキャラ表示を 3D モデルの待機モーションにする [ホームの 3D キャラ表示](2026-11-08-client-home-character-3d.md)（2026-11-08。`HomePage.CreateStage` だけを差し替える。この表の 10 本には入れない）

## 2. 各計画で決めたこと（要約）

### 計画 0 マスタ スキルカードの名前と説明文

- 列は `Card.Name`（`card.csv` の 2 列目）と `CardFace.Description`（`cardFace.csv` の最後）。どちらも Common。空は `IValidatable` で拒否、名前の重複は `[Unique]`
- 説明文は 3.3 の規則で人が書く。効果の CSV との一致は自動で検証しない。36 枚の名前は Uta 独自（ユーザーが確認済み。2026-11-07）

### P1 共通部品・Addressables・TextMeshPro

- 最初のタスクはスパイク（Addressables 4.1.2 と TMP の日本語の動的 SDF が Unity 7 の Play と Windows の IL2CPP で動くか）。動かなければ止まる
- Unity 7 の TMP は Essential Resources の取り込みが無いので、プロジェクトの TMP Settings を作り、既定のフォントを Noto Sans JP にする（確認 3）
- フォントは PR #29 のアセットを使い、作り直さない。太字は Bold の SDF アセット。フォントは Addressables に入れない
- 共通部品の Prefab は 10 個。Addressables のグループ `Screens` に入れるのは画面の Prefab だけ（アドレス = ファイル名）。`UiRoot` が `AddressableAssetLoader` を USN のコンテナーに入れる
- View は「フィールドだけ」の骨格。生成クラスは `TypeCache` で集めるので、P2・P3 は生成クラスを足すだけ。`UiRoot` は各シーンに 1 つ（`Bootstrap`・`Title`・`Home` は foundation、`Produce` は formation が置く）
- EditMode のテストは、Editor を開いたまま `unity command --timeout 300 eval` で `TestRunnerApi` を呼んで回す（P1 の決めたこと 12。コマンドは unity7-evaluation 4章と CLAUDE.md。全計画共通。ユーザーの判断。2026-11-08）。eval で回せなければ、Editor の Test Runner（EditMode）を手動で回してもらい、確認できていないと報告する。止まらない
- Prefab を足すだけの作業（P1〜P3）では、プレイヤーのビルドを確かめない（P1 の決めたこと 19。ユーザーの判断。2026-11-08）

### P2 Title・Home・ガチャ・所持一覧の Prefab

- 画面の Prefab 7 個（`TitlePage`・`HomePage`・`GachaPage`・`GachaRatesModal`・`GachaResultModal`・`OwnedPage`・`SupportDetailPage`）と行の Prefab 3 個。所持一覧の行は P1 の `ListRow`
- ページの形は `Header` →（`TabBar`）→ `Main`。組み方の補助 `ScreenLayout` は P3 も使う
- レベルのボタン 3 つは固定で置く。所持一覧の並び・絞り込みは 2 段。0 件の「ありません」は一覧の外
- 画面ごとのテストで「View のフィールドが表の子を指す」ことを確かめる。P1 の型だけに依存する
- **ホームの構成（2026-11-07 追加。ユーザーの判断）**: 上部のステータス → キャラ表示のステージ `Stage`（`RawImage` + 枠の文字。後から 3D の `RenderTexture` を割り当てられる）→ 「プロデュース中」の帯 → 大きな主ボタン → 下部のナビ（プロデュース・ガチャ・アイドル）。フィールドは `produceButton`・`gachaButton`・`ownedButton` を `nav…Button` に改名し、`resumeButton` を消し、`stageImage`・`stageLabel`・`primaryButton` を足した

### foundation 土台・タイトル・ホーム

- PR #24 のシーン構成の上に作る。起動の流れは `Title` シーンの `TitlePresenter`、ホームは `Home` シーンの `HomePresenter`。基盤の `IScreenNavigator` をそのまま使い、`Replace` は作らない
- C# で UI を組まない（旧版の `UiFactory` などは消す）。`UiRoot` は `Bootstrap`・`Title`・`Home` に置き、各シーンのスコープが `SceneUiInstaller.Install` で登録する。ルートの共通エラーとブロッカーは `ActiveSceneUi` で「いまのシーン」の UI を見つける
- 診断（`-uta-quit-after-check`）は `TitleDiagnostics` に分ける。タイトルの失敗は状態の文を出してもう一度タップ
- ホームの 3 つのボタンの動作（プロデュース・ガチャ・アイドル）は「準備中です。」で、テストはイベントごとに 3 本（`ProduceClicked_`・`GachaClicked_`・`OwnedClicked_ShowsNotReady`）。後の計画は自分のボタンのテストだけを差し替える
- **ホームの構成（2026-11-07 追加。ユーザーの判断）**: `IHomeView` のイベントは 3 つ（主ボタンとナビの「プロデュース」は同じ `ProduceClicked`。`ResumeClicked`・`ResumeProduceAsync` は無い）。キャラ表示は `ICharacterStage`（MVP は `PlaceholderCharacterStage`。`HomePage.CreateStage` が作る）で、`HomePresenter` がレア度の最も高い所持の育成アイドルを選ぶ。`HomePresenter` のコンストラクターに `IGameMaster` が入った（gacha・owned・formation の `HomePresenter` の節も合わせた）
- **`Produce` シーンは一切変えない**（作り直すのは formation）
- 確認が要ることはなし

### gacha ガチャ

- 画面は 3 つ（ページ・排出率・結果のモーダル）。どれも `Home` シーンでホームの上に積む
- 通信の層は `IGachaPort`・`GachaGateway`。共通の `PortResult<T>`・`ServerReasons` と、テストの `FakeGameMaster` を作る（後の計画も使う）
- 抽選のあとの取り直しの失敗は成功として扱う。排出率はレア別・種別の合計を先に、カードごとを後に
- 「もう一度」は Args の関数で呼び戻し、確認ダイアログからやり直す（確認 9）

### owned 所持一覧・サポートカードの強化

- 所持一覧・詳細は `Home` シーンのページ。所持一覧に、編成と上限解放の素材が使う選択モードを持たせる
- 育成アイドルの一覧は `IdolId` の順に固定（確認 10）。サポートカードの詳細と強化は 1 ページ（確認 11）
- レベル上げ・上限解放は確認ダイアログのあとに RPC。上限解放の素材はプレイヤーが選ぶ
- プランの適合の規則は Shared.Domain の `FormationValidator.IsPlanCompatible` に 1 つ（shared/ の変更）
- 表示名は `DisplayText.RarityName`・`PlanName`・`SupportTypeName`・`ParamName`・`Ability`（型と同じ名前のメソッドにしない。week・lesson と同じ規則）

### P3 編成・プロデュースの Prefab

- 画面の Prefab 10 個（`SupplyPage` を含む）と行の Prefab 5 個。`SkillCardRow` は手札と獲得の選択肢で共有
- メニュー（[≡]）の Prefab は作らない（`DialogService.ChooseAsync` で足りる）。ヘッダーは P1 の `Header`
- 数が増減する要素は C# が Instantiate する。編成の 5 枠だけは Prefab に固定で置く
- 色・寸法は P1 の `UiTheme` の値だけ。P1・P2 の型だけに依存する

### formation 編成・プロデュースの開始と再開

- 編成は `Home` シーンのページ、プロデュースの局面の画面は `Produce` シーン。シーンの移動は `ISceneNavigator.GoToProduceAsync`／`GoToHomeAsync`
- `Produce` シーンの底はいつも背景のページ `ProduceBackdropPage`。局面が変わるたびに背景より上を全部閉じてから次の画面を積む（確認 13）
- `ProduceFlow`・`ProduceMenu` は `Produce` シーンのスコープ、`ProduceSession`・ユースケースはルート。取り直しは状態を変えたユースケースが行う
- 局面の画面がまだ無い局面は「準備中です」を出してホームへ戻る（確認 14）
- サーバーの `LessonDto` に `UsableHand`（`Key(19)`）を足す
- screen-design 7.6 は `ProduceStateView` と `PRODUCE_STALE_OPERATION` の 2 つの箇条書きだけを直す。プロデュースの画面の流れ（スキルカード獲得のモーダルの出し方を含む）は 7.6 に書かない（確認 15 は了承済み。7.6 と screen-wireframes を直す仕事は week のタスク 5 が実装時に行う。今は直さない）
- **`Produce` シーンを作り直す**: `UiRoot` を置き、`SceneBuilder.BuildProduce` を `Home` と同じ形にして legacy の補助と `using` を消し、`Produce.unity` を作り直し、#24 の `ProduceView`・`ProducePresenter` を消す

### week 週の行動選択・授業・支給・スキルカードの選択肢

- `ProduceFlow.PushForPhaseAsync` に `WeekSelect`（`WeekPage`）と `CardOffer`（`CardOfferModal`）の `case` を足す
- 授業・支給はページ（`WeekPage` の上）。支給は「受け取る」だけ。レッスンは 1 つのボタン → パラメータの選択ダイアログ
- スキルカード獲得のモーダルは背景のページの上に出る（確認 15。了承済み）
- 授業・休むの結果は前後の差を情報ダイアログで出す。カードの選択肢は常に未強化で出す
- `SkillCardRowView.SetTexts`・`FakeProduceFlow`（`ShowCurrentAsync`・`ExitToHomeAsync`・`HandleStepAsync` の上書き）を作る（lesson も使う）。`DisplayText` に `CardTypeName`・`CostKindName`・`Card`・`Costs`・`Change` を足す

### lesson レッスン・試験・試験結果・プロデュース結果

- `ProduceFlow.PushForPhaseAsync` に `Lesson`・`Exam`・`ExamResult`・`Finished` の `case` と、試験結果の先出しを足す
- レッスン・試験の間は同じページをその場で描き直す。局面が変われば `HandleStepAsync` に任せる
- 試験結果の「次へ」は `ShowCurrentAsync`、プロデュース結果の「ホームへ」は `session.Clear()` → `ExitToHomeAsync`
- 順位表の自分の行だけ C# が文字の色を `Accent` に変える。`DisplayText` に `LessonKindName`・`ExamOutcomeText`・`ExamKindName`・`Status`・`Rank` などを足す
- 起動 → ガチャ → 強化 → 編成 → プロデュースを最後まで → 結果 → ホーム、を Windows で通す（完了条件 6）。順 7 は条件 6 を受け持つ。7（Android）は別の項目。game-spec の順 7 の行は「6（7 は別の項目）」に直す（進行役が行った）

## 3. 前提となる未マージの PR

どちらも 2026-11-07 時点で未マージ。**P1 の実装の前に main へ入っていること**を前提にする（確認 1）。計画 0 の実装 PR も、P1 の前に main へマージする（1章）。

| PR | 中身 | 計画への影響 |
|---|---|---|
| **PR #28** | `external/` のサブモジュールを**全部**消す（UnityScreenNavigator・Supplement・VolTech-MagicOnion・MasterMemoryAddons）。サーバー・shared は `packages/` の `.nupkg`、Unity は `client/uta/Packages/manifest.json` の git URL で参照し、`Uta.slnx` から共通基盤が外れる。固定を上げるときは `tools/pack-foundation.sh`（PR #28 が足す ADR） | 共通基盤のソース（USN の `Runtime/ChinpanGX`・README・`AddressableAssetLoader`、VolTech-MagicOnion のコードなど）は checkout に無くなり、`client/uta/Library/PackageCache/<パッケージ名>@<ハッシュ>/`（USN は `com.harumak.unityscreennavigator`。Unity でパッケージを解決した環境にだけある）か GitHub のリポジトリを読む。マージ前は `external/<名前>`。計画の本文のパスは置き換えず、`external/` のソースを読む計画（P1・foundation・owned・week）の前提に読み替えの 1 行を足した |
| **PR #29** | 日本語フォント Noto Sans JP の TMP フォントアセット（ブランチ `claude/noto-sans-jp-font`。`client/uta/Assets/Fonts/NotoSansJP/` に ttf 2 つ・SDF アセット 2 つ・`OFL.txt`） | P1 の前提 2。P1 はこのアセットを使い、作り直さない |

- 2 つが main に入っていれば、P1 の実装ブランチは `main` から切る
- 入っていなければ、P1 の前提 2 のとおり `claude/noto-sans-jp-font` から切ることもできるが、PR #28 の変更（`manifest.json`・`external/` の削除）は入らない。1 つの PR の base は 1 つしか選べないので、**両方を先にマージする**のが単純（確認 1）

## 4. 実装の流れ

[README](README.md)「計画のブランチと PR（流れ）」のとおり。

1. 10 本の計画（とこの索引）は計画用のブランチ（`claude/client-screens-plan`）に `draft` で書き、レビューの指摘を直して 5章の確認が全件了承されたので、全計画を `ready` にした（2026-11-07）。CLAUDE.md の規則の名前は `claude/plan-<件名>` なので、**PR を出す前に `claude/plan-client-screens` に改名する**（進行役が行う）。設計書のレビュー（`requesting-design-review`）は回した
2. ユーザーが承認したら、`main` 向けに docs だけの PR を作る。マージはユーザー
3. マージされたら、1章の順に、計画ごとに固定の worktree（`tools/worktree-pool.sh start <番号> <新ブランチ> <base>`）で実装する。base は 1章の「積み PR の base」。実装は計画のタスクごとに Sonnet のサブエージェントに任せ、計画に無い設計判断に出会ったら止まって計画を直す
4. **実装の PR は計画ごとに分ける**。前の計画の PR が未マージなら積み PR にし（`gh pr create --base <前の計画のブランチ>`）、本文に前の PR を書く
5. コードレビュー（`requesting-code-review`）のあと、実装と設計書のズレを wiki に反映し、計画を `status: done` にする。この索引の 1章の状態も `done` にする。最後の lesson が done になったら、game-spec.md の順 7 の行を done にする（lesson のタスク 5）

## 5. ユーザーの確認（全件が了承済み）

全 17 件が、2026-11-07 にユーザーの推奨どおり（各計画が提案した案のとおり）の了承で解決した（ユーザーの判断）。詳しい中身は各計画の「この計画で決めたこと」の末尾に移してある。計画 0・foundation には確認が要ることは無かった。18 は、2026-11-07 にユーザーが指示したホームの構成の変更（了承済みとして扱う）。

| # | 件名 | 結果 |
|---|---|---|
| 1 | PR #28・#29 と計画 0 のマージを P1 の実装の前に行う | 了承 |
| 2 | Noto Sans JP（SIL OFL 1.1）のライセンス表示 | 了承 |
| 3 | screen-design 7.3 を TMP Settings の作成と既定フォントの設定に直す | 了承 |
| 4 | 見た目の値（色・寸法・フォントサイズ）は仮 | 了承 |
| 5 | タイトルの文字を「Uta」にする | 了承 |
| 6 | プロデュースの画面の叩き台との差（a〜d） | 了承 |
| 7 | 記号の字形 `≡`・`◀`・`＜`（目視で豆腐なら `◀` を変える） | 了承 |
| 8 | ガチャの叩き台との差（4 点） | 了承 |
| 9 | 「もう一度」でも確認ダイアログを出す | 了承 |
| 10 | 育成アイドルの一覧は `IdolId` の順に固定 | 了承 |
| 11 | サポートカードの詳細と強化を 1 ページにする | 了承 |
| 12 | 上限解放の確認文を常に出す | 了承 |
| 13 | `Produce` シーンの底をいつも背景のページにする | 了承 |
| 14 | week・lesson ができるまでの「プロデュース開始」「再開」の動き | 了承 |
| 15 | スキルカード獲得のモーダルは背景のページの上に出す | 了承 |
| 16 | 順位表のライバルの名前は「ライバル 1・2・3」 | 了承 |
| 17 | レッスンが終わるたびに「レッスン終了」のダイアログで止まる | 了承 |
| 18 | ホームの構成の変更と 3D 対応の枠（上部のステータス・キャラ表示のステージ・大きな主ボタン・下部のナビ。ステージは `RawImage` と `ICharacterStage` で、MVP は仮の枠、3D は拡張点。screen-design 7.8） | ユーザーが 2026-11-07 に指示した（了承済み）。P2 の決めたこと 15、foundation の決めたこと 17 |

- 15 のスキルカード獲得のモーダルの出し方も了承された。ただし、screen-design 7.6 と [screen-wireframes.md](../wiki/overview/screen-wireframes.md) の「共通の方針」を直す仕事は week のタスク 5 が実装時に行う（今は直さない）。formation は 7.6 の 2 つの箇条書きだけを直す

## 6. Unity Editor が要るタスクの扱い

- 計画 0 のサーバー側（`dotnet test`）、owned のタスク 1 の shared/、formation のタスク 1 の server・shared は .NET SDK（と Docker）で確かめられる。**それ以外のタスク（P1〜P3 の全部と、C# の計画の client/ の全部、計画 0 のクライアントのテスト）は、Unity 7（`7000.0.0a12`）の Editor がその worktree の `client/uta` を開いている環境が前提**（`unity status --until-ready` が ready）。Prefab の生成（`unity command eval`）・コンパイル（`unity recompile --format json`）・EditMode のテスト（Editor を開いたまま `TestRunnerApi` を eval で回す。P1 の決めたこと 12。**eval で回せなければ、Editor の Test Runner（EditMode）を手動で回してもらい、確認できていないと報告する。止まらない**）・Play の確認は Editor が要る
- **Editor が無い・開けない環境では、そのタスクを実行しない**。「確認できていない」と明記し、ユーザーに実行を頼む。推測で「通った」と書かない（CLAUDE.md「常に守ること」）。Prefab・`.unity`・Addressables の設定は Editor のスクリプトの生成物なので、手で書かない
- Editor は閉じない（テストは開いたまま回す）。Windows の IL2CPP のビルドはバッチモードで行うので、ビルドが要る計画（C# の計画の IL2CPP の確認）は、Editor を開いている worktree とは別のワークスペースで行う（unity7-evaluation 4章。C# の計画の手順に「Editor を閉じてから」とある箇所は、別のワークスペースで行うなら閉じなくてよい）。P1〜P3 ではビルドしない
- Play の目視（文字の豆腐・レイアウト）はユーザーに見てもらう。テスト・Play のあとコミットする前に、フォントの動的データを空に戻す（`unity command eval 'Uta.Client.Editor.Prefabs.FontAssets.ClearDynamicData(); return "ok";'`。foundation 3.3.7。P1 の決めたこと 4）
- worktree は固定のもの（`../Uta-wt/<番号>`）を使い、Unity の Library を作り直さない。PR #28 のあとは共通基盤のソースが checkout に無いので、ソースを読むタスクは Unity でパッケージを解決済みの worktree の `Library/PackageCache` か GitHub で読む
