using System.Collections.Generic;
using UnityEngine;

namespace Uta.Client.Presentation.Common
{
    /// <summary>DialogPresenter に渡す引数。labels と colors は同じ長さ（DialogOpener が作る）。</summary>
    public sealed class DialogArgs
    {
        public DialogArgs(string message, IReadOnlyList<string> labels, IReadOnlyList<Color> colors)
        {
            Message = message;
            Labels = labels;
            Colors = colors;
        }

        public string Message { get; }
        public IReadOnlyList<string> Labels { get; }
        public IReadOnlyList<Color> Colors { get; }
    }
}
