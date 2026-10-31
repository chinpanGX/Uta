# 設計書レビュー: architecture/repository-layout.md

- 対象: `uta_docs/wiki/architecture/repository-layout.md`（status: draft、updated: 2026-10-28）
- 基準のコミット: main `e365ac0`
- 観点: 整合性（uta-goldfish-consistency）・実装可能性（uta-goldfish-readiness）・設計の批評（uta-goldfish-critique）
- 関係するコード: `shared/Uta.Domain/`、`shared/Uta.Domain.Tests/`、`client/uta/`、`global.json`
- 件数（重複をまとめた後）: Critical 1 / Important 16 / Minor 17
- 注意: この worktree ではサブモジュールが初期化されておらず `external/` が空。整合性はメインの checkout（`C:\Work\Uta\external\...`）の作業ツリーで確かめたため、Uta が固定しているコミットとは中身が違う可能性がある。実装可能性・批評は `external/` の中を読めていない

## 判定

| 観点 | 判定 |
|---|---|
| 整合性 | 直せば |
| 実装可能性 | 直せば（`shared/Uta.Domain` の作業はこのまま進められる。server/・client/・`Uta.Shared` の計画には足りない） |
| 設計の批評 | 直せば |

**まとめ: 直せば。** 層・アセンブリ・規約の骨格は個人開発に見合う最小の分け方で、理由も書かれ、`shared/Uta.Domain` の記述はコードと正確に合っている。ただし `noEngineReferences` を accepted の ADR 0008・0009 と逆に [決定] している（C1）。server/・client/ の計画を書くには、Unity への取り込み方式・asmdef 名と名前空間・外部パッケージの入れ方と版・サーバー内部の依存ルールが足りない。

**「層の分け方は重すぎないか」への批評の回答:** asmdef 4 つと NetArchTest そのものは軽い。重さの実体は、同じデータを 3 つの表現（マスタの生成行 / `Uta.Domain` の型 / `Uta.Shared` の DTO）で持ち、その間の変換とポートを毎回書くこと。プロデュースには必要だが、ガチャ一覧・履歴・プレイヤー情報のような表示するだけのデータにまで同じ作法を強いるのは ADR 0008 の目的（計算結果の一致）に寄与しない。層の数は妥当で、変換の適用範囲を絞れば軽くなる（I13、ADR の見直しの提案）。

## 良い点

- 「どこで動くか・共有するかで分け、機能ごとには分けない」「サーバーは 1 プロジェクト、層はフォルダと名前空間」は個人開発に合った最小の分け方。`Presentation` が `Uta.Domain` を直接参照してよいので、表示用モデルをもう 1 段作らずに済む
- 依存ルール（3・4章）がアセンブリごとの「参照してよいもの」と層ごとの「禁止」の表になっている。6.1 の置き場の判断基準（秘匿 → 共通 → 表示）と「移動のサイン」は迷わない形
- `shared/Uta.Domain` の記述がコードと一致（netstandard2.1・`LangVersion 9.0`、`float`/`double`/`System.Random`/UnityEngine/MessagePack/Ulid/`init` 不使用、`IRandom` は `NextUInt64()` だけ、`Xoshiro256StarStar`・`SplitMix64`・`RandomOps`）。10.4 の xunit.v3 4.0.2・`global.json` の Microsoft.Testing.Platform も一致
- 2章の `external/` の 4 件は `.gitmodules`・CLAUDE.md と一致。6章・10章・11章は ADR 0008・0011・0012、api-design.md、produce.md、glossary と用語・値がそろう
- 10.1「採番の規則は作らない」、11.1「本番環境は分けない」、11.4「ヘルスチェックに DB を含めない」など、要らないものを理由つきで削っている。8章の要検証表は Unity 7 アルファと Cloud Run の未確認事項を事実と分けている
- index.md に登録済み。IP 方針に反する名称はない

## 指摘

### Critical

