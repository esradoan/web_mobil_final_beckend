using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmartCampus.Business.Services
{
    public interface ISmsService
    {
        Task<SmsResultDto> SendSmsAsync(string phoneNumber, string message);
        Task<SmsResultDto> SendVerificationCodeAsync(string phoneNumber);
        Task<bool> VerifyCodeAsync(string phoneNumber, string code);
        Task SendMealReservationNotificationAsync(string phoneNumber, string cafeteriaName, DateTime date, string mealType);
        Task SendEventReminderAsync(string phoneNumber, string eventTitle, DateTime eventDate);
        Task SendClassroomReservationStatusAsync(string phoneNumber, string status, string classroomName, DateTime date);
    }

    // ==================== DTOs ====================

    public class SmsResultDto
    {
        public bool Success { get; set; }
        public string? MessageId { get; set; }
        public string? Error { get; set; }
    }

    // ==================== SERVICE ====================

    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;
        private readonly HttpClient _httpClient;

        // Verification code storage (in production use Redis/cache)
        private static readonly Dictionary<string, (string Code, DateTime Expiry)> VerificationCodes = new();

        public SmsService(IConfiguration configuration, ILogger<SmsService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<SmsResultDto> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                // Normalize phone number
                phoneNumber = NormalizePhoneNumber(phoneNumber);

                // Get SMS provider settings
                var provider = _configuration["Sms:Provider"] ?? "twilio";
                var accountSid = _configuration["Sms:Twilio:AccountSid"];
                var authToken = _configuration["Sms:Twilio:AuthToken"];
                var fromNumber = _configuration["Sms:Twilio:FromNumber"];

                // Check if SMS is enabled
                var isEnabled = _configuration["Sms:Enabled"]?.ToLower() == "true";

                if (!isEnabled)
                {
                    _logger.LogInformation("[SMS-MOCK] To: {Phone}, Message: {Message}", phoneNumber, message);
                    return new SmsResultDto
                    {
                        Success = true,
                        MessageId = $"MOCK-{Guid.NewGuid():N}"[..16]
                    };
                }

                // Send via Twilio
                if (provider.ToLower() == "twilio" && !string.IsNullOrEmpty(accountSid))
                {
                    return await SendViaTwilioAsync(phoneNumber, message, accountSid, authToken!, fromNumber!);
                }

                // Send via NetGSM (Turkey local provider)
                if (provider.ToLower() == "netgsm")
                {
                    return await SendViaNetGsmAsync(phoneNumber, message);
                }

                // Fallback: log message
                _logger.LogWarning("[SMS-NOT-CONFIGURED] To: {Phone}, Message: {Message}", phoneNumber, message);
                return new SmsResultDto
                {
                    Success = true,
                    MessageId = $"LOG-{Guid.NewGuid():N}"[..16]
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMS send failed to {Phone}", phoneNumber);
                return new SmsResultDto
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<SmsResultDto> SendVerificationCodeAsync(string phoneNumber)
        {
            phoneNumber = NormalizePhoneNumber(phoneNumber);

            // Generate 6-digit code
            var code = new Random().Next(100000, 999999).ToString();

            // Store with 5-minute expiry
            VerificationCodes[phoneNumber] = (code, DateTime.UtcNow.AddMinutes(5));

            var message = $"Smart Campus doğrulama kodunuz: {code}. Bu kod 5 dakika geçerlidir.";

            return await SendSmsAsync(phoneNumber, message);
        }

        public Task<bool> VerifyCodeAsync(string phoneNumber, string code)
        {
            phoneNumber = NormalizePhoneNumber(phoneNumber);

            if (VerificationCodes.TryGetValue(phoneNumber, out var stored))
            {
                if (stored.Expiry > DateTime.UtcNow && stored.Code == code)
                {
                    VerificationCodes.Remove(phoneNumber);
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public async Task SendMealReservationNotificationAsync(string phoneNumber, string cafeteriaName, DateTime date, string mealType)
        {
            var mealTypeText = mealType == "lunch" ? "öğle yemeği" : "akşam yemeği";
            var message = $"🍽️ Smart Campus: {date:dd.MM.yyyy} tarihli {mealTypeText} rezervasyonunuz {cafeteriaName} için oluşturuldu. QR kodunuzu uygulamadan görüntüleyebilirsiniz.";

            await SendSmsAsync(phoneNumber, message);
        }

        public async Task SendEventReminderAsync(string phoneNumber, string eventTitle, DateTime eventDate)
        {
            var message = $"📅 Smart Campus Hatırlatma: '{eventTitle}' etkinliği yarın ({eventDate:dd.MM.yyyy}) başlıyor! QR kodunuzu hazır bulundurun.";

            await SendSmsAsync(phoneNumber, message);
        }

        public async Task SendClassroomReservationStatusAsync(string phoneNumber, string status, string classroomName, DateTime date)
        {
            var statusText = status switch
            {
                "approved" => "onaylandı ✅",
                "rejected" => "reddedildi ❌",
                _ => status
            };

            var message = $"🏫 Smart Campus: {date:dd.MM.yyyy} tarihli {classroomName} derslik rezervasyonunuz {statusText}.";

            await SendSmsAsync(phoneNumber, message);
        }

        // ==================== PRIVATE METHODS ====================

        private string NormalizePhoneNumber(string phoneNumber)
        {
            // Remove all non-digit characters
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Turkey number normalization
            if (digits.StartsWith("0"))
                digits = "90" + digits[1..];
            else if (!digits.StartsWith("90") && digits.Length == 10)
                digits = "90" + digits;

            return "+" + digits;
        }

        private async Task<SmsResultDto> SendViaTwilioAsync(string to, string message, string accountSid, string authToken, string fromNumber)
        {
            try
            {
                var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";

                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["To"] = to,
                    ["From"] = fromNumber,
                    ["Body"] = message
                });

                var authString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{accountSid}:{authToken}"));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[Twilio] SMS sent to {Phone}", to);
                    return new SmsResultDto
                    {
                        Success = true,
                        MessageId = System.Text.Json.JsonDocument.Parse(responseBody).RootElement.GetProperty("sid").GetString()
                    };
                }

                _logger.LogError("[Twilio] SMS failed: {Response}", responseBody);
                return new SmsResultDto
                {
                    Success = false,
                    Error = responseBody
                };
            }
            catch (Exception ex)
            {
                return new SmsResultDto { Success = false, Error = ex.Message };
            }
        }

        private async Task<SmsResultDto> SendViaNetGsmAsync(string to, string message)
        {
            try
            {
                var username = _configuration["Sms:NetGsm:Username"];
                var password = _configuration["Sms:NetGsm:Password"];
                var header = _configuration["Sms:NetGsm:Header"] ?? "SMARTCAMPUS";

                if (string.IsNullOrEmpty(username))
                {
                    return new SmsResultDto { Success = false, Error = "NetGSM credentials not configured" };
                }

                // Remove + for NetGSM
                var phone = to.TrimStart('+');

                var url = $"https://api.netgsm.com.tr/sms/send/get?usercode={username}&password={password}&gsmno={phone}&message={Uri.EscapeDataString(message)}&msgheader={header}";

                var response = await _httpClient.GetAsync(url);
                var result = await response.Content.ReadAsStringAsync();

                // NetGSM returns numeric codes
                if (result.StartsWith("00") || result.StartsWith("01") || result.StartsWith("02"))
                {
                    _logger.LogInformation("[NetGSM] SMS sent to {Phone}, Result: {Result}", to, result);
                    return new SmsResultDto
                    {
                        Success = true,
                        MessageId = result.Split(' ').LastOrDefault() ?? result
                    };
                }

                _logger.LogError("[NetGSM] SMS failed: {Result}", result);
                return new SmsResultDto
                {
                    Success = false,
                    Error = $"NetGSM Error: {result}"
                };
            }
            catch (Exception ex)
            {
                return new SmsResultDto { Success = false, Error = ex.Message };
            }
        }
    }
}
