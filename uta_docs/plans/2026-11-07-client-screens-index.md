---
title: 実装計画: client/ の画面 索引
status: draft
updated: 2026-11-07
spec: ../wiki/overview/game-spec.md
---

# 実装計画: client/ の画面 索引

[game-spec.md](../wiki/overview/game-spec.md)「実装計画の単位と順」の順 7（client/ の画面）は、10 本の計画に分けて実装する。このページは、10 本の順番と関係、各計画の「決めたこと」と「ユーザーの確認が要ること」の要約、実装の前提と流れをまとめる。**ユーザーはこのページだけを読めば判断できる**ように書く。細部の正は各計画（要約と食い違ったら各計画が正）。方針の正は [screen-design.md](../wiki/architecture/screen-design.md) 7章（Prefab・Addressables・TextMeshPro）と 8章（シーン構成）、計画の進め方は [README](README.md)「計画のブランチと PR（流れ）」。

## 1. 順番と関係

Prefab の計画（P1〜P3）は、画面の Prefab と View の骨格（`[SerializeField]` のフィールドだけ）を Editor のスクリプトで生成する。C# の計画（foundation〜lesson）は、その骨格に `IXxxView` の実装と Presenter・ユースケースを足す。フィールド名と Prefab の子の対応は Prefab の計画の表が正。

| 順 | 計画 | 何を作るか | 前の計画 | 積み PR の base | 状態 |
|---|---|---|---|---|---|
| 1 | [計画 0 マスタ スキルカードの名前と説明文](2026-11-03-masterdata-card-text.md) | `Card.Name`・`CardFace.Description` の列と 36 枚の値（masterdata・サーバーのテスト） | なし | `main` | draft |
| 2 | [P1 共通部品・Addressables・TextMeshPro](2026-11-07-client-prefabs-common.md) | スパイク、TMP Settings、共通部品の Prefab 10 個（`UiRoot` を含む）、生成の補助 `PrefabBuildUtil`、`PrefabContractTests`、`BuildCommands` の `BuildPlayerContent` | なし（計画 0 の型は使わない）。PR #28・#29 | `main`（PR #28・#29 がマージ済みのとき。3章） | draft |
| 3 | [P2 Title・Home・ガチャ・所持一覧の Prefab](2026-11-07-client-prefabs-screens-a.md) | 画面の Prefab 7 個・行の Prefab 3 個・View の骨格 8 型、画面の補助 `ScreenLayout` | P1 | P1 の実装ブランチ | draft |
| 4 | [foundation 土台・タイトル・ホーム](2026-11-03-client-screens-foundation.md) | ダイアログ・ブロッカー・`PageStack`・シーンのスコープへの登録、`IGameMaster`、業務エラー、タイトル・ホーム、`Bootstrap`・`Title`・`Home` シーンの `UiRoot` | 計画 0・P1・P2 | P2 の実装ブランチ | draft |
| 5 | [gacha ガチャ](2026-11-03-client-screens-gacha.md) | ガチャのページ・排出率・結果のモーダル、`IGachaPort`、`PortResult<T>`・`ServerReasons`・`FakeGameMaster` | foundation・P2 | foundation の実装ブランチ | draft |
| 6 | [owned 所持一覧・サポートカードの強化](2026-11-03-client-screens-owned.md) | 所持一覧（選択モード付き）・サポートカードの詳細と強化、`DisplayText`、`FormationValidator.IsPlanCompatible`（shared/） | gacha | gacha の実装ブランチ | draft |
| 7 | [P3 編成・プロデュースの Prefab](2026-11-07-client-prefabs-screens-b.md) | 画面の Prefab 10 個・行の Prefab 5 個・View の骨格 | P1・P2（C# の型には依存しない） | owned の実装ブランチ | draft |
| 8 | [formation 編成・プロデュースの開始と再開](2026-11-03-client-screens-formation.md) | 編成のページ、`IProducePort`・`ProduceSession`・`ProduceFlow`・`ProduceMenu`、`Produce` シーンの作り直し（`UiRoot`）、`LessonDto.UsableHand`（server・shared） | owned・P3 | P3 の実装ブランチ | draft |
| 9 | [week 週の行動選択・授業・支給・スキルカードの選択肢](2026-11-03-client-screens-week.md) | 週・授業・支給のページ、カードの選択肢のモーダル、`ProduceFlow` の `WeekSelect`・`CardOffer`、`FakeProduceFlow` | formation | formation の実装ブランチ | draft |
| 10 | [lesson レッスン・試験・試験結果・プロデュース結果](2026-11-03-client-screens-lesson.md) | レッスンのページ・山札などの一覧・試験結果・プロデュース結果、`ProduceFlow` の残りの局面。完了条件 6 の通し | week | week の実装ブランチ | draft |

