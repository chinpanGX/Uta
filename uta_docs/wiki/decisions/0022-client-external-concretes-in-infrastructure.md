---
title: ADR 0022 クライアントは外部に触れる具象を Infrastructure に置き、Application には UseCase の IF を置く
status: proposed
updated: 2026-11-08
---

# ADR 0022: クライアントは外部に触れる具象を Infrastructure に置き、Application には UseCase の IF を置く

[repository-layout.md](../architecture/repository-layout.md) のクライアントの層の決定のうち、Application の責務（4章の表の Application の行「外部とのやりとりはポートで定義する」、2章の木の「ユースケース、ポート」）と Infrastructure の責務（4章の表の Infrastructure の行「ポートの実装」）を置き換える。エンティティとリポジトリの IF の置き場は [ADR 0021](0021-domain-entities-and-repositories.md) が決める。サーバーは変えない。

**accepted にする条件**: [クライアントの基盤移行の計画](../../plans/2026-11-08-client-foundation-migration.md) のタスク 2 で、偽の `IPlayerService` を使う `PlayerGatewayTests` が通ること（この ADR の前提を確かめるため）。

## 状況

いまのクライアントは、Application に UseCase の具象（`EnterPlayerUseCase` など）と通信のポート（`IPlayerPort`）を置き、Infrastructure のゲートウェイ（`PlayerGateway`）がポートを実装している。サーバー権威なので、クライアントの UseCase の多くは「通信して結果を詰めるだけ」で、Application の具象はほとんど中身を持たない。

いまの Infrastructure は、基盤のラッパー（`PlayerApi`。`ApiServiceBase<T>`）越しに通信していて、失敗の結果（`ApiResult`）をテストから作れない。基盤移行の計画（ready。未実装）は、共通基盤 11d9b06 の新しい呼び方に移り、`PlayerApi` をやめて共有インターフェース（`IPlayerService`）を直接呼ぶ形にする。計画では、テストは偽の `IPlayerService` と基盤の `FakeRpcErrors.ApiFailureResult<T>` で失敗も作る（計画 4章・5章）。これが通れば、通信の具象を Infrastructure に置いてもテストができる。

## 決定

- **外部に触れる具象は Infrastructure に置く**。「外部に触れる」は、`Uta.Shared.Contracts`（通信）か保存の API（Supplement のセーブデータなど）を直接参照すること。asmdef の参照で判定できる。Domain のリポジトリの IF（[ADR 0021](0021-domain-entities-and-repositories.md)）越しの読み書きは、外部に数えない
- Infrastructure の具象は `I<機能>Service`（Contracts）を直接呼び、DTO を Application・Domain の型に詰め替える。通信のポート（`I<機能>Port`）とゲートウェイの IF は作らない
- **Application には、機能ごとの UseCase の IF（`I<機能>UseCase`）と、その入出力の型を置く**
  - 機能の単位は Contracts の `I<機能>Service` に揃える（`IGachaUseCase` と `IGachaService`）。メソッドは RPC と 1 対 1 ではなく、Presenter の操作の単位にし、手順（呼ぶ順・再試行）を含める（例: プレイヤーの `EnterAsync` は、サインアップ → サインイン → 未作成なら再試行、を 1 つで行う）
  - 使い手が分かれるとき（画面の操作と診断など）は、IF を分けてよい。具象は 1 つのままでよい
  - Contracts に相手が無い機能（端末の認証など）も、同じ形で `I<機能>UseCase` を置く
- **外部に触れない具象は Application に置いてよい**（Shared.Domain を呼ぶだけの計算など）。今の MVP には該当するものが無い（使用可否はサーバーが返し、予測は出さない。[screen-design.md](../architecture/screen-design.md) 6章）
- UseCase でない Application の IF（`ISceneNavigator` など。実装は Bootstrap）は今のまま
- **Presenter は、Application の公開の型（IF・入出力の型・外部に触れない具象）と、画面の `<画面>DtoFactory` だけを受ける**。通信の中身（MagicOnion、DTO、エラーの変換）を意識しない。表示のための読み取り（Domain の読み取り専用のリポジトリ、マスタ）は DtoFactory が行う（DtoFactory は Presentation の型。[naming.md](../architecture/naming.md) 6章）
- 依存のルールは今のまま: Application は Infrastructure・Presentation・`Uta.Shared.Contracts` を参照しない。Presentation は Infrastructure・`Uta.Shared.Contracts` を参照しない

## 理由

- 中身を素通しするだけの Application の具象とポートが無くなり、1 つの操作に要るクラスが減る
- Presentation が通信を意識しない、という目的は、IF を Application に置くことで満たせる
- 外部に触れない計算まで Infrastructure に置く理由はない。Application に置けば、偽物なしでテストできる

## 検討した代替案

- 案A: 今のまま（Application に UseCase の具象、通信はポート越し）。手順のテストはしやすいが、素通しのクラスが増える
- 案B: 1 回呼ぶだけの操作は UseCase を作らず、Presenter がポートを直接呼ぶ。クラスは減るが、Presenter がポート（通信の口）を意識する
- 案C: Application を IF だけにし、外部に触れない具象も Infrastructure に置く。線は単純だが、計算だけのクラスが Infrastructure に入り、層の意味がぼやける

## 影響・リスク

- `IPlayerPort`・`IDeviceAuthPort`・`PlayerGateway`・`DeviceAuthGateway`・`EnterPlayerUseCase`・`SignInUseCase` の置き場と名前が変わる。`IDeviceAuthPort` の診断用のメンバー（`CanReceiveServerErrorAsync`・`InvalidateServerSideTokenAsync`・`TokenInvalidationCount`）を、画面の UseCase の IF と分けるかは、移す実装計画で決める
- **手順のテストが、MagicOnion の `IService<T>` のメンバー・基盤のエラーの表し方・DTO の形の 3 つに結合する**（今の `FakePlayerPort` は切れていた）。重さを今と同じに保つため:
  - 偽のサービスは機能ごとに 1 つ、テストのアセンブリに置いて使い回す（返す値の Queue と、呼び出しの記録を持つ）
  - DTO → Application・Domain の型の詰め替えは、static の純粋な関数（今の `PlayerGateway.ToSnapshot` の形）に分け、詰め替えのテストを手順のテストと別に持つ
- Infrastructure の具象が、手順（呼ぶ順・再試行）とリポジトリへの書き込みを持つ。Infrastructure のテストが厚くなる
- accepted にしたら直すもの:
  - [repository-layout.md](../architecture/repository-layout.md) 2章の木・4章の表
  - [screen-design.md](../architecture/screen-design.md) 7.4（ユースケースの行「通信は `I<機能>Port` 越し」、組み立ての行「ゲートウェイ」）・6章（Presenter は UseCase 越しに受け取る、を残すか）
  - [api-design.md](../architecture/api-design.md) のクライアントの実装の記述（`PlayerGateway`）
  - [glossary.md](../glossary.md) の「ポート」
  - コードレビューのチェックリスト `.claude/skills/requesting-code-review/layers/client.md`（「外部とのやりとりはポートで定義しているか」を、この ADR の線引きに直す。「Presenter は Application の公開の型と DtoFactory だけを受けるか」を足す）
  - main の ready のクライアントの画面の計画（foundation・formation・gacha・owned・week・lesson）。Application に UseCase と Gateway を置く形で書かれているので、実装する前に「読み替え」の節を足す

## 見直す条件

- 外部に触れない処理が増え、Application の具象と Infrastructure の具象の境目で迷うことが多くなったとき
- 通信の具象のテストが、偽の `I<機能>Service` では書きにくいと分かったとき
