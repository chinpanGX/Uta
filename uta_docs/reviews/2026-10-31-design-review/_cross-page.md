# ページをまたぐ矛盾

設計書レビュー（main `e365ac0`）で見つかった、ページ間の食い違い。どちらが正しいかは決めず、両方の場所を挙げる。「見つけたページ」は、どのページのレビューで出たか。

| # | 一方 | もう一方 | 食い違い | 見つけたページ |
|---|---|---|---|---|
| X1 | game-spec.md 56〜62行目（MVP）、glossary.md 22行目「MVP」、ADR 0003 16行目 | gacha.md 6章（GAC-03・04 が必須）、idol-support.md 4章（IDL-02・SUP-02・SUP-03 が必須）、produce.md 9章（PRD-09・10・12 が次点）と 3.2・3.3・5.1（相談・ドリンク・育成アイテムが週の流れに組み込まれている） | MVP に入る範囲。「必須」と MVP の関係が未定義 | game-spec |
| X2 | game-spec.md 52行目、produce.md 6.1（報酬はガチャ通貨・ゴールド・経験値で決定） | screen-wireframes.md 382行目「報酬の中身は未決」、374〜375行目（報酬に「信頼度 Lv 6 → 7」） | 報酬が決まっているか。信頼度が報酬に入るか（produce.md の未決事項には「信頼度の上がり方が未定義」） | game-spec |
| X3 | game-spec.md 32行目「ゲームループ（推測・未確定）」、56行目「MVP … 推測」 | ADR 0003 16行目（MVP は「ガチャ → 編成 → プロデュース → 報酬」と決定） | 決まったことが推測扱い | game-spec |
| X4 | game-spec.md 35行目「報酬（疑似通貨）」、46行目「強化素材」 | produce.md 6.1・glossary.md 65行目「ガチャ通貨」、idol-support.md 1.5・1.6・2.2（育成素材・かけら・サポート強化pt） | 同じものを別の名前で呼んでいる／用語集にない語 | game-spec |
| X5 | game-spec.md 52行目、produce.md 6.1（ゴールドはプロデュースの報酬で入手） | idol-support.md 未決事項 220行目「素材・ゴールドの入手手段」 | ゴールドの入手手段が一方では決定、もう一方では未決 | game-spec |
| X6 | game-spec.md 30行目、repository-layout.md 1章（実装設計書は 4 編） | index.md 33〜39行目（「4編」の見出しの下に lesson-domain-types.md を含む 5 件） | 4 編の範囲 | game-spec |
| X7 | game-spec.md 68行目、masterdata-pipeline.md 6章、screen-design.md 4章（いまはアプリに同梱） | api-design.md 1章 59行目（「バイナリを配信で取得する」） | 今のマスタの入手方法 | game-spec |
| X8 | game-spec.md 19行目（ADR 0003 は accepted で、3D ライブは MVP 対象外） | live.md 9行目（「MVP から外す方向」） | 確定か、方向にとどまるか | game-spec |
| X9 | game-spec.md 53行目、gacha.md 8章（初期付与はガチャ通貨だけ） | idol-support.md 1.8（R を全員分所持、最初のプロデュースで SSR を獲得。[確認] のままで採否が書かれていない） | 初期の所持品 | game-spec |
| X10 | ADR 0008 27行目、ADR 0009 17行目（「`noEngineReferences` は使わない」） | repository-layout.md 3章 90行目・4章 100行目（「`noEngineReferences: true` にする［決定。2026-10-28］」） | **ADR と仕様ページの矛盾**。repository-layout.md のレビューで Critical になりうる。ADR を置き換えるなら新しい ADR が要る | game-spec（対象外として参考） |
| X11 | api-design.md 5章（放棄と破棄の扱いを決定。2026-10-28） | screen-wireframes.md 107行目・494行目（「破棄の扱いは未決」） | 破棄の扱いが決まっているか | game-spec（対象外として参考） |
| X12 | ADR 0011（2026-10-28 の追記あり） | ADR 0010 20行目（「マイグレーションのツールは別途決める」のまま。ADR 0012 への追記なし） | ADR の追記の書き方がそろっていない（ADR は見直し対象外なので参考） | game-spec（対象外として参考） |
| X13 | gacha.md 6章「天井と期間」100行目（バッチ処理が必要） | gacha.md 8章 119行目、api-design.md 1章 `SyncAsync`、screen-design.md 4章、glossary「同期」（バッチにしない） | 交換pt の変換方法 | gacha |
| X14 | gacha.md 4章（常設の交換pt はラインナップ更新時にリセット） | api-design.md 1章 `SyncAsync`（期間が終わったガチャは変換）、3章 `gacha_exchange_points`（`player_id + gacha_id`） | リセットか変換か。リセットする処理がない | gacha |
| X15 | gacha.md 6章 GAC-04（かけら → 交換メダルの変換が必須） | api-design.md 1章 `IGachaService`（RPC なし）、screen-design.md 2章（画面なし）、gacha.md 8章（ショップは後回し） | 必須の機能に API と画面がない | gacha |
| X16 | gacha.md 2章（割引単発は [Uta案]）、6章 GAC 表（記載なし） | screen-wireframes.md 9章（`[ 1日1回 60 ]` ボタン）、api-design.md 1章 `DrawAsync`（単発・10 連だけ）・`SyncAsync`（日付更新の処理は「いまは無し」）・3章（最後に引いた日の列なし） | 割引単発が範囲に入っているか | gacha |
| X17 | gacha.md 5章（かけらをカードのレアで換算） | idol-support.md 1.6・1.8・1.9、glossary「かけら」（アイドルごと）、api-design.md 3章（`player_idols`、「推測」のまま） | かけらを持つ単位。1.8 と 1.9 で同じアイドルに複数カードがあるかも食い違って読める | gacha |
| X18 | produce.md 6.1（「C 100 / B 200 / A 300 / S 500。単発 250 なので 1 回のプロデュースで 0.5〜2 回分」） | gacha.md 2章（単発 250） | 100 ÷ 250 = 0.4 で、下限の「0.5」が合わない | gacha |
| X19 | glossary「交換pt」（200pt でピックアップと交換） | gacha.md 4章（常設ガチャにも 200pt の交換がある） | 定義の範囲 | gacha |
| X20 | api-design.md 1章（交換pt の変換は「gacha.md 4章」を参照）、screen-wireframes.md 9章 422行目（排出率は「gacha 6章」） | gacha.md 8章 / gacha.md 3章 | 参照する章の番号のずれ | gacha |
| X21 | produce.md 6.1（報酬にゴールドを含む） | screen-wireframes.md 8章（最終評価の報酬欄にゴールドがない） | 報酬の表示（X2 と関連） | gacha（対象外として参考） |
| X22 | idol-support.md 2.2 133行目（レベル上げは強化pt） | produce.md 6.1 323行目（ゴールドでサポートカードのレベルアップ [決定]）、screen-wireframes.md 10章 473行目（強化pt だけの表示） | **レベル上げの消費が正面から食い違う（idol-support.md の Critical）** | idol-support |
| X23 | idol-support.md 2.2 134行目、gacha.md 5章（重複は上限解放に使う） | api-design.md 1章 `LimitBreakAsync`・3章 `player_support_cards`（未使用の重複枚数の列なし） | 上限解放が自動か手動か（idol-support.md の Critical） | idol-support |
| X24 | idol-support.md 2.5 178・180行目（そのターンだけ強化） | lesson-card.md 12章 440行目、glossary「レッスン中強化」（レッスン中ずっと）、produce.md 10.5（フラグのみ） | 強化が続く期間 | idol-support |
| X25 | idol-support.md 1.4 56行目（比率は Uta 独自 [決定]） | produce.md 5.1 225〜232行目（割り振りは 4 型のみ）・10.3 450行目 | 試験の審査ターンの割り振り | idol-support |
| X26 | idol-support.md 1.7 99行目「全特別レッスン率」、2.3 154行目 | screen-wireframes.md 1章 131行目・10章 477行目（パラメータ別）、produce.md 10.3 434行目（週 1 回の判定） | 率の単位と判定の回数（乱数の順に影響） | idol-support |
| X27 | idol-support.md 1.8 106〜108行目（かけら 100 で解放、R 全員の初期所持、SSR を選ぶ） | gacha.md 5章・8章、api-design.md 1・3章、screen-design.md、game-spec.md・gacha.md の未決事項 | 入手の経路と置き場がない（X9 と関連） | idol-support |
| X28 | idol-support.md 1.5・2.2（素材 1〜4、強化pt） | api-design.md 3章 87〜89・96行目（`players` の列・汎用所持テーブルにしない）、produce.md 6.1（ランク以外の報酬は作らない） | 保存の場所と入手手段 | idol-support |
| X29 | idol-support.md 1.2・2.3（+8.5%） | masterdata-pipeline.md 3章 73行目（倍率はパーセント）、ADR 0007、glossary（確率は万分率・倍率はパーセント） | 整数の単位 | idol-support |
| X30 | idol-support.md 2.3 のトリガーの種類 | masterdata-pipeline.md 3.1 155〜164行目（`TriggerKind` はレッスンのみ）・120行目（アビリティにも流用する）、lesson-card.md 7章（必要な op がない） | 決定済みの enum で表せない | idol-support |
| X31 | idol-support.md 1.2（信頼度がない） | produce.md 2章・3.5・5.1・5.2・10.5、api-design.md 3章 | 信頼度の持ち場所がない | idol-support |
| X32 | idol-support.md 2.1（プランの制約なし） | screen-wireframes.md 1章 138行目（プランが合わないカードは選べない） | 編成のルールがワイヤーにしかない | idol-support |
| X33 | idol-support.md 2.6（レンタルの位置なし） | produce.md 10.3、lesson-card.md 10章 414行目（「編成の順」） | 乱数・効果の順が決まらない | idol-support |
| X34 | idol-support.md 1.2・IDL-03（パラメータボーナス） | produce.md 4.2 203行目（仕上げボーナスの 3 等分） | 処理の順 | idol-support |
| X35 | idol-support.md 未決事項 220行目（ゴールドの入手手段が未決） | produce.md 6.1（決定済み） | 古い未決事項（X5 と同じ） | idol-support |
| X36 | idol-support.md 3章 197行目 | gacha.md 7章 107行目 | 完凸後の重複の扱いを 2 ページが持つ | idol-support |
| X37 | idol-support.md SUP-05、produce.md 10.3 460行目（スキルカードサポートはフェーズ 5 以降） | lesson-card.md 15.6（フェーズ 5 に含まれない） | フェーズ | idol-support |
| X38 | api-design.md 1章 44行目（`IProduceService` は PRD-01〜14、SUP-06） | idol-support.md IDL-03・IDL-04・SUP-03〜05（プロデュース中に動く） | 機能 ID の対応の抜け | idol-support |

