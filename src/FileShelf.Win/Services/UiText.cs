using System.Globalization;
using System.Resources;

namespace FileShelf.Win.Services;

public static class UiText
{
    public const string English = "en";
    public const string Chinese = "zh-CN";

    private static readonly ResourceManager Strings = new(
        "FileShelf.Win.Resources.Strings",
        typeof(UiText).Assembly);

    private static readonly CultureInfo NeutralCulture = CultureInfo.InvariantCulture;
    private static readonly CultureInfo ChineseCulture = CultureInfo.GetCultureInfo(Chinese);

    public static bool IsChinese(string languageCode)
    {
        return string.Equals(languageCode, Chinese, StringComparison.OrdinalIgnoreCase);
    }

    public static string Get(string languageCode, string key)
    {
        return Strings.GetString(key, GetCulture(languageCode)) ?? key;
    }

    public static string FormatTrayTooltip(string languageCode, int itemCount)
    {
        return Format(
            languageCode,
            "TrayTooltip",
            itemCount,
            Get(languageCode, itemCount == 1 ? "Item" : "Items"));
    }

    public static string FormatItemCount(string languageCode, int itemCount)
    {
        return Format(
            languageCode,
            "ItemCount",
            itemCount,
            Get(languageCode, itemCount == 1 ? "Item" : "Items"));
    }

    public static string FormatSelection(string languageCode, int selectedCount, int itemCount)
    {
        return Format(
            languageCode,
            "Selection",
            selectedCount,
            itemCount,
            Get(languageCode, itemCount == 1 ? "Item" : "Items"));
    }

    public static string FormatGroupName(string languageCode, int count)
    {
        return Format(languageCode, "GroupName", count);
    }

    public static string FormatGroupSummary(string languageCode, int count)
    {
        return Format(languageCode, "GroupSummary", count);
    }

    public static string FormatCount(string languageCode, string key, int count)
    {
        return key switch
        {
            "ReleaseToStageCount" => Format(languageCode, key, count, Get(languageCode, count == 1 ? "Item" : "Items")),
            _ => Get(languageCode, key)
        };
    }

    public static string FormatVersion(string languageCode, string key, string version)
    {
        return key switch
        {
            "UpdateAvailable" or "UpdateCurrent" => Format(languageCode, key, version),
            _ => Get(languageCode, key)
        };
    }

    public static string FormatPath(string languageCode, string key, string path)
    {
        return Format(languageCode, key, path);
    }

    private static string Format(string languageCode, string key, params object[] args)
    {
        var culture = GetCulture(languageCode);
        return string.Format(culture, Get(languageCode, key), args);
    }

    private static CultureInfo GetCulture(string languageCode)
    {
        return IsChinese(languageCode) ? ChineseCulture : NeutralCulture;
    }
}
