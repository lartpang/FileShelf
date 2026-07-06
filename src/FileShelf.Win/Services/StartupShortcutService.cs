using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FileShelf.Win.Services;

public sealed class StartupShortcutService
{
    private const string ShortcutFileName = "FileShelf.Win.lnk";
    private const uint StgmRead = 0;

    private readonly string _shortcutPath;

    public StartupShortcutService()
    {
        _shortcutPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            ShortcutFileName);
    }

    public bool IsEnabled()
    {
        return File.Exists(_shortcutPath) && IsOwnedShortcut(_shortcutPath);
    }

    public void SetEnabled(bool enabled)
    {
        SetEnabled(enabled, UiText.English);
    }

    public void SetEnabled(bool enabled, string languageCode)
    {
        if (enabled)
        {
            Enable(languageCode);
            return;
        }

        Disable();
    }

    private void Enable(string languageCode)
    {
        var targetPath = GetCurrentExecutablePath();
        var startupDirectory = Path.GetDirectoryName(_shortcutPath);
        if (!string.IsNullOrWhiteSpace(startupDirectory))
        {
            Directory.CreateDirectory(startupDirectory);
        }

        if (File.Exists(_shortcutPath) && !IsOwnedShortcut(_shortcutPath))
        {
            throw new StartupShortcutConflictException(_shortcutPath);
        }

        CreateShortcut(_shortcutPath, targetPath, UiText.Get(languageCode, "StartupShortcutDescription"));
    }

    private void Disable()
    {
        if (!File.Exists(_shortcutPath) || !IsOwnedShortcut(_shortcutPath))
        {
            return;
        }

        File.Delete(_shortcutPath);
    }

    private static string GetCurrentExecutablePath()
    {
        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
        {
            return Path.GetFullPath(Environment.ProcessPath);
        }

        var assemblyPath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrWhiteSpace(assemblyPath))
        {
            return Path.GetFullPath(assemblyPath);
        }

        throw new StartupExecutablePathUnavailableException();
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string description)
    {
        var link = (IShellLinkW)new ShellLink();
        try
        {
            link.SetPath(targetPath);
            link.SetWorkingDirectory(Path.GetDirectoryName(targetPath) ?? AppContext.BaseDirectory);
            link.SetDescription(description);
            link.SetIconLocation(targetPath, 0);

            ((IPersistFile)link).Save(shortcutPath, true);
        }
        finally
        {
            Marshal.FinalReleaseComObject(link);
        }
    }

    private static bool IsOwnedShortcut(string shortcutPath)
    {
        var shortcut = TryReadShortcut(shortcutPath);
        if (shortcut is null)
        {
            return false;
        }

        return IsOwnedDescription(shortcut.Description)
            || PathsEqual(shortcut.TargetPath, GetCurrentExecutablePath());
    }

    private static bool IsOwnedDescription(string description)
    {
        return string.Equals(description, UiText.Get(UiText.English, "StartupShortcutDescription"), StringComparison.Ordinal)
            || string.Equals(description, UiText.Get(UiText.Chinese, "StartupShortcutDescription"), StringComparison.Ordinal);
    }

    private static ShortcutInfo? TryReadShortcut(string shortcutPath)
    {
        var link = (IShellLinkW)new ShellLink();
        try
        {
            ((IPersistFile)link).Load(shortcutPath, StgmRead);

            var targetPath = new StringBuilder(4096);
            link.GetPath(targetPath, targetPath.Capacity, IntPtr.Zero, 0);

            var description = new StringBuilder(1024);
            link.GetDescription(description, description.Capacity);

            return new ShortcutInfo(targetPath.ToString(), description.ToString());
        }
        catch
        {
            return null;
        }
        finally
        {
            Marshal.FinalReleaseComObject(link);
        }
    }

    private static bool PathsEqual(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private sealed record ShortcutInfo(string TargetPath, string Description);

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
            int cchMaxPath,
            IntPtr pfd,
            uint fFlags);

        void GetIDList(out IntPtr ppidl);

        void SetIDList(IntPtr pidl);

        void GetDescription(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName,
            int cchMaxName);

        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        void GetWorkingDirectory(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir,
            int cchMaxPath);

        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

        void GetArguments(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs,
            int cchMaxPath);

        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

        void GetHotkey(out short pwHotkey);

        void SetHotkey(short wHotkey);

        void GetShowCmd(out int piShowCmd);

        void SetShowCmd(int iShowCmd);

        void GetIconLocation(
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
            int cchIconPath,
            out int piIcon);

        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

        void Resolve(IntPtr hwnd, uint fFlags);

        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);

        void IsDirty();

        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);

        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);

        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }
}

public sealed class StartupShortcutConflictException : IOException
{
    public StartupShortcutConflictException(string shortcutPath)
        : base($"Startup shortcut is already used by another app: {shortcutPath}")
    {
        ShortcutPath = shortcutPath;
    }

    public string ShortcutPath { get; }
}

public sealed class StartupExecutablePathUnavailableException : IOException
{
    public StartupExecutablePathUnavailableException()
        : base(nameof(StartupExecutablePathUnavailableException))
    {
    }
}
