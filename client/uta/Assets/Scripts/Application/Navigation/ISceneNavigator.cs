using Cysharp.Threading.Tasks;

namespace Uta.Client.Application.Navigation
{
    public interface ISceneNavigator
    {
        UniTask GoToTitleAsync();
        UniTask GoToHomeAsync();
        UniTask GoToProduceAsync();
    }
}
