---
title: ADR 0021 各側の Domain にエンティティとリポジトリの IF を置く
status: proposed
updated: 2026-11-08
---

# ADR 0021: 各側の Domain にエンティティとリポジトリの IF を置く

[repository-layout.md](../architecture/repository-layout.md) の層の決定のうち、Domain の責務（4章の表の Domain の行）、クライアントの Domain の層を作らないこと（3章の Client.Domain の行、6.1 の「Client.Domain は今は作らない」）、サーバーの依存の向き（3章の表の Domain の行）を置き換える。[ADR 0008](0008-shared-domain-and-onion.md) の「共通の型を各側のエンティティが包む（合成）」は変えず、そのエンティティの置き場と名前を決める。

## 状況

ADR 0008 は「共通の型を各側のエンティティが包む（合成）」としているが、エンティティをどの層に何という名前で置くかは決めていない。

- サーバー: `Domain/` にあるのはルールと抽選だけ。エンティティにあたる型（`OwnedIdol`・`OwnedSupportCard`・`PlayerSnapshot`）とリポジトリの IF（`IPlayerRepository` など）は Application にある
- クライアント: Domain の層（asmdef）が無い。所持データは Application の `PlayerCache`（具象）が持ち、Presenter が直接受ける

## 決定

### 両側で共通

- **Domain に、エンティティとリポジトリの IF を置く**
- **エンティティの名前は `<名詞>Entity`**（`PlayerEntity`・`IdolEntity`・`SupportCardEntity`）。所持を表す `Owned` は付けない。Shared.Domain の型を包む（合成）形は ADR 0008 のまま
- **リポジトリの IF は `I<名詞>Repository`、実装は Infrastructure の `<名詞>Repository`**
- **Shared.Domain は変えない**。ルール・状態・計算だけを持ち、エンティティもリポジトリも置かない

### クライアント

- **Domain の層を asmdef `Uta.Client.Domain` で作る**（`Assets/Scripts/Domain/`）。参照するのは `Uta.Shared.Domain`・`Uta.Client.MasterData`（[ADR 0020](0020-masterdata-referenced-outside-shared-domain.md)）・UniTask。参照されるのは Application・Infrastructure・Presentation・Bootstrap
- **リポジトリはエンティティごとに分け、処理は取得・Upsert・Remove だけ**。保存先はローカル（メモリのキャッシュ・セーブデータ）。サーバーとの通信はリポジトリに書かない
- **リポジトリの IF は、読み取り専用と書き込みに分ける**（.NET の `IReadOnlyList<T>` / `IList<T>` と同じ形）
  - `IReadOnly<名詞>Repository`: 取得だけ
  - `I<名詞>Repository : IReadOnly<名詞>Repository`: Upsert・Remove を足す
  - 名前はエンティティ名から `Entity` を除いたもの
  - 実装は 1 つのクラスで、DI には両方の IF で登録する（同じインスタンス）
  - 書き込まない使い手（表示用の Dto を作る Factory など）には `IReadOnly<名詞>Repository` を渡す
- **UI の状態はエンティティに入れない**。今の計画で `PlayerCache` に入る予定の UI の状態（`DiscardNoticeShown` など）は、Application の状態の型に置く（型の名前は実装計画で決める）

### サーバー

- **リポジトリの IF を Application から Domain に移し、メソッドは今の SQL の形のまま**にする。条件付きの更新（`TrySpend…`・`TryLimitBreakAsync`）、楽観ロック（`TryUpdateAsync`。[ADR 0010](0010-database.md)）、加算、追記（ガチャ履歴・プロデュース結果）は、1 文の SQL で正しさを守っているので、取得・Upsert・Remove の形に揃えない
- リポジトリはテーブルの単位のまま（`IGachaRepository` のように、エンティティを持たないものも残す）
- **読み取り専用の IF は今は作らない**。書き込まない使い手が現れたら、クライアントと同じ形で分ける
- **Domain が `TxContext`（`VolTech.Server.Transaction`）を参照してよい**（リポジトリの IF の引数に使うため）。Domain の型は `TxContext` を IF の引数として受け渡すだけにし、`TxContext.Connection` に触れない（触れるのは Infrastructure だけ。コードレビューで守る）

