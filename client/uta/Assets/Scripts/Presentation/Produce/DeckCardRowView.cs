using TMPro;
using UnityEngine;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>山札などの一覧のカードの行（Prefab DeckCardRow のルート）。フィールドだけ。</summary>
    [DisallowMultipleComponent]
    public sealed class DeckCardRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
    }
}
