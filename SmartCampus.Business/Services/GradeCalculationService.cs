namespace SmartCampus.Business.Services
{
    public interface IGradeCalculationService
    {
        string CalculateLetterGrade(decimal? midterm, decimal? final, decimal? homework);
        decimal CalculateGradePoint(string letterGrade);
        decimal CalculateTotalGrade(decimal? midterm, decimal? final, decimal? homework);
    }

    /// <summary>
    /// Not hesaplama servisi - Harf notu ve GPA hesaplamaları
    /// </summary>
    public class GradeCalculationService : IGradeCalculationService
    {
        // Ağırlıklar
        private const decimal MidtermWeight = 0.30m;
        private const decimal FinalWeight = 0.50m;
        private const decimal HomeworkWeight = 0.20m;

        public decimal CalculateTotalGrade(decimal? midterm, decimal? final, decimal? homework)
        {
            decimal total = 0;
            decimal totalWeight = 0;

            if (midterm.HasValue)
            {
                total += midterm.Value * MidtermWeight;
                totalWeight += MidtermWeight;
            }
            if (final.HasValue)
            {
                total += final.Value * FinalWeight;
                totalWeight += FinalWeight;
            }
            if (homework.HasValue)
            {
                total += homework.Value * HomeworkWeight;
                totalWeight += HomeworkWeight;
            }

            // Eksik notlar varsa, mevcut ağırlığa göre normalize et
            return totalWeight > 0 ? total / totalWeight * 100 / 100 : 0;
        }

        public string CalculateLetterGrade(decimal? midterm, decimal? final, decimal? homework)
        {
            // Final notu girilmeden harf notu hesaplanmaz
            if (!final.HasValue) return "";

            decimal total = (midterm ?? 0) * MidtermWeight + 
                           final.Value * FinalWeight + 
                           (homework ?? 0) * HomeworkWeight;

            // Eksik notları hesaba kat
            if (!midterm.HasValue) total = total / (1 - MidtermWeight);
            if (!homework.HasValue && midterm.HasValue) total = total / (1 - HomeworkWeight);

            return total switch
            {
                >= 90 => "AA",
                >= 85 => "BA",
                >= 80 => "BB",
                >= 75 => "CB",
                >= 70 => "CC",
                >= 65 => "DC",
                >= 60 => "DD",
                _ => "FF"
            };
        }

        public decimal CalculateGradePoint(string letterGrade)
        {
            return letterGrade switch
            {
                "AA" => 4.0m,
                "BA" => 3.5m,
                "BB" => 3.0m,
                "CB" => 2.5m,
                "CC" => 2.0m,
                "DC" => 1.5m,
                "DD" => 1.0m,
                "FF" => 0.0m,
                _ => 0.0m
            };
        }
    }
}