| X39 | produce.md 5.2 261行目・6章 314行目（千分率）、api-design.md 4章 150行目 | glossary.md 93行目、masterdata-pipeline.md 3章 73行目、lesson-domain-types.md 19行目（倍率はパーセント） | 倍率の単位（X29 と合わせて決める） | produce |
| X40 | produce.md 10.5 492行目・10.6 507行目（マスタの版・ルールの版） | api-design.md 3.1 111〜118行目・5章 154〜161行目（`app_version`・`state_version` だけ）、repository-layout.md 10.5 | 版の定義と破棄の条件がない | produce |
| X41 | produce.md API案 519〜528行目・10.4 468〜477行目 | api-design.md 1章 44行目（`AbandonAsync`）・54〜55行目（`ExecuteLessonActionAsync` の範囲）、screen-wireframes.md 41〜44行目 | `AbandonAsync` がない。相談・選択肢・ドリンク破棄・再挑戦の RPC がない（produce.md の Critical） | produce |
| X42 | produce.md 5.1 218〜219行目・6.1・7章 332行目（不合格で終了、経験値は不合格区分あり） | api-design.md 3章 93行目（`produce_results` の終わり方に不合格がない） | 不合格の扱い（produce.md の Critical） | produce |
| X43 | produce.md 10.3 443・445行目（カードごとに判定） | idol-support.md 2.4 164行目・3章 198行目（イベントごとに %） | サポートイベントの判定の単位 | produce |
| X44 | produce.md 3.5 160行目（共通カードは提示されない、おすすめ効果で絞る） | idol-support.md 1.2 37行目（共通はどのプランでも登場）、lesson-card.md 3.1・masterdata-pipeline.md 3章 92行目・`CardDefinition`（おすすめ効果の列がない） | 提示の候補の絞り込み | produce |
| X45 | produce.md 6章の式 | screen-wireframes.md 365〜369行目（7,130 + 1,700 + 2,650 = 11,480 なのに「11,620」「A+」。式では A） | 例の数値の計算違い（ワイヤーは「仕様が正」） | produce |
| X46 | produce.md 4.2（仕上げもパラメータを 1 つ選ぶ）・10.4 470行目 | screen-wireframes.md 184行目（仕上げ・試験の週は行動を選ばず次へ） | 仕上げのパラメータを選ぶ画面と操作 | produce |
| X47 | produce.md 10.0 376行目（[決定] の根拠） | lesson-card.md 9.6（[Uta案]、`decimal` も残る） | 提案の節を決定の根拠にしている | produce |
| X48 | produce.md 10.2 406行目（「実装時に固定」） | lesson-domain-types.md 4章 97行目（[Uta案]）、`shared/Uta.Domain/Rng/RandomStreams.cs` 65行目（実装済み・テストベクタあり） | ラベルと実装の状態のずれ | produce |
| X49 | produce.md 10.5 487行目（根拠に ADR 0010） | ADR 0010 17行目（該当の列の記述なし） | 根拠の取り違え | produce |
| X50 | produce.md 5.3 277行目（上級は R にライバルのスコア増加を上乗せ） | lesson-card.md 7章・masterdata-pipeline.md 3.1（表す `Op` がない） | 効果を表せない | produce |
| X51 | produce.md 5.1 242行目（中間合格で育成アイテムを「獲得」） | screen-wireframes.md 354行目（選択式かは未決） | 選ぶのか固定か | produce |

