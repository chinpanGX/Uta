---
title: ADR 0020 生成したマスタは Shared.Domain 以外のどの層から参照してもよい
status: proposed
updated: 2026-11-08
---

# ADR 0020: 生成したマスタは Shared.Domain 以外のどの層から参照してもよい

[repository-layout.md](../architecture/repository-layout.md) の「生成物を参照してよいのは Infrastructure だけ」（3章）、「マスタの生成物は Infrastructure に置き、Application へはポート越しに必要な値だけを渡す」（4章）の決定と、クライアントの生成物の置き場（3章「生成したマスタの置き場」のクライアントの部分、5章の `IsExternalInit` を `Uta.Client.Infrastructure` に置く決定）を置き換える。[ADR 0008](0008-shared-domain-and-onion.md) の決定（Shared.Domain は MasterMemory・生成物に依存しない）は変えない。ADR 0008 の理由のうち「`Domain` / `Application` をマスタの形式から独立させられる」は、この ADR で当てはまらなくなる。

前提: クライアントの Domain の層は [ADR 0021](0021-domain-entities-and-repositories.md) で作る。決定の出どころ: マスタを Shared.Domain 以外から参照してよいこと、`MemoryDatabase` をシングルトンで Inject することは、2026-11-08 にユーザーが決めた（[raw の記録](../../raw/2026-11-08-naming-and-layers-decisions.md)）。`Uta.Client.MasterData` に移すこと、Bootstrap からのやり直し、DB に保存しないこと、Presentation では表示だけにすることは、同じ日の設計書のレビューで Claude が出した案を、ユーザーが推奨どおりに採った（コミット 661131a・a7dff22 のメッセージ）。移すファイルの一覧と寿命の規則は、レビューの指摘で書き足した。

**accepted にする時点**: [ADR 0022](0022-client-external-concretes-in-infrastructure.md) の「accepted にする時点と、進めてよいこと」と同じ（proposed のまま順 0 で実装し、順 0 の PR で 0020〜0022 をまとめて判断する。サーバーの Catalog をやめるのはサーバー側の実装計画）。

## 状況

いまは、生成したマスタ（名前空間 `Uta.MasterData`、`MemoryDatabase`）を参照してよいのは各側の Infrastructure だけ（サーバーのアーキテクチャテスト `MasterData_IsReferencedOnlyByInfrastructure`）。クライアントの生成物は `Uta.Client.Infrastructure` の asmdef の中（`Assets/Scripts/Infrastructure/MasterData/`）にある。

そのため、サーバーはマスタのテーブルごとに、読み取りの IF（`IGachaCatalog` など 6 個）、その実装（`MasterGachaCatalog` など）、詰め替え先の型（`GachaDefinition` など）を持っている。マスタの列を足すたびに通す場所が多く、型変換のコードが増えるほど保守のコストが上がる。一方で、この IF はテストの差し替えに使われていない（UseCase のテストは本物のマスタを使う結合テスト）。

## 決定

