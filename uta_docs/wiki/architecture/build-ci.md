---
title: ビルドと実機の確かめ方
status: draft
updated: 2026-11-17
---

# ビルドと実機の確かめ方

[repository-layout.md](repository-layout.md) 7章（ビルド・CI）の実体。いまは手で回すビルドと、実機での確かめ方だけを書く。CI は未実装（7章の「これから作るもの」）。確かめた結果は [unity7-evaluation.md](unity7-evaluation.md) に書き、このページには手順と決めごとを書く。

MVP の完了条件 7（[game-spec.md](../overview/game-spec.md)「完了条件」）の確かめ方はこのページが正。実装計画は [plans/2026-11-11-android-smoke.md](../../plans/2026-11-11-android-smoke.md)。

**このページの状態（2026-11-17）**: 2章の Windows の IL2CPP のビルドと、5.2 の Test Runner のプレイヤー実行（Windows）は今ある。3章の `BuildAndroidIl2Cpp` と `ProjectSettings.asset` の Android の設定は入れたが、Android のビルド・実機での確かめ（3〜5章の Android の部分）はまだ回していない。サーバーのクラウド化の後に回す（4章）

## 1. 共通 [決定]

- Unity のバッチモードのビルドは、Editor が開いていない worktree で回す。同じプロジェクトを開いた Editor があると競合する（[unity7-evaluation.md](unity7-evaluation.md) 4章）。Editor は閉じない
- ビルド用の worktree は、番号の worktree（`tools/worktree-pool.sh`）と別に、ビルドターゲットごとに 1 つ持ち、使い回す: Windows は `C:/Work/Uta-wt/build-foundation`、Android は `C:/Work/Uta-wt/build-android`（2026-11-16 に決定。Windows と行き来すると Library の再インポートになるため）。どちらも detached で、ビルドの前に `git checkout --detach <コミット>` で合わせる
- ビルド用の worktree には `git worktree lock` を掛ける（2026-11-16）。`worktree-pool.sh cleanup` は、番号でない worktree を、HEAD が `origin/main` に入っていて変更が無ければ `git worktree remove` で消す（Library は `.gitignore` 済みなので変更に数えない）。ロックした worktree は消されない
- ビルド用の worktree は `client/uta/Assets/Packages/`（NuGetForUnity の復元先。`.gitignore` 済み）を、Editor で開いている worktree からコピーしてから使う。起動時の自動復元は、ほかにコンパイルエラーがあると走らないため（[unity7-evaluation.md](unity7-evaluation.md) 2章）
- ビルドの成果物は `client/uta/Build/`（`.gitignore` 済み）

## 2. Windows の IL2CPP [決定]

- 入口: `Uta.Client.Editor.BuildCommands.BuildWindowsIl2Cpp`（`client/uta/Build/Windows/Uta.exe`）。コマンドは [CLAUDE.md](../../../CLAUDE.md)「ビルド・テスト・実行」
- スタンドアロンのスクリプトバックエンドを、プロジェクトの設定でも IL2CPP にする（2026-11-16 に決定。`ProjectSettings.asset` の `scriptingBackend` に `Standalone: 1` を足す）。`BuildWindowsIl2Cpp` は前から IL2CPP を指定しているので結果は変わらない。Test Runner がスタンドアロンのテスト用のプレイヤーを作るとき（5.2）に、IL2CPP で作らせるため。Editor の Play は Mono のまま

## 3. Android の IL2CPP [決定。2026-11-16]

