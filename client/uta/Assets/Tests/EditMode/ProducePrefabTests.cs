using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Uta.Client.Presentation.Formation;
using Uta.Client.Presentation.Produce;

namespace Uta.Client.Infrastructure.Tests
{
    /// <summary>
    /// 編成・プロデュースの画面と行の Prefab の確かめ（P3 の計画 6章）。
    /// パスは生成クラスの PrefabBuildUtil.ScreenPath・PartPath を使わず、期待値をここで固定する（生成クラスのパスの書き間違いを見つけるため）。
    /// </summary>
    public sealed class ProducePrefabTests
    {
        private const string FormationPagePath = "Assets/Prefabs/Screens/Formation/FormationPage.prefab";
        private const string ProduceBackdropPagePath = "Assets/Prefabs/Screens/Produce/ProduceBackdropPage.prefab";
        private const string WeekPagePath = "Assets/Prefabs/Screens/Produce/WeekPage.prefab";
        private const string ClassPagePath = "Assets/Prefabs/Screens/Produce/ClassPage.prefab";
        private const string SupplyPagePath = "Assets/Prefabs/Screens/Produce/SupplyPage.prefab";
        private const string CardOfferModalPath = "Assets/Prefabs/Screens/Produce/CardOfferModal.prefab";
        private const string LessonPagePath = "Assets/Prefabs/Screens/Produce/LessonPage.prefab";
        private const string DeckListModalPath = "Assets/Prefabs/Screens/Produce/DeckListModal.prefab";
        private const string ExamResultPagePath = "Assets/Prefabs/Screens/Produce/ExamResultPage.prefab";
        private const string ProduceResultPagePath = "Assets/Prefabs/Screens/Produce/ProduceResultPage.prefab";

        private const string ButtonPartPath = "Assets/Prefabs/Parts/Button.prefab";
        private const string FormationSlotRowPath = "Assets/Prefabs/Parts/FormationSlotRow.prefab";
        private const string SkillCardRowPath = "Assets/Prefabs/Parts/SkillCardRow.prefab";
        private const string DeckSectionHeaderRowPath = "Assets/Prefabs/Parts/DeckSectionHeaderRow.prefab";
        private const string DeckCardRowPath = "Assets/Prefabs/Parts/DeckCardRow.prefab";
        private const string ExamRankingRowPath = "Assets/Prefabs/Parts/ExamRankingRow.prefab";

        private static readonly (string Path, Type View)[] Screens =
        {
            (FormationPagePath, typeof(FormationPage)),
            (ProduceBackdropPagePath, typeof(ProduceBackdropPage)),
            (WeekPagePath, typeof(WeekPage)),
            (ClassPagePath, typeof(ClassPage)),
            (SupplyPagePath, typeof(SupplyPage)),
            (CardOfferModalPath, typeof(CardOfferModal)),
            (LessonPagePath, typeof(LessonPage)),
            (DeckListModalPath, typeof(DeckListModal)),
            (ExamResultPagePath, typeof(ExamResultPage)),
            (ProduceResultPagePath, typeof(ProduceResultPage)),
        };

        private static readonly (string Path, Type View)[] Rows =
        {
            (FormationSlotRowPath, typeof(FormationSlotRowView)),
            (SkillCardRowPath, typeof(SkillCardRowView)),
            (DeckSectionHeaderRowPath, typeof(DeckSectionHeaderRowView)),
            (DeckCardRowPath, typeof(DeckCardRowView)),
            (ExamRankingRowPath, typeof(ExamRankingRowView)),
        };

