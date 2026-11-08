using TMPro;
using UnityEngine;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>山札などの一覧の見出しの行（Prefab DeckSectionHeaderRow のルート）。フィールドだけ。</summary>
    [DisallowMultipleComponent]
    public sealed class DeckSectionHeaderRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text headerText;
    }
}
