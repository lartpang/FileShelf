using System.IO;
using System.Text.Json;
using FileShelf.Win.Models;

namespace FileShelf.Win.Services;

public sealed class SettingsService
{
    private static readonly HashSet<string> ValidDockModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "LeftTop",
        "LeftCenter",
        "LeftBottom",
        "RightTop",
        "RightCenter",
        "RightBottom"
    };

    private static readonly HashSet<string> ValidLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        UiText.English,
        UiText.Chinese
    };

    private readonly string _settingsPath;
    private readonly LoggerService _logger;

    public SettingsService(LoggerService logger)
    {
        _logger = logger;
        _settingsPath = PortablePaths.SettingsPath;

        try
        {
            Directory.CreateDirectory(PortablePaths.DataDirectory);
        }
        catch (Exception ex)
        {
            _logger.Error("Portable data directory unavailable", ex);
        }
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            var defaults = CreateDefaultSettings();
            Normalize(defaults);
            TrySave(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            Normalize(settings);
            return settings;
        }
        catch
        (Exception ex)
        {
            _logger.Error("Settings load failed; defaults restored", ex);
            var brokenPath = Path.Combine(
                Path.GetDirectoryName(_settingsPath)!,
                $"settings.broken.{DateTime.Now:yyyyMMddHHmmss}.json");
            try
            {
                File.Move(_settingsPath, brokenPath, overwrite: true);
            }
            catch (Exception moveException)
            {
                _logger.Error("Broken settings backup failed", moveException);
            }

            var defaults = CreateDefaultSettings();
            TrySave(defaults);
            return defaults;
        }
    }

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_settingsPath, json);
    }

    private void TrySave(AppSettings settings)
    {
        try
        {
            Save(settings);
        }
        catch (Exception ex)
        {
            _logger.Error("Settings save failed", ex);
        }
    }

    private void Normalize(AppSettings settings)
    {
        var changed = false;
        if (!string.Equals(settings.TriggerMode, "Manual", StringComparison.OrdinalIgnoreCase))
        {
            settings.TriggerMode = "Manual";
            changed = true;
        }

        if (settings.EnableDragTrigger)
        {
            settings.EnableDragTrigger = false;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.DesignProfile))
        {
            settings.DesignProfile = "YoinkEdge";
            changed = true;
        }

        if (!ValidLanguages.Contains(settings.LanguageCode))
        {
            settings.LanguageCode = UiText.English;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.ShelfDockMode) || !ValidDockModes.Contains(settings.ShelfDockMode))
        {
            settings.ShelfDockMode = "RightCenter";
            changed = true;
        }

        if (changed)
        {
            TrySave(settings);
        }
    }

    private static AppSettings CreateDefaultSettings()
    {
        return new AppSettings
        {
            DesignProfile = "YoinkEdge",
            EnableDragTrigger = false,
            TriggerMode = "Manual",
            ShelfDockMode = "RightCenter"
        };
    }
}
