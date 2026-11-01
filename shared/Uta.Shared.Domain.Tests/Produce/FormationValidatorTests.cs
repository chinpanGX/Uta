using System.Collections.Generic;
using Uta.Shared.Domain.Produce;
using Xunit;

namespace Uta.Shared.Domain.Tests.Produce
{
    public class FormationValidatorTests
    {
        private static readonly ProduceCatalog Catalog = TestProduceMaster.Create();

        [Theory]
        [InlineData(1, new[] { 6, 8 }, FormationCheck.Ok)]
        [InlineData(1, new int[0], FormationCheck.Ok)]
        [InlineData(1, new[] { 1, 2, 3, 4, 5 }, FormationCheck.Ok)]
        [InlineData(1, new[] { 1, 1 }, FormationCheck.DuplicateCardId)]
        [InlineData(1, new[] { 7 }, FormationCheck.PlanMismatch)]
        [InlineData(2, new[] { 9 }, FormationCheck.PlanMismatch)]
        [InlineData(1, new[] { 7, 7 }, FormationCheck.DuplicateCardId)]
        public void Validate_Cases_ReturnsExpected(int idolId, int[] cardIds, FormationCheck expected)
        {
            Assert.Equal(expected, FormationValidator.Validate(Catalog, idolId, cardIds));
        }

        [Fact]
        public void Validate_UnknownIdol_Throws()
        {
            Assert.Throws<KeyNotFoundException>(() => FormationValidator.Validate(Catalog, 99, new[] { 1 }));
        }

        [Fact]
        public void Validate_UnknownCard_Throws()
        {
            Assert.Throws<KeyNotFoundException>(() => FormationValidator.Validate(Catalog, 1, new[] { 99 }));
        }
    }
}