- **生成したマスタは、Shared.Domain 以外のどの層から参照してもよい**。サーバーの Api・Application・Domain・Infrastructure、クライアントの Domain・Application・Infrastructure・Presentation
- **Shared.Domain だけは参照しない**。生成物はクライアント用・サーバー用で別のソースで、`shared/` に置かない（[ADR 0007](0007-masterdata-with-addons.md)）ため、構造上参照できない。Shared.Domain に渡すときだけ、Shared.Domain の型に詰め替える（今の `CardRow` → `CardCatalog.Create` の形）
- **クライアントの生成物は、専用の asmdef `Uta.Client.MasterData` に移す**（`Assets/Scripts/MasterData/`）。Infrastructure の asmdef の中に置いたままでは、ほかの層から参照すると Infrastructure への依存になるため
  - 移すもの: 生成ソース（`Generated/`）、名前空間を指定する `MasterDataAssembly.cs`（`MasterMemoryGeneratorOptions`。無いと `MemoryDatabase` が `Uta.MasterData` に出ない）、生成ソースのための `IsExternalInit.cs`
  - `masterdata/masterdata.yaml` の client の出力先（`source`）を新しい置き場に変える
  - `Uta.Client.MasterData` が参照するのは MasterMemory・MessagePack（`precompiledReferences`）だけ
  - 参照する側の asmdef（Domain・Application・Infrastructure・Presentation・Bootstrap・テスト）は、`Uta.Client.MasterData` を `references` に足す
  - `overrideReferences: true` の asmdef（今の Application・Presentation・テストと、新しく作る Domain）は、`precompiledReferences` に `MasterMemory.dll`・`MessagePack.dll`（要るなら `MasterMemory.Annotations.dll`）を足す（今のテストの asmdef と同じ形）。`overrideReferences: false` の asmdef（今の Infrastructure・Bootstrap）は、DLL が暗黙に参照されるので足さない。どの DLL が要るかは、移す実装計画でコンパイルして確かめる
  - サーバーは 1 プロジェクトなので、置き場（`Infrastructure/MasterData/`）は変えず、アーキテクチャテストだけを変える
- **`MemoryDatabase` はシングルトンで作り、必要なクラスに Inject する**
  - サーバー: 起動時に読み、DI にシングルトンで登録する（今の `Program.cs` の形）
  - クライアント: 非同期で読み終えてから、ルートの `LifetimeScope`（`BootstrapEntry` が作る）に登録し、それから使うクラスを解決する
  - `MemoryDatabase` から作る Shared.Domain の型（`CardCatalog` など）も、同じ寿命のシングルトンにする
  - `MemoryDatabase` とそこから作った値を、ルートの `LifetimeScope` より長く生きる場所（static のフィールドなど）に置かない。ルートのスコープより先に捨てられるもの（エンティティ、Presenter、要求ごとの値）がマスタの値を持つことは妨げない
  - マスタをダウンロードで更新する方式に移ったら、実行中のインスタンスを差し替えず、Bootstrap からやり直してルートの `LifetimeScope` ごと作り直す
- **マスタの行の型を DB に保存しない**。保存するのは ID と、プレイヤーごとの値だけ
- **クライアントの Presentation では、マスタを表示にだけ使う**。マスタの値でゲームのルールを計算しない（それは Shared.Domain かサーバーで行う）。Presentation でマスタを読むのは、表示用の値を作る DtoFactory（[ADR 0022](0022-client-external-concretes-in-infrastructure.md)）
- 通信の DTO は今どおり `Uta.Shared.Contracts` の型で、Contracts は生成物を参照できないので、マスタの行の型は構造上 DTO に入らない

## 理由

- 詰め替えのための IF・実装・型が無くなり、コード量と保守のコストが下がる
- サーバー専用の列が漏れる心配は増えない。`[ServerOnly]` と `[ClientOnly]` の出し分けは、生成物がそれぞれの側にしか無いことで守られている（ADR 0007）
- テストも重くならない。生成された行の型は `{ get; init; }` の普通のクラスなのでテストで直接組める。表引きが要るテストは、MasterMemory の `DatabaseBuilder` で小さな `MemoryDatabase` を作る（Uta ではまだ使っていない。最初に使う実装計画で確かめる）

## 検討した代替案

- 案A: 今のまま Infrastructure に閉じる。マスタの形式から独立できるが、上の保守のコストが残る
- 案B: Application までにし、サーバーの Domain とクライアントの Presentation は値を受け取る。サーバーの Domain の抽選（`GachaPicker`）は今もプリミティブ（重みの列）を受けているので、この線でも詰め替えは少ない。ただ、Presentation の表示のために、マスタの値を Application で受け渡す型が画面ごとに要る（計画にあった `IGameMaster` とその読み取りの型）ので採らない
- 案B': サーバーの Domain だけはマスタを参照しない。Domain のルールを、マスタの形式から切り離しておける。ただ、ADR 0021 でサーバーの Domain にエンティティとリポジトリの IF を置き、ルールが今より多くマスタの値を使うので、層ごとに例外を作らない（ユーザーの判断。型変換のコストを減らすことを優先する）