| 項目 | 値 |
|---|---|
| 入口 | `Uta.Client.Editor.BuildCommands.BuildAndroidIl2Cpp`。成果物は `client/uta/Build/Android/Uta.apk` |
| 形式 | APK（AAB にしない。`adb install` で入れるため）。`EditorUserBuildSettings.buildAppBundle = false` |
| スクリプトバックエンド | IL2CPP（`ProjectSettings.asset` は既に `Android: 1`。入口でも指定する。Windows と同じ） |
| CPU | ARM64 だけ（`ProjectSettings.asset` の `AndroidTargetArchitectures: 2` は既に ARM64。入口でも指定する） |
| パッケージ名 | `com.chinpangx.uta`（`ProjectSettings.asset` の `applicationIdentifier` の `Android`）。アカウントのファイルの鍵には使わない（[unity7-evaluation.md](unity7-evaluation.md) 4章） |
| 署名 | Unity の既定のデバッグ用の鍵（`~/.android/debug.keystore`）。キーストアはリポジトリに入れない |
| 最小・目標の API レベル | 最小 26（今の設定のまま）、目標は「入っている最新」（今の設定 `0` のまま） |
| SDK・NDK・JDK | Unity Hub で入れた `7000.0.0a12` の Android のモジュールに同梱のもの（`Editor/Data/PlaybackEngines/AndroidPlayer/` の `SDK`・`NDK`・`OpenJDK`。2026-11-11 に、SDK の `platforms` に `android-34`・`android-36`・`android-37.0` があることを確かめた）。別に入れない |
| Addressables | 入口が `AddressableAssetSettings.BuildPlayerContent` を先に実行する（Windows と同じ）。アクティブなビルドターゲットが Android でなければ止める |
| 置き場 | `C:/Work/Uta-wt/build-android`（1章） |
| 端末 | arm64（`ro.product.cpu.abi` が `arm64-v8a`）、Android 8.0（API 26）以上。USB デバッグ |

- `adb` は同梱の SDK の `platform-tools/adb.exe`（PATH には無い）。手順では `ADB="/c/Program Files/Unity/Hub/Editor/7000.0.0a12/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe"` と置いて呼ぶ
- 確かめたもの（2026-11-17。結果は [unity7-evaluation.md](unity7-evaluation.md) 2章）: Test Runner のプレイヤー実行（Windows の IL2CPP）
- 確かめていないもの（サーバーのクラウド化の後の計画で確かめる）: Unity 7 Alpha の Android のビルド（Gradle・IL2CPP の arm64）、YetAnotherHttpHandler のネイティブ（`android-arm64` の `.so` はパッケージにある）、MasterMemory・VContainer・MessagePack の実機での動作、Managed Stripping の既定の値で落ちないか、**Test Runner の Android でのプレイヤー実行**（Test Framework 1.9.0 は、テスト用のプレイヤーを開発ビルドで作り、Android ではパッケージ名を `com.UnityTestRunner.UnityTestRunner` に変えて入れる。ソースを読んだだけ）

## 4. 実機からサーバーへの接続 [決定。2026-11-17。ユーザーの判断]

- Android の実機は、サーバーをクラウド（dev 環境。[repository-layout.md](repository-layout.md) 11章）に置いてからつなぐ。2026-11-16 に決めた `adb reverse` でローカルのサーバーにつなぐ方式（下の箇条）は使わない。接続先の渡し方は、クラウド化の後の計画で決める（6章）
- （2026-11-16 の案。使わない）- USB でつなぎ、`adb reverse tcp:5000 tcp:5000` で、端末の `localhost:5000` を PC の `localhost:5000` に流す。クライアントの接続先は既定の `http://localhost:5000`（`ServerEndpoint`）のままで、コードは変えない。サーバーはローカルの手順（[CLAUDE.md](../../../CLAUDE.md)「ビルド・テスト・実行」）のまま起動する（外に公開しない・ファイアウォールを開けない）
- Android では `-uta-server-url` の起動引数を渡す手段を作らない。dev 環境（Cloud Run）につなぐ手段は、dev 環境を作るときに決める（[repository-layout.md](repository-layout.md) 11章）
- 平文の HTTP/2（h2c）: YetAnotherHttpHandler は Android の通信の仕組みを使わず自前のネイティブで通信するので、Android の平文の通信の制限（Network Security Config）にかからないと見込む（推測。計画のタスク 4 で確かめる）
- `adb reverse` は USB を抜く・端末を再起動すると消える。スモークの前に毎回実行する
- アカウントのファイルは `persistentDataPath` の `account-http___localhost_5000.dat`。最初からやり直すときは、アプリのデータを消す（`adb shell pm clear com.chinpangx.uta`）