        // 4章の表から取った。4章の表を変えたときは、この一覧も同じ変更で直す
        private static readonly (string Prefab, string[] Children)[] NamedChildren =
        {
            ("Assets/Prefabs/Screens/Formation/FormationPage.prefab", new[]
            {
                "Body/Header",
                "Body/Header/BackButton",
                "Body/Main/IdolPanel/IdolText",
                "Body/Main/IdolPanel/SelectIdolButton",
                "Body/Main/SlotList/Slot0",
                "Body/Main/SlotList/Slot0/LabelText",
                "Body/Main/SlotList/Slot0/SelectButton",
                "Body/Main/SlotList/Slot0/ClearButton",
                "Body/Main/SlotList/Slot1",
                "Body/Main/SlotList/Slot1/LabelText",
                "Body/Main/SlotList/Slot1/SelectButton",
                "Body/Main/SlotList/Slot1/ClearButton",
                "Body/Main/SlotList/Slot2",
                "Body/Main/SlotList/Slot2/LabelText",
                "Body/Main/SlotList/Slot2/SelectButton",
                "Body/Main/SlotList/Slot2/ClearButton",
                "Body/Main/SlotList/Slot3",
                "Body/Main/SlotList/Slot3/LabelText",
                "Body/Main/SlotList/Slot3/SelectButton",
                "Body/Main/SlotList/Slot3/ClearButton",
                "Body/Main/SlotList/Slot4",
                "Body/Main/SlotList/Slot4/LabelText",
                "Body/Main/SlotList/Slot4/SelectButton",
                "Body/Main/SlotList/Slot4/ClearButton",
                "Body/Main/ScenarioRow/ScenarioText",
                "Body/Main/ScenarioRow/ChangeScenarioButton",
                "Body/Main/StartButton",
            }),
            ("Assets/Prefabs/Screens/Produce/ProduceBackdropPage.prefab", new[]
            {
                "Body/Main/HeaderText",
            }),
            ("Assets/Prefabs/Screens/Produce/WeekPage.prefab", new[]
            {
                "Body/Header",
                "Body/Header/TitleText",
                "Body/Header/RightSlot/MenuButton",
                "Body/Main/NextExamText",
                "Body/Main/StatusPanel/StatusText",
                "Body/Main/ActionList",
            }),
            ("Assets/Prefabs/Screens/Produce/ClassPage.prefab", new[]
            {
                "Body/Header",
                "Body/Main/OptionList",
            }),
            ("Assets/Prefabs/Screens/Produce/SupplyPage.prefab", new[]
            {
                "Body/Header",
                "Body/Main/ReceiveButton",
            }),
            ("Assets/Prefabs/Screens/Produce/CardOfferModal.prefab", new[]
            {
                "Panel/CardList",
                "Panel/DeckCountText",
                "Panel/DeclineButton",
            }),
            ("Assets/Prefabs/Screens/Produce/LessonPage.prefab", new[]
            {
                "Body/Header",
                "Body/Header/TitleText",
                "Body/Header/RightSlot/MenuButton",
                "Body/Main/TurnText",
                "Body/Main/ProgressText",
                "Body/Main/StatusText",
                "Body/Main/LastResultText",
                "Body/Main/PilesButton",
                "Body/Main/HandScroll/Viewport/Content",
                "Body/Main/SkipButton",
            }),
            ("Assets/Prefabs/Screens/Produce/DeckListModal.prefab", new[]
            {
                "Panel/ListScroll/Viewport/Content",
                "Panel/CloseButton",
            }),
            ("Assets/Prefabs/Screens/Produce/ExamResultPage.prefab", new[]
            {
                "Body/Main/TitleText",
                "Body/Main/RankingScroll/Viewport/Content",
                "Body/Main/OutcomeText",
                "Body/Main/RetryButton",
                "Body/Main/NextButton",
            }),
            ("Assets/Prefabs/Screens/Produce/ProduceResultPage.prefab", new[]
            {
                "Body/Main/RankText",
                "Body/Main/LinesText",
                "Body/Main/HomeButton",
            }),
            ("Assets/Prefabs/Parts/SkillCardRow.prefab", new[]
            {
                "TitleText",
                "CostsText",
                "DescriptionText",
            }),
            ("Assets/Prefabs/Parts/ExamRankingRow.prefab", new[]
            {
                "Text",
            }),
        };

        [Test]
        public void ProduceScreens_ExistWithRootView()
        {
            foreach (var screen in Screens)
            {
                AssertRootView(screen.Path, screen.View);
            }
        }

        [Test]
        public void ProduceRows_ExistWithRowView()
        {
            foreach (var row in Rows)
            {
                AssertRootView(row.Path, row.View);
            }
        }

        [Test]
        public void ProduceScreens_HaveNamedChildren()
        {
            var missing = new List<string>();
            foreach (var entry in NamedChildren)
            {
                var prefab = ScreenPrefabAssert.Load(entry.Prefab);
                foreach (var child in entry.Children)
                {
                    if (prefab.transform.Find(child) == null)
                    {
                        missing.Add(entry.Prefab + ": " + child);
                    }
                }
            }
            Assert.That(missing, Is.Empty);
        }

        [Test]
        public void HeaderBackButton_ActiveOnlyWhereBackExists()
        {
            AssertBackButtonActive(WeekPagePath, false);
            AssertBackButtonActive(LessonPagePath, false);
            AssertBackButtonActive(FormationPagePath, true);
            AssertBackButtonActive(ClassPagePath, true);
            AssertBackButtonActive(SupplyPagePath, true);
        }

