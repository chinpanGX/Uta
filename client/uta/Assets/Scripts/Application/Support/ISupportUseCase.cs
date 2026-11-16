using Cysharp.Threading.Tasks;

namespace Uta.Client.Application.Support
{
    public interface ISupportUseCase
    {
        /// <summary>
        /// supportCardUniqueId が null・空なら ArgumentException（通信しない）。サーバーの業務エラーは
        /// SUPPORT_NOT_ENOUGH_POINTS → NotEnoughPoints、PLAYER_NOT_ENOUGH_GOLD → NotEnoughGold、SUPPORT_INVALID_TARGET_LEVEL → InvalidTarget、SUPPORT_NOT_OWNED → NotOwned。
        /// ほかの失敗は Failed（共通のダイアログは ApiCore が出している。取り直さない）。成功したら所持データを取り直し（失敗しても成功を返す）、Succeeded。
        /// 業務エラーのときも取り直す（失敗しても結果は変えない）。
        /// </summary>
        UniTask<LevelUpResult> LevelUpAsync(string supportCardUniqueId, int targetLevel);

        /// <summary>
        /// どちらかの ID が null・空、または 2 つが同じなら ArgumentException（通信しない）。SUPPORT_MAX_LIMIT_BREAK → MaxLimitBreak、SUPPORT_INVALID_MATERIAL → InvalidMaterial、
        /// SUPPORT_NOT_OWNED → NotOwned、ほかは Failed。成功の扱いは LevelUpAsync と同じ。
        /// </summary>
        UniTask<LimitBreakResult> LimitBreakAsync(string supportCardUniqueId, string materialUniqueId);
    }
}
