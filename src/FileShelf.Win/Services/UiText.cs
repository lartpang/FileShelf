namespace FileShelf.Win.Services;

public static class UiText
{
    public const string English = "en";
    public const string Chinese = "zh-CN";

    public static bool IsChinese(string languageCode)
    {
        return string.Equals(languageCode, Chinese, StringComparison.OrdinalIgnoreCase);
    }

    public static string Get(string languageCode, string key)
    {
        var zh = IsChinese(languageCode);
        return key switch
        {
            "PortableTitle" => zh ? "FileShelf（便携版）" : "FileShelf (Portable)",
            "Settings" => zh ? "设置" : "Settings",
            "Language" => zh ? "界面语言" : "Language",
            "English" => "English",
            "Chinese" => "中文",
            "Size" => zh ? "窗口大小" : "Size",
            "LeftTop" => zh ? "左侧，上方" : "Left, top",
            "LeftCenter" => zh ? "左侧，居中" : "Left, center",
            "LeftBottom" => zh ? "左侧，下方" : "Left, bottom",
            "RightTop" => zh ? "右侧，上方" : "Right, top",
            "RightCenter" => zh ? "右侧，居中" : "Right, center",
            "RightBottom" => zh ? "右侧，下方" : "Right, bottom",
            "SizeSmall" => zh ? "小" : "Small",
            "SizeMedium" => zh ? "中" : "Medium",
            "SizeLarge" => zh ? "大" : "Large",
            "SizeCustom" => zh ? "自定义" : "Custom",
            "Data" => zh ? "数据" : "Data",
            "Log" => zh ? "日志" : "Log",
            "BrowseDataPath" => zh ? "选择数据文件夹" : "Choose data folder",
            "BrowseLogPath" => zh ? "选择日志文件" : "Choose log file",
            "ClearShelf" => zh ? "清空暂存架" : "Clear shelf",
            "ClearUnpinned" => zh ? "清空未固定项" : "Clear Unpinned",
            "Cancel" => zh ? "取消" : "Cancel",
            "Save" => zh ? "保存" : "Save",
            "Hide" => zh ? "隐藏" : "Hide",
            "Clear" => zh ? "清空" : "Clear",
            "Collapse" => zh ? "收起" : "Collapse",
            "CollapseToTray" => zh ? "收起到托盘" : "Collapse to tray",
            "CollapseToIcon" => zh ? "收起为悬浮图标" : "Collapse to floating icon",
            "IconTooltip" => zh ? "双击打开 FileShelf，拖入文件可暂存" : "Double-click to open FileShelf; drop files to stage",
            "AddToShelf" => zh ? "添加到暂存架" : "Add to Shelf",
            "AddFiles" => zh ? "添加文件" : "Add Files",
            "AddFolder" => zh ? "添加文件夹" : "Add Folder",
            "RestoreRemoved" => zh ? "恢复最近移除" : "Restore Removed",
            "DropFiles" => zh ? "拖入文件" : "Drop files here",
            "DropMoreFiles" => zh ? "继续拖入文件" : "Drop more files",
            "AlreadyStaged" => zh ? "已在暂存架中" : "Already staged",
            "NoValidFiles" => zh ? "未找到可暂存文件" : "No valid files",
            "DragAllFromCount" => zh ? "拖动这里可拖出全部暂存项" : "Drag this handle to drag all staged items out",
            "DragSelectionFromCount" => zh ? "拖动这里可拖出所选暂存项" : "Drag this handle to drag selected staged items out",
            "PathOnly" => zh ? "文件保留在原处，FileShelf 只保存路径。" : "Files stay in place. FileShelf only keeps paths.",
            "ReleaseToStage" => zh ? "松开以暂存" : "Release to stage",
            "ReleaseToStageShort" => zh ? "松开暂存" : "Release",
            "ReleaseToStageHint" => zh ? "只记录路径，源文件保持原位。" : "Only paths are stored. Source files stay in place.",
            "Items" => zh ? "项" : "items",
            "Item" => zh ? "项" : "item",
            "About" => zh ? "关于" : "About",
            "Exit" => zh ? "退出" : "Exit",
            "Pin" => zh ? "固定" : "Pin",
            "Unpin" => zh ? "取消固定" : "Unpin",
            "StackSelected" => zh ? "叠放所选项" : "Stack Selected",
            "SelectAllForDragOut" => zh ? "全选以拖出" : "Select all for drag-out",
            "SplitGroup" => zh ? "拆分组" : "Split Group",
            "Open" => zh ? "打开" : "Open",
            "OpenAll" => zh ? "打开全部" : "Open All",
            "RevealInExplorer" => zh ? "在资源管理器中显示" : "Reveal in Explorer",
            "Remove" => zh ? "移除" : "Remove",
            "FileNotFound" => zh ? "文件不存在" : "File not found",
            "SomeFilesMissing" => zh ? "部分文件不存在" : "Some files are missing",
            "AboutTitle" => zh ? "关于 FileShelf" : "About FileShelf",
            "AboutSubtitle" => zh ? "轻量文件拖放暂存架" : "Lightweight file drag-and-drop shelf",
            "Version" => zh ? "版本" : "Version",
            "Runtime" => zh ? "运行时" : "Runtime",
            "Update" => zh ? "更新" : "Update",
            "UpdateChecking" => zh ? "正在检查更新..." : "Checking for updates...",
            "UpdateNotConfigured" => zh ? "未配置更新源" : "Update source is not configured",
            "UpdateCheckFailed" => zh ? "检查更新失败" : "Update check failed",
            "OpenRelease" => zh ? "打开发布页" : "Open Release",
            "Mode" => zh ? "模式" : "Mode",
            "Development" => zh ? "开发" : "Development",
            "DevelopmentValue" => zh ? "C# / WPF 便携桌面程序" : "C# / WPF portable desktop app",
            "Scope" => zh ? "范围" : "Scope",
            "ScopeValue" => zh ? "只保存路径；不复制、移动、删除源文件；不管理剪贴板。" : "Stores paths only; never copies, moves, or deletes source files; no clipboard management.",
            "Close" => zh ? "关闭" : "Close",
            "SettingsSaveFailed" => zh ? "设置保存失败。" : "Settings could not be saved.",
            _ => key
        };
    }

