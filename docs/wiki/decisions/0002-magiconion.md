---
title: ADR 0002 通信方式にMagicOnionを採用
status: accepted
updated: 2026-10-25
---

# ADR 0002: 通信方式に MagicOnion（gRPC）を採用

## 状況

Unity クライアントと ASP.NET Core サーバーの間の API 通信方式を決める必要がある。双方 C# であり、型の共有による開発効率を重視する。

## 決定

- 通信方式は MagicOnion（gRPC / HTTP/2）を採用する
- API は Unary を基本とする。StreamingHub は必要になるまで使わない
- サービスインターフェース、DTO、マスタ型は `shared/` に置き、Unity とサーバーで共有する

## 理由

- C# インターフェースを共有するだけで、型安全な通信ができる
- 個人開発で、REST の API 定義・クライアント生成の手間を減らせる
- 育成1ターン、ガチャなどは Unary で十分に表現できる

## 検討した代替案

- REST（JSON）: 汎用性は高いが、DTO と呼び出しコードの二重管理が発生しやすい
- 素の gRPC（.proto）: 言語非依存だが、今回は C# 同士のため MagicOnion の利点が大きい

## 影響・リスク（要確認）

- **Unity 側の HTTP/2 トランスポート**: Unity 標準の HTTP スタックは gRPC に向かない。YetAnotherHttpHandler 等を使う構成を想定。Windows / Android / iOS の3環境で動作確認が必要
- **iOS の IL2CPP（AOT）制約**: リフレクションやジェネリクスの問題が出やすい。Source Generator / 事前コード生成を使い、iOS 実機ビルドを早期に通す
- **シリアライザ**: MessagePack-CSharp または MemoryPack。別 ADR で決定する
- **インフラ**: HTTP/2 対応のロードバランサー／リバースプロキシが必要。デプロイ先決定時に別 ADR を追加する
- ライブラリのバージョンと推奨構成は更新が早い。実装直前に公式 README で最新を確認する

## 最初の検証（縦切り）

1. `shared/` に `IAuthService`（ログイン）を定義
2. ASP.NET Core 側で実装
3. Unity から Windows → Android 実機 → iOS 実機の順に呼び出す

## 見直す条件

上記3環境のいずれかで、許容できない問題が解決できない場合。
