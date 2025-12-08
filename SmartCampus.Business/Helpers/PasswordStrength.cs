using System.Text.RegularExpressions;

namespace SmartCampus.Business.Helpers
{
    public static class PasswordStrength
    {
        public static (int Score, string Feedback) Evaluate(string password)
        {
            if (string.IsNullOrEmpty(password)) return (0, "Password is empty");

            int score = 0;
            if (password.Length >= 8) score++;
            if (password.Length >= 12) score++;
            if (Regex.IsMatch(password, @"[0-9]")) score++;
            if (Regex.IsMatch(password, @"[a-z]") && Regex.IsMatch(password, @"[A-Z]")) score++;
            if (Regex.IsMatch(password, @"[\W_]")) score++;

            string feedback = score switch
            {
                0 or 1 => "Week",
                2 or 3 => "Medium",
                4 => "Strong",
                5 => "Very Strong",
                _ => "Weak"
            };

            return (score, feedback);
        }
    }
}
