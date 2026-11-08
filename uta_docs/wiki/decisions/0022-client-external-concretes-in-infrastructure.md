---
title: ADR 0022 クライアントは外部に触れる具象を Infrastructure に置き、Application には UseCase の IF を置く
status: proposed
updated: 2026-11-08
---

# ADR 0022: クライアントは外部に触れる具象を Infrastructure に置き、Application には UseCase の IF を置く

[repository-layout.md](../architecture/repository-layout.md) のクライアントの層の決定のうち、Application の責務（4章の表の Application の行「外部とのやりとりはポートで定義する」、2章の木の「ユースケース、ポート」）と Infrastructure の責務（4章の表の Infrastructure の行「ポートの実装」）を置き換える。エンティティとリポジトリの IF の置き場は [ADR 0021](0021-domain-entities-and-repositories.md) が決める。サーバーは変えない。

決定はどれも 2026-11-08 にユーザーが決めた（経緯は [raw の記録](../../raw/2026-11-08-naming-and-layers-decisions.md) と、設計書のレビューでの判断を書いたコミットのメッセージ）。

**accepted にする条件**: Player（サインアップ・サインイン）をこの形に移す実装計画（[基盤移行の計画](../../plans/2026-11-08-client-foundation-migration.md)。順 0）で確かめる。その計画では、具象の中身を移す前に、今の `EnterPlayerUseCaseTests` の場合分け（先にサインインして未作成ならサインアップしてからサインインし直す、サインアップの後も未作成なら失敗、など）を、偽の `IPlayerService` を使う手順のテストとして書く。「中身を移す前」は、UseCase の IF・中身の無い具象・偽・テストを先に書いてテストが失敗することを確かめ、そのあと今の `EnterPlayerUseCase`・`PlayerGateway` から中身を移す、という順を指す。確かめるのは、偽物で失敗も作れて手順のテストが書けること（機構の確認）。テストの重さが受け入れられるかは、ユーザーがその PR で判断する。書けない、または重すぎると判断したら、この ADR は採らず、案A（今の形）に戻す。

**accepted にする時点と、進めてよいこと** [決定。2026-11-08。ユーザーの判断]:

- ADR 0020・0021・0022 の 3 本は、**proposed のまま順 0（上の計画）で実装し、順 0 の PR で 3 本まとめて accepted にするかを判断する**。順 0 より前に accepted にはしない
- proposed の間に進めてよいこと: ready のクライアントの画面の計画と共通レスポンスの計画に、3 本の形への「読み替え」の節を案として書くこと、順 0 を実装すること。順 0 以外の読み替えの節の実装は、accepted の後
- **採らないと判断したら**、順 0 の同じ PR で、ADR に由来する部分を今の形に戻す: asmdef `Uta.Client.Domain`・`Uta.Client.MasterData` の追加と生成物の移動（生成物は Infrastructure に戻す）、エンティティとリポジトリ（→ `PlayerCache` など）、UseCase の IF と Infrastructure の具象とその偽・テスト（→ ポートとゲートウェイ）、`MemoryDatabase` の DI への登録（ADR 0020。[masterdata-pipeline.md](../architecture/master/masterdata-pipeline.md)「`MemoryDatabase` 自体は DI に登録しない」に戻す）。**戻さないもの**（[naming.md](../architecture/naming.md) に由来し、ADR の採否と関係ない）: 改名 `ApiClient`・`ErrorDefinitions`・`MagicOnionClientInitializer`・`CardCatalogFactory`、テストのアセンブリ `Uta.Client.Tests.EditMode` と名前空間 `Uta.Client.Tests`、`MasterDataLoader.LoadFromStreamingAssetsAsync` で読んでから `CardCatalogFactory.Create(db)` する形（Factory は I/O をしない: naming.md 7章）。後の計画の本文に出る旧い名前は、この改名後の名前で読む。各計画の「ADR 0020〜0022 の読み替え」の節は使わない（「基盤 11d9b06 への読み替え」の節は使う）。この一覧が正で、計画の節はこの一覧を指す
- 3 本のうちサーバーに関わる決定（ADR 0020 のサーバーの Catalog をやめること、ADR 0021 のサーバーのリポジトリの IF を Domain に移すこと）は、順 0 で accepted にしても、サーバーのコードはサーバー側の実装計画まで今の形のまま。それまでサーバーの wiki（`server/`・`repository-layout.md` のサーバーの部分・`glossary.md` の Server.Domain）は今のコードの説明として残す

