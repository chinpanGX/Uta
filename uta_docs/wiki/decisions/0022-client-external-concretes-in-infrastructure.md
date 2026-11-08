---
title: ADR 0022 クライアントは外部に触れる具象を Infrastructure に置き、Application には UseCase の IF を置く
status: proposed
updated: 2026-11-08
---

# ADR 0022: クライアントは外部に触れる具象を Infrastructure に置き、Application には UseCase の IF を置く

[repository-layout.md](../architecture/repository-layout.md) のクライアントの層の決定のうち、Application の責務（4章の表の Application の行「外部とのやりとりはポートで定義する」、2章の木の「ユースケース、ポート」）と Infrastructure の責務（4章の表の Infrastructure の行「ポートの実装」）を置き換える。エンティティとリポジトリの IF の置き場は [ADR 0021](0021-domain-entities-and-repositories.md) が決める。サーバーは変えない。

決定はどれも 2026-11-08 にユーザーが決めた（経緯は [raw の記録](../../raw/2026-11-08-naming-and-layers-decisions.md) と、設計書のレビューでの判断を書いたコミットのメッセージ）。

**accepted にする条件**: Player（サインアップ・サインイン）をこの形に移す実装計画で確かめる。その計画では、具象を移す前に、今の `EnterPlayerUseCaseTests` の場合分け（先にサインインして未作成ならサインアップしてからサインインし直す、サインアップの後も未作成なら失敗、など）を、偽の `IPlayerService` を使う手順のテストとして書く。確かめるのは、偽物で失敗も作れて手順のテストが書けること（機構の確認）。テストの重さが受け入れられるかは、ユーザーがその PR で判断する。書けない、または重すぎると判断したら、この ADR は採らず、案A（今の形）に戻す。

**accepted の前に進めてよいこと**: ready のクライアントの画面の計画 6 本と共通レスポンスの計画に、この ADR と ADR 0020・0021 の形への「読み替え」の節を、案として先に書いておく（実装は accepted の後）。

## 状況

いまのクライアントは、Application に UseCase の具象（`EnterPlayerUseCase` など）と通信のポート（`IPlayerPort`）を置き、Infrastructure のゲートウェイ（`PlayerGateway`）がポートを実装している。サーバー権威なので、クライアントの UseCase の多くは「通信して結果を詰めるだけ」で、Application の具象はほとんど中身を持たない。

いまの Infrastructure は、基盤のラッパー（`PlayerApi`。`ApiServiceBase<T>`）越しに通信していて、失敗の結果（`ApiResult`）をテストから作れない。基盤移行の計画（[2026-11-08-client-foundation-migration.md](../../plans/2026-11-08-client-foundation-migration.md)。ready・未実装）は、共通基盤 11d9b06 の新しい呼び方に移り、`PlayerApi` をやめて共有インターフェース（`IPlayerService`）を直接呼ぶ形にする。計画では、テストは偽の `IPlayerService` と基盤の `FakeRpcErrors.ApiFailureResult<T>` で失敗も作る（計画 4章・5章）。これが通れば、通信の具象を Infrastructure に置いてもテストができる。

## 決定