#### C1. `Application` の asmdef の `noEngineReferences: true` が、accepted の ADR 0008・0009 と正反対
- 場所: 3章 90行目（「`noEngineReferences: true` にし…[決定。2026-10-28]」）、4章の表 Application 行 100行目、7章 172行目、8章 189行目、未決事項の解決メモ 312行目
- 出した観点: 整合性 C1・批評 1・実装可能性 6（どちらを書くか選べない）
- 問題: ADR 0008（accepted）27行目「asmdef の `noEngineReferences` は使わず、レビューまたはアナライザーで守る」・48行目、ADR 0009（accepted）17・37行目「使わない」（32行目は `ValueTask` なら強制できることを不採用の比較材料にしている）。`.claude/skills/requesting-code-review/layers/client.md` 10行目も ADR 側。ADR は書き換えられておらず、置き換える ADR もない（log.md 23行目に決めたことの記録はある）。CLAUDE.md「既存 ADR は書き換えず、Superseded にして新規 ADR を作る」に反する。「できなければレビューで守る」という逃げ道付きの [決定] で、どちらが正か分からない
- 直し方（ユーザーが選ぶ）: (a) 新しい ADR で ADR 0008・0009 の該当部分を置き換え、layers/client.md 10行目と ADR 0009 の代替案の比べ方も見直す (b) ページを ADR に戻す（「使わない。レビューまたはアナライザーで守る」。試す価値があれば 8章の要検証に残し、100・172・312行目の「強制する」を外す）。批評の別案: ADR 0011 と同じ「2026-10-28 追記」の形で「試し、UniTask と両立しなければレビューで守る」と追記する（ただし CLAUDE.md の ADR 運用との関係はユーザーが判断）。いずれも「UniTask を参照したまま有効にできるか」の検証結果で (a) が成り立つかが決まる

### Important

#### I1. `shared/` を Unity に取り込む方式が未確定で、予測できる衝突（`bin/`・`obj/`）も書かれていない
- 場所: 2章（71・72行目）、8章（181行目）、3章の表、`shared/Uta.Domain/Uta.Domain.csproj`、`client/uta/Packages/manifest.json`
- 出した観点: 実装可能性 1・批評 2・整合性（未決事項の候補）
- 問題: 「`file:` 参照（想定）」「Compile Include または ProjectReference」だけで未検証。`shared/Uta.Domain` に `package.json` も asmdef もなく、`manifest.json` に参照も UniTask・VContainer・MessagePack もない。`package.json` と asmdef の名前、`.csproj` と `.asmdef` の共存、`.meta` の扱い、`manifest.json` に書く行が決まっていない。さらに `dotnet build shared/Uta.Domain` で `bin/`・`obj/` ができ、Unity がパッケージとして読むと DLL をプラグインとして取り込み、asmdef のソースと型が二重になる（未知ではなく構成から予測できる）
- 直し方（選択肢）: (a) `shared/Uta.Domain` 直下に `package.json` と asmdef を置き `file:` で参照（`Uta.Domain.csproj` の `BaseOutputPath`/`BaseIntermediateOutputPath` を `bin~/`・`obj~/` か `shared/` の外に向ける。`Uta.Domain.Tests/` はパッケージの外） (b) Unity 側に asmdef だけ置きソースをリンク（Windows でシンボリックリンクが扱いにくい） (c) dll を `Plugins` に置く（IL2CPP 向けのコンパイル経路・ソース共有の方針・デバッグに影響）。検証を先に済ませるか、方式を [決定] / [Uta案] で確定して中身（asmdef 名、`package.json` の `name`、`manifest.json` の行）まで書く。8章の確かめ方に「`dotnet build` 後に Unity を開いて型の重複が出ないこと」を足す

#### I2. クライアントの asmdef 名・名前空間の規則と、外部パッケージの入れ方が決まっていない
- 場所: 3章の表（フォルダは `Assets/Scripts/Application` など、asmdef 名の列なし）
- 出した観点: 実装可能性 2
- 問題: asmdef 名が `Application` / `Uta.Application` / `Uta.Client.Application` のどれか（`UnityEngine.Application` と衝突しうる）。server/・client/ のルート名前空間がない（NetArchTest は名前空間で判定するのでテストが書けない）。UniTask・VContainer・MessagePack・MagicOnion クライアント・MasterMemory・Supplement を Unity に入れる手段（UPM の git URL / scoped registry / NuGetForUnity）、版、サブモジュールの `file:` パスがない。UniTask の asmdef 名（`UniTask`）も表に書くとよい
- 直し方: 3章の表に「asmdef 名」「ルート名前空間」の列を足し、「Unity 依存パッケージの入れ方と版」の表を足す。決まっていなければ未決事項へ

