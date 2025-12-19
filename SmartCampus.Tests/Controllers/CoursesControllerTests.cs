#nullable disable
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartCampus.API.Controllers;
using SmartCampus.Business.DTOs;
using SmartCampus.Business.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Controllers
{
    public class CoursesControllerTests
    {
        private readonly Mock<ICourseService> _mockCourseService;
        private readonly CoursesController _controller;

        public CoursesControllerTests()
        {
            _mockCourseService = new Mock<ICourseService>();
            _controller = new CoursesController(_mockCourseService.Object);
        }

        [Fact]
        public async Task GetCourses_ReturnsOk_WithResult()
        {
            // Arrange
            // Using correct PaginatedResponse type instead of anonymous object
            var expectedResult = new PaginatedResponse<CourseDto> 
            { 
                Data = new List<CourseDto>(), 
                Pagination = new PaginationInfo { Total = 0 }
            };
            
            _mockCourseService.Setup(x => x.GetCoursesAsync(1, 10, null, null, null))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetCourses();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
        }

        [Fact]
        public async Task GetCourse_ReturnsOk_WhenExists()
        {
            // Arrange
            var courseDto = new CourseDto { Id = 1, Name = "Test Course" };
            _mockCourseService.Setup(x => x.GetCourseByIdAsync(1)).ReturnsAsync(courseDto);

            // Act
            var result = await _controller.GetCourse(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(courseDto, okResult.Value);
        }

        [Fact]
        public async Task GetCourse_ReturnsNotFound_WhenNotExists()
        {
            // Arrange
            _mockCourseService.Setup(x => x.GetCourseByIdAsync(It.IsAny<int>())).ReturnsAsync((CourseDto)null);

            // Act
            var result = await _controller.GetCourse(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateCourse_ReturnsCreatedAtAction_WhenSuccessful()
        {
            // Arrange
            var dto = new CreateCourseDto { Name = "New Course" };
            var createdCourse = new CourseDto { Id = 10, Name = "New Course" };
            _mockCourseService.Setup(x => x.CreateCourseAsync(dto)).ReturnsAsync(createdCourse);

            // Act
            var result = await _controller.CreateCourse(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(createdCourse, createdResult.Value);
            Assert.Equal(nameof(CoursesController.GetCourse), createdResult.ActionName);
        }

        [Fact]
        public async Task CreateCourse_ReturnsBadRequest_OnException()
        {
            // Arrange
            var dto = new CreateCourseDto();
            _mockCourseService.Setup(x => x.CreateCourseAsync(dto)).ThrowsAsync(new Exception("Error"));

            // Act
            var result = await _controller.CreateCourse(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCourse_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var dto = new UpdateCourseDto { Name = "Updated" };
            var updatedCourse = new CourseDto { Id = 1, Name = "Updated" };
            _mockCourseService.Setup(x => x.UpdateCourseAsync(1, dto)).ReturnsAsync(updatedCourse);

            // Act
            var result = await _controller.UpdateCourse(1, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(updatedCourse, okResult.Value);
        }

        [Fact]
        public async Task UpdateCourse_ReturnsNotFound_WhenNotExists()
        {
            // Arrange
            var dto = new UpdateCourseDto();
            _mockCourseService.Setup(x => x.UpdateCourseAsync(1, dto)).ReturnsAsync((CourseDto)null);

            // Act
            var result = await _controller.UpdateCourse(1, dto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteCourse_ReturnsNoContent_WhenSuccessful()
        {
            // Arrange
            _mockCourseService.Setup(x => x.DeleteCourseAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteCourse(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteCourse_ReturnsNotFound_WhenNotExists()
        {
            // Arrange
            _mockCourseService.Setup(x => x.DeleteCourseAsync(1)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteCourse(1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
