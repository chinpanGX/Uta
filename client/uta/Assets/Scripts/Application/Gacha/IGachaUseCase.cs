using Cysharp.Threading.Tasks;

namespace Uta.Client.Application.Gacha
{
    public interface IGachaUseCase
    {
        /// <summary>ガチャの一覧（GetGachasAsync）を取り、gachaId の交換pt を返す。一覧に無ければ 0。通信の失敗は null（共通のダイアログは ApiCore が出している）。</summary>
        UniTask<int?> GetExchangePointsAsync(int gachaId);

        /// <summary>
        /// count は 1 か 10（それ以外は ArgumentOutOfRangeException）。費用はマスタの SingleCost／TenCost。
        /// 所持のガチャ通貨（リポジトリ）が費用より少なければ通信せず NotEnoughCurrency。
        /// サーバーの通貨不足は NotEnoughCurrency、ほかの失敗は Failed（共通のダイアログは ApiCore が出している）。
        /// 成功したら所持データを取り直し（失敗しても成功を返す）、Succeeded を返す。
        /// </summary>
        UniTask<DrawResult> DrawAsync(int gachaId, int count);
    }
}
