using System;

namespace Uta.Client.Presentation.Common
{
    /// <summary>この印の付いた [SerializeField] の参照は null でよい（PrefabContractTests が見逃す）。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class OptionalReferenceAttribute : Attribute
    {
    }
}