- 並行しない。後の計画は前の計画の型・偽・Prefab を使う（各計画の 1章の前提）
- 「積み PR の base」は実装の PR（4章）の base。前の計画の実装 PR が main に入ったら、後の PR の base を `main` に付け替える（CLAUDE.md「Git」。付け替えとマージはユーザーが行う）
- 状態は各計画の frontmatter の `status`。計画が `done` になったら、この表も `done` にする

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
- `unity test --mode EditMode` が動くことを確かめ、unity7-evaluation 4章と CLAUDE.md に足す

### P2 Title・Home・ガチャ・所持一覧の Prefab

- 画面の Prefab 7 個（`TitlePage`・`HomePage`・`GachaPage`・`GachaRatesModal`・`GachaResultModal`・`OwnedPage`・`SupportDetailPage`）と行の Prefab 3 個。所持一覧の行は P1 の `UtaListRow`
- ページの形は `Header` →（`TabBar`）→ `Main`。組み方の補助 `ScreenLayout` は P3 も使う
- レベルのボタン 3 つは固定で置く。所持一覧の並び・絞り込みは 2 段。0 件の「ありません」は一覧の外
- 画面ごとのテストで「View のフィールドが表の子を指す」ことを確かめる。P1 の型だけに依存する

### foundation 土台・タイトル・ホーム

- PR #24 のシーン構成の上に作る。起動の流れは `Title` シーンの `TitlePresenter`、ホームは `Home` シーンの `HomePresenter`。基盤の `IScreenNavigator` をそのまま使い、`Replace` は作らない
- C# で UI を組まない（旧版の `UiFactory` などは消す）。`UiRoot` は `Bootstrap`・`Title`・`Home` に置き、各シーンのスコープが `SceneUiInstaller.Install` で登録する。ルートの共通エラーとブロッカーは `ActiveSceneUi` で「いまのシーン」の UI を見つける
- 診断（`-uta-quit-after-check`）は `TitleDiagnostics` に分ける。タイトルの失敗は状態の文を出してもう一度タップ
- ホームの 4 つのボタンは「準備中です。」で、テストはボタンごとに 4 本（`ProduceClicked_`・`ResumeClicked_`・`GachaClicked_`・`OwnedClicked_ShowsNotReady`）。後の計画は自分のボタンのテストだけを差し替える
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
- メニュー（[≡]）の Prefab は作らない（`DialogService.ChooseAsync` で足りる）。ヘッダーは P1 の `UtaHeader`
- 数が増減する要素は C# が Instantiate する。編成の 5 枠だけは Prefab に固定で置く
- 色・寸法は P1 の `UiTheme` の値だけ。P1・P2 の型だけに依存する

### formation 編成・プロデュースの開始と再開

- 編成は `Home` シーンのページ、プロデュースの局面の画面は `Produce` シーン。シーンの移動は `ISceneNavigator.GoToProduceAsync`／`GoToHomeAsync`
- `Produce` シーンの底はいつも背景のページ `ProduceBackdropPage`。局面が変わるたびに背景より上を全部閉じてから次の画面を積む（確認 13）
- `ProduceFlow`・`ProduceMenu` は `Produce` シーンのスコープ、`ProduceSession`・ユースケースはルート。取り直しは状態を変えたユースケースが行う
- 局面の画面がまだ無い局面は「準備中です」を出してホームへ戻る（確認 14）
- サーバーの `LessonDto` に `UsableHand`（`Key(19)`）を足す
- **`Produce` シーンを作り直す**: `UiRoot` を置き、`SceneBuilder.BuildProduce` を `Home` と同じ形にして legacy の補助と `using` を消し、`Produce.unity` を作り直し、#24 の `ProduceView`・`ProducePresenter` を消す

