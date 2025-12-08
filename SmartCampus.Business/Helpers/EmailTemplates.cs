namespace SmartCampus.Business.Helpers
{
    public static class EmailTemplates
    {
        public static string GetVerificationEmail(string name, string verificationUrl)
        {
            return $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <div style='background-color: #f4f4f4; padding: 20px;'>
        <div style='background-color: white; padding: 20px; border-radius: 5px;'>
            <h2 style='color: #333;'>Welcome to Smart Campus!</h2>
            <p>Hi {name},</p>
            <p>Please verify your email address by clicking the link below:</p>
            <a href='{verificationUrl}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Verify Email</a>
            <p>If you didn't create an account, you can safely ignore this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        public static string GetPasswordResetEmail(string resetUrl)
        {
            return $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <div style='background-color: #f4f4f4; padding: 20px;'>
        <div style='background-color: white; padding: 20px; border-radius: 5px;'>
            <h2 style='color: #333;'>Reset Your Password</h2>
            <p>You requested a password reset. Click the button below to reset it:</p>
            <a href='{resetUrl}' style='dbackground-color: #dc3545; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a>
            <p>If you didn't request this, ignore this email.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