## 5. 完了条件 7 の確かめ方

### 5.1 スモーク（起動 → ログイン → ガチャ 1 回）

手動。端末を手で操作する。

1. ローカルのサーバーを起動する（PostgreSQL・`migrate`・`dotnet run`）
2. `adb install -r <APK>`、`adb shell pm clear com.chinpangx.uta`、`adb reverse tcp:5000 tcp:5000`
3. `adb logcat -c` のあと、`adb logcat`（タグで絞らない。クラッシュの行は `Unity` のタグに出ない）をファイルに取り始める
4. 起動し、タイトル → スタート → ホーム → ガチャ → 単発を 1 回 → 結果 → ガチャの画面に戻る
5. アプリを終了する（`adb shell am force-stop com.chinpangx.uta`）
6. もう一度起動し、タイトル → スタート → ホームまで進む

通ったとみなすもの:

- 操作した人の答え: 4・6 でエラーのダイアログが出なかった。4 でガチャの結果（1 枚）が表示された
- logcat に、4 の間の `[Uta] master: ok`・`[Uta] sign-up: created`・`[Uta] sign-in: ok`、6 の間の `[Uta] sign-up: skipped`・`[Uta] sign-in: ok` がある（`TitlePresenter` が出す。6 の `skipped` で、保存したアカウントで入り直したことが分かる）
- ガチャ通貨が単発 1 回分減っている: 6 の `sign-in: ok` の行の `gachaCurrency=` の値が、4 の `sign-in: ok` の行の値から、マスタ `gacha.csv` の `GachaId` 1 の `SingleCost`（2026-11-16 時点で 250）を引いた値と同じ
- logcat の `Unity` のタグの行に `Exception` を含む行が無い。`AndroidRuntime` のタグの `FATAL EXCEPTION` の行の次の `Process: com.chinpangx.uta` の行が無い（ほかのアプリの行は見ない）

### 5.2 IL2CPP の計算の一致

比べ方の決定は [lesson-phases.md](lesson-phases.md) 8章と [produce-domain.md](produce-domain.md) 7章（項目ごと。ハッシュは作らない）。ここでは置き場と回し方を決める。

- **範囲** [決定。2026-11-16。ユーザーの判断]: レッスンの再生だけ。プロデュース（週の進行・`ProduceCatalog.Create`）は MVP では含めない。プロデュースのテストのマスタ（`TestProduceMaster`）は `masterdata/csv/` をディスクから読むので、実機に持ち込む仕組みが別に要るため
- **テストのカードの置き場** [決定。2026-11-16。ユーザーの「進めて」で推奨どおり]: テストのカード（`TestCards`・`Phase1Cards`〜`Phase4Cards`・`SyntheticCards`・`TestCardCatalog`）を、`Uta.Shared.Domain.Tests` から新しいプロジェクト `shared/Uta.Shared.Domain.Testing`（netstandard2.1・C# 9。Unity のパッケージ `com.uta.shared.domain.testing`）に移す。.NET のテストと Unity のプレイヤーのテストが、同じカードの定義・同じ再生の手順・同じ期待値を使うため（写すと、写しのずれと IL2CPP の不一致を見分けられない）
  - 名前空間は移す前の `Uta.Shared.Domain.Tests.Lesson` のまま（参照している 14 ファイルを変えないため。「ルート名前空間はアセンブリ名と同じ」の規則の例外）
  - Unity の asmdef `Uta.Shared.Domain.Testing` は `defineConstraints` に `UNITY_INCLUDE_TESTS` を持ち、テストを含むビルドでだけコンパイルされる（本番のプレイヤーに入れない）。`file:` のパッケージでこの制約を効かせるため、`client/uta/Packages/manifest.json` の `testables` に `com.uta.shared.domain.testing` を入れる