### week 週の行動選択・授業・支給・スキルカードの選択肢

- `ProduceFlow.PushForPhaseAsync` に `WeekSelect`（`WeekPage`）と `CardOffer`（`CardOfferModal`）の `case` を足す
- 授業・支給はページ（`WeekPage` の上）。支給は「受け取る」だけ。レッスンは 1 つのボタン → パラメータの選択ダイアログ
- スキルカード獲得のモーダルは背景のページの上に出る（確認 15。保留）
- 授業・休むの結果は前後の差を情報ダイアログで出す。カードの選択肢は常に未強化で出す
- `SkillCardRowView.SetTexts`・`FakeProduceFlow`（`ShowCurrentAsync`・`ExitToHomeAsync`・`HandleStepAsync` の上書き）を作る（lesson も使う）。`DisplayText` に `CardTypeName`・`CostKindName`・`Card`・`Costs`・`Change` を足す

### lesson レッスン・試験・試験結果・プロデュース結果

- `ProduceFlow.PushForPhaseAsync` に `Lesson`・`Exam`・`ExamResult`・`Finished` の `case` と、試験結果の先出しを足す
- レッスン・試験の間は同じページをその場で描き直す。局面が変われば `HandleStepAsync` に任せる
- 試験結果の「次へ」は `ShowCurrentAsync`、プロデュース結果の「ホームへ」は `session.Clear()` → `ExitToHomeAsync`
- 順位表の自分の行だけ C# が文字の色を `Accent` に変える。`DisplayText` に `LessonKindName`・`ExamOutcomeText`・`ExamKindName`・`Status`・`Rank` などを足す
- 起動 → ガチャ → 強化 → 編成 → プロデュースを最後まで → 結果 → ホーム、を Windows で通す（完了条件 6）

## 3. 前提となる未マージの PR

どちらも 2026-11-07 時点で未マージ。**P1 の実装の前に main へ入っていること**を前提にする（確認 1）。

| PR | 中身 | 計画への影響 |
|---|---|---|
| **PR #28** | `external/` のサブモジュールを**全部**消す（UnityScreenNavigator・Supplement・VolTech-MagicOnion・MasterMemoryAddons）。サーバー・shared は `packages/` の `.nupkg`、Unity は `client/uta/Packages/manifest.json` の git URL で参照し、`Uta.slnx` から共通基盤が外れる。固定を上げるときは `tools/pack-foundation.sh`（PR #28 が足す ADR） | 共通基盤のソース（USN の `Runtime/ChinpanGX`・README・`AddressableAssetLoader`、VolTech-MagicOnion のコードなど）は checkout に無くなり、`client/uta/Library/PackageCache/<パッケージ名>@<ハッシュ>/`（USN は `com.harumak.unityscreennavigator`。Unity でパッケージを解決した環境にだけある）か GitHub のリポジトリを読む。マージ前は `external/<名前>`。計画の本文のパスは置き換えず、`external/` のソースを読む計画（P1・foundation・owned・week）の前提に読み替えの 1 行を足した |
| **PR #29** | 日本語フォント Noto Sans JP の TMP フォントアセット（ブランチ `claude/noto-sans-jp-font`。`client/uta/Assets/Fonts/NotoSansJP/` に ttf 2 つ・SDF アセット 2 つ・`OFL.txt`） | P1 の前提 2。P1 はこのアセットを使い、作り直さない |

- 2 つが main に入っていれば、P1 の実装ブランチは `main` から切る
- 入っていなければ、P1 の前提 2 のとおり `claude/noto-sans-jp-font` から切ることもできるが、PR #28 の変更（`manifest.json`・`external/` の削除）は入らない。1 つの PR の base は 1 つしか選べないので、**両方を先にマージする**のが単純（確認 1）

