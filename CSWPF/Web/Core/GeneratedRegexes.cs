using System.Text.RegularExpressions;

namespace CSWPF.Web.Core;

internal static partial class GeneratedRegexes {
    private const string CdKeyPattern = @"^[0-9A-Z]{4,7}-[0-9A-Z]{4,7}-[0-9A-Z]{4,7}(?:(?:-[0-9A-Z]{4,7})?(?:-[0-9A-Z]{4,7}))?$";
    private const string DecimalPattern = @"[0-9\.,]+";
    private const RegexOptions DefaultOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
    private const string DigitsPattern = @"\d+";
    private const string NonAsciiPattern = @"[^\u0000-\u007F]+";

#if NETFRAMEWORK
	internal static Regex CdKey() => new(CdKeyPattern, DefaultOptions);
	internal static Regex Decimal() => new(DecimalPattern, DefaultOptions);
	internal static Regex Digits() => new(DigitsPattern, DefaultOptions);
	internal static Regex NonAscii() => new(NonAsciiPattern, DefaultOptions);
#else
    [GeneratedRegex(CdKeyPattern, DefaultOptions)]
    internal static partial Regex CdKey();

    [GeneratedRegex(DecimalPattern, DefaultOptions)]
    internal static partial Regex Decimal();

    [GeneratedRegex(DigitsPattern, DefaultOptions)]
    internal static partial Regex Digits();

    [GeneratedRegex(NonAsciiPattern, DefaultOptions)]
    internal static partial Regex NonAscii();
#endif
}