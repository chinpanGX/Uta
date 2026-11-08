---
title: ADR 0022 クライアントは外部に触れる具象を Infrastructure に置き、Application には UseCase の IF を置く
status: proposed
updated: 2026-11-08
---

# ADR 0022: クライアントは外部に触れる具象を Infrastructure に置き、Application には UseCase の IF を置く

[ADR 0008](0008-shared-domain-and-onion.md) のうち、クライアントの「外部とのやりとりは Application のポートで定義する」の部分を置き換える（ADR 0008 のそれ以外は有効。サーバーは変えない）。

## 状況

いまのクライアントは、Application に UseCase の具象（`EnterPlayerUseCase` など）と通信のポート（`IPlayerPort`）を置き、Infrastructure のゲートウェイ（`PlayerGateway`）がポートを実装している。サーバー権威なので、クライアントの UseCase の多くは「通信して結果を詰めるだけ」で、Application の具象はほとんど中身を持たない。

共通基盤 11d9b06 で呼び方が変わり（[移行の計画](../../plans/2026-11-08-client-foundation-migration.md)）、ラッパーの `PlayerApi` が無くなった。Infrastructure は共有インターフェース（`IPlayerService`）を直接呼び、テストでは偽の `IPlayerService` と基盤の `FakeRpcErrors.ApiFailureResult<T>` で失敗も作れる。通信の具象を Infrastructure に置いても、テストができるようになった。

## 決定

- **外部（通信・保存）に触れる具象は Infrastructure に置く**。Infrastructure の具象は `I<機能>Service`（`Uta.Shared.Contracts`）を直接呼び、DTO を Application・Domain の型に詰め替える。通信のポート（`I<機能>Port`）とゲートウェイの IF は作らない
- **Application には、機能ごとに 1 つの UseCase の IF（`I<機能>UseCase`）と、その入出力の型を置く**。例: `IGachaUseCase` に `DrawAsync`・`ExchangeAsync`（Contracts の `I<機能>Service` と対）
- **外部に触れない具象は Application に置いてよい**（試験の順位の見込み、効果の予測など、Shared.Domain を呼ぶだけの処理）。線引きは「外部に触れるか」の 1 つ
- **Presentation（Presenter）は Application の IF だけを見る**。通信の中身（MagicOnion、DTO、エラーの変換）を意識しない
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

- `IPlayerPort`・`IDeviceAuthPort`・`PlayerGateway`・`EnterPlayerUseCase` の置き場と名前が変わる。手順のテスト（`EnterPlayerUseCaseTests`）は、偽の `IPlayerService` を使う形に書き直す
- main の ready のクライアントの画面の計画（foundation・formation・gacha・owned・week・lesson）は、Application に UseCase と Gateway を置く形で書かれている。実装する前に「読み替え」の節を足す
- Infrastructure の具象が、手順（呼ぶ順・再試行）とリポジトリへの書き込みを持つ。Infrastructure のテストが厚くなる
- UseCase の IF が機能ごとなので、機能が大きくなると IF のメソッドが増える

## 見直す条件

- 外部に触れない処理が増え、Application の具象と Infrastructure の具象の境目で迷うことが多くなったとき
- 通信の具象のテストが、偽の `I<機能>Service` では書きにくいと分かったとき
