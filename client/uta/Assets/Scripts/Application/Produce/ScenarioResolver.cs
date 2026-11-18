using System;
using System.Linq;
using ZLinq;
using Uta.MasterData;

namespace Uta.Client.Application.Produce
{
    /// <summary>シナリオの並び（ScenarioId の昇順）から、番号で ID・名前を引き当てる。</summary>
    public sealed class ScenarioResolver
    {
        private readonly Scenario[] scenarios;

        public ScenarioResolver(MemoryDatabase memoryDatabase)
        {
            scenarios = memoryDatabase.ScenarioTable.All.OrderBy(scenario => scenario.ScenarioId).ToArray();
        }

        /// <summary>シナリオの数。</summary>
        public int Count
        {
            get { return scenarios.Length; }
        }

        /// <summary>index の ScenarioId。範囲外なら ArgumentOutOfRangeException。</summary>
        public int IdAt(int index)
        {
            return At(index).ScenarioId;
        }

        /// <summary>index の Name。範囲外なら ArgumentOutOfRangeException。</summary>
        public string NameAt(int index)
        {
            return At(index).Name;
        }

        private Scenario At(int index)
        {
            if (index < 0 || index >= scenarios.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return scenarios[index];
        }
    }
}