- **外部に触れる具象は Infrastructure に置く**。「外部に触れる」は、次のどれかを直接参照すること（asmdef の参照で判定できる）: `Uta.Shared.Contracts`（通信）、共通基盤のクライアント（VolTech-MagicOnion の `VolTech.Client`・`VolTech.Server.Abstractions` など。端末の認証もここ）、保存の API（Supplement のセーブデータなど）、マスタの読み込み（`UnityWebRequest`・`MasterMemory.Addons.Core`）。UnityEngine の API の使用だけは asmdef で判定できないので、コードレビューで見る。UnityScreenNavigator（画面の遷移）は外部に数えない。Domain のリポジトリの IF（[ADR 0021](0021-domain-entities-and-repositories.md)）越しの読み書きは、外部に数えない。Bootstrap（組み立て）はこの線の外
- Infrastructure の具象は `I<機能>Service`（Contracts）を直接呼び、DTO を Application・Domain の型に詰め替える。通信のポート（`I<機能>Port`）とゲートウェイの IF は作らない
- **Application には、機能ごとの UseCase の IF（`I<機能>UseCase`）と、その出力の型を置く**（入力は引数で受け、型を作らない。[naming.md](../architecture/naming.md) 1章）
  - 機能の単位は Contracts の `I<機能>Service` に揃える（`IGachaUseCase` と `IGachaService`）。メソッドは RPC と 1 対 1 ではなく、Presenter の操作の単位にし、手順（呼ぶ順・再試行・取り直し）を含める（例: プレイヤーの `EnterAsync` は、サインインして未作成ならサインアップしてサインインし直す、までを 1 つで行う）
  - 使い手が分かれるとき（画面の操作と診断など）は、IF を分けてよい。具象は 1 つのままでよい
  - Contracts に相手が無い機能（端末の認証など）も、同じ形で `I<機能>UseCase` を置く
  - 業務エラーの reason を Presenter に渡す共通の戻り値の型（計画の `PortResult<T>`）は、UseCase の IF の戻り値の型として Application に残す（名前は naming.md 2章の `UseCaseResult<T>`）。残す理由: Presenter が reason で画面ごとの文言を出し分けるため。Infrastructure の具象が `ErrorCode` を比べて reason に直す
- **外部に触れない具象は Application に置いてよい**。ready の計画にあるもの:
  - 計算: 排出率の計算（gacha の計画の `GachaRateCalculator`）、所持一覧の組み立て（owned の計画の `OwnedListBuilder`。名前は `OwnedListFactory` にする）
  - ルール: 編成のプランの判定（formation の計画の、`FormationValidator.IsPlanCompatible` とマスタで合わない枠を外す処理）
  - 状態: プロデュース中の状態（formation の計画の `ProduceSession`。ADR 0021 の UI の状態の型）
  - 手順のうち通信を伴うもの（formation の計画の `ProduceActions`・`StartProduceUseCase`・`ResumeProduceUseCase`・`AbandonProduceUseCase`）は、外部に触れるので Infrastructure の `IProduceUseCase` の具象に移す（`ProduceSession` への書き込みも具象が行う）
  - 効果の予測（Shared.Domain を呼ぶもの）は MVP では出さない（[screen-design.md](../architecture/screen-design.md) 6章）
- UseCase でない Application の IF（`ISceneNavigator` など。実装は Bootstrap）は今のまま
- **Presenter が業務の操作と読み取りのために受けるのは、Application の公開の型（UseCase の IF・出力の型・外部に触れない具象・状態の型）と、画面の `<画面>DtoFactory` だけ**。通信の中身（MagicOnion、DTO、エラーの変換）を意識しない。表示のための読み取り（Domain の読み取り専用のリポジトリ、マスタ、`ICardCatalog`）は DtoFactory が行う。View・画面の補助（`DialogOpener`（計画の `DialogService`）・`PageStack`・`ProduceFlow` など Presentation の型）・基盤の画面の IF（`IScreenNavigator`）・Application の `ISceneNavigator` は、これまでどおり受けてよい
- **`<画面>DtoFactory` は Presentation の型で、View に渡す表示用の値（`<View 名>Dto`）を作る**（[naming.md](../architecture/naming.md) 6章）。リポジトリに書かず、通信もしない。ready の計画で Presenter が行っていた表示用の組み立て（static の組み立ての呼び出しを含む）は DtoFactory に移り、Presenter のコンストラクターは DtoFactory を受ける形になる
- 依存のルールは今のまま: Application は Infrastructure・Presentation・`Uta.Shared.Contracts` を参照しない。Presentation は Infrastructure・`Uta.Shared.Contracts` を参照しない

## 理由

