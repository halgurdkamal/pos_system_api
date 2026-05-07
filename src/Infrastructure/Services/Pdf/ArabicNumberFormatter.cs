using System.Globalization;

namespace pos_system_api.Infrastructure.Services.Pdf;

/// <summary>
/// Utility for formatting numbers in Arabic/English
/// </summary>
public static class ArabicNumberFormatter
{
    private static readonly Dictionary<char, char> EnglishToArabicDigits = new()
    {
        {'0', '٠'}, {'1', '١'}, {'2', '٢'}, {'3', '٣'}, {'4', '٤'},
        {'5', '٥'}, {'6', '٦'}, {'7', '٧'}, {'8', '٨'}, {'9', '٩'}
    };

    /// <summary>
    /// Convert English digits to Arabic digits
    /// </summary>
    public static string ToArabicDigits(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = input.ToCharArray();
        for (int i = 0; i < result.Length; i++)
        {
            if (EnglishToArabicDigits.ContainsKey(result[i]))
            {
                result[i] = EnglishToArabicDigits[result[i]];
            }
        }
        return new string(result);
    }

    /// <summary>
    /// Format number with currency and locale
    /// </summary>
    public static string FormatCurrency(decimal amount, string currency, bool useArabicDigits)
    {
        var formatted = amount.ToString("N0", CultureInfo.InvariantCulture);
        
        if (useArabicDigits)
        {
            formatted = ToArabicDigits(formatted);
        }

        return $"{formatted} {currency}";
    }

    /// <summary>
    /// Format number with decimal places
    /// </summary>
    public static string FormatNumber(decimal number, int decimals, bool useArabicDigits)
    {
        var format = decimals > 0 ? $"N{decimals}" : "N0";
        var formatted = number.ToString(format, CultureInfo.InvariantCulture);
        
        if (useArabicDigits)
        {
            formatted = ToArabicDigits(formatted);
        }

        return formatted;
    }

    /// <summary>
    /// Get text direction based on language
    /// </summary>
    public static bool IsRightToLeft(string language)
    {
        return language.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
    }
}
