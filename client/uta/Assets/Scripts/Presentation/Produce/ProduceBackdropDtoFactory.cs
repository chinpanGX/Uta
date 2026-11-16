using System;
using Uta.Client.Application.Produce;
using Uta.Client.Presentation.Common;
using Uta.Shared.Domain.Lesson;

namespace Uta.Client.Presentation.Produce
{
    /// <summary>背景のページの表示の値と文字を作る。状態を持たず、通信・遷移・View に触れない。</summary>
    public sealed class ProduceBackdropDtoFactory
    {
        /// <summary>p が null なら ArgumentNullException。</summary>
        public ProduceBackdropViewDto Create(ProduceStateResult p)
        {
            if (p == null)
            {
                throw new ArgumentNullException(nameof(p));
            }

            var header = "週 " + p.Week + "\n"
                + DisplayText.ParamName(ParamKind.Singing) + " " + p.Singing + "　"
                + DisplayText.ParamName(ParamKind.Rhythm) + " " + p.Rhythm + "　"
                + DisplayText.ParamName(ParamKind.Expression) + " " + p.Expression + "　"
                + "体力 " + p.Hp + " / " + p.MaxHp;
            return new ProduceBackdropViewDto(header);
        }
    }
}
