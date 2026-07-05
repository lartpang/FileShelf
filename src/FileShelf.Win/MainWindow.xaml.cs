using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using FileShelf.Win.Models;
using FileShelf.Win.Services;
using Forms = System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;
using WpfButton = System.Windows.Controls.Button;
using WpfContextMenu = System.Windows.Controls.ContextMenu;
using WpfMenuItem = System.Windows.Controls.MenuItem;
using WpfSeparator = System.Windows.Controls.Separator;

namespace FileShelf.Win;

public partial class MainWindow : Window
{
    private const double IconWindowSize = 64;
    private const double IconSurfaceSize = 52;
    private const double DefaultPanelWidth = 335;
    private const double PanelMinimumWidth = 220;
    private const double PanelMinimumHeight = 320;
    private const double ResizeBorderThickness = 6;
    private const int WmNcHitTest = 0x0084;
    private const int HtClient = 1;
    private const int HtLeft = 10;
    private const int HtRight = 11;
    private const int HtTop = 12;
    private const int HtTopLeft = 13;
    private const int HtTopRight = 14;
    private const int HtBottom = 15;
    private const int HtBottomLeft = 16;
    private const int HtBottomRight = 17;

    private readonly DragDropService _dragDropService = new();
    private readonly LoggerService _logger;
    private readonly List<RemovedShelfBatch> _removedShelfBatches = new();
    private readonly ShelfStateService _shelfStateService;
    private AppSettings _settings;
    private bool _allowClose;
    private bool _dragAllFromCount;
    private bool _isHidingToTray;
    private bool _isDragActive;
    private bool _isPanelOpen;
    private bool _isIconMouseDown;
    private bool _isDraggingIcon;
    private bool _isDraggingOut;
    private bool _hasIconPosition;
    private int _dropNoticeToken;
    private int _hideAnimationToken;
    private double _iconLeft;
    private double _iconTop;
    private double _iconDragStartLeft;
    private double _iconDragStartTop;
    private double? _runtimePanelWidth;
    private double? _runtimePanelHeight;
    private bool _isSettingPanelSize;
    private WpfPoint _dragStartPoint;
    private WpfPoint _iconDragStartScreenPoint;
    private ShelfItem? _dragItem;
    private ShelfItem[] _dragSelectionSnapshot = Array.Empty<ShelfItem>();

    public MainWindow(
        AppSettings settings,
        LoggerService logger,
        ShelfStateService shelfStateService)
    {
        InitializeComponent();
        LoadFloatingIconLogo();
        _settings = settings;
        _logger = logger;
        _shelfStateService = shelfStateService;
        Width = IconWindowSize;
        Height = IconWindowSize;
        DataContext = this;
        Items.CollectionChanged += (_, _) => UpdateEmptyState();
        SizeChanged += MainWindow_SizeChanged;
        LoadShelfState();
        ApplySettings(settings);
        UpdateEmptyState();
    }

    private void LoadFloatingIconLogo()
    {
        var resourcePath = Path.Combine(AppContext.BaseDirectory, "Resources");
        var imageSource = TryLoadLargestIconFrame(Path.Combine(resourcePath, "FileShelfIconNotion.ico"));
        if (imageSource is null)
        {
            return;
        }

        IconLogoImage.Source = imageSource;
    }

    private static ImageSource? TryLoadLargestIconFrame(string iconPath)
    {
        if (!File.Exists(iconPath))
        {
            return null;
        }

        using var stream = File.OpenRead(iconPath);
        var decoder = new IconBitmapDecoder(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);
        var frame = decoder.Frames
            .OrderByDescending(candidate => candidate.PixelWidth * candidate.PixelHeight)
            .FirstOrDefault();
        frame?.Freeze();
        return frame;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (PresentationSource.FromVisual(this) is HwndSource source)
        {
            source.AddHook(WndProc);
        }
    }

    public ObservableCollection<ShelfItem> Items { get; } = new();

    public event EventHandler<int>? ItemCountChanged;

    public int ItemCount => Items.Sum(item => item.FilePaths.Count);

    public bool IsHidingToTray => _isHidingToTray;

    public bool IsPanelOpen => _isPanelOpen;

    public void ShowIconAtDefault()
    {
        if (!_hasIconPosition)
        {
            var area = GetWorkArea(null);
            SetIconPosition(area.Right - IconWindowSize - 8, area.Top + (area.Height - IconWindowSize) / 2, area);
        }

        SetIconMode();
        if (!IsVisible)
        {
            Show();
        }

        Topmost = true;
    }

    public void ShowPanel(bool activate = true, WpfPoint? triggerPosition = null)
    {
        CancelHideAnimation();
        var anchor = triggerPosition ?? new WpfPoint(_iconLeft + IconWindowSize / 2, _iconTop + IconWindowSize / 2);
        var area = GetWorkArea(anchor);
        SetPanelMode(area);
        PositionPanel(anchor, area);

        if (!IsVisible)
        {
            Show();
        }

        WindowState = WindowState.Normal;
        Topmost = true;
        if (activate)
        {
            Activate();
        }

        PlayShowAnimation();
    }

