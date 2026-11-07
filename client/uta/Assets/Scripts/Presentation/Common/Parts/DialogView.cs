using TMPro;
using UnityEngine;

namespace Uta.Client.Presentation.Common.Parts
{
    /// <summary>Dialog 部品の View。ボタンは DialogService が buttonPrefab から実行時に buttonList へ並べる。</summary>
    public sealed class DialogView : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private RectTransform buttonList;
        [SerializeField] private ButtonView buttonPrefab;
    }
}