- 中身を素通しするだけの Application の具象とポートが無くなり、1 つの操作に要るクラスが減る
- Presentation が通信を意識しない、という目的は、IF を Application に置くことで満たせる
- 外部に触れない計算まで Infrastructure に置く理由はない。Application に置けば、偽物なしでテストできる
- Presenter のテストは、偽の `I<機能>UseCase` を 1 つ渡すだけになる

## 検討した代替案

- 案A: 今のまま（Application に UseCase の具象、通信はポート越し）。手順のテストはしやすいが、素通しのクラスが増える
- 案B: 1 回呼ぶだけの操作は UseCase を作らず、Presenter がポートを直接呼ぶ。クラスは減るが、Presenter がポート（通信の口）を意識する
- 案C: Application を IF だけにし、外部に触れない具象も Infrastructure に置く。線は単純だが、計算だけのクラスが Infrastructure に入り、層の意味がぼやける

## 影響・リスク

- `IPlayerPort`・`IDeviceAuthPort`・`PlayerGateway`・`DeviceAuthGateway`・`EnterPlayerUseCase`・`SignInUseCase`・`RefreshPlayerUseCase`（計画）の置き場と名前が変わる。`IDeviceAuthPort` の診断用のメンバー（`CanReceiveServerErrorAsync`・`InvalidateServerSideTokenAsync`・`TokenInvalidationCount`）を、画面の UseCase の IF と分けるかは、移す実装計画で決める
- **手順のテストが、MagicOnion の `IService<T>` のメンバー・基盤のエラーの表し方・DTO の形の 3 つに結合する**（今の `FakePlayerPort` は切れていた）。重さを今と同じに保つため、テストの規約を [screen-design.md](../architecture/screen-design.md) に書く:
  - 偽のサービスは機能ごとに 1 つ（`Fake<機能>Service`）、テストのアセンブリに置いて使い回す（返す値の Queue と、呼び出しの記録を持つ）
  - Presenter のテストの偽物は `Fake<機能>UseCase` で、同じくテストのアセンブリに置く
  - DTO → Application・Domain の型の詰め替えは、static の純粋な関数（今の `PlayerGateway.ToSnapshot` の形）に分け、詰め替えのテストを手順のテストと別に持つ
  - 基盤移行の計画の private の偽物（`PlayerGatewayTests` の中の偽の `IPlayerService`）と、画面の計画の読み替えの節の「`PlayerGatewayTests` と同じく」は、この規約に読み替える
- Infrastructure の具象が、手順（呼ぶ順・再試行）とリポジトリへの書き込みを持つ。Infrastructure のテストが厚くなる
- accepted にしたら直すもの:
  - [repository-layout.md](../architecture/repository-layout.md) 2章の木・4章の表
  - [screen-design.md](../architecture/screen-design.md) 7.4（ユースケースの行「通信は `I<機能>Port` 越し」、組み立ての行「ゲートウェイ」）・6章（Presenter は UseCase 越しに受け取る）。上のテストの規約と、foundation の計画 3.3 の画面の共通ルールもここに移す
  - [api-design.md](../architecture/api-design.md) のクライアントの実装の記述（`PlayerGateway`）
  - [glossary.md](../glossary.md) の「ポート」
  - コードレビューのチェックリスト `.claude/skills/requesting-code-review/layers/client.md`（「外部とのやりとりはポートで定義しているか」を、この ADR の線引きに直す。「Presenter が業務の操作と読み取りのために受けるのは、Application の公開の型と DtoFactory だけか」を足す）
  - main の ready のクライアントの画面の計画（foundation・formation・gacha・owned・week・lesson）。Application に UseCase と Gateway を置く形で書かれているので、実装する前に「読み替え」の節を足す

## 見直す条件

- 外部に触れない処理が増え、Application の具象と Infrastructure の具象の境目で迷うことが多くなったとき
- 通信の具象のテストが、偽の `I<機能>Service` では書きにくいと分かったとき
