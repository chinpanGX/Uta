using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityScreenNavigator;
using Uta.Client.Application.Idol;
using ISceneNavigator = Uta.Client.Application.Navigation.ISceneNavigator;
using Uta.Client.Application.Owned;
using Uta.Client.Application.Produce;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Owned;
using VContainer;

namespace Uta.Client.Presentation.Formation
{
    [AssetAddress(nameof(FormationPage))]
    public sealed class FormationPresenter : IPresenter
    {
        public const int SupportSlots = 5;

        private readonly IFormationView formationView;
        private readonly FormationDtoFactory formationDtoFactory;
        private readonly FormationNormalizer formationNormalizer;
        private readonly FeaturedIdolPicker featuredIdolPicker;
        private readonly ScenarioResolver scenarioResolver;
        private readonly IProduceUseCase produceUseCase;
        private readonly DialogOpener dialogOpener;
        private readonly IScreenNavigator screenNavigator;
        private readonly ISceneNavigator sceneNavigator;
        private string[] slots = new string[SupportSlots];   // サポートカードの所持ID。空は null
        private int idolId;                                   // 0 = 未選択
        private int scenarioIndex;
        private bool running;
        private bool disposed;

        [Inject]
        public FormationPresenter(FormationPage formationPage, FormationDtoFactory formationDtoFactory, FormationNormalizer formationNormalizer, FeaturedIdolPicker featuredIdolPicker,
            ScenarioResolver scenarioResolver, IProduceUseCase produceUseCase, DialogOpener dialogOpener, IScreenNavigator screenNavigator, ISceneNavigator sceneNavigator)
            : this((IFormationView)formationPage, formationDtoFactory, formationNormalizer, featuredIdolPicker, scenarioResolver, produceUseCase, dialogOpener, screenNavigator, sceneNavigator)
        {
        }

        public FormationPresenter(IFormationView formationView, FormationDtoFactory formationDtoFactory, FormationNormalizer formationNormalizer, FeaturedIdolPicker featuredIdolPicker,
            ScenarioResolver scenarioResolver, IProduceUseCase produceUseCase, DialogOpener dialogOpener, IScreenNavigator screenNavigator, ISceneNavigator sceneNavigator)
        {
            this.formationView = formationView;
            this.formationDtoFactory = formationDtoFactory;
            this.formationNormalizer = formationNormalizer;
            this.featuredIdolPicker = featuredIdolPicker;
            this.scenarioResolver = scenarioResolver;
            this.produceUseCase = produceUseCase;
            this.dialogOpener = dialogOpener;
            this.screenNavigator = screenNavigator;
            this.sceneNavigator = sceneNavigator;
        }

        public UniTask InitializeAsync()
        {
            formationView.IdolClicked += OnIdolClicked;
            formationView.SlotClicked += OnSlotClicked;
            formationView.SlotClearClicked += OnSlotClearClicked;
            formationView.ScenarioClicked += OnScenarioClicked;
            formationView.StartClicked += OnStartClicked;
            formationView.BackClicked += OnBackClicked;
            idolId = featuredIdolPicker.Pick();
            scenarioIndex = 0;
            Refresh();
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            disposed = true;
            formationView.IdolClicked -= OnIdolClicked;
            formationView.SlotClicked -= OnSlotClicked;
            formationView.SlotClearClicked -= OnSlotClearClicked;
            formationView.ScenarioClicked -= OnScenarioClicked;
            formationView.StartClicked -= OnStartClicked;
            formationView.BackClicked -= OnBackClicked;
        }

        public UniTask SelectIdolAsync()
        {
            return RunAsync(async () =>
            {
                var presenter = await screenNavigator.PushPageAsync<OwnedPresenter, OwnedArgs>(new OwnedArgs(OwnedSelectionCondition.ForIdol()), playAnimation: false);
                var selection = await screenNavigator.WaitForPopAsync<OwnedSelection>(presenter);
                if (disposed || selection == null || selection.IsCancelled || selection.IdolId == idolId)
                {
                    return;
                }

                idolId = selection.IdolId;
                slots = formationNormalizer.RemoveIncompatible(idolId, slots, out var removed);
                Refresh();
                if (removed > 0)
                {
                    await dialogOpener.ShowInfoAsync("プランが合わないサポートカードを外しました。");
                }
            });
        }

