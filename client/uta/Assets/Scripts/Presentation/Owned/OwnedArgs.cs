using Uta.Client.Application.Owned;

namespace Uta.Client.Presentation.Owned
{
    public sealed class OwnedArgs
    {
        /// <summary>selection が null なら閲覧モード。</summary>
        public OwnedArgs(OwnedSelectionCondition selection)
        {
            Selection = selection;
        }

        public OwnedSelectionCondition Selection { get; }

        public static OwnedArgs Browse()
        {
            return new OwnedArgs(null);
        }
    }
}
