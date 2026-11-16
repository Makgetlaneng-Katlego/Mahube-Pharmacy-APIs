using System.Text.RegularExpressions;

namespace Mahube_Pharmacy.Helpers;

public static class EmailHelper
{
    public static bool IsValid(string email) => Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
}

public static class PhoneHelper
{
    public static bool TryNormalize(string phone, out string normalized)
    {
        normalized = phone.Replace(" ", "").Replace("-", "");
        if (normalized.StartsWith("0")) normalized = "+27" + normalized.Substring(1);
        return Regex.IsMatch(normalized, @"^\+[1-9]\d{1,14}$");
    }
}

public static class PasswordHelper
{
    public static bool IsStrong(string password)
    {
        return password.Length >= 8 &&
               password.Any(char.IsDigit) &&
               password.Any(c => !char.IsLetterOrDigit(c));
    }
}