## 4. 実装の流れ

[README](README.md)「計画のブランチと PR（流れ）」のとおり。

1. 10 本の計画（とこの索引）は計画用のブランチ（`claude/client-screens-plan`）に `draft` で書いた。設計書のレビュー（`requesting-design-review`）を回し、5章の確認の答えを反映して `ready` にする
2. ユーザーが承認したら、`main` 向けに docs だけの PR を作る。マージはユーザー
3. マージされたら、1章の順に、計画ごとに固定の worktree（`tools/worktree-pool.sh start <番号> <新ブランチ> <base>`）で実装する。base は 1章の「積み PR の base」。実装は計画のタスクごとに Sonnet のサブエージェントに任せ、計画に無い設計判断に出会ったら止まって計画を直す
4. **実装の PR は計画ごとに分ける**。前の計画の PR が未マージなら積み PR にし（`gh pr create --base <前の計画のブランチ>`）、本文に前の PR を書く
5. コードレビュー（`requesting-code-review`）のあと、実装と設計書のズレを wiki に反映し、計画を `status: done` にする。この索引の 1章の状態も `done` にする。最後の lesson が done になったら、game-spec.md の順 7 の行を done にする（lesson のタスク 5）

## 5. ユーザーの確認が要ること（全計画ぶん）

各計画の「ユーザーの確認が要ること」を 1 か所に集め、重複をまとめた。番号はこのページの通し番号（括弧の中が元の計画と番号）。

**進め方**

1. **PR #28・#29 を、P1 の実装の前に main へマージしてよいか**（3章）。片方だけでは P1 の前提がそろわない

**見た目・部品（P1・P2・P3）**

2. **Noto Sans JP（SIL OFL 1.1）のライセンス表示**: `OFL.txt` をリポジトリに入れ、アプリ内のライセンス表示の画面は MVP では作らず、ストアに出すときに作る（P1 の 1）
3. **screen-design 7.3 の「TMP の Essential Resources を取り込む」を「プロジェクトの TMP Settings を作り、既定のフォントを設定する」に直したこと**（P1 の 2）。Unity 7 の uGUI 7.0.0 で取り込み自体が無くなったため。2026-11-07 のユーザーの判断の中身を変えるので確認する
4. **見た目の値（色・寸法・フォントサイズ）は仮**（P1 の 3、P2 の 2）。旧 foundation の値に、ヘッダーの色と二次ボタンの色を足した。角丸・アイコンの画像は使わない
5. **タイトルの文字を「Uta」にしてよいか**（P2 の 1）。ロゴの画像は作らない
6. **プロデュースの画面の叩き台との差**（P3 の 1）: a. 手札は縦の一覧（1 枚 = 名前・消費・説明文の 3 行）、b. ステータスは文字の行（札にしない）、c. 週の行動は縦に並んだボタン（2×2 にしない）、d. 順位表は 1 行 1 文字列で、自分の行の文字だけ `Accent` の色
7. **記号の字形 `≡`・`◀`・`＜` が Noto Sans JP にある前提**（P3 の 2、lesson の 3）。`◀` は推測。目視で豆腐なら、順位表の自分の印を `"（あなた）"` などに変える（その時点で確認する）

**ガチャ（gacha）**

8. **叩き台（screen-wireframes 9）との差**（gacha の 1）: ガチャ名の行は固定の「ガチャ」、画像は出さない、結果は 1 枠 1 行の文字、「NEW」は育成アイドルを初めて得たときだけ（サポートカードは「所持に追加」）
9. **結果の「もう一度」でも、引く前の確認ダイアログを出してよいか**（gacha の 2）

**所持一覧・強化（owned）**

10. **育成アイドルの一覧を `IdolId` の順に固定し、並び・絞り込みをサポートカードだけにしてよいか**（owned の 1。叩き台 10 からの縮小）
11. **サポートカードの詳細と強化を 1 ページにしてよいか**（owned の 2。screen-design 2・3章の「詳細 ─▶ 強化」の 2 枚からの変更）
12. **上限解放の確認文「強化に使った強化pt・ゴールドは戻りません」を、素材の Lv にかかわらず常に出してよいか**（owned の 3。仕様 2.2.1 は「素材のレベルが 1 より大きいとき」）

