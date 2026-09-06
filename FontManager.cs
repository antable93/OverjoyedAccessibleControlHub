namespace OverjoyedVersion3;

/// <summary>
/// Unified, platform-agnostic entry point for font-related queries. Each platform's system font
/// lookup lives in its own NativeMethods/native interop, guarded here by #if so a future MacCatalyst
/// implementation can slot in alongside Windows.
/// </summary>
public static class FontManager
{
    public static IReadOnlyList<string> GetSystemFontFamilies()
    {
#if WINDOWS
        return NativeMethods.GetSystemFontFamilies();
#else
        return [];
#endif
    }
}
