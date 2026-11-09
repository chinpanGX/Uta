using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using Uta.Client.Presentation.Common.Parts;
using VContainer;

namespace Uta.Client.Presentation.Common
{
    /// <summary>ダイアログ（Prefab Dialog）の Presenter。USN の Overlay に出る。押されたボタンの添字を CompleteAsync で返す。</summary>
    [AssetAddress("Dialog")]
    public sealed class DialogPresenter : IPresenter, IScreenWithArgs<DialogArgs>
    {
        private readonly DialogView dialogView;
        private readonly DialogArgs dialogArgs;
        private readonly IScreenNavigator screenNavigator;
        private int selectedIndex = -1;   // -1: 押されずに閉じた（シーンを閉じて ScreenNavigator が Dispose された）
        private bool clicked;

        [Inject]
        public DialogPresenter(DialogView dialogView, DialogArgs dialogArgs, IScreenNavigator screenNavigator)
        {
            this.dialogView = dialogView;
            this.dialogArgs = dialogArgs;
            this.screenNavigator = screenNavigator;
        }

        public UniTask InitializeAsync()
        {
            dialogView.SetMessage(dialogArgs.Message);
            for (var i = 0; i < dialogArgs.Labels.Count; i++)
            {
                var index = i;
                var button = dialogView.AddButton("Button" + i, dialogArgs.Labels[i], dialogArgs.Colors[i]);
                button.Button.onClick.AddListener(() => OnClicked(index));
            }

            return UniTask.CompletedTask;
        }

        public UniTask<object> CompleteAsync()
        {
            return UniTask.FromResult<object>(selectedIndex);
        }

        public void Dispose()
        {
        }

        private void OnClicked(int index)
        {
            if (clicked)
            {
                return;
            }

            clicked = true;
            selectedIndex = index;
            screenNavigator.PopOverlayAsync(this, playAnimation: false).Forget();
        }
    }
}