## 状況

いまのクライアントは、Application に UseCase の具象（`EnterPlayerUseCase` など）と通信のポート（`IPlayerPort`）を置き、Infrastructure のゲートウェイ（`PlayerGateway`）がポートを実装している。サーバー権威なので、クライアントの UseCase の多くは「通信して結果を詰めるだけ」で、Application の具象はほとんど中身を持たない。

いまの Infrastructure は、基盤のラッパー（`PlayerApi`。`ApiServiceBase<T>`）越しに通信していて、失敗の結果（`ApiResult`）をテストから作れない。基盤移行の計画（[2026-11-08-client-foundation-migration.md](../../plans/2026-11-08-client-foundation-migration.md)。ready・未実装）は、共通基盤 11d9b06 の新しい呼び方に移り、`PlayerApi` をやめて共有インターフェース（`IPlayerService`）を直接呼ぶ形にする。計画では、テストは偽の `IPlayerService` と基盤の `FakeRpcErrors.ApiFailureResult<T>` で失敗も作る（計画 4章・5章）。これが通れば、通信の具象を Infrastructure に置いてもテストができる。この計画には、この ADR の形（Player を `IPlayerUseCase` と Infrastructure の具象に移す）への読み替えの節を足し、この ADR の accepted の条件を確かめる計画にした（2026-11-08）。

## 決定

- **外部に触れる具象は Infrastructure に置く**。「外部に触れる」は、次のどれかを直接参照すること（asmdef の参照で判定できる）: `Uta.Shared.Contracts`（通信）、共通基盤のクライアント（VolTech-MagicOnion の `VolTech.Client`・`VolTech.Server.Abstractions` など。端末の認証もここ）、保存の API（Supplement のセーブデータなど）、マスタの読み込み（`UnityWebRequest`・`MasterMemory.Addons.Core`）。UnityEngine の API の使用だけは asmdef で判定できないので、コードレビューで見る。UnityScreenNavigator（画面の遷移）は外部に数えない。Domain のリポジトリの IF（[ADR 0021](0021-domain-entities-and-repositories.md)）越しの読み書きは、外部に数えない。Bootstrap（組み立て）はこの線の外
- Infrastructure の具象は `I<機能>Service`（Contracts）を直接呼び、DTO を Application・Domain の型に詰め替える。通信のポート（`I<機能>Port`）とゲートウェイの IF は作らない
- **Application には、機能ごとの UseCase の IF（`I<機能>UseCase`）と、その出力の型を置く**（入力は引数で受け、型を作らない。[naming.md](../architecture/naming.md) 1章）
  - 機能の単位は Contracts の `I<機能>Service` に揃える（`IGachaUseCase` と `IGachaService`）。メソッドは RPC と 1 対 1 ではなく、Presenter の操作の単位にし、手順（呼ぶ順・再試行・取り直し）を含める（例: プレイヤーの `EnterAsync` は、サインインして未作成ならサインアップしてサインインし直す、までを 1 つで行う）
  - 使い手が分かれるとき（画面の操作と診断など）は、IF を分けてよい。具象は 1 つのままでよい
  - Contracts に相手が無い機能（端末の認証など）も、同じ形で `I<機能>UseCase` を置く
  - 業務エラーの reason を Presenter に渡す共通の戻り値の型（計画の `PortResult<T>`）は、**Presenter が reason で画面ごとの文言を出し分ける操作があるときだけ**、UseCase の IF の戻り値の型として Application に作る（名前は naming.md 2章の `UseCaseResult<T>`。reason の定数 `ServerReasons` も同じ）。Infrastructure の具象が `ErrorCode` を比べて reason に直す。結果を enum（`<操作>ResultKind`）で返せる操作は enum を返す。ready の計画（gacha・owned・formation）はどの操作も enum に写しているので、今は作らない見込み [決定。2026-11-08。ユーザーの判断。「残す」としていたのを直した]