| X52 | masterdata-pipeline.md 3.1・glossary.md 83行目・lesson-card.md 7章（`Op` / `TriggerKind`、`StatusKind` に `Genki`・`CostDown`/`CostUp`、`EveryNTurns`、`Ge`/`Le`/`Eq`、`Turn`/`TargetPercent`、型付きの `Status`/`Trigger` 列、`Persistent` の `Arg1`=ターン数） | `shared/Uta.Domain/Lesson/LessonEnums.cs`・`EffectExecutor.cs`・lesson-domain-types.md 2.1（`EffectOp` / `PersistentTrigger`、`Genki` なし・`CostHalf`/`CostDouble`、`EveryNTurns` なし、`GreaterOrEqual` 等、`TurnNumber`/`ClearTargetPercent`、`Arg1` に int、`RatioSource`/`ResourceKind`、`Persistent` の `Arg1`=トリガー） | **enum の名前・値・列の対応がマスタの仕様とコードで別物（lesson-card.md の Critical）** | lesson-card |
| X53 | lesson-card.md 7.5 `condition?`・masterdata-pipeline.md 3.1 147行目（発動のたびに見る） | lesson-domain-types.md 2.1 68行目（子の効果内条件）・`EffectExecutor.cs` 73〜81行目（付与時の効果内条件として判定） | **持続効果の条件の意味（lesson-card.md の Critical）** | lesson-card |
| X54 | api-design.md 3.1 126行目（持続効果・発動予約は効果グループ ID で保存） | `LessonState.cs` の `PersistentEffect` / `ScheduledEffect`（`EffectDef` の一覧でグループ ID なし） | 保存の形に変換できない | lesson-card |
| X55 | lesson-card.md 9.6（[Uta案] decimal も可）。produce.md 10.0・repository-layout.md 118行目・ADR 0007 37行目・masterdata-pipeline.md 81行目が根拠として参照 | lesson-domain-types.md 19行目（[決定] 整数の固定小数点）、コード（`decimal` なし） | 数値型が案のまま（X47 と同じ） | lesson-card |
| X56 | lesson-card.md 9.6・13章 452行目（「サーバー検証」） | ADR 0008・repository-layout.md 6章 135行目（予測はずれてよい、確定はサーバー） | 計算の一致の目的 | lesson-card |
| X57 | lesson-card.md 11章 431行目・produce.md 10.5 496行目・api-design.md 3.1 125行目（初付与のフェーズを記録） | `StatusSet.cs`（bool の印） | 保存の形の言葉 | lesson-card |
| X58 | lesson-card.md 8章（付与時に指定）・masterdata-pipeline.md 3.1 138行目（`AddPercent` の `Arg1` がターン数、0 = ずっと） | lesson-domain-types.md 85行目（フェーズ 4 で決める。いまは値だけ） | 集中強化・好印象強化の持続 | lesson-card |
| X59 | lesson-card.md 4章（「山札が 0 枚になった瞬間」に再シャッフル） | lesson-domain-types.md 5章 [Uta案]・`Zones.Draw`（ドロー時にも再シャッフル） | 乱数を引く時点（再現性） | lesson-card |
| X60 | lesson-card.md 15.2（フェーズ 1 はコスト consume・hpDirect のみ） | lesson-domain-types.md 6章・コード（コスト・条件は全種類を先に実装） | 実装範囲のずれ | lesson-card |
| X61 | lesson-card.md 15.3〜15.5（`AddFixedGenki` 等をフェーズ 2 に割り当て） | masterdata-pipeline.md 3.1 149行目（どのフェーズのカードにも使われないものは「未定」） | フェーズの割り当て | lesson-card |

