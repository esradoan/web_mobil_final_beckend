using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SmartCampus.API.Middleware;
using System.Text.Json;
using Xunit;

namespace SmartCampus.Tests.Middleware
{
    public class ExceptionMiddlewareTests
    {
        private readonly Mock<ILogger<ExceptionMiddleware>> _loggerMock;

        public ExceptionMiddlewareTests()
        {
            _loggerMock = new Mock<ILogger<ExceptionMiddleware>>();
        }

        [Fact]
        public async Task InvokeAsync_Should_Call_Next_When_No_Exception()
        {
            // Arrange
            var context = new DefaultHttpContext();
            bool nextCalled = false;
            RequestDelegate next = (ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new ExceptionMiddleware(next, _loggerMock.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_Should_Handle_Exception_And_Return_500()
        {
            // Arrange
            var context = new DefaultHttpContext();
            // Start the response body stream so we can read from it later
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (ctx) => throw new Exception("Test Exception");

            var middleware = new ExceptionMiddleware(next, _loggerMock.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(500, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);

            // Reset stream position to read response
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            
            Assert.Contains("Internal Server Error from the custom middleware", responseBody);
            Assert.Contains("Test Exception", responseBody);

            // Verify logging
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
