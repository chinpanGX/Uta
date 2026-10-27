---
title: ADR 0006 Unity 7 を早期に触ることをプロジェクトの目的に加える
status: accepted
updated: 2026-10-27
---

# ADR 0006: Unity 7 を早期に触ることをプロジェクトの目的に加える

## 状況

Unity 7 が発表された（Unite Seoul 2026）。公開されている予定は次のとおり（出典: [Unite Seoul 2026 Keynote Recap](https://unity.com/blog/unite-seoul-keynote-2026-recap)、2026-10-26 時点）。

- Preview は 12 月、正式リリースは 2027 年 Q1 を目標とする。一部機能は正式リリース前にベータで提供される
- CoreCLR を出荷し、.NET 10 / C# 14 を使えるようにする。MSBuild もサポートし、Editor 外でのコンパイルが可能になる
- Unity 6 の直接の継続で、互換性を壊す変更は予定していない（公式の説明）。互換性確認のため Project Auditor が Unity 6 に統合されている

Uta は技術先行のプロジェクトであり、Unity 7 に早く触れて知見を得る場としても使いたい。

## 決定

- Uta の目的に **「Unity 7 を早期段階で触る」** を追加する。ゲームシステムの縦切り実装と並ぶ、二本目の目的とする
- クライアント基盤とサーバー基盤（[ADR 0005](0005-foundation-submodule.md) の基盤。Unity 側の UPM パッケージ部分と `shared/` を含む）を Unity 7 で動かして検証する
- **Unity のバージョンは Unity 7 で確定する**（2026-10-27。現在のプロジェクトは `7000.0.0a12`）。Unity 6 系では進めない。Preview（12 月予定）・正式版が出たら追従し、互換性の確認結果を wiki に残す
- 検証で分かった互換性の問題・移行手順・新機能の使い勝手は、`docs/raw/` に記録し、wiki に反映する
- 目的の優先順位: Unity 7 のために縦切りの MVP 範囲を広げない。Unity 7 の検証は MVP の実機ビルドと並行して行う

## 理由

- 技術先行のプロジェクトとして、新バージョンの評価は目的に合う
- CoreCLR / .NET 10 / C# 14 が使えるなら、サーバー（.NET 10）と Unity で言語バージョンの差が小さくなり、`shared/` の制約（C# 9）が変わりうる。早く確認する価値が大きい
- 互換性を壊す変更がない方針なので、Unity 6 系の資産・知見を流用しやすい

## 検討した代替案

- 案A: Unity 7 の正式リリースまで検証しない。知見を得る時期が遅れるため不採用
- 案B: 最初から Unity 7 のみで開発する。当初は、実機ビルドを早期に通す方針（CLAUDE.md）と衝突するため不採用としたが、2026-10-27 に**採用へ変更**した（Alpha を使うリスクは受け入れる）

## 影響・リスク

- いまは Alpha（`7000.0.0a12`）で、Preview（12 月予定）より前の不安定な状態の可能性がある。Android / iOS の実機ビルド（IL2CPP / AOT）が Preview で通るかは**未確認**
- CoreCLR 下での iOS 向けの扱い（IL2CPP を使い続けるか、AOT の制約がどう変わるか）は**推測できない**。公式情報を確認する
- `client/`・`shared/` の C# 9 制約（CLAUDE.md、`.editorconfig`）は、Unity 7 で C# 14 が使えると確認できるまで変えない。変えるときは別 ADR を起こす
- MagicOnion・MessagePack など Unity 向けライブラリが Unity 7 で動くかを確認する必要がある（Source Generator の扱いを含む）
- 「サーバー基盤も Unity 7 で試す」の範囲は未確定（下記）

## 未決事項

- 「サーバー基盤を Unity 7 で試す」の範囲: (a) 基盤の Unity 側パッケージと `shared/` を Unity 7 で参照する、(b) サーバーのコードを Unity 7 のプロジェクト（MSBuild）で扱う、(c) サーバープロセス自体を Unity 7 上で動かす、のどれか。いまは (a) を前提に書いている（推測）
- Alpha → Preview → 正式版への追従方針と、バージョン固定の方法（`ProjectSettings/ProjectVersion.txt`、CI）
- Unity 7 Preview の入手方法・ライセンス条件
- 検証結果を残すページ（`architecture/` に Unity 7 検証のページを作る予定）

## 見直す条件

- Unity 7 の提供時期が大きく遅れたとき
- Preview の段階で、基盤（MagicOnion / MessagePack）が動かないと分かったとき
