using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartCampus.Business.DTOs;

namespace SmartCampus.Business.Services
{
    public interface ITranscriptPdfService
    {
        byte[] GenerateTranscript(TranscriptDto transcript);
    }

    public class TranscriptPdfService : ITranscriptPdfService
    {
        static TranscriptPdfService()
        {
            // QuestPDF License - Community license for open source
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateTranscript(TranscriptDto transcript)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, transcript));
                    page.Content().Element(c => ComposeContent(c, transcript));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, TranscriptDto transcript)
        {
            container.Column(column =>
            {
                // University Name
                column.Item().AlignCenter().Text("SMART CAMPUS ÜNİVERSİTESİ")
                    .FontSize(16).Bold();
                
                column.Item().AlignCenter().Text("ÖĞRENCİ TRANSKRİPTİ")
                    .FontSize(14).Bold();

                column.Item().PaddingVertical(10).LineHorizontal(1);

                // Student Info
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Öğrenci Adı: {transcript.StudentName}");
                        col.Item().Text($"Öğrenci No: {transcript.StudentNumber}");
                    });
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Bölüm: {transcript.Department}");
                        col.Item().Text($"Toplam Kredi: {transcript.TotalCredits}");
                    });
                });

                column.Item().PaddingVertical(5).LineHorizontal(1);
            });
        }

        private void ComposeContent(IContainer container, TranscriptDto transcript)
        {
            container.PaddingVertical(10).Column(column =>
            {
                foreach (var semester in transcript.Semesters)
                {
                    // Semester Header
                    column.Item().Background(Colors.Grey.Lighten3).Padding(5)
                        .Text($"{semester.Semester} {semester.Year} - GANO: {semester.Gpa:F2}")
                        .Bold();

                    // Courses Table
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Course Code
                            columns.RelativeColumn(4); // Course Name
                            columns.RelativeColumn(1); // Credits
                            columns.RelativeColumn(1); // Grade
                            columns.RelativeColumn(1); // Points
                        });

                        // Table Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(3)
                                .Text("Kod").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(3)
                                .Text("Ders Adı").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(3)
                                .Text("Kredi").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(3)
                                .Text("Not").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(3)
                                .Text("Puan").Bold();
                        });

                        // Course Rows
                        foreach (var course in semester.Courses)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                                .Padding(3).Text(course.CourseCode);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                                .Padding(3).Text(course.CourseName);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                                .Padding(3).AlignCenter().Text(course.Credits.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                                .Padding(3).AlignCenter().Text(course.LetterGrade ?? "-");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                                .Padding(3).AlignCenter().Text(course.GradePoint?.ToString("F2") ?? "-");
                        }
                    });

                    column.Item().PaddingVertical(10);
                }

                // CGPA Summary
                column.Item().PaddingTop(20).Background(Colors.Blue.Lighten4).Padding(10)
                    .Row(row =>
                    {
                        row.RelativeItem().Text("GENEL NOT ORTALAMASI (CGPA):").Bold();
                        row.RelativeItem().AlignRight().Text($"{transcript.Cgpa:F2}").Bold().FontSize(14);
                    });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().LineHorizontal(1);
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text(text => 
                        text.Span($"Oluşturulma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(8));
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Sayfa ").FontSize(8);
                        text.CurrentPageNumber().FontSize(8);
                        text.Span(" / ").FontSize(8);
                        text.TotalPages().FontSize(8);
                    });
                });
                col.Item().PaddingTop(5).AlignCenter()
                    .Text(text => text.Span("Bu belge elektronik olarak oluşturulmuştur.").FontSize(8).Italic());
            });
        }
    }
}
