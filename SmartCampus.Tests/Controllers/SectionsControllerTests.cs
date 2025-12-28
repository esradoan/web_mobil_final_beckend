using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Controllers
{
    public class SectionsControllerTests
    {
        private readonly Mock<ICourseService> _mockCourseService;
        private readonly SectionsController _controller;

        public SectionsControllerTests()
        {
            _mockCourseService = new Mock<ICourseService>();
            _controller = new SectionsController(_mockCourseService.Object);
        }

        [Fact]
        public async Task GetSections_ReturnsOk_WithSections()
        {
            // Arrange
            var sections = new List<CourseSectionDto> { new CourseSectionDto { Id = 1, SectionNumber = "01" } };
            _mockCourseService.Setup(x => x.GetSectionsAsync(null, null, null, null))
                .ReturnsAsync(sections);

            // Act
            var result = await _controller.GetSections();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetSection_ReturnsOk_WhenExists()
        {
            // Arrange
            var section = new CourseSectionDto { Id = 1, SectionNumber = "01" };
            _mockCourseService.Setup(x => x.GetSectionByIdAsync(1)).ReturnsAsync(section);

            // Act
            var result = await _controller.GetSection(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(section, okResult.Value);
        }

        [Fact]
        public async Task CreateSection_ReturnsCreated_WhenSuccessful()
        {
            // Arrange
            var dto = new CreateSectionDto { CourseId = 1, SectionNumber = "01" };
            var createdSection = new CourseSectionDto { Id = 1, SectionNumber = "01" };
            _mockCourseService.Setup(x => x.CreateSectionAsync(dto)).ReturnsAsync(createdSection);

            // Act
            var result = await _controller.CreateSection(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(createdSection, createdResult.Value);
        }

        [Fact]
        public async Task CreateSection_ReturnsBadRequest_OnException()
        {
            // Arrange
            var dto = new CreateSectionDto();
            _mockCourseService.Setup(x => x.CreateSectionAsync(dto)).ThrowsAsync(new System.Exception("Error"));

            // Act
            var result = await _controller.CreateSection(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSection_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var dto = new UpdateSectionDto { Capacity = 100 };
            var updatedSection = new CourseSectionDto { Id = 1, SectionNumber = "01", Capacity = 100 };
            _mockCourseService.Setup(x => x.UpdateSectionAsync(1, dto)).ReturnsAsync(updatedSection);

            // Act
            var result = await _controller.UpdateSection(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(updatedSection, okResult.Value);
        }

        [Fact]
        public async Task UpdateSection_ReturnsNotFound_WhenNotExists()
        {
            // Arrange
            var dto = new UpdateSectionDto();
            _mockCourseService.Setup(x => x.UpdateSectionAsync(1, dto)).ReturnsAsync((CourseSectionDto)null);

            // Act
            var result = await _controller.UpdateSection(1, dto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteSection_ReturnsNoContent_WhenSuccessful()
        {
            // Arrange
            _mockCourseService.Setup(x => x.DeleteSectionAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteSection(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteSection_ReturnsNotFound_WhenNotExists()
        {
            // Arrange
            _mockCourseService.Setup(x => x.DeleteSectionAsync(1)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteSection(1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