## 影響・リスク

- サーバーの `I<名詞>Catalog`・`Master<名詞>Catalog`・詰め替え先の型（`GachaDefinition` など）と、その詰め替えのテストをやめる。クライアントの計画にある `IGameMaster`（Application のマスタの読み取り口）は作らない
- **詰め替えと導出を分ける**: やめるのは詰め替え（列の値を別の型に写すだけのもの）。マスタから計算して作る値（ガチャのプールの重みの列など）は、使う側（サーバーの UseCase）が `MemoryDatabase` から作り、Domain の型（`GachaPicker` が受ける形）に渡す。表引きのキーが足りないときは、マスタの定義に `[SecondaryKey]` を足す（今の主キー・副キーで足りるかは、移す実装計画で確かめる）
- **enum が 2 系統ある**: 生成したマスタの enum（`Uta.MasterData.Plan` など）と Shared.Domain の enum（`Uta.Shared.Domain.Lesson.Plan` など）は別の型で、同じ名前のものがある（2 系統にするのは [masterdata-pipeline.md](../architecture/master/masterdata-pipeline.md) の決定）。これまで詰め替えの IF の中にまとまっていたキャスト（`(Plan)(int)x.Plan`）が、使う場所ごとに散る。同じファイルで両方を使うときは、Shared.Domain の名前空間を `using` し、マスタの enum は名前空間付きで書く
- Shared.Domain の型を組み立てる `MasterCardCatalog`・`MasterProduceCatalog` は残す（名前は [naming.md](../architecture/naming.md) 7章で `CardCatalogFactory`・`ProduceCatalogFactory` に変える）
- マスタの列の名前・構成を変えると、参照している層すべてに波及する（コンパイルエラーで見つかる）
- マスタの値を使ったゲームのルールが、クライアントの Application・Presentation に書かれやすくなる。結果に影響するルールは Shared.Domain に置く、というレビューの観点（[repository-layout.md](../architecture/repository-layout.md) 6章）で止める
- accepted にしたら直すもの:
  - [repository-layout.md](../architecture/repository-layout.md): 2章の木（クライアントの `MasterData/` の置き場）、3章の asmdef の表と「`Uta.MasterData` を参照してよいのは Infrastructure だけ」「生成したマスタの置き場」、4章の表の禁止の列と「マスタの生成物は Infrastructure に置き」、5章の `IsExternalInit` の置き場
  - [masterdata-pipeline.md](../architecture/master/masterdata-pipeline.md): クライアントの生成物の置き場と、「`MemoryDatabase` 自体は DI に登録しない」
  - [screen-design.md](../architecture/screen-design.md) 7.5・7.8 の `IGameMaster`
  - [gacha-support-server.md](../architecture/server/gacha-support-server.md)・[produce-server.md](../architecture/server/produce-server.md) の「マスタは Infrastructure の `MemoryDatabase` から読む」の記述
  - `masterdata/masterdata.yaml` の client の出力先
  - サーバーのアーキテクチャテスト `MasterData_IsReferencedOnlyByInfrastructure` を「`Uta.Shared.Domain` は `Uta.MasterData` を参照しない」の形に変える
  - コードレビューのチェックリスト `.claude/skills/requesting-code-review/layers/client.md`（「マスタの生成物が Infrastructure に留まっているか」を、この ADR の形に直す）
  - ready のクライアントの画面の計画（`IGameMaster` を使う形）を読み替える

## 見直す条件

- マスタの差し替え（ダウンロード）で、Bootstrap からのやり直しが体験として受け入れられないとき
- マスタの形式の変更が、画面や UseCase に頻繁に波及するようになったとき（読み取りの層を戻す）