        [Test]
        public void LessonPage_TurnTextIsFirstInMain()
        {
            var prefab = ScreenPrefabAssert.Load(LessonPagePath);
            Assert.AreEqual(new[] { "BackButton", "TitleText", "RightSlot" }, ChildNames(prefab, "Body/Header"));
            Assert.AreEqual(
                new[] { "TurnText", "ProgressText", "StatusText", "LastResultText", "PilesButton", "HandScroll", "SkipButton" },
                ChildNames(prefab, "Body/Main"));
        }

        [Test]
        public void FormationPage_HasFiveSlotsInOrder()
        {
            var view = ScreenPrefabAssert.RootView<FormationPage>(ScreenPrefabAssert.Load(FormationPagePath));
            var slots = new SerializedObject(view).FindProperty("slots");
            Assert.IsNotNull(slots, "FormationPage.slots not found");
            Assert.AreEqual(5, slots.arraySize);
            for (var i = 0; i < slots.arraySize; i++)
            {
                var slot = slots.GetArrayElementAtIndex(i).objectReferenceValue as Component;
                Assert.IsNotNull(slot, "slots[" + i + "] is null");
                Assert.AreEqual("Slot" + i, slot.gameObject.name, "slots[" + i + "]");
            }
        }

        [Test]
        public void RowPrefabFields_ReferencePartAssets()
        {
            ScreenPrefabAssert.FieldIsAsset(LoadView<WeekPage>(WeekPagePath), "actionButtonPrefab", ButtonPartPath);
            ScreenPrefabAssert.FieldIsAsset(LoadView<ClassPage>(ClassPagePath), "optionButtonPrefab", ButtonPartPath);
            ScreenPrefabAssert.FieldIsAsset(LoadView<CardOfferModal>(CardOfferModalPath), "cardRowPrefab", SkillCardRowPath);
            ScreenPrefabAssert.FieldIsAsset(LoadView<LessonPage>(LessonPagePath), "handRowPrefab", SkillCardRowPath);
            var deckList = LoadView<DeckListModal>(DeckListModalPath);
            ScreenPrefabAssert.FieldIsAsset(deckList, "sectionHeaderPrefab", DeckSectionHeaderRowPath);
            ScreenPrefabAssert.FieldIsAsset(deckList, "cardRowPrefab", DeckCardRowPath);
            ScreenPrefabAssert.FieldIsAsset(LoadView<ExamResultPage>(ExamResultPagePath), "rankingRowPrefab", ExamRankingRowPath);
        }

        [Test]
        public void RuntimeLists_AreEmpty()
        {
            AssertNoChildren(WeekPagePath, "Body/Main/ActionList");
            AssertNoChildren(ClassPagePath, "Body/Main/OptionList");
            AssertNoChildren(CardOfferModalPath, "Panel/CardList");
            AssertNoChildren(LessonPagePath, "Body/Main/HandScroll/Viewport/Content");
            AssertNoChildren(DeckListModalPath, "Panel/ListScroll/Viewport/Content");
            AssertNoChildren(ExamResultPagePath, "Body/Main/RankingScroll/Viewport/Content");
        }

        private static void AssertRootView(string path, Type viewType)
        {
            var prefab = ScreenPrefabAssert.Load(path);
            Assert.IsNotNull(prefab.GetComponent(viewType), path + ": root has no " + viewType.Name);
        }

        private static T LoadView<T>(string path) where T : Component
        {
            return ScreenPrefabAssert.RootView<T>(ScreenPrefabAssert.Load(path));
        }

        private static Transform FindChild(GameObject prefab, string path, string childPath)
        {
            var child = prefab.transform.Find(childPath);
            if (child == null)
            {
                Assert.Fail(path + ": " + childPath + " not found");
            }
            return child;
        }

        private static void AssertBackButtonActive(string path, bool expected)
        {
            var backButton = FindChild(ScreenPrefabAssert.Load(path), path, "Body/Header/BackButton");
            Assert.AreEqual(expected, backButton.gameObject.activeSelf, path + ": Body/Header/BackButton.activeSelf");
        }

        private static string[] ChildNames(GameObject prefab, string childPath)
        {
            var parent = FindChild(prefab, AssetDatabase.GetAssetPath(prefab), childPath);
            var names = new string[parent.childCount];
            for (var i = 0; i < parent.childCount; i++)
            {
                names[i] = parent.GetChild(i).name;
            }
            return names;
        }

        private static void AssertNoChildren(string path, string childPath)
        {
            var parent = FindChild(ScreenPrefabAssert.Load(path), path, childPath);
            Assert.AreEqual(0, parent.childCount, path + ": " + childPath + " has children");
        }
    }
}
