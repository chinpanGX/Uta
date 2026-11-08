---
title: ADR 0021 各側の Domain にエンティティとリポジトリの IF を置く
status: accepted
updated: 2026-11-08
---

# ADR 0021: 各側の Domain にエンティティとリポジトリの IF を置く

> **accepted**（2026-11-08。ユーザーの判断。[基盤移行の計画](../../plans/2026-11-08-client-foundation-migration.md)の PR [#44](https://github.com/chinpanGX/Uta/pull/44) で 0020〜0022 をまとめて accepted にした）。クライアントの「accepted にしたら直すもの」は同じ PR で直した。サーバーに関わるものは、[ADR 0022](0022-client-external-concretes-in-infrastructure.md) の「accepted にする時点と、進めてよいこと」のとおり、サーバー側の実装計画で直す
> リポジトリのメソッドの名前は [naming.md](../architecture/naming.md) 2 章が正。本文の `ReplaceAll`（全件の置き換え）は、`Clear()` と `Upsert(IReadOnlyList<T>)` に分けた（2026-11-08。ユーザーの判断。[基盤移行の計画](../../plans/2026-11-08-client-foundation-migration.md)の R10）

[repository-layout.md](../architecture/repository-layout.md) の層の決定のうち、Domain の責務（4章の表の Domain の行）、クライアントの Domain の層を作らないこと（3章の Client.Domain の行、6.1 の「Client.Domain は今は作らない」）、サーバーの依存の向き（3章の表の Domain・Infrastructure の行）を置き換える。[ADR 0008](0008-shared-domain-and-onion.md) の「共通の型を各側のエンティティが包む（合成）」は変えず、そのエンティティの置き場と名前を決める。

決定はどれも 2026-11-08 にユーザーが決めた（経緯は [raw の記録](../../raw/2026-11-08-naming-and-layers-decisions.md)・[Presentation の記録](../../raw/2026-11-08-presentation-rules-decisions.md) と、設計書のレビューでの判断を書いたコミットのメッセージ）。

**accepted にする時点**: [ADR 0022](0022-client-external-concretes-in-infrastructure.md) の「accepted にする時点と、進めてよいこと」と同じ（proposed のまま順 0 で実装し、順 0 の PR で 0020〜0022 をまとめて判断する。サーバーのコードはサーバー側の実装計画まで今の形のまま）。

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

- **Domain の層を asmdef `Uta.Client.Domain` で作る**（`Assets/Scripts/Domain/`）。参照するのは `Uta.Shared.Domain` と `Uta.Client.MasterData`（[ADR 0020](0020-masterdata-referenced-outside-shared-domain.md)。MasterMemory・MessagePack の DLL の参照も ADR 0020 の形）だけ。参照されるのは Application・Infrastructure・Presentation・Bootstrap・テスト
- **リポジトリの IF は同期にする**（UniTask を参照しない）。今の保存先はメモリのキャッシュだけで、非同期にする理由が無い。同期なら、読む側が書き込みの途中の値を見ることもない。非同期が要る保存先（セーブデータなど）は、Infrastructure が起動時に読み込んでからリポジトリに入れる。前提: リポジトリの読み書きはすべてメインスレッドで行い、共通レスポンスの反映（`PlayerCommonApplier`）は 3 つのリポジトリへの書き込みの間で await しない。基盤が Applier をどのスレッドで呼ぶかは、共通レスポンスの計画で基盤のソースを読んで確かめ、実行での確認は `Common` を返す RPC を最初にクライアントから呼ぶ gacha の計画の Play で行う（共通レスポンスの計画の時点では、クライアントがその RPC をまだ呼ばないため）。`PlayerUseCase`（サインイン）の書き込みのスレッドは、基盤移行の計画の診断で確かめる
- **取得のメソッド名は、1 件が `Find`（無ければ null）、全件が `FindAll`**（サーバーの `FindAsync`・`FindAllAsync` と同じ語。[naming.md](../architecture/naming.md) 2章）[決定。2026-11-08。ユーザーの判断]
- **リポジトリはエンティティごとに分け、処理は最大で取得・Upsert・Remove・全件の置き換え（`ReplaceAll`）の 4 つ**。各リポジトリは使うものだけを持つ（1 人分の `IPlayerRepository` は取得・Upsert だけ、など）。保存先はローカル（メモリのキャッシュ）。サーバーとの通信はリポジトリに書かない。`ReplaceAll` はサインインで全件を取り直したときに使う（サーバーで消えたものもキャッシュから消える）
- **リポジトリの IF は、読み取り専用と書き込みに分ける**（.NET の `IReadOnlyList<T>` / `IList<T>` と同じ形）
  - `IReadOnly<名詞>Repository`: 取得だけ
  - `I<名詞>Repository : IReadOnly<名詞>Repository`: Upsert・Remove・`ReplaceAll` を足す
  - 名前はエンティティ名から `Entity` を除いたもの
  - 実装は 1 つのクラスで、DI には両方の IF で登録する（同じインスタンス）
  - 分ける目的は、層として書き込みを禁じたい使い手（Presentation の DtoFactory）に、書けない型を渡すこと。Presentation には `IReadOnly<名詞>Repository` だけを渡す
  - 両方の IF を DI に登録するので、DtoFactory が `I<名詞>Repository` を受けることは型では止められない。型が止めるのは「`IReadOnly…` を受けた DtoFactory が書くこと」まで。DtoFactory が `IReadOnly…` だけを受けることは、コードレビューで守る（`.claude/skills/requesting-code-review/layers/client.md` に足す）
- **UI の状態はエンティティに入れない**。今の計画で `PlayerCache` に入る予定の UI の状態（`DiscardNoticeShown` など）と、プロデュース中の状態（計画の `ProduceSession`）は、Application の状態の型に置く（型の名前は実装計画で決める）

### サーバー

- **リポジトリの IF を Application から Domain に移し、メソッドは今の SQL の形のまま**にする。条件付きの更新（`TrySpend…`・`TryLimitBreakAsync`）、楽観ロック（`TryUpdateAsync`。[ADR 0010](0010-database.md)）、加算、追記（ガチャ履歴・プロデュース結果）は、1 文の SQL で正しさを守っているので、取得・Upsert・Remove の形に揃えない。**取得のメソッド名だけはクライアントと揃える**: 1 件は `FindAsync`（今のまま）、全件は `ListAsync` → `FindAllAsync`（サーバー側を移す実装計画で改名する）[決定。2026-11-08。ユーザーの判断]
- リポジトリはテーブルの単位のまま（`IGachaRepository` のように、エンティティを持たないものも残す）
- **IF のシグネチャに出る型は、すべて IF と一緒に Domain に移す**。今 Application にあるもの: `OwnedIdol`・`OwnedSupportCard`・`PlayerSnapshot`（エンティティに改名）、`PlayerBalance`・`ProduceReward`・`ProduceSessionStatus`・`ProduceSessionData`・`ProduceSessionRecord`・`ProduceResultRecord`・`GachaHistoryRow`。移さないと、アーキテクチャテスト（Domain は Application を参照しない）が落ちる
- **読み取り専用の IF は作らない**。分ける目的（Presentation に書けない型を渡す）に当たる使い手が、サーバーにはいない。共通レスポンスの `PlayerDiffCollector` はリポジトリを読むだけだが、Infrastructure の型で、その層はもともと書き込みもする
- **Domain が `TxContext`（`VolTech.Server.Transaction`）を参照してよい**（リポジトリの IF の引数に使うため）。Domain の型は `TxContext` を IF の引数として受け渡すだけにし、`TxContext.Connection` に触れない（触れるのは Infrastructure だけ。コードレビューで守る）

## 理由

- エンティティと、その保存の口が同じ層に揃い、UseCase は手順だけを持つ形になる（オニオンアーキテクチャの本来の形）
- クライアントでは、`IReadOnly…` を受けた使い手は書けないので、書き込みの口を探すレビューが「受ける型が `IReadOnly…` か」の確認だけで済む
- サーバーの正しさ（二重払いをしない、古い操作で上書きしない）は条件付きの 1 文の更新で守っている。リポジトリの形をクライアントに揃えると、判定をエンティティに移し、ロックを別に持つことになり、壊れやすくなる
- サーバーとクライアントで、同じ役割に同じ名前を使える

## 検討した代替案

- 案A: 今のまま Application に置く。動いているが、ADR 0008 の「エンティティ」の置き場が決まらないままになる
- 案B: クライアントのサーバーとの通信もリポジトリにする（所持データの出どころはサーバーのため）。リポジトリが「保存先」の意味でなくなり、ガチャを引くなどの操作まで入るので採らない
- 案C: エンティティに接尾辞を付けない（`Player`・`Idol`）。短いが、DTO・マスタの行の型と見分けにくい
- 案D: サーバーも取得・Upsert・Remove に揃える。両側の形は揃うが、上の理由で採らない
- 案E: サーバーのリポジトリの IF は Application に残す（Domain が `TxContext` を見ない）。依存の表は今のままで済むが、両側でエンティティとリポジトリの置き場が揃わない
- 案F: クライアントは `PlayerEntity` が所持の一覧を持つ集約にして、リポジトリを 1 組にする。全件の置き換えは自然に書けるが、サーバーの 3 つのリポジトリ（プレイヤー・アイドル・サポートカード）や、共通レスポンスの差分（アイドル・カードごと）と 1 対 1 で対応しなくなるので採らない
- 案G: クライアントのリポジトリの IF を非同期（UniTask）にする。将来の非同期の保存先にそのまま対応できるが、読む側がすべて await になり、書き込みの途中を読まれうる

## 影響・リスク

- サーバー: 上の型を Application から Domain に移し、名前を変える。直すページ:
  - [repository-layout.md](../architecture/repository-layout.md) 3章の依存の表（Domain の「参照してよいもの」に `TxContext` と生成したマスタ、Infrastructure の行の「Application のポートを実装する」）。アーキテクチャテストの禁止の列は変わらない（`TxContext` の参照は禁止に入っていない）
  - [produce-server.md](../architecture/server/produce-server.md)・[gacha-support-server.md](../architecture/server/gacha-support-server.md) の、型の置き場の表（`Uta.Server/Application/…` の行）
  - [server-foundation.md](../architecture/server/server-foundation.md) の、リポジトリの IF の置き場の記述
- サーバー: `ProduceSessionData`・`ProduceSessionRecord`・`ProduceResultRecord`・`GachaHistoryRow` をエンティティにするか（`<名詞>Entity` にするか、DB の 1 行の写しの `<名詞>Record` にするか）は、移す実装計画で決める（未決）。Domain に移すことは決まり
- クライアント: `Uta.Client.Domain` の asmdef を足す。直すページ:
  - [repository-layout.md](../architecture/repository-layout.md) 2章の木、3章の asmdef の表と Client.Domain の行、4章の表の Domain の行、6.1 の「Client.Domain は今は作らない」
  - [glossary.md](../glossary.md) の Server.Domain・Client.Domain・ポートの項。エンティティ・リポジトリの項を足す
  - [screen-design.md](../architecture/screen-design.md) 7.5 の `PlayerCache.Current`
- **共通レスポンスの player_diff（`PlayerDiff`）はエンティティではない**。`PlayerDiff` は Contracts の DTO で、操作の RPC（ガチャ・強化・プロデュースの完了など）の結果を、個々のレスポンスの項目から拾わず、1 つの共通の処理（`PlayerCommonApplier`）でクライアントのリポジトリに反映するための入れ物。反映の処理が DTO をエンティティに直し、各リポジトリに Upsert・Remove する。サインインは今どおり全件を置き換え（アイドル・サポートカードは `ReplaceAll`、1 人分のプレイヤーは `Upsert`）、サインインでの取り直し（計画の `RefreshPlayerUseCase`。読み替えで `IPlayerUseCase.RefreshAsync`）も安全網として残す（[共通レスポンスの計画](../../plans/2026-11-08-common-response-player-diff.md) 0章 4・9）
- 共通レスポンスの計画は次のように読み替える（計画の読み替えの節に書いた）: Application の `PlayerDiffData` と `PlayerCache.Apply` は作らない。`PlayerCommonApplier` が `IPlayerRepository`・`IIdolRepository`・`ISupportCardRepository` に書く。サーバー側の `PlayerDiffCollector` の `Touch…` を呼ぶ位置と引数は変わらない。サーバー側を移す実装計画では、コレクターが受けるリポジトリの IF の置き場（Domain）・エンティティの型の名前・`ListAsync` → `FindAllAsync` の改名に合わせて直す
- ready のクライアントの画面の計画（`PlayerCache` を受ける形）を読み替える。[api-design.md](../architecture/api-design.md) の型の名前も、改名のときに直す

## 見直す条件

- クライアントのリポジトリの IF が大きくなり、読み取りと書き込みの 2 つでは足りなくなったとき
- クライアントの Domain の層が薄すぎて、層を分ける手間に見合わないと分かったとき
- クライアントに非同期でしか読めない保存先が増え、起動時の読み込みで済まなくなったとき（IF を非同期にする）
- サーバーの Domain が `TxContext.Connection` に触れるコードが出てきたとき（IF を Application に戻す）