    public void PlayShowAnimation()
    {
        CancelHideAnimation();
        var area = GetWorkArea(new WpfPoint(Left + Width / 2, Top + Height / 2));
        var isLeft = Left < area.Left + area.Width / 2;
        var startOffset = isLeft ? -18 : 18;
        ShelfSlideTransform.X = startOffset;
        Opacity = 0.88;

        var duration = TimeSpan.FromMilliseconds(130);
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        ShelfSlideTransform.BeginAnimation(
            TranslateTransform.XProperty,
            new DoubleAnimation(0, duration) { EasingFunction = easing });
        BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(1, duration) { EasingFunction = easing });
    }

    public void CancelHideAnimation()
    {
        if (!_isHidingToTray)
        {
            return;
        }

        _hideAnimationToken++;
        _isHidingToTray = false;
        ShelfSlideTransform.BeginAnimation(TranslateTransform.XProperty, null);
        BeginAnimation(OpacityProperty, null);
        ShelfSlideTransform.X = 0;
        Opacity = 1;
    }

    public void HideToTray()
    {
        if (!IsVisible || _isHidingToTray || !_isPanelOpen)
        {
            return;
        }

        _isHidingToTray = true;
        var animationToken = ++_hideAnimationToken;
        var isLeft = _iconLeft < Left;
        var endOffset = isLeft ? -18 : 18;
        var duration = TimeSpan.FromMilliseconds(110);
        var easing = new CubicEase { EasingMode = EasingMode.EaseIn };
        var slideAnimation = new DoubleAnimation(endOffset, duration) { EasingFunction = easing };
        slideAnimation.Completed += (_, _) =>
        {
            if (animationToken != _hideAnimationToken)
            {
                return;
            }

            SetIconMode();
            ShelfSlideTransform.BeginAnimation(TranslateTransform.XProperty, null);
            BeginAnimation(OpacityProperty, null);
            ShelfSlideTransform.X = 0;
            Opacity = 1;
            _isHidingToTray = false;
        };

        ShelfSlideTransform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
        BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0.88, duration) { EasingFunction = easing });
    }

    private void SetIconMode()
    {
        _isPanelOpen = false;
        MinWidth = IconWindowSize;
        MinHeight = IconWindowSize;
        ResizeMode = ResizeMode.NoResize;
        ShelfShell.Visibility = Visibility.Collapsed;
        IconShell.Visibility = Visibility.Visible;
        Width = IconWindowSize;
        Height = IconWindowSize;
        if (!_hasIconPosition)
        {
            var area = GetWorkArea(null);
            SetIconPosition(area.Right - IconWindowSize - 8, area.Top + (area.Height - IconWindowSize) / 2, area);
        }
        else
        {
            Left = _iconLeft;
            Top = _iconTop;
        }

        ApplyDropVisualState(false, force: true);
    }

    private void SetPanelMode(Rect area)
    {
        _isPanelOpen = true;
        MinWidth = PanelMinimumWidth;
        MinHeight = PanelMinimumHeight;
        ResizeMode = ResizeMode.CanResize;
        IconShell.Visibility = Visibility.Collapsed;
        ShelfShell.Visibility = Visibility.Visible;
        var targetWidth = _runtimePanelWidth ?? DefaultPanelWidth;
        var targetHeight = _runtimePanelHeight ?? area.Height / 3;
        _isSettingPanelSize = true;
        try
        {
            Width = Clamp(targetWidth, PanelMinimumWidth, area.Width - 16);
            Height = Clamp(targetHeight, PanelMinimumHeight, area.Height - 16);
        }
        finally
        {
            _isSettingPanelSize = false;
        }
    }

    private void PositionPanel(WpfPoint anchor, Rect area)
    {
        var openToLeft = anchor.X >= area.Left + area.Width / 2;
        var left = openToLeft
            ? anchor.X - Width + IconSurfaceSize / 2
            : anchor.X - IconSurfaceSize / 2;
        var top = anchor.Y - Height / 2;

        Left = Clamp(left, area.Left + 8, area.Right - Width - 8);
        Top = Clamp(top, area.Top + 8, area.Bottom - Height - 8);
        var dockMode = openToLeft ? "RightCenter" : "LeftCenter";
        ApplyDockVisual(dockMode);
    }

    private void SetIconPosition(double left, double top, Rect? workArea = null)
    {
        var area = workArea ?? GetWorkArea(new WpfPoint(left + IconWindowSize / 2, top + IconWindowSize / 2));
        _iconLeft = Clamp(left, area.Left, area.Right - IconWindowSize);
        _iconTop = Clamp(top, area.Top, area.Bottom - IconWindowSize);
        _hasIconPosition = true;
        if (!_isPanelOpen)
        {
            Left = _iconLeft;
            Top = _iconTop;
        }
    }

    private Rect GetWorkArea(WpfPoint? position)
    {
        if (position is null)
        {
            return SystemParameters.WorkArea;
        }

        var devicePosition = ToDeviceScreenPoint(position.Value);
        var screen = Forms.Screen.FromPoint(new System.Drawing.Point(
            (int)Math.Round(devicePosition.X),
            (int)Math.Round(devicePosition.Y)));
        var area = screen.WorkingArea;
        var topLeft = ToDipScreenPoint(new WpfPoint(area.Left, area.Top));
        var bottomRight = ToDipScreenPoint(new WpfPoint(area.Right, area.Bottom));
        return new Rect(topLeft, bottomRight);
    }

    private WpfPoint ToDipScreenPoint(WpfPoint screenPoint)
    {
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget is null
            ? screenPoint
            : source.CompositionTarget.TransformFromDevice.Transform(screenPoint);
    }

    private WpfPoint ToDeviceScreenPoint(WpfPoint screenPoint)
    {
        var source = PresentationSource.FromVisual(this);
        return source?.CompositionTarget is null
            ? screenPoint
            : source.CompositionTarget.TransformToDevice.Transform(screenPoint);
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        return maximum < minimum ? minimum : Math.Clamp(value, minimum, maximum);
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        if (_isPanelOpen)
        {
            var area = GetWorkArea(new WpfPoint(Left + Width / 2, Top + Height / 2));
            SetPanelMode(area);
            PositionPanel(new WpfPoint(_iconLeft + IconWindowSize / 2, _iconTop + IconWindowSize / 2), area);
        }
        else
        {
            SetIconMode();
        }

        ApplyLanguage();
    }

    public void ApplyLanguage()
    {
        Title = UiText.Get(_settings.LanguageCode, "PortableTitle");
        AddButton.FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets");
        AddButton.Content = "\uE710";
        AddButton.ToolTip = UiText.Get(_settings.LanguageCode, "AddToShelf");
        IconShell.ToolTip = UiText.Get(_settings.LanguageCode, "IconTooltip");
        CountDragHandle.ToolTip = UiText.Get(_settings.LanguageCode, "DragAllFromCount");
        EmptyTitleTextBlock.Text = UiText.Get(_settings.LanguageCode, "DropFiles");
        EmptyHintTextBlock.Text = UiText.Get(_settings.LanguageCode, "PathOnly");
        if (_isDragActive)
        {
            ApplyDropVisualState(true, force: true);
        }

        foreach (var item in Items)
        {
            item.ApplyLanguage(_settings.LanguageCode);
        }

        UpdateEmptyState();
    }

    public void ClearShelf()
    {
        var removed = RemoveShelfItemsAsBatch(Items.Where(item => !item.IsPinned).ToArray());
        if (removed > 0)
        {
            _logger.Info($"Shelf clear removed unpinned items; removedCount={removed}; totalCount={Items.Count}");
            PersistShelf();
        }
    }

    public void AllowClose()
    {
        _allowClose = true;
        Close();
    }

    public void SaveShelfState()
    {
        PersistShelf();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        HideToTray();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Topmost = true;
        ShowIconAtDefault();
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_isPanelOpen || _isSettingPanelSize)
        {
            return;
        }

        _runtimePanelWidth = Width;
        _runtimePanelHeight = Height;
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (_isPanelOpen && !_isDraggingOut)
        {
            HideToTray();
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmNcHitTest || !_isPanelOpen || _isHidingToTray)
        {
            return IntPtr.Zero;
        }

        var screenPoint = ToDipScreenPoint(GetScreenPoint(lParam));
        var x = screenPoint.X - Left;
        var y = screenPoint.Y - Top;
        var visualLeft = ShelfShell.Margin.Left;
        var visualTop = ShelfShell.Margin.Top;
        var visualRight = Width - ShelfShell.Margin.Right;
        var visualBottom = Height - ShelfShell.Margin.Bottom;
        var onLeft = x >= visualLeft && x <= visualLeft + ResizeBorderThickness;
        var onRight = x >= visualRight - ResizeBorderThickness && x <= visualRight;
        var onTop = y >= visualTop && y <= visualTop + ResizeBorderThickness;
        var onBottom = y >= visualBottom - ResizeBorderThickness && y <= visualBottom;

        if (onTop && onLeft)
        {
            handled = true;
            return new IntPtr(HtTopLeft);
        }

        if (onTop && onRight)
        {
            handled = true;
            return new IntPtr(HtTopRight);
        }

        if (onBottom && onLeft)
        {
            handled = true;
            return new IntPtr(HtBottomLeft);
        }

        if (onBottom && onRight)
        {
            handled = true;
            return new IntPtr(HtBottomRight);
        }

        if (onLeft)
        {
            handled = true;
            return new IntPtr(HtLeft);
        }

        if (onRight)
        {
            handled = true;
            return new IntPtr(HtRight);
        }

        if (onTop)
        {
            handled = true;
            return new IntPtr(HtTop);
        }

        if (onBottom)
        {
            handled = true;
            return new IntPtr(HtBottom);
        }

        handled = true;
        return new IntPtr(HtClient);
    }

    private static WpfPoint GetScreenPoint(IntPtr lParam)
    {
        var value = lParam.ToInt64();
        var x = (short)(value & 0xFFFF);
        var y = (short)((value >> 16) & 0xFFFF);
        return new WpfPoint(x, y);
    }

    private void IconShell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            _isIconMouseDown = false;
            _isDraggingIcon = false;
            IconShell.ReleaseMouseCapture();
            ShowPanel();
            e.Handled = true;
            return;
        }

        _isIconMouseDown = true;
        _isDraggingIcon = false;
        _iconDragStartScreenPoint = ToDipScreenPoint(PointToScreen(e.GetPosition(this)));
        _iconDragStartLeft = _iconLeft;
        _iconDragStartTop = _iconTop;
        IconShell.CaptureMouse();
        e.Handled = true;
    }

    private void IconShell_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (!_isIconMouseDown || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var screenPosition = ToDipScreenPoint(PointToScreen(e.GetPosition(this)));
        var deltaX = screenPosition.X - _iconDragStartScreenPoint.X;
        var deltaY = screenPosition.Y - _iconDragStartScreenPoint.Y;
        if (!_isDraggingIcon &&
            Math.Abs(deltaX) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(deltaY) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        _isDraggingIcon = true;
        SetIconPosition(_iconDragStartLeft + deltaX, _iconDragStartTop + deltaY);
        e.Handled = true;
    }

    private void IconShell_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isIconMouseDown && !_isDraggingIcon)
        {
            return;
        }

        _isIconMouseDown = false;
        _isDraggingIcon = false;
        IconShell.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void IconShell_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void TitleBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            return;
        }

        if (IsInsideButton(e.OriginalSource as DependencyObject) ||
            IsInsideElement(e.OriginalSource as DependencyObject, CountDragHandle))
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // DragMove can throw if the mouse button is released during event dispatch.
        }
    }

    private static bool IsInsideButton(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is WpfButton)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private static bool IsInsideElement(DependencyObject? source, DependencyObject target)
    {
        while (source is not null)
        {
            if (ReferenceEquals(source, target))
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private void ApplyDockVisual(string shelfDockMode)
    {
        var isLeft = shelfDockMode.StartsWith("Left", StringComparison.OrdinalIgnoreCase);
        ShelfShell.CornerRadius = isLeft
            ? new CornerRadius(0, 14, 14, 0)
            : new CornerRadius(14, 0, 0, 14);
        ShelfShadow.Direction = isLeft ? 0 : 180;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        ShowShelfActionMenu(AddButton);
        e.Handled = true;
    }

    private void ShowShelfActionMenu(UIElement placementTarget)
    {
        var menu = new WpfContextMenu
        {
            PlacementTarget = placementTarget
        };

        var addFilesItem = new WpfMenuItem
        {
            Header = UiText.Get(_settings.LanguageCode, "AddFiles")
        };
        addFilesItem.Click += (_, _) => AddFilesFromDialog();
        menu.Items.Add(addFilesItem);

        var addFolderItem = new WpfMenuItem
        {
            Header = UiText.Get(_settings.LanguageCode, "AddFolder")
        };
        addFolderItem.Click += (_, _) => AddFolderFromDialog();
        menu.Items.Add(addFolderItem);

        if (Items.Count > 1)
        {
            menu.Items.Add(new WpfSeparator());
            var selectAllItem = new WpfMenuItem
            {
                Header = UiText.Get(_settings.LanguageCode, "SelectAllForDragOut")
            };
            selectAllItem.Click += (_, _) => SelectAllShelfItems();
            menu.Items.Add(selectAllItem);
        }

        if (_removedShelfBatches.Count > 0)
        {
            menu.Items.Add(new WpfSeparator());
            var restoreItem = new WpfMenuItem
            {
                Header = UiText.Get(_settings.LanguageCode, "RestoreRemoved")
            };
            restoreItem.Click += (_, _) => RestoreRecentlyRemoved();
            menu.Items.Add(restoreItem);
        }

        if (Items.Any(item => !item.IsPinned))
        {
            menu.Items.Add(new WpfSeparator());
            var clearItem = new WpfMenuItem
            {
                Header = UiText.Get(_settings.LanguageCode, "ClearUnpinned")
            };
            clearItem.Click += (_, _) => ClearShelf();
            menu.Items.Add(clearItem);
        }

        menu.IsOpen = true;
    }

    private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ShelfItem item)
        {
            return;
        }

        RemoveShelfItems(GetSelectedItemsForAction(item));
        e.Handled = true;
    }

    private void ShelfList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateEmptyState();
    }

    private void ShelfList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Delete)
        {
            return;
        }

        var selectedItems = ShelfList.SelectedItems
            .OfType<ShelfItem>()
            .Where(Items.Contains)
            .ToArray();
        if (selectedItems.Length == 0)
        {
            return;
        }

        RemoveShelfItems(selectedItems);
        e.Handled = true;
    }

    private void SelectAllShelfItems()
    {
        ShelfList.SelectAll();
        ShelfList.Focus();
        UpdateEmptyState();
    }

    private void PinItemButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ShelfItem item)
        {
            return;
        }

        TogglePin(item);
        e.Handled = true;
    }

    private void RevealItemButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ShelfItem item)
        {
            return;
        }

        RevealShelfItem(item);
        e.Handled = true;
    }

    private void FileItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ShelfItem item)
        {
            return;
        }

        var menu = new WpfContextMenu();
        var openItem = new WpfMenuItem
        {
            Header = UiText.Get(_settings.LanguageCode, "Open")
        };
        openItem.Click += (_, _) => OpenShelfItem(item);
        menu.Items.Add(openItem);

        if (item.IsGroup)
        {
            var openAllItem = new WpfMenuItem
            {
                Header = UiText.Get(_settings.LanguageCode, "OpenAll")
            };
            openAllItem.Click += (_, _) => OpenAllShelfItemPaths(item);
            menu.Items.Add(openAllItem);
        }

        var revealItem = new WpfMenuItem
        {
            Header = UiText.Get(_settings.LanguageCode, "RevealInExplorer")
        };
        revealItem.Click += (_, _) => RevealShelfItem(item);
        menu.Items.Add(revealItem);
        menu.Items.Add(new WpfSeparator());

        var pinItem = new WpfMenuItem
        {
            Header = item.IsPinned
                ? UiText.Get(_settings.LanguageCode, "Unpin")
                : UiText.Get(_settings.LanguageCode, "Pin")
        };
        pinItem.Click += (_, _) => TogglePin(item);
        menu.Items.Add(pinItem);

        if (item.IsGroup)
        {
            var splitGroupItem = new WpfMenuItem
            {
                Header = UiText.Get(_settings.LanguageCode, "SplitGroup")
            };
            splitGroupItem.Click += (_, _) => SplitShelfGroup(item);
            menu.Items.Add(splitGroupItem);
        }

        if (ShelfList.SelectedItems.OfType<ShelfItem>().Count() >= 2)
        {
            var stackSelectedItem = new WpfMenuItem
            {
                Header = UiText.Get(_settings.LanguageCode, "StackSelected")
            };
            stackSelectedItem.Click += (_, _) => StackSelectedShelfItems();
            menu.Items.Add(stackSelectedItem);
        }

        var actionItems = GetSelectedItemsForAction(item);
        var removeItem = new WpfMenuItem
        {
            Header = actionItems.Length > 1
                ? UiText.Get(_settings.LanguageCode, "RemoveSelected")
                : UiText.Get(_settings.LanguageCode, "Remove")
        };
        removeItem.Click += (_, _) => RemoveShelfItems(actionItems);
        menu.Items.Add(removeItem);

        menu.PlacementTarget = sender as UIElement;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void Window_DragEnter(object sender, WpfDragEventArgs e)
    {
        ApplyDragEffects(e);
        if (e.Effects != System.Windows.DragDropEffects.None)
        {
            CancelDropNotice();
        }

        ApplyDropVisualState(e.Effects != System.Windows.DragDropEffects.None, GetDraggedPathCount(e));
    }

    private void Window_DragOver(object sender, WpfDragEventArgs e)
    {
        ApplyDragEffects(e);
        ApplyDropVisualState(e.Effects != System.Windows.DragDropEffects.None, GetDraggedPathCount(e));
    }

    private void Window_DragLeave(object sender, WpfDragEventArgs e)
    {
        ApplyDropVisualState(false);
    }

    private void Window_Drop(object sender, WpfDragEventArgs e)
    {
        ApplyDropVisualState(false);
        var paths = _dragDropService.ExtractFilePaths(e.Data);
        var added = AddPathsAsShelfItem(paths);

        _logger.Info($"Files dropped; addedCount={added}; totalCount={Items.Count}");
        if (added > 0)
        {
            CancelDropNotice();
            PersistShelf();
            UpdateEmptyState();
        }
        else
        {
            UpdateEmptyState();
            ShowDropNotice(paths.Count > 0 ? "AlreadyStaged" : "NoValidFiles");
        }
    }

    private void FileItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 &&
            !IsInsideButton(e.OriginalSource as DependencyObject) &&
            (sender as FrameworkElement)?.DataContext is ShelfItem item)
        {
            OpenShelfItem(item);
            e.Handled = true;
            return;
        }

        _dragStartPoint = e.GetPosition(this);
        _dragItem = (sender as FrameworkElement)?.DataContext as ShelfItem;
        _dragSelectionSnapshot = GetSelectedItemsForAction(_dragItem);
    }

    private void FileItem_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragItem is null)
        {
            return;
        }

        var currentPosition = e.GetPosition(this);
        if (Math.Abs(currentPosition.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(currentPosition.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var dragItems = GetDragItems();
        DragShelfItems(dragItems);
        _dragItem = null;
        _dragSelectionSnapshot = Array.Empty<ShelfItem>();
    }

    private void CountText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Items.Count == 0)
        {
            return;
        }

        _dragStartPoint = e.GetPosition(this);
        _dragAllFromCount = true;
        e.Handled = true;
    }

    private void CountText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragAllFromCount = false;
    }

    private void CountText_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (!_dragAllFromCount || e.LeftButton != MouseButtonState.Pressed)
        {
            _dragAllFromCount = false;
            return;
        }

        var currentPosition = e.GetPosition(this);
        if (Math.Abs(currentPosition.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(currentPosition.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        _dragAllFromCount = false;
        DragShelfItems(GetCountHandleDragItems());
        e.Handled = true;
    }

    private void DragShelfItems(ShelfItem[] dragItems)
    {
        if (dragItems.Length == 0)
        {
            return;
        }

        foreach (var item in dragItems)
        {
            item.RefreshExists();
        }

        var paths = dragItems
            .SelectMany(item => item.FilePaths)
            .Where(path => File.Exists(path) || Directory.Exists(path))
            .ToArray();

        if (paths.Length == 0)
        {
            _logger.Warning("Drag-out skipped; no selected items exist");
            return;
        }

        var data = _dragDropService.CreateFileDropData(paths);
        _isDraggingOut = true;
        var effect = System.Windows.DragDropEffects.None;
        try
        {
            effect = System.Windows.DragDrop.DoDragDrop(this, data, System.Windows.DragDropEffects.Copy);
        }
        finally
        {
            _isDraggingOut = false;
        }

        if (effect != System.Windows.DragDropEffects.None)
        {
            RemoveDraggedOutItems(dragItems);
        }

        UpdateEmptyState();
        _logger.Info($"Drag-out completed; itemCount={paths.Length}; effect={effect}");
        if (!IsActive && _isPanelOpen)
        {
            HideToTray();
        }
    }

    private ShelfItem[] GetDragItems()
    {
        if (_dragItem is null)
        {
            return Array.Empty<ShelfItem>();
        }

        var snapshot = _dragSelectionSnapshot
            .Where(Items.Contains)
            .ToArray();
        return snapshot.Length > 0 ? snapshot : new[] { _dragItem };
    }

    private ShelfItem[] GetSelectedItemsForAction(ShelfItem? item)
    {
        if (item is null)
        {
            return Array.Empty<ShelfItem>();
        }

        if (!ShelfList.SelectedItems.Contains(item))
        {
            return new[] { item };
        }

        var selectedItems = ShelfList.SelectedItems
            .OfType<ShelfItem>()
            .Where(Items.Contains)
            .ToArray();
        return selectedItems.Length > 0 ? selectedItems : new[] { item };
    }

    private ShelfItem[] GetCountHandleDragItems()
    {
        var selectedItems = ShelfList.SelectedItems.OfType<ShelfItem>().ToArray();
        return selectedItems.Length > 0 ? selectedItems : Items.ToArray();
    }

    private void ApplyDragEffects(WpfDragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
            ? System.Windows.DragDropEffects.Copy
            : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void ApplyDropVisualState(bool isActive, int draggedPathCount = 0, bool force = false)
    {
        if (_isDragActive == isActive && draggedPathCount == 0 && !force)
        {
            return;
        }

        _isDragActive = isActive;
        if (!_isPanelOpen)
        {
            IconShell.Width = isActive ? 58 : IconSurfaceSize;
            IconShell.Height = isActive ? 58 : IconSurfaceSize;
            IconShell.Background = new SolidColorBrush(Colors.Transparent);
            IconLogoImage.Width = isActive ? 50 : 46;
            IconLogoImage.Height = isActive ? 50 : 46;
            IconLogoImage.Margin = isActive ? new Thickness(0, -10, 0, 0) : new Thickness(0);
            IconDropHint.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
            IconDropHintTextBlock.Text = UiText.Get(_settings.LanguageCode, "ReleaseToStageShort");
            return;
        }

        DropTargetBorder.Background = new SolidColorBrush(
            isActive
                ? System.Windows.Media.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)
                : System.Windows.Media.Color.FromArgb(0xFA, 0xFF, 0xFF, 0xFF));
        EmptyIconSurface.Width = isActive ? 74 : 64;
        EmptyIconSurface.Height = isActive ? 74 : 64;
        EmptyIconSurface.Background = new SolidColorBrush(
            isActive
                ? System.Windows.Media.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)
                : System.Windows.Media.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
        EmptyIconText.FontSize = isActive ? 40 : 34;
        EmptyIconText.Foreground = new SolidColorBrush(
            isActive
                ? System.Windows.Media.Color.FromRgb(0x00, 0x00, 0x00)
                : System.Windows.Media.Color.FromRgb(0x14, 0x14, 0x14));
        AddButton.Background = new SolidColorBrush(
            isActive
                ? System.Windows.Media.Color.FromArgb(0x18, 0x00, 0x00, 0x00)
                : System.Windows.Media.Color.FromArgb(0x10, 0x00, 0x00, 0x00));

        var dropTitle = isActive && draggedPathCount > 0
            ? UiText.FormatCount(_settings.LanguageCode, "ReleaseToStageCount", draggedPathCount)
            : isActive
            ? UiText.Get(_settings.LanguageCode, "ReleaseToStage")
            : UiText.Get(_settings.LanguageCode, "DropFiles");
        EmptyTitleTextBlock.Text = dropTitle;
        EmptyHintTextBlock.Text = isActive
            ? UiText.Get(_settings.LanguageCode, "ReleaseToStageHint")
            : UiText.Get(_settings.LanguageCode, "PathOnly");
    }

    private void ShowDropNotice(string textKey)
    {
        var token = ++_dropNoticeToken;
        var text = UiText.Get(_settings.LanguageCode, textKey);
        if (!_isPanelOpen)
        {
            IconDropHint.Visibility = Visibility.Visible;
            IconDropHintTextBlock.Text = text;
            _ = ResetDropNoticeLater(token);
            return;
        }

        if (Items.Count > 0)
        {
            AddButton.Visibility = Visibility.Visible;
            AddButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x18, 0x00, 0x00, 0x00));
        }
        else
        {
            EmptyState.Visibility = Visibility.Visible;
            EmptyTitleTextBlock.Text = text;
            EmptyHintTextBlock.Text = UiText.Get(_settings.LanguageCode, "PathOnly");
        }

        _ = ResetDropNoticeLater(token);
    }

    private void CancelDropNotice()
    {
        _dropNoticeToken++;
    }

    private async Task ResetDropNoticeLater(int token)
    {
        await Task.Delay(1400);
        if (token != _dropNoticeToken)
        {
            return;
        }

        ApplyDropVisualState(false, force: true);
        UpdateEmptyState();
    }

    private static int GetDraggedPathCount(WpfDragEventArgs e)
    {
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) ||
            e.Data.GetData(System.Windows.DataFormats.FileDrop) is not string[] paths)
        {
            return 0;
        }

        return paths.Length;
    }

    private int AddPathsAsShelfItem(IEnumerable<string> paths)
    {
        var pathsToAdd = paths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(path => !Items.Any(item => item.ContainsPath(path)))
            .ToArray();

        if (pathsToAdd.Length == 0)
        {
            return 0;
        }

        var item = ShelfItem.FromPaths(pathsToAdd);
        item.ApplyLanguage(_settings.LanguageCode);
        Items.Add(item);
        return pathsToAdd.Length;
    }

    private void AddFilesFromDialog()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            CheckFileExists = true,
            Title = UiText.Get(_settings.LanguageCode, "AddFiles")
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        AddSelectedPaths(dialog.FileNames);
    }

    private void AddFolderFromDialog()
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = UiText.Get(_settings.LanguageCode, "AddFolder"),
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            return;
        }

        AddSelectedPaths(new[] { dialog.SelectedPath });
    }

    private void AddSelectedPaths(IEnumerable<string> paths)
    {
        var added = AddPathsAsShelfItem(paths);

        if (added == 0)
        {
            ShowDropNotice("AlreadyStaged");
            return;
        }

        _logger.Info($"Paths added manually; addedCount={added}; totalCount={Items.Count}");
        PersistShelf();
        UpdateEmptyState();
    }

    private void RemoveShelfItem(ShelfItem item)
    {
        RemoveShelfItems(new[] { item });
    }

    private void RemoveShelfItems(ShelfItem[] items)
    {
        var removed = RemoveShelfItemsAsBatch(items);
        if (removed == 0)
        {
            return;
        }

        _logger.Info($"Shelf items removed; removedCount={removed}; totalCount={Items.Count}");
        PersistShelf();
        UpdateEmptyState();
    }

    private void TogglePin(ShelfItem item)
    {
        item.IsPinned = !item.IsPinned;
        _logger.Info($"Shelf item pin changed; isPinned={item.IsPinned}; pathCount={item.FilePaths.Count}");
        PersistShelf();
        UpdateEmptyState();
    }

    private void SplitShelfGroup(ShelfItem item)
    {
        if (!item.IsGroup)
        {
            return;
        }

        var index = Items.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        Items.RemoveAt(index);
        var inserted = 0;
        foreach (var path in item.FilePaths)
        {
            if (Items.Any(existing => existing.ContainsPath(path)))
            {
                continue;
            }

            var splitItem = ShelfItem.FromPath(path, item.IsPinned, item.AddedAt);
            splitItem.ApplyLanguage(_settings.LanguageCode);
            Items.Insert(index + inserted, splitItem);
            inserted++;
        }

        _logger.Info($"Shelf group split; insertedCount={inserted}; totalCount={Items.Count}");
        PersistShelf();
        UpdateEmptyState();
    }

    private void StackSelectedShelfItems()
    {
        var selectedItems = ShelfList.SelectedItems.OfType<ShelfItem>()
            .Select(item => new { Item = item, Index = Items.IndexOf(item) })
            .Where(entry => entry.Index >= 0)
            .OrderBy(entry => entry.Index)
            .ToArray();
        if (selectedItems.Length < 2)
        {
            return;
        }

        var paths = selectedItems
            .SelectMany(entry => entry.Item.FilePaths)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (paths.Length < 2)
        {
            return;
        }

        var insertIndex = selectedItems[0].Index;
        var isPinned = selectedItems.Any(entry => entry.Item.IsPinned);
        var addedAt = selectedItems.Min(entry => entry.Item.AddedAt);

        foreach (var entry in selectedItems.OrderByDescending(entry => entry.Index))
        {
            Items.RemoveAt(entry.Index);
        }

        var stackedItem = ShelfItem.FromPaths(paths, isPinned, addedAt);
        stackedItem.ApplyLanguage(_settings.LanguageCode);
        stackedItem.RefreshExists();
        Items.Insert(Math.Clamp(insertIndex, 0, Items.Count), stackedItem);
        ShelfList.SelectedItems.Clear();
        ShelfList.SelectedItem = stackedItem;

        _logger.Info($"Shelf items stacked; sourceCount={selectedItems.Length}; pathCount={paths.Length}; totalCount={Items.Count}");
        PersistShelf();
        UpdateEmptyState();
    }

    private void RemoveDraggedOutItems(IEnumerable<ShelfItem> dragItems)
    {
        var removed = RemoveShelfItemsAsBatch(dragItems.Where(item => !item.IsPinned).ToArray());
        if (removed == 0)
        {
            return;
        }

        _logger.Info($"Drag-out removed unpinned shelf items; removedCount={removed}; totalCount={Items.Count}");
        PersistShelf();
    }

    private void LoadShelfState()
    {
        foreach (var item in _shelfStateService.Load())
        {
            if (Items.Any(existing => item.FilePaths.Any(existing.ContainsPath)))
            {
                continue;
            }

            Items.Add(item);
        }

        if (Items.Count > 0)
        {
            _logger.Info($"Shelf state restored; itemCount={Items.Count}");
        }
    }

    private void PersistShelf()
    {
        _shelfStateService.Save(Items);
    }

    private void OpenShelfItem(ShelfItem item)
    {
        item.RefreshExists();
        if (!item.Exists)
        {
            _logger.Warning($"Open skipped; missing item; pathCount={item.FilePaths.Count}");
            return;
        }

        var path = GetFirstExistingPath(item);
        if (path is null)
        {
            _logger.Warning($"Open skipped; no existing paths in item; pathCount={item.FilePaths.Count}");
            return;
        }

        OpenPath(path);
    }

    private void OpenAllShelfItemPaths(ShelfItem item)
    {
        item.RefreshExists();
        var paths = item.FilePaths
            .Where(path => File.Exists(path) || Directory.Exists(path))
            .ToArray();
        if (paths.Length == 0)
        {
            _logger.Warning($"Open all skipped; no existing paths in item; pathCount={item.FilePaths.Count}");
            return;
        }

        foreach (var path in paths)
        {
            OpenPath(path);
        }

        _logger.Info($"Shelf item group opened; pathCount={paths.Length}");
    }

    private void OpenPath(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
            _logger.Info("Shelf item opened");
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException or InvalidOperationException)
        {
            _logger.Error($"Open failed; errorType={ex.GetType().Name}");
        }
    }

    private void RevealShelfItem(ShelfItem item)
    {
        item.RefreshExists();
        if (!item.Exists)
        {
            _logger.Warning($"Reveal skipped; missing item; pathCount={item.FilePaths.Count}");
            return;
        }

        var path = GetFirstExistingPath(item);
        if (path is null)
        {
            _logger.Warning($"Reveal skipped; no existing paths in item; pathCount={item.FilePaths.Count}");
            return;
        }

        var arguments = Directory.Exists(path)
            ? $"\"{path}\""
            : $"/select,\"{path}\"";

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", arguments)
            {
                UseShellExecute = true
            });
            _logger.Info("Shelf item revealed");
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException or InvalidOperationException)
        {
            _logger.Error($"Reveal failed; errorType={ex.GetType().Name}");
        }
    }

    private static string? GetFirstExistingPath(ShelfItem item)
    {
        return item.FilePaths.FirstOrDefault(path => File.Exists(path) || Directory.Exists(path));
    }

    private void RememberRemovedItems(IEnumerable<RemovedShelfItem> removedItems)
    {
        var batchItems = removedItems
            .Where(removedItem => removedItem.Index >= 0)
            .OrderBy(removedItem => removedItem.Index)
            .ToArray();
        if (batchItems.Length == 0)
        {
            return;
        }

        _removedShelfBatches.Add(new RemovedShelfBatch(batchItems));
        if (_removedShelfBatches.Count > 20)
        {
            _removedShelfBatches.RemoveAt(0);
        }
    }

    private int RemoveShelfItemsAsBatch(IEnumerable<ShelfItem> itemsToRemove)
    {
        var removedItems = itemsToRemove
            .Distinct()
            .Select(item => new RemovedShelfItem(item, Items.IndexOf(item)))
            .Where(removedItem => removedItem.Index >= 0)
            .OrderBy(removedItem => removedItem.Index)
            .ToArray();
        if (removedItems.Length == 0)
        {
            return 0;
        }

        var removed = 0;
        foreach (var removedItem in removedItems.OrderByDescending(item => item.Index))
        {
            if (Items.Remove(removedItem.Item))
            {
                removed++;
            }
        }

        if (removed > 0)
        {
            RememberRemovedItems(removedItems);
        }

        return removed;
    }

    private void RestoreRecentlyRemoved()
    {
        if (_removedShelfBatches.Count == 0)
        {
            return;
        }

        var batch = _removedShelfBatches[^1];
        _removedShelfBatches.RemoveAt(_removedShelfBatches.Count - 1);

        var restored = 0;
        foreach (var removedItem in batch.Items)
        {
            if (removedItem.Item.FilePaths.Any(path => Items.Any(existing => existing.ContainsPath(path))))
            {
                continue;
            }

            removedItem.Item.ApplyLanguage(_settings.LanguageCode);
            removedItem.Item.RefreshExists();
            Items.Insert(Math.Clamp(removedItem.Index, 0, Items.Count), removedItem.Item);
            restored++;
        }

        if (restored == 0)
        {
            return;
        }

        _logger.Info($"Removed shelf items restored; restoredCount={restored}; totalCount={Items.Count}");
        PersistShelf();
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        var hasItems = Items.Count > 0;
        EmptyState.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
        AddButton.Visibility = Visibility.Visible;
        var selectedCount = ShelfList.SelectedItems.OfType<ShelfItem>().Sum(item => item.FilePaths.Count);
        var hasSelection = ShelfList.SelectedItems.Count > 0;
        CountText.Text = hasSelection
            ? UiText.FormatSelection(_settings.LanguageCode, selectedCount, ItemCount)
            : UiText.FormatItemCount(_settings.LanguageCode, ItemCount);
        CountDragHandle.Opacity = hasItems ? 1 : 0.55;
        CountDragHandle.Cursor = hasItems ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.Arrow;
        CountDragHandle.ToolTip = hasItems
            ? UiText.Get(_settings.LanguageCode, hasSelection ? "DragSelectionFromCount" : "DragAllFromCount")
            : null;
        CountDragHandle.Background = new SolidColorBrush(hasSelection
            ? System.Windows.Media.Color.FromRgb(0x14, 0x14, 0x14)
            : hasItems
                ? System.Windows.Media.Color.FromArgb(0x10, 0x00, 0x00, 0x00)
                : System.Windows.Media.Color.FromArgb(0x08, 0x00, 0x00, 0x00));
        CountIconSurface.Background = new SolidColorBrush(hasSelection
            ? Colors.White
            : hasItems
                ? Colors.White
                : System.Windows.Media.Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF));
        CountIconText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x14, 0x14, 0x14));
        CountText.Foreground = new SolidColorBrush(hasSelection ? Colors.White : System.Windows.Media.Color.FromRgb(0x14, 0x14, 0x14));
        ItemCountChanged?.Invoke(this, ItemCount);
    }

    private sealed record RemovedShelfItem(ShelfItem Item, int Index);

    private sealed record RemovedShelfBatch(RemovedShelfItem[] Items);
}
