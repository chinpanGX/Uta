---
title: ADR 0009 クライアントは uGUI + UnityScreenNavigator・VContainer・UniTask・縦画面にする
status: accepted
updated: 2026-10-27
---

# ADR 0009: クライアントは uGUI + UnityScreenNavigator・VContainer・UniTask・縦画面にする

## 状況

クライアントをオニオンアーキテクチャにする（[ADR 0008](0008-shared-domain-and-onion.md)）にあたり、画面の実装手段、DI、非同期処理、画面の向きを決める必要がある。`external/` には `UnityScreenNavigator`（uGUI 向けの画面遷移）と、VContainer に依存する `Supplement` を取り込み済み（[ADR 0005](0005-foundation-submodule.md)）。

## 決定

- **画面は uGUI + UnityScreenNavigator で作る**
- **DI は VContainer**。組み立て（Composition Root）は `Bootstrap` で行う
- **非同期処理は UniTask**。クライアントの `Application` から参照してよい（`noEngineReferences` は使わない）。`shared/Uta.Domain` は同期処理だけにする
- **画面は縦向き**。Windows は縦長のウィンドウで動かす

詳細は [architecture/screen-design.md](../architecture/screen-design.md)、[architecture/repository-layout.md](../architecture/repository-layout.md)。

## 理由

- UnityScreenNavigator はすでに取り込み済みで、uGUI は知見が多い
- VContainer は `Supplement` の依存と揃う
- UniTask は MagicOnion のクライアントや Unity の非同期 API と相性が良く、割り当ても少ない
- 縦向きは、参考にしているゲームシステムの操作感（カードを手札から選ぶ）に近く、モバイルなら片手で遊べる

## 検討した代替案

- UI Toolkit: Unity の今後の主流で、「Unity 7 を早期に触る」目的（[ADR 0006](0006-adopt-unity-7-early.md)）にも合うが、Unity 7 での実行時の対応状況が未確認で、UnityScreenNavigator も使えない。試すなら、設定画面など一部から入れる
- `Application` で `ValueTask` を使う: UnityEngine 非依存を asmdef で強制できるが、Infrastructure と Presentation で UniTask との変換が要る
- 横向き: PC の画面は活かせるが、モバイルでの操作感と、参考の構造から離れる

## 影響・リスク

- `Application` の UnityEngine 非依存は、コンパイルで強制できない。レビューまたはアナライザーで守る
- UnityScreenNavigator・VContainer・UniTask の Unity 7（`7000.0.0a12`）での動作は未確認。実機ビルド（Windows / Android / iOS）で早期に確かめる
- 縦向きのため、PC の横長の画面では左右が余る

## 見直す条件

- Unity 7 で uGUI・UnityScreenNavigator・VContainer・UniTask のいずれかが動かないとき
- UI Toolkit の実行時の対応が十分だと確認できたとき（画面単位での移行を検討）