| X62 | repository-layout.md 3章 76・88行目（基盤は 2 つに分ける。根拠は基盤リポジトリ `docs/design/project-structure.md`） | server-foundation.md「プロジェクト構成」24〜36行目（6 プロジェクト） | 基盤のプロジェクトの分け方 | repository-layout |
| X63 | server-foundation.md「Uta での扱い（推測）」84行目（`shared/` に Uta 固有の「マスタ型」を置く） | repository-layout.md 2章 45・71行目、CLAUDE.md、ADR 0007（accepted。マスタ型は `shared/` に置かない） | **ADR 0007 に反する（server-foundation.md のレビューで Critical 相当）** | repository-layout |
| X64 | repository-layout.md 未決事項 312行目・api-design.md 23行目（`Uta.Shared` は Uta 固有のものだけ置き `Abstractions` を参照。2026-10-28 に解決） | ADR 0005 未決事項 61行目・server-foundation.md 未確定事項 89行目（未決のまま） | `Uta.Shared` と基盤の `Abstractions` の関係 | repository-layout |
| X65 | repository-layout.md 11章、ADR 0010・0012（accepted。PostgreSQL・Dapper・DbUp に決定） | server-foundation.md 21行目「推奨（未確定）」・91行目（未確定事項「DB の最終決定」） | DB とマイグレーションの決定状況 | repository-layout |
| X66 | repository-layout.md 5章 121行目（`init` は `IsExternalInit` を置けば使える）・122行目（`record`・`required`・nullable 値型は [Uta案]）・8章 188行目 | lesson-domain-types.md「決まっていること」18行目（`Uta.Domain` では `record`・`init`・nullable の注釈を使わない [決定]） | `shared/` で使わない書き方の決定状況 | repository-layout |
| X67 | repository-layout.md 10.2 230行目（期間終了時に変換と読める） | gacha.md 8章 119行目・api-design.md 1章 47〜52行目（起動時の `SyncAsync`） | 交換pt の変換方式（X13 と関連） | repository-layout |
| X68 | repository-layout.md 1章 29行目（進め方の要約に実装計画・レビューの段階がない） | CLAUDE.md「実装前のルール」（ADR 0014） | 実装の手順 | repository-layout |
| X69 | repository-layout.md 7章 165行目（実際に動くコマンドはまだない） | CLAUDE.md「ビルド・テスト・実行」（`dotnet build` / `dotnet test` / lint のコマンドあり） | 今ある検証 | repository-layout |
| X70 | repository-layout.md 2章 50〜51行目・3章 74行目（`Uta.Domain`・`Uta.Shared` は [Uta案]） | ADR 0008（accepted。名前で決定） | 名前の決定状況 | repository-layout |
| X71 | repository-layout.md 11.2 282行目（`dotnet run -- migrate`）・280行目（`dotnet test` に対象なし） | ADR 0012（`dotnet run --project server/Uta.Server -- migrate`）、CLAUDE.md（`dotnet test --project shared/Uta.Domain.Tests`） | コマンドの書き方 | repository-layout |
| X72 | CLAUDE.md（`.editorconfig` は client/・server/・shared/ にも置く） | 実際は `client/uta/.editorconfig` | 置き場のずれ（参考） | repository-layout（対象外として参考） |
| X73 | ADR 0005 未決事項 60行目（`client/Packages/manifest.json`） | 実際は `client/uta/Packages/manifest.json` | パスのずれ（proposed の ADR。参考） | repository-layout（対象外として参考） |

repository-layout.md の Critical（`noEngineReferences`）は X10、数値型の根拠は X55 と同じ。

lesson-card.md のレビューで出たもののうち、X13 相当（スキルカードサポートの持続）は X24、`noEngineReferences` は X10 と同じ。

produce.md のレビューで出たもののうち、X2（報酬の画面）・X11（破棄の用語）・X24（スキルカードサポートの持続）・X25（ターン割り振り）・X26（特別レッスン率）・X31（信頼度）・X18（0.5〜2 回分）は既出。

なお idol-support.md の「かけらの単位」（1.1・1.6・1.9 と gacha.md 5章・api-design.md 3章）は X17 と同じ。
