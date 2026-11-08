---
title: ADR 0021 各側の Domain にエンティティとリポジトリの IF を置く
status: proposed
updated: 2026-11-08
---

# ADR 0021: 各側の Domain にエンティティとリポジトリの IF を置く

[ADR 0008](0008-shared-domain-and-onion.md) のうち、層の責務（Domain は「ゲームルール、値オブジェクト、enum、ID 型」、外部とのやりとりのポートは Application）の部分を置き換える（ADR 0008 のそれ以外は有効）。

## 状況

ADR 0008 は「共通の型を各側のエンティティが包む（合成）」としているが、エンティティをどの層に何という名前で置くかは決めていない。

- サーバー: `Domain/` にあるのはルールと抽選だけ。エンティティにあたる型（`OwnedIdol`・`OwnedSupportCard`・`PlayerSnapshot`・`ProduceSessionData`）とリポジトリの IF（`IPlayerRepository` など）は Application にある
- クライアント: Domain の層（asmdef）が無い。所持データは Application の `PlayerCache`（具象）が持ち、Presenter が直接受ける

## 決定

- **サーバーもクライアントも、Domain にエンティティとリポジトリの IF を置く**。クライアントには Domain の層（asmdef）を新しく作る
- **エンティティの名前は `<名詞>Entity`**（`PlayerEntity`・`IdolEntity`・`SupportCardEntity`・`ProduceSessionEntity`）。所持を表す `Owned` は付けない。Shared.Domain の型を包む（合成）形は ADR 0008 のまま
- **リポジトリの IF は、読み取り専用と書き込みに分ける**（.NET の `IReadOnlyList<T>` / `IList<T>` と同じ形）
  - `IReadOnly<名詞>Repository`: 取得だけ
  - `I<名詞>Repository : IReadOnly<名詞>Repository`: Upsert・Remove を足す
  - 名前はエンティティ名から `Entity` を除いたもの。エンティティごとに分ける
  - 実装は 1 つのクラスで、DI には両方の IF で登録する（同じインスタンス）
- **リポジトリの処理は取得・Upsert・Remove だけ**。サーバーとの通信はリポジトリに書かない。保存先は、サーバーは DB、クライアントはローカル（メモリのキャッシュ・セーブデータ）
- **書き込まない使い手には `IReadOnly<名詞>Repository` を渡す**（クライアントの表示用の Dto を作る Factory など）
- **Shared.Domain は変えない**。ルール・状態・計算だけを持ち、エンティティもリポジトリも置かない

## 理由

- エンティティと、その保存の口が同じ層に揃い、UseCase は手順だけを持つ形になる（オニオンアーキテクチャの本来の形）
- 読み取り専用の IF を分けると、書いてはいけない使い手が書けないことを、レビューではなく型で守れる
- サーバーとクライアントで、同じ役割に同じ名前を使える

## 検討した代替案

- 案A: 今のまま Application に置く。動いているが、ADR 0008 の「エンティティ」の置き場が決まらないままになる
- 案B: クライアントのサーバーとの通信もリポジトリにする（所持データの出どころはサーバーのため）。リポジトリが「保存先」の意味でなくなり、ガチャを引くなどの操作まで入るので採らない
- 案C: エンティティに接尾辞を付けない（`Player`・`Idol`）。短いが、DTO・マスタの行の型と見分けにくい

## 影響・リスク

- サーバー: エンティティにあたる型とリポジトリの IF を Application から Domain に移し、名前を変える。アーキテクチャテストの層の型の集合が変わる
- サーバーの Domain が、リポジトリの IF の引数に使う `TxContext`（`VolTech.Server.Transaction`）を参照するようになる（今は Application が参照している）
- クライアント: Domain の asmdef を足す。リポジトリの IF が `UniTask` を返すなら、Domain が UniTask を参照する
- 共通レスポンスの player_diff（`PlayerDiff`）はエンティティではない。クライアントのリポジトリへの Upsert の経路を 1 つにするための入れ物で、反映の処理（`PlayerCommonApplier`）がエンティティに直して各リポジトリに Upsert・Remove する。main の計画 [2026-11-08-common-response-player-diff.md](../../plans/2026-11-08-common-response-player-diff.md) の `PlayerDiffData`・`PlayerCache.Apply` は、この形に読み替える
- ready のクライアントの画面の計画（`PlayerCache` を受ける形）を読み替える

## 見直す条件

- リポジトリの IF が大きくなり、読み取りと書き込みの 2 つでは足りなくなったとき
- クライアントの Domain の層が薄すぎて、層を分ける手間に見合わないと分かったとき
