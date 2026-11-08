---
title: ADR 0020 生成したマスタは Shared.Domain 以外のどの層から参照してもよい
status: proposed
updated: 2026-11-08
---

# ADR 0020: 生成したマスタは Shared.Domain 以外のどの層から参照してもよい

[ADR 0008](0008-shared-domain-and-onion.md) のうち「`Domain` / `Application` をマスタの形式から独立させる」の部分を置き換える（ADR 0008 のそれ以外は有効）。

## 状況

いまは、生成したマスタ（名前空間 `Uta.MasterData`、`MemoryDatabase`）を参照してよいのは各側の Infrastructure だけで、Application へは「ポート越しに必要な値だけ」を渡す（[repository-layout.md](../architecture/repository-layout.md) 3章・4章、サーバーのアーキテクチャテスト `MasterData_IsReferencedOnlyByInfrastructure`）。

そのため、サーバーはマスタのテーブルごとに、読み取りの IF（`IGachaCatalog` など 7 個）、その実装（`MasterGachaCatalog` など）、詰め替え先の型（`GachaDefinition` など）を持っている。マスタの列を足すたびに通す場所が多く、型変換のコードが増えるほど保守のコストが上がる。一方で、この IF はテストの差し替えに使われていない（UseCase のテストは本物のマスタを使う結合テスト）。

## 決定

- **生成したマスタは、Shared.Domain 以外のどの層から参照してもよい**。サーバーの Api・Application・Domain・Infrastructure、クライアントの Domain・Application・Infrastructure・Presentation
- **Shared.Domain だけは参照しない**。生成物はクライアント用・サーバー用で別のソースで、`shared/` に置かない（[ADR 0007](0007-masterdata-with-addons.md)）ため、構造上参照できない。Shared.Domain に渡すときだけ、Shared.Domain の型に詰め替える（今の `CardRow` → `CardCatalog.Create` の形）
- **`MemoryDatabase` はシングルトンで作り、必要なクラスに Inject する**
  - サーバー: 起動時に読み、DI にシングルトンで登録する（今の `Program.cs` の形）
  - クライアント: 非同期で読み終えてから DI に登録し、それから使うクラスを解決する。マスタをダウンロードで更新する方式に移ったら、実行中のインスタンスを差し替えず、使うスコープを作り直す（タイトルに戻る）
  - `MemoryDatabase` から作る Shared.Domain の型（`CardCatalog` など）も、同じ寿命のシングルトンにする
- **マスタの行の型を外へ出さない**: DB に保存しない。通信の DTO に入れない（DTO は今どおり `Uta.Shared.Contracts` の型）
- **クライアントの Presentation では、マスタを表示にだけ使う**。マスタの値でゲームのルールを計算しない（それは Shared.Domain かサーバーで行う）

## 理由

- 詰め替えのための IF・実装・型が無くなり、コード量と保守のコストが下がる
- サーバー専用の列が漏れる心配は増えない。`[ServerOnly]` と `[ClientOnly]` の出し分けは、生成物がそれぞれの側にしか無いことで守られている（ADR 0007）
- テストも重くならない。生成された行の型は `{ get; init; }` の普通のクラスなのでテストで直接組める。表引きが要るテストは `DatabaseBuilder` で小さな `MemoryDatabase` を作れる
- テーブルをまとめ直す処理（`GachaDefinition` のプールなど）は、MasterMemory の副インデックス（`FindBy...`）で引けば済むことが多い

## 検討した代替案

- 案A: 今のまま Infrastructure に閉じる。マスタの形式から独立できるが、上の保守のコストが残る
- 案B: Application までにし、サーバーの Domain とクライアントの Presentation は値を受け取る。線を 1 本残せるが、サーバーの Domain（抽選など）に渡すための詰め替えが残る

## 影響・リスク

- サーバーの `I<名詞>Catalog`・`Master<名詞>Catalog`・詰め替え先の型と、その詰め替えのテストをやめる。クライアントの計画にある `IGameMaster`（Application のマスタの読み取り口）は作らない。ready の画面の計画を読み替える
- Shared.Domain の型を組み立てる `MasterCardCatalog`・`MasterProduceCatalog` は残す（名前は [naming.md](../architecture/naming.md) で `CardCatalogLoader`・`ProduceCatalogLoader` に変える）
- マスタの列の名前・構成を変えると、参照している層すべてに波及する（コンパイルエラーで見つかる）
- アーキテクチャテスト `MasterData_IsReferencedOnlyByInfrastructure` を、「`Uta.Shared.Domain` は `Uta.MasterData` を参照しない」の形に変える。クライアントは asmdef の参照を足す
- マスタの値を使ったゲームのルールが、クライアントの Application・Presentation に書かれやすくなる。結果に影響するルールは Shared.Domain に置く、というレビューの観点（[repository-layout.md](../architecture/repository-layout.md) 6章）で止める

## 見直す条件

- マスタの差し替え（ダウンロード）で、シングルトンを掴んだままのクラスが問題になったとき
- マスタの形式の変更が、画面や UseCase に頻繁に波及するようになったとき（読み取りの層を戻す）