**プロデュース（formation・week・lesson）**

13. **`Produce` シーンの底をいつも背景のページ（週・パラメータ・体力だけの画面）にしてよいか**（formation の 1）。局面が変わるたびに、一瞬この背景が見えてから次の画面が出る
14. **week・lesson ができるまで、「プロデュース開始」「再開」は `Produce` シーンへ移ってすぐ「この局面の画面は準備中です: WeekSelect」を出してホームへ戻る動きになる**（formation の 2）。セッションはサーバーに残る
15. **[保留。ユーザーの了承待ち] スキルカード獲得のモーダルを、授業・支給のページの上ではなく、背景のページの上に出してよいか**（week の 1）。[screen-wireframes.md](../wiki/overview/screen-wireframes.md)「共通の方針」の決定（2026-11-01。ユーザーの判断）は「レッスン・授業・支給の画面の上に重ねる」。13 の「局面が変わるたびに背景より上を全部閉じる」に合わせると、モーダルが出る前に授業・支給のページ（lesson ではレッスンのページ）は閉じる。授業の上がった値のダイアログは閉じる前に出すので、結果は見える。代わりの案（`CardOffer` のときだけ閉じない分岐を `ProduceFlow` に入れる）は採っていない。**了承が出るまで screen-wireframes.md と screen-design 7.6 は直さない**（了承されたら week のタスク 5 で screen-wireframes の決定の文を直す）
16. **試験の順位表のライバルの名前を、仮に「ライバル 1・2・3」にしてよいか**（lesson の 1）。`ExamResultDto` は NPC の名前を返さないため
17. **レッスンが終わるたびに「レッスン終了」と上がった値のダイアログで 1 回止まる動きでよいか**（lesson の 2。ターン終了時の効果の表示を MVP から外した代わり）

計画 0・foundation には確認が要ることは無い（計画 0 のカードの名前は確認済み。foundation の判断は 2026-11-07 に決めたことに移した）。

## 6. Unity Editor が要るタスクの扱い

- 計画 0 のサーバー側（`dotnet test`）、owned のタスク 1 の shared/、formation のタスク 1 の server・shared は .NET SDK（と Docker）で確かめられる。**それ以外のタスク（P1〜P3 の全部と、C# の計画の client/ の全部、計画 0 のクライアントのテスト）は、Unity 7（`7000.0.0a12`）の Editor がその worktree の `client/uta` を開いている環境が前提**（`unity status --until-ready` が ready）。Prefab の生成（`unity command eval`）・コンパイル（`unity recompile --format json`）・EditMode のテスト（`unity test --mode EditMode`。P1 のタスク 5 で使えると確かめてから）・Play の確認は Editor が要る
- **Editor が無い・開けない環境では、そのタスクを実行しない**。「確認できていない」と明記し、ユーザーに実行を頼む。推測で「通った」と書かない（CLAUDE.md「常に守ること」）。Prefab・`.unity`・Addressables の設定は Editor のスクリプトの生成物なので、手で書かない
- Windows の IL2CPP のビルドはバッチモードで行い、同じプロジェクトを開いている Editor を先に閉じる（CLAUDE.md「ビルド・テスト・実行」）
- Play の目視（文字の豆腐・レイアウト）はユーザーに見てもらう。テスト・Play のあとコミットする前に、フォントの動的データを空に戻す（`unity command eval 'Uta.Client.Editor.Prefabs.FontAssets.ClearDynamicData(); return "ok";'`。foundation 3.3.7。P1 の決めたこと 4）
- worktree は固定のもの（`../Uta-wt/<番号>`）を使い、Unity の Library を作り直さない。PR #28 のあとは共通基盤のソースが checkout に無いので、ソースを読むタスクは Unity でパッケージを解決済みの worktree の `Library/PackageCache` か GitHub で読む