#### I3. サーバーの内部の依存方向とアーキテクチャテストのルールがない
- 場所: 2章（`Api/ Application/ Domain/ Infrastructure/`）、3章の `Uta.Server` 行、4章（クライアント寄りの表）
- 出した観点: 実装可能性 3
- 問題: `Api` と `Application` が `Uta.Shared`（DTO）を参照してよいか、DTO ↔ Domain のマッピングを `Api` と `Infrastructure` のどちらでするか。`Api` → `Application` → `Domain`、`Infrastructure` → `Application`（ポート）の許可・禁止のリスト（NetArchTest のルール）がない
- 直し方: サーバー用の「フォルダ × 参照してよいもの」の表を足し、禁止ルールを NetArchTest のテストにする単位まで書く

#### I4. サーバーが参照する基盤のプロジェクトとその参照方法が曖昧で、基盤の分け方の根拠が wiki と食い違う
- 場所: 3章 76・88行目（「基盤の設計書の『2 つに分ける理由』と同じ考え方」）、`Uta.Server`・`Uta.Shared` の行、server-foundation.md「プロジェクト構成」24〜36行目（6 プロジェクト）
- 出した観点: 実装可能性 4・整合性 I5
- 問題: 基盤は `Server.Abstractions`・`Status`・`Auth`・`Transaction`・`Logging`・`Hosting` の 6 つ（server-foundation.md）なのに、本ページは「2 つに分ける」前提で、根拠は基盤リポジトリの `docs/design/project-structure.md`（リンクなし）。どれを `ProjectReference` するか、`external/VolTech-MagicOnion/` 内の `.csproj` の相対パス、`AddVoltechServer()` の呼び出し順、基盤のタグ（server-foundation.md で未確定）、`Uta.Shared`（netstandard2.1）が参照する `Abstractions` がプロジェクト参照か Unity 側のパッケージか、が書かれていない
- 直し方: 根拠として基盤リポジトリの docs にリンクする。参照する基盤プロジェクトの一覧・パス・固定するタグを表にする。タグが決まらないなら server/ の計画に入れない旨を書く

#### I5. server/ 周りのファイルの中身が決まっていない
- 場所: 2章、10.4（247行目「作成時に決める」）、11章
- 出した観点: 実装可能性 5・整合性（未決事項の候補）
- 問題: ソリューションファイル（`.sln` / `.slnx`）の置き場と名前、サーバーの結合テストプロジェクトの名前・場所（NetArchTest の置き場もこれで決まる）、`server/compose.yaml` の PostgreSQL の版・ポート・DB 名・開発用の接続情報の扱い（シークレットを入れない）、`migrate` の引数の仕様、Dockerfile なしの `PublishContainer` で足りるか、NuGet の版（Dapper・Npgsql・Cysharp.Ulid・Testcontainers・NetArchTest・ZLogger・`Grpc.AspNetCore.HealthChecks`）
- 直し方: パッケージ一覧と版を表にして追記する。テストの置き場を決めるか未決事項に移す

#### I6. 8章の要検証が、どの作業をブロックするかが分からない
- 場所: 8章の表（12 項目）
- 出した観点: 実装可能性 6
- 問題: 「確かめたら消す」だけ。`shared/` の取り込み、`noEngineReferences` と UniTask の併用、`IsExternalInit` の置き方は client の asmdef 計画の内容を左右する
- 直し方: 8章に「ブロックする作業」列を足すか、計画の冒頭に「前提の検証」タスクを置く

