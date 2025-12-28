using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using System.Collections.Generic;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class TranscriptPdfServiceTests
    {
        private readonly TranscriptPdfService _service;

        public TranscriptPdfServiceTests()
        {
            _service = new TranscriptPdfService();
        }

        private TranscriptDto CreateSampleTranscript()
        {
            return new TranscriptDto
            {
                StudentName = "Test Student",
                StudentNumber = "12345",
                Department = "Computer Science",
                Cgpa = 3.5m,
                TotalCredits = 120,
                Semesters = new List<SemesterGradesDto>
                {
                    new SemesterGradesDto
                    {
                        Semester = "Fall",
                        Year = 2024,
                        Gpa = 3.5m,
                        SemesterCredits = 30,
                        Courses = new List<GradeDto>
                        {
                            new GradeDto { CourseCode = "CS101", CourseName = "Intro to CS", Credits = 3, LetterGrade = "AA", GradePoint = 4.0m },
                            new GradeDto { CourseCode = "CS102", CourseName = "Data Structures", Credits = 3, LetterGrade = "BA", GradePoint = 3.5m }
                        }
                    }
                }
            };
        }

        [Fact]
        public void GenerateTranscript_WithValidData_ShouldReturnPdfBytes()
        {
            // Arrange
            var transcript = CreateSampleTranscript();

            // Act
            var result = _service.GenerateTranscript(transcript);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void GenerateTranscript_ShouldReturnValidPdfHeader()
        {
            // Arrange
            var transcript = CreateSampleTranscript();

            // Act
            var result = _service.GenerateTranscript(transcript);

            // Assert - PDF files start with %PDF
            Assert.True(result.Length > 4);
            Assert.Equal(0x25, result[0]); // %
            Assert.Equal(0x50, result[1]); // P
            Assert.Equal(0x44, result[2]); // D
            Assert.Equal(0x46, result[3]); // F
        }

        [Fact]
        public void GenerateTranscript_WithEmptySemesters_ShouldStillGenerate()
        {
            // Arrange
            var transcript = new TranscriptDto
            {
                StudentName = "Test",
                StudentNumber = "123",
                Department = "CS",
                Cgpa = 0,
                TotalCredits = 0,
                Semesters = new List<SemesterGradesDto>()
            };

            // Act
            var result = _service.GenerateTranscript(transcript);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void GenerateTranscript_WithMultipleSemesters_ShouldGenerate()
        {
            // Arrange
            var transcript = new TranscriptDto
            {
                StudentName = "Test Student",
                StudentNumber = "12345",
                Department = "CS",
                Cgpa = 3.2m,
                TotalCredits = 60,
                Semesters = new List<SemesterGradesDto>
                {
                    new SemesterGradesDto { Semester = "Fall", Year = 2023, Gpa = 3.0m, SemesterCredits = 30, Courses = new List<GradeDto>() },
                    new SemesterGradesDto { Semester = "Spring", Year = 2024, Gpa = 3.4m, SemesterCredits = 30, Courses = new List<GradeDto>() }
                }
            };

            // Act
            var result = _service.GenerateTranscript(transcript);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void GenerateTranscript_ShouldProduceLargeOutput()
        {
            // Arrange
            var transcript = CreateSampleTranscript();

            // Act
            var result = _service.GenerateTranscript(transcript);

            // Assert - PDF should be at least a few KB
            Assert.True(result.Length > 1000);
        }
    }
}
