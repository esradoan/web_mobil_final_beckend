namespace SmartCampus.Business.DTOs
{
    public class RegisterResponseDto
    {
        public UserDto User { get; set; } = null!;
        public string VerificationUrl { get; set; } = string.Empty;
        public string? VerificationToken { get; set; } // For development/mock email
    }
}