#### I7. `shared/` で使わない書き方の決め方が、lesson-domain-types.md と食い違い、理由も弱い
- 場所: 5章（121行目「`init` は `IsExternalInit` を置けば使える」、122行目「`record`・`required`・nullable の値型を使わない [Uta案]」）、8章 188行目、lesson-domain-types.md「決まっていること」18行目（`Uta.Domain` では `record`・`init`・nullable の注釈を使わない [決定]）
- 出した観点: 実装可能性 7・批評 4・整合性（X6）
- 問題: `IsExternalInit` が「推測・要確認」のままで、`init` を使う型を書く前に定義の置き場が要る。`record`/`required`/nullable 値型の禁止の理由はマスタ生成ソースの制約（アドオンのコード生成）で、Unity の制約ではない。`record`（C# 9）と `int?` は Unity で使える。「そろえる」は理由になっていない（`CostDef`・`ConditionDef` を `readonly struct` で手書きしているのはこの制約の帰結に見える）
- 直し方（選択肢）: (a) `init` を禁止して `IsExternalInit` を不要にする（lesson-domain-types.md と同じ） (b) 各アセンブリに `internal` で置く（要検証）。批評の案: 122行目を削り、制約を「C# 9・リフレクション依存なし（・`IsExternalInit`）」に絞る。残すなら具体の理由を書く。lesson-domain-types.md とそろえる

#### I8. 7章「実際に動くコマンドはまだない」が CLAUDE.md とコードの現状と合わない
- 場所: 7章の見出し「（未実装）」と 165行目
- 出した観点: 整合性 I1
- 問題: CLAUDE.md には `dotnet build shared/Uta.Domain`・`dotnet test --project shared/Uta.Domain.Tests`・`python -I .claude/skills/linting-wiki/lint_wiki.py` があり、csproj と `global.json` も実在する。7章が「入れたい検証」に挙げる `Uta.Domain` の単体テストも既にある（lesson-domain-types.md の実装メモでは 49 件）
- 直し方: 「`Uta.Domain` のビルドと単体テストは CLAUDE.md のコマンドで動く。CI とそれ以外の検証は未実装」と、今あるものとこれから作るものを分けて書く

#### I9. 2章の構成図で、今あるフォルダと作る予定のフォルダの区別がつかない
- 場所: 2章の構成図（36〜69行目）
- 出した観点: 整合性 I2
- 問題: 実在するのは `shared/Uta.Domain/`・`shared/Uta.Domain.Tests/`・`client/uta/` だけ（`server/` は `.editorconfig` と `.gitkeep` のみ）。`masterdata/`・`shared/Uta.Shared/`・`server/compose.yaml`・`server/Uta.Server/`・`client/uta/Assets/Scripts/` 以下はない。凡例 [決定] / [Uta案] は「決めたか」の印で「あるか」ではないので、`server/compose.yaml # [決定]` は今あるものと読める。CLAUDE.md の「存在しないファイルをあるものとして書かない」に当たり、直近のコミット 1b8c2da で CLAUDE.md 側だけ「未作成」を明記した
- 直し方: 未作成のものに「（未作成）」を付け、CLAUDE.md の書き方にそろえる

#### I10. `Uta.Domain`・`Uta.Shared` の名前を [Uta案] としているが、ADR 0008 で決まっている
- 場所: 2章 50〜51行目、3章の見出し「アセンブリ名は Uta案」（74行目）
- 出した観点: 整合性 I3
- 直し方: この 2 つは [決定]（ADR 0008）にし、[Uta案] として残すのは `Uta.Server` とクライアントの asmdef 名だけにする

#### I11. 1章の「進め方」の要約が今の CLAUDE.md「実装前のルール」と違う
- 場所: 1章 29行目（「仕様 → 本編 → ADR → 実装 → ズレを反映 → `log.md`」）
- 出した観点: 整合性 I4
- 問題: 実装計画（`uta_docs/plans/`）・設計レビュー・Sonnet による実装・コードレビューの段階が抜けている（ADR 0014 は proposed）
- 直し方: 「CLAUDE.md の『実装前のルール』に従う」とだけ書くか、今の手順に合わせる