- **外部に触れない具象は Application に置いてよい**。ready の計画にあるもの:
  - 計算: 排出率の計算（gacha の計画の `GachaRateCalculator`）、所持一覧の組み立て（owned の計画の `OwnedListBuilder`。名前は `OwnedListFactory` にする）
  - ルール: 編成のプランの判定（formation の計画の、`FormationValidator.IsPlanCompatible` とマスタで合わない枠を外す処理）
  - 状態: プロデュース中の状態（formation の計画の `ProduceSession`。ADR 0021 の UI の状態の型）
  - 手順のうち通信を伴うもの（formation の計画の `ProduceActions`・`StartProduceUseCase`・`ResumeProduceUseCase`・`AbandonProduceUseCase`）は、外部に触れるので Infrastructure の `IProduceUseCase` の具象に移す（`ProduceSession` への書き込みも具象が行う）
  - **状態の型への書き込みは、通信を伴わないものも含めて UseCase の具象が行う**。Presenter は状態の型を読む（DtoFactory 越しか、状態の型を受けて読む）が、書かない。UI の表示済みの印（試験結果を出したか）や画面を離れるときのクリアも、UseCase の IF にメソッドを置いて具象が書く（formation の計画の `IProduceUseCase.MarkExamResultShown()`・`ClearSession()`）[決定。2026-11-08。ユーザーの判断]
  - 効果の予測（Shared.Domain を呼ぶもの）は MVP では出さない（[screen-design.md](../architecture/screen-design.md) 6章）
- UseCase でない Application の IF（`ISceneNavigator` など。実装は Bootstrap）は今のまま
- **Presenter が業務の操作と読み取りのために受けるのは、Application の公開の型（UseCase の IF・出力の型・外部に触れない具象・状態の型）と、画面の `<画面>DtoFactory` だけ**。通信の中身（MagicOnion、DTO、エラーの変換）を意識しない。表示のための読み取り（Domain の読み取り専用のリポジトリ、マスタ、`ICardCatalog`）は DtoFactory が行う。View・画面の補助（`DialogOpener`（計画の `DialogService`）・`PageStack`・`ProduceFlow` など Presentation の型）・基盤の画面の IF（`IScreenNavigator`）・Application の `ISceneNavigator` は、これまでどおり受けてよい
- **`<画面>DtoFactory` は Presentation の型で、View に渡す表示用の値（`<View 名>Dto`）を作る**（[naming.md](../architecture/naming.md) 6章）。リポジトリに書かず、通信もしない。ready の計画で Presenter が行っていた表示用の組み立て（static の組み立ての呼び出しを含む）は DtoFactory に移り、Presenter のコンストラクターは DtoFactory を受ける形になる
  - DtoFactory が出すのは表示用の値だけ。Presenter が操作に使う値（開始するシナリオの ID など、UseCase に渡す業務の入力）は DtoFactory から取らず、外部に触れない Application の具象（formation の計画の `ScenarioResolver` など）から取る。表示用の Dto に、Presenter が操作の分岐に使う印（`ResumeVisible` など、表示と同じ値）を載せるのはよい [決定。2026-11-08]
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
  - Presenter のテストの偽物は `Fake<機能>UseCase` で、同じくテストのアセンブリに置く。返す値の Queue の要素は `Func<UniTask<T>>` にし、本物の具象が行う副作用（状態の型・リポジトリへの書き込み）と例外は、テストが積む `Func` の中で再現する。偽そのものは状態の型・リポジトリに書かない（書く処理を偽に二重に持たせないため）。例外: `IProduceUseCase.MarkExamResultShown()` のような、状態の型に書くだけのメソッドは、偽が受けた状態の型に書いてよい
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
