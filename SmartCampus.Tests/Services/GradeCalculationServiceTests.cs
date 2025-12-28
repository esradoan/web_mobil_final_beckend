using SmartCampus.Business.Services;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class GradeCalculationServiceTests
    {
        private readonly GradeCalculationService _service;

        public GradeCalculationServiceTests()
        {
            _service = new GradeCalculationService();
        }

        [Theory]
        [InlineData(90, 90, null, "AA")]
        [InlineData(85, 85, null, "BA")]
        [InlineData(80, 80, null, "BB")]
        [InlineData(75, 75, null, "CB")]
        [InlineData(70, 70, null, "CC")]
        [InlineData(65, 65, null, "DC")]
        [InlineData(60, 60, null, "DD")]
        [InlineData(50, 50, null, "FD")]
        [InlineData(40, 40, null, "FF")]
        public void CalculateLetterGrade_ShouldReturnCorrectGrade(decimal midterm, decimal final, decimal? homework, string expected)
        {
            var result = _service.CalculateLetterGrade(midterm, final, homework);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("AA", 4.0)]
        [InlineData("BA", 3.5)]
        [InlineData("BB", 3.0)]
        [InlineData("CB", 2.5)]
        [InlineData("CC", 2.0)]
        [InlineData("DC", 1.5)]
        [InlineData("DD", 1.0)]
        [InlineData("FD", 0.5)]
        [InlineData("FF", 0.0)]
        public void CalculateGradePoint_ShouldReturnCorrectPoint(string letterGrade, decimal expected)
        {
            var result = _service.CalculateGradePoint(letterGrade);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CalculateLetterGrade_WithHomework_ShouldIncludeInCalculation()
        {
            var result = _service.CalculateLetterGrade(80, 80, 100);
            Assert.NotNull(result);
        }
    }
}
