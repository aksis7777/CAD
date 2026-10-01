using System.Text.RegularExpressions;

namespace MiniPdm.Domain.Objects;

public static partial class ObjectIdentity
{
    [GeneratedRegex(@"\A[А-ЯЁ]{4}\.[0-9]{6}\.[0-9]{3}\z", RegexOptions.CultureInvariant)]
    private static partial Regex DesignationPattern();

    public static bool IsValidDesignation(string designation) => DesignationPattern().IsMatch(designation);

    public static string NormalizeStandardName(string name) =>
        Regex.Replace(name.Trim(), @"\s+", " ").ToUpperInvariant();
}