## 理由

- エンティティと、その保存の口が同じ層に揃い、UseCase は手順だけを持つ形になる（オニオンアーキテクチャの本来の形）
- クライアントでは、読み取り専用の IF を分けると、書いてはいけない使い手が書けないことを、レビューではなく型で守れる
- サーバーの正しさ（二重払いをしない、古い操作で上書きしない）は条件付きの 1 文の更新で守っている。リポジトリの形をクライアントに揃えると、判定をエンティティに移し、ロックを別に持つことになり、壊れやすくなる
- サーバーとクライアントで、同じ役割に同じ名前を使える

## 検討した代替案

- 案A: 今のまま Application に置く。動いているが、ADR 0008 の「エンティティ」の置き場が決まらないままになる
- 案B: クライアントのサーバーとの通信もリポジトリにする（所持データの出どころはサーバーのため）。リポジトリが「保存先」の意味でなくなり、ガチャを引くなどの操作まで入るので採らない
- 案C: エンティティに接尾辞を付けない（`Player`・`Idol`）。短いが、DTO・マスタの行の型と見分けにくい
- 案D: サーバーも取得・Upsert・Remove に揃える。両側の形は揃うが、上の理由で採らない
- 案E: サーバーのリポジトリの IF は Application に残す（Domain が `TxContext` を見ない）。依存の表は今のままで済むが、両側でエンティティとリポジトリの置き場が揃わない

## 影響・リスク

- サーバー: エンティティにあたる型とリポジトリの IF を Application から Domain に移し、名前を変える。[repository-layout.md](../architecture/repository-layout.md) 3章の依存の表（Domain の「参照してよいもの」に `TxContext` と生成したマスタ、Infrastructure の行の「Application のポートを実装する」）を直す。アーキテクチャテストの禁止の列は変わらない（`TxContext` の参照は禁止に入っていない）
- サーバー: `ProduceSessionData`・`ProduceSessionRecord`・`ProduceResultRecord`・`GachaHistoryRow` をエンティティにするか、どの名前にするかは、移す実装計画で決める（未決）
- クライアント: `Uta.Client.Domain` の asmdef を足し、[repository-layout.md](../architecture/repository-layout.md) 3章の asmdef の表と、6.1・[glossary.md](../glossary.md) の Client.Domain の「今は作らない」を直す
- **共通レスポンスの player_diff（`PlayerDiff`）はエンティティではない**。`PlayerDiff` は Contracts の DTO で、操作の RPC（ガチャ・強化・プロデュースの完了など）の結果を、個々のレスポンスの項目から拾わず、1 つの共通の処理（`PlayerCommonApplier`）でクライアントのリポジトリに反映するための入れ物。反映の処理が DTO をエンティティに直し、各リポジトリに Upsert・Remove する。サインインは今どおり全件を置き換え、`RefreshPlayerUseCase` も安全網として残す（[共通レスポンスの計画](../../plans/2026-11-08-common-response-player-diff.md) 0章 4・9）
- 共通レスポンスの計画は次のように読み替える: Application の `PlayerDiffData` と `PlayerCache.Apply` は作らない。`PlayerCommonApplier` が `IPlayerRepository`・`IIdolRepository`・`ISupportCardRepository` に書く。サーバー側（`PlayerDiffCollector` の `Touch…` を呼ぶ位置と引数）は、リポジトリの IF の置き場が変わるだけで中身は変わらない
- ready のクライアントの画面の計画（`PlayerCache` を受ける形）を読み替える。[produce-server.md](../architecture/server/produce-server.md)・[gacha-support-server.md](../architecture/server/gacha-support-server.md)・[api-design.md](../architecture/api-design.md) の型の名前も、改名のときに直す

## 見直す条件

- クライアントのリポジトリの IF が大きくなり、読み取りと書き込みの 2 つでは足りなくなったとき
- クライアントの Domain の層が薄すぎて、層を分ける手間に見合わないと分かったとき
- サーバーの Domain が `TxContext.Connection` に触れるコードが出てきたとき（IF を Application に戻す）
