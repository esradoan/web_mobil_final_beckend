using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SmartCampus.Business.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class SmsServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<SmsService>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private SmsService _service;

        public SmsServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<SmsService>>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

            // Default config (overridden in specific tests)
            _mockConfig.Setup(c => c["Sms:Enabled"]).Returns("true");
            _mockConfig.Setup(c => c["Sms:Provider"]).Returns("twilio");
            _mockConfig.Setup(c => c["Sms:Twilio:AccountSid"]).Returns("AC123");
            _mockConfig.Setup(c => c["Sms:Twilio:AuthToken"]).Returns("token");
            _mockConfig.Setup(c => c["Sms:Twilio:FromNumber"]).Returns("+1234");
            
            _service = new SmsService(_mockConfig.Object, _mockLogger.Object, _httpClient);
        }

        [Fact]
        public async Task SendSmsAsync_WhenDisabled_ShouldReturnMockSuccess()
        {
            _mockConfig.Setup(c => c["Sms:Enabled"]).Returns("false");
            // Re-create service to pick up new config
            _service = new SmsService(_mockConfig.Object, _mockLogger.Object, _httpClient);

            var result = await _service.SendSmsAsync("+905551234567", "Test message");

            Assert.True(result.Success);
            Assert.StartsWith("MOCK-", result.MessageId);
        }

        [Fact]
        public async Task SendSmsAsync_ViaTwilio_Success()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Post && 
                        req.RequestUri.ToString().Contains("Accounts/AC123/Messages.json")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"sid\": \"SM12345\"}")
                });

            // Act
            var result = await _service.SendSmsAsync("+905551234567", "Test Twilio");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("SM12345", result.MessageId);
        }

        [Fact]
        public async Task SendSmsAsync_ViaTwilio_Failure()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Invalid Parameter")
                });

            // Act
            var result = await _service.SendSmsAsync("+905551234567", "Test Fail");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid Parameter", result.Error);
        }

        [Fact]
        public async Task SendSmsAsync_ViaNetGsm_Success()
        {
            // Arrange
            _mockConfig.Setup(c => c["Sms:Provider"]).Returns("netgsm");
            _mockConfig.Setup(c => c["Sms:NetGsm:Username"]).Returns("user");
            _mockConfig.Setup(c => c["Sms:NetGsm:Password"]).Returns("pass");
            _service = new SmsService(_mockConfig.Object, _mockLogger.Object, _httpClient);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Get && 
                        req.RequestUri.ToString().Contains("api.netgsm.com.tr")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("00 123456789") // NetGSM success code starts with 00, 01, 02
                });

            // Act
            var result = await _service.SendSmsAsync("+905551234567", "Test NetGSM");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("123456789", result.MessageId);
        }

        [Fact]
        public async Task SendSmsAsync_ViaNetGsm_Failure()
        {
            // Arrange
            _mockConfig.Setup(c => c["Sms:Provider"]).Returns("netgsm");
            _mockConfig.Setup(c => c["Sms:NetGsm:Username"]).Returns("user");
            _service = new SmsService(_mockConfig.Object, _mockLogger.Object, _httpClient);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("30") // NetGSM error code
                });

            // Act
            var result = await _service.SendSmsAsync("+905551234567", "Test NetGSM Fail");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("NetGSM Error: 30", result.Error);
        }

        [Fact]
        public async Task SendSmsAsync_ViaNetGsm_MissingCredentials_ShouldFail()
        {
            // Arrange
            _mockConfig.Setup(c => c["Sms:Provider"]).Returns("netgsm");
            _mockConfig.Setup(c => c["Sms:NetGsm:Username"]).Returns(""); // Missing
            _service = new SmsService(_mockConfig.Object, _mockLogger.Object, _httpClient);

            // Act
            var result = await _service.SendSmsAsync("+905551234567", "Test");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("NetGSM credentials not configured", result.Error);
        }

        [Fact]
        public async Task SendSmsAsync_UnknownProvider_ShouldLogWarningAndMockSuccess()
        {
            // Arrange
            _mockConfig.Setup(c => c["Sms:Provider"]).Returns("unknown_provider");
            _service = new SmsService(_mockConfig.Object, _mockLogger.Object, _httpClient);

            // Act
            var result = await _service.SendSmsAsync("+905551234567", "Test");

            // Assert
            Assert.True(result.Success);
            Assert.StartsWith("LOG-", result.MessageId);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("SMS-NOT-CONFIGURED")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SendSmsAsync_Exception_ShouldReturnError()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Network Error"));

            // Act
            var result = await _service.SendSmsAsync("+905551234567", "Test Ex");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Network Error", result.Error);
        }

        [Fact]
        public async Task SendVerificationCodeAsync_ShouldStoreCodeInDictionary()
        {
            // Should work even if SMS send fails or is mocked (using Disabled mode for simplicity in this test)
            _mockConfig.Setup(c => c["Sms:Enabled"]).Returns("false");
            _service = new SmsService(_mockConfig.Object, _mockLogger.Object, _httpClient);

            var phone = "+905559998877";
            var result = await _service.SendVerificationCodeAsync(phone);

            Assert.True(result.Success);

            // Reflection check
            var field = typeof(SmsService).GetField("VerificationCodes", BindingFlags.NonPublic | BindingFlags.Static);
            var dict = field!.GetValue(null) as Dictionary<string, (string Code, DateTime Expiry)>;
            
            Assert.NotNull(dict);
            Assert.True(dict.ContainsKey(phone));
            Assert.Equal(6, dict[phone].Code.Length);
        }

        [Fact]
        public async Task VerifyCodeAsync_ShouldReturnTrue_WhenCodeIsCorrect()
        {
            _mockConfig.Setup(c => c["Sms:Enabled"]).Returns("false");
            _service = new SmsService(_mockConfig.Object, _mockLogger.Object, _httpClient);

            var phone = "+905551112233";
            await _service.SendVerificationCodeAsync(phone);

            var field = typeof(SmsService).GetField("VerificationCodes", BindingFlags.NonPublic | BindingFlags.Static);
            var dict = field!.GetValue(null) as Dictionary<string, (string Code, DateTime Expiry)>;
            var correctCode = dict![phone].Code;

            var result = await _service.VerifyCodeAsync(phone, correctCode);

            Assert.True(result);
        }

        [Fact]
        public async Task VerifyCodeAsync_ShouldReturnFalse_WhenCodeIsWrong()
        {
            _mockConfig.Setup(c => c["Sms:Enabled"]).Returns("false");
            _service = new SmsService(_mockConfig.Object, _mockLogger.Object, _httpClient);

            var phone = "+905551112233";
            await _service.SendVerificationCodeAsync(phone);

            var result = await _service.VerifyCodeAsync(phone, "000000");

            Assert.False(result);
        }

        [Fact]
        public async Task SendMealReservationNotificationAsync_ShouldFormatMessage()
        {
            _mockConfig.Setup(c => c["Sms:Enabled"]).Returns("false");
            _service = new SmsService(_mockConfig.Object, _mockLogger.Object, _httpClient);

            await _service.SendMealReservationNotificationAsync("+90123", "Main Cafeteria", DateTime.Today, "lunch");
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Main Cafeteria") && v.ToString().Contains("öğle")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SendClassroomReservationStatusAsync_ShouldFormatMessage()
        {
             _mockConfig.Setup(c => c["Sms:Enabled"]).Returns("false");
             _service = new SmsService(_mockConfig.Object, _mockLogger.Object, _httpClient);

             await _service.SendClassroomReservationStatusAsync("+90123", "approved", "Room 101", DateTime.Today);
            
             _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Room 101") && v.ToString().Contains("onaylandı")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SendEventReminderAsync_ShouldFormatMessage()
        {
            _mockConfig.Setup(c => c["Sms:Enabled"]).Returns("false");
             _service = new SmsService(_mockConfig.Object, _mockLogger.Object, _httpClient);

            // Act
            await _service.SendEventReminderAsync("+90555123", "Spring Fest", DateTime.Today.AddDays(1));

            // Verify
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Spring Fest") && v.ToString().Contains("Smart Campus Hatırlatma")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
