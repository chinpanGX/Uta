using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;
using Object = UnityEngine.Object;

namespace Uta.Client.Editor.Prefabs
{
    /// <summary>Prefab の生成の共通の補助。作業は一時的なプレビュー用のシーンで行い、開いているシーンを汚さない。</summary>
    public static class PrefabBuildUtil
    {
        public const string PartsFolder = "Assets/Prefabs/Parts";
        public const string ScreensFolder = "Assets/Prefabs/Screens";

        public static string PartPath(string partName)
        {
            return PartsFolder + "/" + partName + ".prefab";
        }

        public static string ScreenPath(string feature, string name)
        {
            return ScreensFolder + "/" + feature + "/" + name + ".prefab";
        }

        // --- 作業用のシーン ---

        // --- 作成・保存 ---

        public static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }
            var index = folderPath.LastIndexOf('/');
            if (index <= 0)
            {
                throw new ArgumentException("invalid folder path: " + folderPath);
            }
            var parent = folderPath.Substring(0, index);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderPath.Substring(index + 1));
        }

        public static GameObject SavePrefab(GameObject root, string assetPath)
        {
            var index = assetPath.LastIndexOf('/');
            EnsureFolder(assetPath.Substring(0, index));
            PrefabUtility.SaveAsPrefabAsset(root, assetPath, out var success);
            Object.DestroyImmediate(root);
            if (!success)
            {
                throw new InvalidOperationException("prefab save failed: " + assetPath);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        // --- 子 ---

        public static RectTransform AddChild(Transform parent, string name)
        {
            return AddChild(parent, name, RectSpec.Stretch());
        }

        public static RectTransform AddChild(Transform parent, string name, RectSpec spec)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            var rect = (RectTransform)child.transform;
            SetRect(rect, spec);
            return rect;
        }

        public static GameObject InstantiatePart(Transform parent, string partName, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PartPath(partName));
            if (prefab == null)
            {
                throw new InvalidOperationException("part not found: " + partName);
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            return instance;
        }

        public static T InstantiatePart<T>(Transform parent, string partName, string name) where T : Component
        {
            var instance = InstantiatePart(parent, partName, name);
            var component = instance.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException("component " + typeof(T).Name + " not found on part: " + partName);
            }
            return component;
        }

        private static Transform FindChild(Transform parent, string childName, string partName)
        {
            var child = parent.Find(childName);
            if (child == null)
            {
                throw new InvalidOperationException("child " + childName + " not found on part: " + partName);
            }
            return child;
        }

        // --- RectTransform ---

        public static void SetRect(RectTransform rect, RectSpec spec)
        {
            rect.anchorMin = spec.AnchorMin;
            rect.anchorMax = spec.AnchorMax;
            rect.pivot = spec.Pivot;
            rect.offsetMin = spec.OffsetMin;
            rect.offsetMax = spec.OffsetMax;
            MarkModified(rect);
        }

        // --- 見た目 ---

        public static Image AddImage(GameObject target, Color color, bool raycastTarget)
        {
            var image = target.GetComponent<Image>();
            if (image == null)
            {
                image = target.AddComponent<Image>();
            }
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = raycastTarget;
            MarkModified(image);
            return image;
        }

        public static Button AddButton(GameObject target, Graphic targetGraphic)
        {
            var button = target.GetComponent<Button>();
            if (button == null)
            {
                button = target.AddComponent<Button>();
            }
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = targetGraphic;
            var colors = button.colors;
            colors.normalColor = UiTheme.TintNormal;
            colors.highlightedColor = UiTheme.TintHighlighted;
            colors.pressedColor = UiTheme.TintPressed;
            colors.selectedColor = UiTheme.TintNormal;
            colors.disabledColor = UiTheme.TintDisabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            MarkModified(button);
            return button;
        }

        public static TextMeshProUGUI AddText(GameObject target, string text, TextSpec spec)
        {
            var tmp = target.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = target.AddComponent<TextMeshProUGUI>();
            }
            tmp.text = text;
            ApplyText(tmp, spec);
            return tmp;
        }

        public static void ApplyText(TMP_Text text, TextSpec spec)
        {
            text.font = DefaultFont;
            text.fontSize = spec.FontSize;
            text.color = spec.Color;
            text.alignment = spec.Alignment;
            text.textWrappingMode = spec.Wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = spec.Ellipsis ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
            text.richText = false;
            text.raycastTarget = false;
            text.enableAutoSizing = false;
            text.fontStyle = FontStyles.Normal;
            MarkModified(text);
        }

        public static TMP_FontAsset DefaultFont
        {
            get { return FontAssets.LoadRegular(); }
        }

        public static TMP_FontAsset BoldFont
        {
            get { return FontAssets.LoadBold(); }
        }

        // --- レイアウト ---

        public static VerticalLayoutGroup AddVertical(GameObject target, LayoutSpec spec)
        {
            var group = target.GetComponent<VerticalLayoutGroup>();
            if (group == null)
            {
                group = target.AddComponent<VerticalLayoutGroup>();
            }
            ApplyLayout(group, spec);
            return group;
        }

        public static HorizontalLayoutGroup AddHorizontal(GameObject target, LayoutSpec spec)
        {
            var group = target.GetComponent<HorizontalLayoutGroup>();
            if (group == null)
            {
                group = target.AddComponent<HorizontalLayoutGroup>();
            }
            ApplyLayout(group, spec);
            return group;
        }

        private static void ApplyLayout(HorizontalOrVerticalLayoutGroup group, LayoutSpec spec)
        {
            group.spacing = spec.Spacing;
            group.padding = new RectOffset(spec.Left, spec.Right, spec.Top, spec.Bottom);
            group.childAlignment = spec.Alignment;
            group.childControlWidth = spec.ControlWidth;
            group.childControlHeight = spec.ControlHeight;
            group.childForceExpandWidth = spec.ExpandWidth;
            group.childForceExpandHeight = spec.ExpandHeight;
            group.childScaleWidth = false;
            group.childScaleHeight = false;
            MarkModified(group);
        }

        public static LayoutElement SetLayout(GameObject target, float minHeight = -1f, float preferredHeight = -1f,
            float preferredWidth = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            var element = target.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = target.AddComponent<LayoutElement>();
            }
            if (minHeight != -1f)
            {
                element.minHeight = minHeight;
            }
            if (preferredHeight != -1f)
            {
                element.preferredHeight = preferredHeight;
            }
            if (preferredWidth != -1f)
            {
                element.preferredWidth = preferredWidth;
            }
            if (flexibleWidth != -1f)
            {
                element.flexibleWidth = flexibleWidth;
            }
            if (flexibleHeight != -1f)
            {
                element.flexibleHeight = flexibleHeight;
            }
            MarkModified(element);
            return element;
        }

        public static ContentSizeFitter AddVerticalFitter(GameObject target)
        {
            var fitter = target.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = target.AddComponent<ContentSizeFitter>();
            }
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            MarkModified(fitter);
            return fitter;
        }

        // --- 部品を置く（部品の Prefab をネストして値をオーバーライドする） ---

        public static TextMeshProUGUI AddLabel(Transform parent, string name, string text, TextSpec spec)
        {
            var tmp = InstantiatePart<TextMeshProUGUI>(parent, "Label", name);
            ApplyText(tmp, spec);
            tmp.text = text;
            MarkModified(tmp);
            return tmp;
        }

        public static ButtonView AddButtonPart(Transform parent, string name, string label, ButtonStyle style = ButtonStyle.Primary)
        {
            var view = InstantiatePart<ButtonView>(parent, "Button", name);
            var labelText = FindChild(view.transform, "Label", "Button").GetComponent<TMP_Text>();
            labelText.text = label;
            MarkModified(labelText);
            var image = view.GetComponent<Image>();
            image.color = StyleColor(style);
            MarkModified(image);
            return view;
        }

        private static Color StyleColor(ButtonStyle style)
        {
            switch (style)
            {
                case ButtonStyle.Secondary:
                    return UiTheme.ButtonSecondary;
                case ButtonStyle.Danger:
                    return UiTheme.ButtonDanger;
                default:
                    return UiTheme.ButtonPrimary;
            }
        }

        public static RectTransform AddPanel(Transform parent, string name)
        {
            return (RectTransform)InstantiatePart(parent, "Panel", name).transform;
        }

        public static HeaderView AddHeader(Transform parent, string title)
        {
            var view = InstantiatePart<HeaderView>(parent, "Header", "Header");
            var titleText = FindChild(view.transform, "TitleText", "Header").GetComponent<TMP_Text>();
            titleText.text = title;
            MarkModified(titleText);
            return view;
        }

        public static TabBarView AddTabBar(Transform parent, string name, string[] tabNames, string[] labels)
        {
            if (tabNames == null || labels == null || tabNames.Length == 0 || tabNames.Length != labels.Length)
            {
                throw new ArgumentException("tabNames and labels must be non-empty and have the same length");
            }
            var bar = InstantiatePart<TabBarView>(parent, "TabBar", name);
            var tabs = new TabView[tabNames.Length];
            for (var i = 0; i < tabNames.Length; i++)
            {
                var tab = InstantiatePart<TabView>(bar.transform, "Tab", tabNames[i]);
                var labelText = FindChild(tab.transform, "Label", "Tab").GetComponent<TMP_Text>();
                labelText.text = labels[i];
                MarkModified(labelText);
                tabs[i] = tab;
            }
            SetRefs(bar, "tabs", tabs);
            return bar;
        }

        public static RectTransform AddScrollList(Transform parent, string name, out ScrollRect scrollRect)
        {
            var root = AddChild(parent, name);
            scrollRect = root.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
            SetLayout(root.gameObject, flexibleHeight: 1f);

            var viewport = AddChild(root, "Viewport");
            AddImage(viewport.gameObject, UiTheme.Transparent, true);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = AddChild(viewport, "Content",
                new RectSpec(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero));
            AddVertical(content.gameObject, new LayoutSpec(24f, 0, 0, 0, 0, TextAnchor.UpperCenter, true, true, true, false));
            AddVerticalFitter(content.gameObject);

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        // --- 画面の雛形 ---

        public static TPage CreatePage<TPage>(PrefabBuildSession session, string name, out RectTransform body) where TPage : PageBase
        {
            var root = session.CreateRoot(name);
            AddImage(root, UiTheme.Background, true);
            var page = root.AddComponent<TPage>();
            body = AddChild(root.transform, "Body");
            body.gameObject.AddComponent<SafeAreaFitter>();
            SetRef(page, "body", body);
            return page;
        }

        public static TModal CreateModal<TModal>(PrefabBuildSession session, string name, out RectTransform panel) where TModal : ModalBase
        {
            var root = session.CreateRoot(name);
            var modal = root.AddComponent<TModal>();
            panel = AddChild(root.transform, "Panel", RectSpec.Center(UiTheme.ModalWidth, 0f));
            AddImage(panel.gameObject, UiTheme.Panel, true);
            AddVertical(panel.gameObject, new LayoutSpec(24f, 40, 40, 40, 40, TextAnchor.UpperCenter, true, true, true, false));
            AddVerticalFitter(panel.gameObject);
            SetRef(modal, "panel", panel);
            return modal;
        }

        // --- 参照の代入 ---

        public static void SetRef(Object target, string fieldName, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new InvalidOperationException(target.GetType().Name + "." + fieldName + " not found");
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetRefs(Object target, string fieldName, Object[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new InvalidOperationException(target.GetType().Name + "." + fieldName + " not found");
            }
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- Addressables ---

        public static void RegisterScreen(string prefabPath)
        {
            AddressablesSetup.RegisterScreen(prefabPath);
        }

        /// <summary>component がプレハブのインスタンスの一部なら、変更を記録する。値を変えたあとに呼ぶ。</summary>
        public static void MarkModified(Component component)
        {
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }
    }
}
