using System;
using System.Collections.Generic;

namespace Uta.Shared.Domain.Produce
{
    /// <summary>育成に連れて行くサポートカード 1 枚。</summary>
    public sealed class SupportCardInput
    {
        public SupportCardInput(int cardId, int level, int limitBreak)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            if (limitBreak < 0 || limitBreak > ProduceRules.MaxLimitBreak)
            {
                throw new ArgumentOutOfRangeException(nameof(limitBreak));
            }

            CardId = cardId;
            Level = level;
            LimitBreak = limitBreak;
        }

        public int CardId { get; }
        public int Level { get; }
        public int LimitBreak { get; }
    }

    /// <summary>育成の開始時の入力。</summary>
    public sealed class ProduceStartInput
    {
        public ProduceStartInput(int scenarioId, int idolId, int playerLevel, IReadOnlyList<SupportCardInput> supportCards)
        {
            if (supportCards == null)
            {
                throw new ArgumentNullException(nameof(supportCards));
            }

            if (playerLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(playerLevel));
            }

            if (supportCards.Count > ProduceRules.MaxSupportCards)
            {
                throw new ArgumentException("サポートカードは " + ProduceRules.MaxSupportCards + " 枚までです。", nameof(supportCards));
            }

            var copy = new SupportCardInput[supportCards.Count];
            for (var i = 0; i < copy.Length; i++)
            {
                if (supportCards[i] == null)
                {
                    throw new ArgumentException("サポートカードに null が含まれています。", nameof(supportCards));
                }

                copy[i] = supportCards[i];
            }

            ScenarioId = scenarioId;
            IdolId = idolId;
            PlayerLevel = playerLevel;
            SupportCards = copy;
        }

        public int ScenarioId { get; }
        public int IdolId { get; }

        /// <summary>カードの UnlockLv の絞り込みに使う（produce.md 3.5）</summary>
        public int PlayerLevel { get; }

        /// <summary>枠の順</summary>
        public IReadOnlyList<SupportCardInput> SupportCards { get; }
    }
}