- **再生のシナリオ**: `Uta.Shared.Domain.Testing` の `LessonReplayScenario.RunNormal`（通常のレッスン）と `RunExam`（中間試験）。36 枚のデッキ（フェーズ 1〜4 のカードを 1 枚ずつ）で、使えるカードがあれば手札の先頭から使い、無ければスキップして、レッスンの終わりまで進める。値と手順は今の `LessonReplayTests` と同じ
- **期待値**: `Uta.Shared.Domain.Testing` の `LessonReplayExpected` に、.NET で再生した最後の状態の `LessonStateComparer.Describe` の行を定数で書く。.NET の単体テスト（`LessonReplayExpectedTests`）が、期待値とシナリオの結果が同じであることを毎回確かめる（シナリオやルールを変えて期待値が古くなったら、.NET のテストが落ちる）
- **プレイヤーのテスト**: `client/uta/Assets/Tests/SharedDomainReplay/`（asmdef `Uta.Client.Tests.SharedDomainReplay`。PlayMode のテスト。参照は `Uta.Shared.Domain`・`Uta.Shared.Domain.Testing`・Unity Test Framework）。シナリオを再生し、期待値と項目ごとに比べる
- **回し方**: Test Runner のプレイヤー実行（バッチモードの `-runTests -testPlatform <StandaloneWindows64|Android>`。Unity がテスト用のプレイヤーを作って起動し、結果を受け取る）。先に Windows（`build-foundation`）で回して仕組みを確かめ、次に Android（`build-android`）で回す。Editor の PlayMode では回さない（Mono なので IL2CPP の確認にならず、`BootstrapPlayMode` が Play を Bootstrap シーンから始めるので、サーバーへの接続が混ざる）。CI では回さない（[ADR 0008](../decisions/0008-shared-domain-and-onion.md)）

通ったとみなすもの（Windows・Android とも）:

- 結果の XML の `<test-run>` が `total="3" passed="3" failed="0"`。3 件のうち 1 件（`Player_IsIl2CppPlayer`）は、`ENABLE_IL2CPP` でない・Editor で走った・プラットフォームが Android か Windows のプレイヤーでない、のどれかなら落ちる。IL2CPP のプレイヤーで走ったことは、ログの文言ではなくこのテストで確かめる
- XML の `Player_IsIl2CppPlayer` の `<output>` の `platform=` が、Android では `Android`、Windows では `WindowsPlayer`

### 5.3 Android で通らないとき [決定。2026-10-31]

[game-spec.md](../overview/game-spec.md)「完了条件」の代え方のとおり、Windows の IL2CPP スタンドアロンで代え、Android は iOS と同じ別の項目に回す。代えるときの手順:

- 計算の一致: 5.2 の Windows の結果で代える
- スモーク: `BuildWindowsIl2Cpp` で作った `Uta.exe` を引数なしで起動し、5.1 の 4〜6 を手で行う（5 はウィンドウを閉じる）。ログは `Player.log`

代えるかどうかは、計画のスパイクの「止まる条件」に当たったときに、ユーザーが決める。2026-11-17 に、Android の端末が手元に無く、実機はクラウド化の後に回すと決めた（ユーザーの判断）。計算の一致は 5.2 の Windows の結果で代えた。スモークは Windows では代えない（Windows のクライアントの通しは完了条件 6 が受け持つ）

## 6. 未決事項

- iOS の実機ビルド（[ADR 0006](../decisions/0006-adopt-unity-7-early.md)。Unity 7 の対応状況しだいの別の項目）
- CI（[repository-layout.md](repository-layout.md) 7章）
- dev 環境（Cloud Run）に実機からつなぐときの接続先の渡し方（4章）
- プロデュースの再生の IL2CPP の一致（5.2。MVP の後）
