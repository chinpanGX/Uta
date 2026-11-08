using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityScreenNavigator;
using UnityScreenNavigator.Runtime.Core.Modal;
using UnityScreenNavigator.Runtime.Core.Page;
using Uta.Client.Presentation.Common;
using Uta.Client.Presentation.Common.Parts;

namespace Uta.Client.Editor.Prefabs.Parts
{
    /// <summary>UiRoot（Canvas・ページ/モーダル/オーバーレイの層・ブロッカー・EventSystem）を作る。</summary>
    public sealed class UiRootBuilder : IPrefabBuilder
    {
        public int Order
        {
            get { return 190; }
        }

        public string AssetPath
        {
            get { return PrefabBuildUtil.PartPath("UiRoot"); }
        }

        public void Build()
        {
            var root = PrefabBuildUtil.CreateRoot("UiRoot");

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            canvas.pixelPerfect = false;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UiTheme.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            scaler.referencePixelsPerUnit = 100f;

            root.AddComponent<GraphicRaycaster>();
            root.AddComponent<WindowAspectKeeper>();
            var uiRoot = root.AddComponent<UiRoot>();

            // 上から順に後ろ → 手前
            var background = PrefabBuildUtil.AddChild(root.transform, "Background");
            PrefabBuildUtil.AddImage(background.gameObject, UiTheme.Background, false);

            var pages = PrefabBuildUtil.AddChild(root.transform, "Pages");
            var pageContainer = pages.gameObject.AddComponent<PageContainer>();

            var modals = PrefabBuildUtil.AddChild(root.transform, "Modals");
            var modalContainer = modals.gameObject.AddComponent<ModalContainer>();

            var busy = PrefabBuildUtil.InstantiatePart<BusyIndicator>(root.transform, "BusyBlocker", "Busy");

            // Busy より手前なので、通信中でもダイアログ（DialogPresenter）は押せる
            var overlays = PrefabBuildUtil.AddChild(root.transform, "Overlays");
            overlays.gameObject.AddComponent<ModalContainer>();
            var overlayContainer = overlays.gameObject.AddComponent<OverlayContainer>();

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(root.transform, false);
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            PrefabBuildUtil.SetRef(uiRoot, "canvas", canvas);
            PrefabBuildUtil.SetRef(uiRoot, "pageContainer", pageContainer);
            PrefabBuildUtil.SetRef(uiRoot, "modalContainer", modalContainer);
            PrefabBuildUtil.SetRef(uiRoot, "overlayContainer", overlayContainer);
            PrefabBuildUtil.SetRef(uiRoot, "busyIndicator", busy);
            PrefabBuildUtil.SavePrefab(root, AssetPath);
        }
    }
}