    public static string FormatTrayTooltip(string languageCode, int itemCount)
    {
        return IsChinese(languageCode)
            ? $"FileShelf - 当前暂存 {itemCount} 项"
            : $"FileShelf - {itemCount} {(itemCount == 1 ? "item" : "items")}";
    }

    public static string FormatItemCount(string languageCode, int itemCount)
    {
        return IsChinese(languageCode)
            ? $"{itemCount} 项"
            : $"{itemCount} {(itemCount == 1 ? "item" : "items")}";
    }

    public static string FormatSelection(string languageCode, int selectedCount, int itemCount)
    {
        return IsChinese(languageCode)
            ? $"已选 {selectedCount} 项 / 共 {itemCount} 项"
            : $"{selectedCount} selected / {itemCount} {(itemCount == 1 ? "item" : "items")}";
    }

    public static string FormatGroupName(string languageCode, int count)
    {
        return IsChinese(languageCode)
            ? $"{count} 个文件"
            : $"{count} files";
    }

    public static string FormatGroupSummary(string languageCode, int count)
    {
        return IsChinese(languageCode)
            ? $"一组 {count} 个暂存路径"
            : $"{count} staged paths";
    }

    public static string FormatCount(string languageCode, string key, int count)
    {
        var zh = IsChinese(languageCode);
        return key switch
        {
            "ReleaseToStageCount" => zh
                ? $"松开以暂存 {count} 项"
                : $"Release to stage {count} {(count == 1 ? "item" : "items")}",
            _ => Get(languageCode, key)
        };
    }

    public static string FormatVersion(string languageCode, string key, string version)
    {
        var zh = IsChinese(languageCode);
        return key switch
        {
            "UpdateAvailable" => zh ? $"发现新版本 {version}" : $"New version available: {version}",
            "UpdateCurrent" => zh ? $"已是最新版本（{version}）" : $"Up to date ({version})",
            _ => Get(languageCode, key)
        };
    }
}
