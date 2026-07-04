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
            "AltDrag" => zh ? "按住 Alt 拖拽时显示暂存架" : "Show shelf while Alt-dragging",
            "AltDragHint" => zh ? "可选全局鼠标 Hook；退出时释放，不读取文件路径。" : "Optional global mouse hook. It is released on exit and never reads file paths.",
            "Trigger" => zh ? "触发方式" : "Trigger",
            "Position" => zh ? "窗口位置" : "Position",
            "Size" => zh ? "窗口大小" : "Size",
            "IgnoredApps" => zh ? "忽略应用" : "Ignored apps",
            "IgnoredAppsHint" => zh ? "输入进程名，用逗号分隔，例如 devenv, chrome。" : "Enter process names separated by commas, for example devenv, chrome.",
            "TriggerHint" => zh ? "手动模式只通过托盘/快捷键显示；其他模式会注册轻量鼠标 Hook，退出时释放。" : "Manual uses tray/hotkey only; other modes register a lightweight mouse hook that is released on exit.",
            "TriggerHintManual" => zh ? "只通过托盘图标、快捷键或手动操作显示暂存架；不注册鼠标 Hook。" : "Shows from tray, hotkey, or manual actions only. No mouse hook is registered.",
            "TriggerHintAnyDrag" => zh ? "开始拖拽后立即显示暂存架；适合希望它主动出现的工作流。" : "Shows as soon as dragging starts. Best when you want the shelf to appear proactively.",
            "TriggerHintAltDrag" => zh ? "只有按住 Alt 拖拽时显示暂存架；干扰最少。" : "Shows only while Alt-dragging. This keeps interruptions low.",
            "TriggerHintDockZone" => zh ? "从暂存架所在边缘区域开始拖拽时显示；接近 Yoink 的边缘暂存架习惯。" : "Shows when dragging starts from the shelf edge area, similar to an edge shelf workflow.",
            "TriggerHintScreenEdge" => zh ? "拖到当前屏幕的暂存架边缘时显示；适合多显示器使用。" : "Shows when dragging reaches the shelf edge on the current screen. Good for multi-monitor use.",
            "TriggerManual" => zh ? "手动显示" : "Manual",
            "TriggerAnyDrag" => zh ? "开始拖拽时立即显示" : "Show immediately while dragging",
            "TriggerAltDrag" => zh ? "按住 Alt 拖拽时" : "Alt-drag",
            "TriggerDockZone" => zh ? "从暂存架区域开始拖拽时" : "Start dragging from shelf area",
            "TriggerScreenEdge" => zh ? "拖到当前屏幕边缘时" : "Drag to current screen edge",
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
            "Hotkey" => zh ? "快捷键" : "Hotkey",
            "Status" => zh ? "状态" : "Status",
            "Registered" => zh ? "已注册" : "Registered",
            "Unavailable" => zh ? "不可用" : "Unavailable",
            "Data" => zh ? "数据" : "Data",
            "Log" => zh ? "日志" : "Log",
            "ClearShelf" => zh ? "清空暂存架" : "Clear shelf",
            "ClearUnpinned" => zh ? "清空未固定项" : "Clear Unpinned",
            "Cancel" => zh ? "取消" : "Cancel",
            "Save" => zh ? "保存" : "Save",
            "Hide" => zh ? "隐藏" : "Hide",
            "Clear" => zh ? "清空" : "Clear",
            "Collapse" => zh ? "收起" : "Collapse",
            "CollapseToTray" => zh ? "收起到托盘" : "Collapse to tray",
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

    public static string FormatIgnoreApp(string languageCode, string processName)
    {
        return IsChinese(languageCode)
            ? $"忽略 {processName}"
            : $"Ignore {processName}";
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
}
