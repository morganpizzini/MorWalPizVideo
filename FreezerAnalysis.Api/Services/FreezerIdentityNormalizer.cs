using System.Globalization;
using System.Text;

namespace FreezerAnalysis.Api.Services;

internal static class FreezerIdentityNormalizer
{
    public static string NormalizeName(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        return new string(decomposed
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }
}
