using System;
using Uta.Shared.Domain.Produce;

namespace Uta.Client.Application.Produce
{
    /// <summary>
    /// 進行中のプロデュースの最新の応答（Application の状態の型）。ルートのスコープの Singleton で、Home・Produce のシーンを往復しても残る。
    /// 書き込みは具象の ProduceUseCase が行う（ADR 0022）。結果の画面は Current.Result を読む。
    /// </summary>
    public sealed class ProduceSessionState
    {
        /// <summary>無ければ null。</summary>
        public ProduceStateResult Current { get; private set; }

        /// <summary>直前の Set の前の Current（授業・支給・休むの結果の差に使う）。無ければ null。</summary>
        public ProduceStateResult Previous { get; private set; }

        /// <summary>試験結果を画面に出したか。Set で false に戻る。</summary>
        public bool ExamResultShown { get; set; }

        /// <summary>Current があり、Phase が Nothing でも Finished でもない。</summary>
        public bool HasActive
        {
            get { return Current != null && Current.Phase != ProducePhase.Nothing && Current.Phase != ProducePhase.Finished; }
        }

        /// <summary>next が null なら ArgumentNullException。Previous = Current; Current = next; ExamResultShown = false。</summary>
        public void Set(ProduceStateResult next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            Previous = Current;
            Current = next;
            ExamResultShown = false;
        }

        /// <summary>Current・Previous を null、ExamResultShown を false にする。</summary>
        public void Clear()
        {
            Current = null;
            Previous = null;
            ExamResultShown = false;
        }
    }
}
