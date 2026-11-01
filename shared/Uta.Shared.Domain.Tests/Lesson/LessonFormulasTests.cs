using Uta.Shared.Domain.Lesson;
using Xunit;

namespace Uta.Shared.Domain.Tests.Lesson
{
    public sealed class LessonFormulasTests
    {
        [Theory]
        [InlineData(4, false, false, 0, 0, 4)]
        [InlineData(4, true, false, 0, 0, 2)]
        [InlineData(3, true, false, 0, 0, 2)] // 1.5 → 切り上げ
        [InlineData(3, false, true, 0, 0, 6)]
        [InlineData(3, true, true, 0, 0, 3)] // 両方あり → 1倍
        [InlineData(4, false, false, 0, 1, 3)]
        [InlineData(1, false, false, 0, 2, 0)] // 0 未満にならない
        [InlineData(5, true, false, 0, 1, 2)] // ceil(2.5 − 1)
        public void FinalConsume_Cases_MatchFormula(int cost, bool half, bool dbl, int add, int reduce, int expected)
        {
            Assert.Equal(expected, LessonFormulas.FinalConsume(cost, half, dbl, add, reduce));
        }

        [Fact]
        public void ParamGain_NoStatus_ReturnsCardValue()
        {
            Assert.Equal(17, LessonFormulas.ParamGain(17, 0, 0, 100, 0, 0, 100, null, 0));
        }

        [Fact]
        public void ParamGain_FocusScale150_RoundsUpOnce()
        {
            // 17 + 3 × 1.5 = 21.5 → 22
            Assert.Equal(22, LessonFormulas.ParamGain(17, 3, 0, 150, 0, 0, 100, null, 0));
        }

        [Fact]
        public void ParamGain_GoodConditionScale200_DoublesGoodEffect()
        {
            // 10 × (1 + 0.5 × 2) = 20
            Assert.Equal(20, LessonFormulas.ParamGain(10, 0, 0, 100, 2, 0, 200, null, 0));
        }

        [Fact]
        public void ParamGain_Excellent_AddsTenPercentPerGoodTurn()
        {
            // 10 × (1 + 0.5 + 0.1 × 3) = 18
            Assert.Equal(18, LessonFormulas.ParamGain(10, 0, 0, 100, 3, 1, 100, null, 0));
        }

        [Fact]
        public void ParamGain_GoodConditionAndBonus_RoundsAtEnd()
        {
            // 9 × 1.5 × 1.3 = 17.55 → 18（途中で丸めると 14 × 1.3 = 18.2 → 19 になる）
            Assert.Equal(18, LessonFormulas.ParamGain(9, 0, 0, 100, 1, 0, 100, new[] { 30 }, 0));
        }

        [Theory]
        [InlineData(4, 150, 6)]
        [InlineData(3, 130, 4)] // 3.9 → 4
        [InlineData(0, 200, 0)]
        public void RatioParam_Cases_RoundUp(int source, int percent, int expected)
        {
            Assert.Equal(expected, LessonFormulas.RatioParam(source, percent));
        }

        [Fact]
        public void ShieldGain_Blocked_ReturnsZero()
        {
            Assert.Equal(0, LessonFormulas.ShieldGain(4, 2, true));
            Assert.Equal(6, LessonFormulas.ShieldGain(4, 2, false));
        }

        [Theory]
        [InlineData(4, false, 4)]
        [InlineData(4, true, 0)]
        public void FixedShieldGain_Cases(int value, bool blocked, int expected)
        {
            Assert.Equal(expected, LessonFormulas.FixedShieldGain(value, blocked));
        }

        [Theory]
        [InlineData(7, 50, 3)]
        [InlineData(7, 0, 0)]
        [InlineData(7, 100, 7)]
        [InlineData(1, 50, 0)]
        public void ScaledShield_Cases(int shield, int percent, int expected)
        {
            Assert.Equal(expected, LessonFormulas.ScaledShield(shield, percent));
        }

        [Theory]
        [InlineData(9, 150, 14)]
        [InlineData(3, 120, 4)]
        [InlineData(5, 130, 7)]
        [InlineData(10, 0, 0)]
        public void ExamScore_Cases(int paramGain, int scoreBonusPercent, int expected)
        {
            Assert.Equal(expected, LessonFormulas.ExamScore(paramGain, scoreBonusPercent));
        }

        [Fact]
        public void ImpressionGain_Boost10Percent_RoundsUp()
        {
            Assert.Equal(5, LessonFormulas.ImpressionGain(5, 0, null));
            Assert.Equal(6, LessonFormulas.ImpressionGain(5, 10, null)); // 5.5 → 6
        }
    }
}