#### I12. ULID を DB で `uuid` にする理由が根拠のない最適化で、バイト順の未検証リスクもある
- 場所: 10.1（215〜222行目）
- 出した観点: 批評 3
- 問題: 理由は「インデックスが小さい」だが、個人開発の確認用 DB で 16 バイトと 26 バイトの差が効く件数の根拠がない。`Ulid` ↔ `Guid` 変換はバイト順の扱いに注意が要り（批評の知識の範囲、要確認）、`uuid` 列で ULID を選んだ理由の「時刻順」が保たれるか確かめていない。Dapper の TypeHandler も増える
- 直し方（選択肢）: (a) DB も 26 文字の `text` / `char(26)` にする（DTO・ログ・DB が同じ表現、TypeHandler 不要、辞書順 = 時刻順。容量が問題になったら移す） (b) `uuid` を維持するなら 8章に「変換で時刻順が保たれるか」を加え、TypeHandler の置き場（`Infrastructure/`）を書く

#### I13. マスタの生成行 → `Uta.Domain` の型への変換が、クライアントとサーバーで二重に書かれる
- 場所: 4章 106行目、6.1、masterdata-pipeline.md 1章（生成物は側ごとに別のクラス）
- 出した観点: 批評 5
- 問題: `Card`/`CardFace`/`Effect`/`Condition` の複数テーブルを `CardDefinition`（`EffectDef` の入れ子）に組み立てるコードが、クライアントとサーバーの両方の `Infrastructure/` に要る。生成クラスが側ごとに別なので共有できず、ずれると予測と確定がずれる（ADR 0008 が避けたい経路）。書かれていないリスク
- 直し方（選択肢）: (a) `Uta.Domain` に `ICardRow` のような小さな読み取り専用インターフェースを置き、`masterdata/Definitions/` の定義クラスがそれを実装し、組み立ては `Uta.Domain` に 1 つ（要検証: コード生成が基底リストを保持するか、`masterdata/` から `Uta.Domain` を参照できるか） (b) 二重化を受け入れ、Infrastructure は「行の配列をそのまま渡すだけ」に限定し、グループ ID の解決や入れ子の組み立ては `Uta.Domain` 側の関数で行う規約を書く。lesson-card.md の C1（マスタの形と Domain の形）と一緒に決める

#### I14. 「アプリのバージョン」の正がどこにもない
- 場所: 6章 135行目、10.5（252〜254行目）、api-design.md 5章、server-foundation.md（`server_status.toml` は未確定）
- 出した観点: 批評 6
- 問題: 強制アップデート・セッション破棄・ルール変更の揃えがすべて `x-app-version` に依存しているのに、クライアントがこの整数をどこで持つか（定数か `bundleVersion` から導くか）、サーバーの最低バージョンをどこで設定するか、誰がいつ上げるか（`Uta.Domain` を変えたコミットで上げる、など）がない
- 直し方: 未決事項に挙げる。例: 「クライアントの `x-app-version` は `Bootstrap` の定数 1 か所」「サーバーの最低バージョンは環境変数（Secret Manager と同じ経路）」。produce.md の I3（マスタの版・ルールの版）と一緒に決める

#### I15. ロールバックと「DbUp は戻せない」の組み合わせの方針がない
- 場所: 11.2 手順 6（285行目。推測のまま）、ADR 0012「影響・リスク」
- 出した観点: 批評 7・実装可能性（Minor）
- 問題: 手順 6 で「1 つ前のリビジョンに戻す」と書くが、手順 3 でスキーマは前に進んでいる。古い API が新しいスキーマで動く保証がない
- 直し方（選択肢）: (a) dev 環境ではロールバックせず前に進めて直す（本番がないので成立。最も単純） (b) マイグレーションを後方互換（追加のみ、削除は 1 リリース後）に限る規約を書く

#### I16. NetArchTest の保守状況と .NET 10 / xunit.v3 での動作が未確認のまま [決定]
- 場所: 3章 88行目
- 出した観点: 批評 8
- 問題: `NetArchTest.Rules` は長く更新されていない可能性がある（批評の知識の範囲、要確認）。xunit.v3 4.x + Microsoft.Testing.Platform との組み合わせも未確認
- 直し方: 8章の要検証に加える。代案は保守されているフォーク（`NetArchTest.eNhancedEdition`）や ArchUnitNET、使えなければ Roslyn アナライザーや単純な文字列検査

