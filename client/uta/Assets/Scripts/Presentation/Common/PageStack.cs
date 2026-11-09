using System;
using Cysharp.Threading.Tasks;
using UnityScreenNavigator;
using UnityScreenNavigator.Runtime.Core.Modal;
using UnityScreenNavigator.Runtime.Core.Page;
using VContainer;

namespace Uta.Client.Presentation.Common
{
    /// <summary>ページ・モーダルの枚数を数えて、シーンの底のページまで戻る。</summary>
    public sealed class PageStack
    {
        private readonly Func<int> pageCount;
        private readonly Func<int> modalCount;

        /// <summary>VContainer が使う。</summary>
        [Inject]
        public PageStack(PageContainer pageContainer, ModalContainer modalContainer)
            : this(() => pageContainer.Pages.Count, () => modalContainer.Modals.Count)
        {
        }

        /// <summary>テスト用。</summary>
        public PageStack(Func<int> pageCount, Func<int> modalCount)
        {
            this.pageCount = pageCount;
            this.modalCount = modalCount;
        }

        /// <summary>積まれているページの枚数（呼んだ時点）。</summary>
        public int Depth => pageCount();

        /// <summary>積まれているモーダルの枚数（呼んだ時点）。</summary>
        public int ModalDepth => modalCount();

        /// <summary>
        /// シーンの底のページ（ホームなど）へ戻る。ModalDepth > 0 なら screenNavigator.PopModalAsync(playAnimation: false, popCount: ModalDepth) を待ち、
        /// そのあと Depth > 1 なら screenNavigator.PopPageAsync(playAnimation: false, popCount: Depth - 1) を待つ。底が 1 枚以下なら Pop を呼ばない。
        /// </summary>
        public async UniTask PopToRootAsync(IScreenNavigator screenNavigator)
        {
            var modals = ModalDepth;
            if (modals > 0)
            {
                await screenNavigator.PopModalAsync(playAnimation: false, popCount: modals);
            }

            var pages = Depth;
            if (pages > 1)
            {
                await screenNavigator.PopPageAsync(playAnimation: false, popCount: pages - 1);
            }
        }
    }
}