        public UniTask SelectSlotAsync(int index)
        {
            return RunAsync(async () =>
            {
                if (index < 0 || index >= SupportSlots)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (idolId == 0)
                {
                    await dialogOpener.ShowInfoAsync("先に育成アイドルを選んでください。");
                    return;
                }

                var excludedUids = new List<string>();
                for (var i = 0; i < slots.Length; i++)
                {
                    if (i != index && slots[i] != null)
                    {
                        excludedUids.Add(slots[i]);
                    }
                }

                var presenter = await screenNavigator.PushPageAsync<OwnedPresenter, OwnedArgs>(new OwnedArgs(OwnedSelectionCondition.ForSupportCard(idolId, excludedUids)), playAnimation: false);
                var selection = await screenNavigator.WaitForPopAsync<OwnedSelection>(presenter);
                if (disposed || selection == null || selection.IsCancelled)
                {
                    return;
                }

                slots[index] = selection.SupportCardUid;
                Refresh();
            });
        }

        public void ClearSlot(int index)
        {
            if (running || disposed || index < 0 || index >= SupportSlots)
            {
                return;
            }

            slots[index] = null;
            Refresh();
        }

        public void NextScenario()
        {
            if (running || disposed || scenarioResolver.Count < 2)
            {
                return;
            }

            scenarioIndex = (scenarioIndex + 1) % scenarioResolver.Count;
            Refresh();
        }

        public UniTask StartAsync()
        {
            if (idolId == 0 || scenarioResolver.Count == 0)
            {
                return UniTask.CompletedTask;
            }

            return RunAsync(async () =>
            {
                var uids = new List<string>();
                foreach (var uid in slots)
                {
                    if (uid != null)
                    {
                        uids.Add(uid);
                    }
                }

                var kind = await produceUseCase.StartAsync(idolId, uids, scenarioResolver.IdAt(scenarioIndex));
                if (disposed)
                {
                    return;
                }

                switch (kind)
                {
                    case ProduceStartResultKind.Succeeded:
                        await sceneNavigator.GoToProduceAsync();   // Home シーンごと編成のページも破棄される。以後は View に触らない
                        break;
                    case ProduceStartResultKind.InvalidFormation:
                        await dialogOpener.ShowInfoAsync("編成が正しくありません。");
                        break;
                    case ProduceStartResultKind.AlreadyInProgress:
                        await dialogOpener.ShowInfoAsync("進行中のプロデュースがあります。ホームから再開してください。");
                        await screenNavigator.PopPageAsync(this, playAnimation: false);
                        break;
                    case ProduceStartResultKind.InvalidArgument:
                        await dialogOpener.ShowInfoAsync("開始できませんでした。");
                        break;
                    case ProduceStartResultKind.Failed:
                        break;
                }
            });
        }

        public UniTask BackAsync()
        {
            return RunAsync(() => screenNavigator.PopPageAsync(this, playAnimation: false));
        }

        /// <summary>実行中は無視する。想定外の例外は記録してお知らせを出す（ボタンの処理の共通の形）。</summary>
        private async UniTask RunAsync(Func<UniTask> action)
        {
            if (running)
            {
                return;
            }

            running = true;
            try
            {
                await action();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                await dialogOpener.ShowInfoAsync("エラーが発生しました: " + e.Message);
            }
            finally
            {
                running = false;
            }
        }

        private void Refresh()
        {
            formationView.Set(formationDtoFactory.Create(idolId, slots, scenarioIndex));
        }

        private void OnIdolClicked()
        {
            SelectIdolAsync().Forget();
        }

        private void OnSlotClicked(int index)
        {
            SelectSlotAsync(index).Forget();
        }

        private void OnSlotClearClicked(int index)
        {
            ClearSlot(index);
        }

        private void OnScenarioClicked()
        {
            NextScenario();
        }

        private void OnStartClicked()
        {
            StartAsync().Forget();
        }

        private void OnBackClicked()
        {
            BackAsync().Forget();
        }
    }
}