### Minor

- **M1.** frontmatter の `updated: 2026-10-28` が古い（log.md 14行目、2026-10-31 に「repository-layout.md・CLAUDE.md の `tests/` を削除」）（整合性）
- **M2.** 凡例の [決定]（16行目）の定義「2026-10-27 にユーザーが確認した方針（後者は ADR 0007〜0010 に記録）」と使い方が合わない。本文の [決定。2026-10-28] の多く（NetArchTest、ULID の持ち方、テストの置き場、11.4 の一部）はどの ADR にもない。定義を実態に合わせる（整合性）
- **M3.** 11.2 の手順 3（282行目）「`dotnet run -- migrate`」と ADR 0012 の「`dotnet run --project server/Uta.Server -- migrate`」で書き方が違う（整合性）
- **M4.** 11.2 の手順 1（280行目）の `dotnet test` に `--project` もソリューションの指定もない。ルートにソリューションはなく、CLAUDE.md は `dotnet test --project shared/Uta.Domain.Tests`（整合性）
- **M5.** 2章 72行目「（推測。ADR 0005 の未決事項と同じ）」は対象がずれている（ADR 0005 はサブモジュールの中のパッケージの話で、`shared/` はサブモジュールではない）。根拠の「基盤の『ソースで共有する』方式（server-foundation.md）」も server-foundation.md にない。基盤リポジトリの docs にリンクする（整合性）
- **M6.** 2章 71行目「ADR 0002 / CLAUDE.md からの変更点」が古い（CLAUDE.md は更新済みで「マスタ型は置かない」）（整合性）
- **M7.** 2章の構成図に決定済みの置き場の一部がない: `masterdata/gen.ps1`（masterdata-pipeline.md 7章）、バイナリの出力先 `client/uta/Assets/StreamingAssets/MasterData/`・`server/Uta.Server/MasterData/masterdata.bytes`（同 5章、ADR 0013）、ルートの `global.json`（整合性）
- **M8.** 10.2（230行目）「ガチャの期間終了時の、交換pt から交換メダルへの変換」は期間の終わりに行うように読める。実際は起動時の `SyncAsync`（gacha.md 8章、api-design.md 1章）。リンク先を gacha.md 8章にする（整合性。gacha.md の I1・I3 と関連）
- **M9.** 10.3（236行目）「シードと乱数の位置を保存」が、api-design.md 3.1・produce.md 10.5 の「マスターシードとストリームごとの `xoshiro256**` の状態（と引いた回数）」と呼び方が違う（整合性）
- **M10.** 4章の Domain の禁止に MessagePack がない（ADR 0010 41行目・produce.md 10.5 は明記）。3章の「なし（BCL のみ）」で実質は含まれるが明記するとそろう（整合性）
- **M11.** 10.3「シードは暗号用の乱数で生成（推測）」: produce.md 側に決定があるならリンクで置き換える（実装可能性）
- **M12.** 6章の `Uta.Domain` のテスト（同じシードで同じ結果）の期待値（ゴールデン値）の持ち方（コードかファイルか）がない（実装可能性）
- **M13.** `Uta.Shared` の csproj に MagicOnion / MessagePack の版をどう書くかが、8章の「バージョンが揃うか」の検証待ち（実装可能性）
- **M14.** 7章の「入れたい検証」に CI で回すものと手動で回すものが混ざっている。実機ビルドを CI に入れるには Unity のライセンスと macOS ランナーが要る。「CI で回す（dotnet test、`master-gen --check`、アーキテクチャテスト）」と「手動（実機ビルド）」に分ける（批評 9）
- **M15.** 11.1（271行目）の「ローカル（確認）」環境は、dev が唯一のデプロイ先で本番がないなら削るか「任意」にできる（批評 10）
- **M16.** Testcontainers は `server/compose.yaml` の PostgreSQL でも足りる（10.4 246行目、11.1 275行目）。技術先行で触りたいなら採用でよいので、理由を「触る目的」と書く（批評 11）
- **M17.** 3章の `Application` の参照の UniTask は、Unity パッケージの asmdef 名（`UniTask`）で書くとよい（実装可能性。I2 と一緒に）

## 未決事項に加える候補

- `noEngineReferences` を使うか（C1。ADR の置き換えか、ページを戻すか）
- `shared/` を Unity に取り込む具体的な方式と検証結果、`bin/`・`obj/` の出力先（I1）
- クライアントの asmdef 名、サーバー・クライアントのルート名前空間（I2）
- Unity 側の外部パッケージ（UniTask、VContainer、MessagePack、MagicOnion、MasterMemory、Supplement）の入れ方と版（I2）
- サーバーの層ごとの参照ルール、DTO ↔ Domain のマッピングの置き場（I3）
- 参照する基盤プロジェクトと基盤のタグの固定（I4。server-foundation.md の未決事項と同じ）
- サーバーのソリューション、テストプロジェクトの場所、NuGet の版一覧、`compose.yaml` の中身（I5）
- `init` を使うか（`IsExternalInit`）、`record`/`required`/nullable 値型の制約の範囲（I7）
- ULID の DB 上の型（`uuid` か `char(26)` か）（I12）
- マスタの行 → `Uta.Domain` の変換の置き方（I13）
- アプリのバージョン（`x-app-version`）の正、サーバーの最低バージョンの置き場、上げる規則（I14）
- dev 環境でのロールバック方針（I15）
- 10.6 の「何を保存するか」: MVP では認証トークンだけか。だけなら AES + HMAC のセーブデータ基盤が要るか（批評）

## ADR の見直しの提案

- **ADR 0008 / 0009 の `noEngineReferences`**（C1 の (a)）: 新しい ADR で置き換えるか、ページを ADR に戻すか。ADR 0011 と同じ「追記」の形にする案も批評から出ているが、CLAUDE.md の「既存 ADR は書き換えず、Superseded にして新規 ADR を作る」との関係はユーザーが判断する
- **ADR 0008「`Application` は `Uta.Shared` を参照しない」を表示専用のデータに限って緩める案**（批評）: ADR はこの代替を「API の変更が画面まで波及する」として退けているが、クライアントが 1 つで作る人も 1 人なら、マッピング層は変更を防がず直す場所を 1 つ増やすだけ。ガチャ一覧・履歴・プレイヤー情報のように `Uta.Domain` のロジックが触らないデータは DTO を Application の読み取りモデルとしてそのまま使い、マッピングはプロデュース（`Uta.Domain` の状態を作るもの）に限る。層の数は変えず、変換の範囲だけ絞る

## 判断しなかったこと（レビュー担当が挙げたもの）

- `external/` のリンク先（`MasterMemoryAddons/docs/dev/structure.md`、Supplement の docs）と基盤の実際の `.csproj` 構成: この worktree でサブモジュールが未初期化のため確認できていない（整合性はメインの checkout の作業ツリーで一部確認）。2章の `VolTech-MagicOnion` の説明が古いか（メインの checkout の基盤には `Runtime/Client/ApiCore` がある）も、固定コミットを確かめられないため判断していない
- `noEngineReferences: true` と UniTask の参照を両立できるかの技術的な可否（8章の検証の対象）
- Cloud Run の gRPC・ヘルスチェックの設定の技術的な正しさ、Cloud Run / Cloud SQL / Secret Manager の具体的な設定値
- masterdata の列・生成の仕様（masterdata-pipeline.md の担当）、機能 ID がほかの 3 編で守られているか
- クライアントが `GetStateAsync` の DTO から山札順なしで `Uta.Domain` の状態を作れるか（produce.md 10.5 / api-design.md の範囲）
- `server/Uta.Server/Domain/` と `Uta.Domain` の名前の紛らわしさ（好みの範囲）
- 対象ページ以外の小さなずれ（`_cross-page.md` に記録）: CLAUDE.md の「`.editorconfig` は client/・server/・shared/ にも置く」と実際の `client/uta/.editorconfig`、ADR 0005 未決事項 60行目の `client/Packages/manifest.json`（実際は `client/uta/Packages/manifest.json`）、log.md 23行目の「`tests/`」
