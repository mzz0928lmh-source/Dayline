using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Dayline;

// Run the real WPF controls off-screen; sample content never touches the user's journal.
internal static class SelfChecks
{
    internal static void RunNative(App app, string directory, bool checkBackdrop = true)
    {
        Directory.CreateDirectory(directory);
        var report = new List<string>();
        var notebook = new Notebook();
        notebook.Get(DateTime.Today).Goals.Add(new Goal { Text = "新的外观，轻一点。" });
        var window = new MainWindow(notebook, () => true);
        window.HideRequested += () => { window.FlushPendingChanges(); window.Conceal(); };
        app.MainWindow = window;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle();
        Native.Place(window, Native.PointerScreen(), notebook.Appearance);
        Native.GetWindowRect(hwnd, out var bounds);
        var pattern = new Grid();
        pattern.ColumnDefinitions.Add(new ColumnDefinition()); pattern.ColumnDefinitions.Add(new ColumnDefinition());
        var blue = new Border { Background = new SolidColorBrush(Color.FromRgb(81, 143, 180)) };
        var sand = new Border { Background = new SolidColorBrush(Color.FromRgb(222, 172, 106)) };
        Grid.SetColumn(sand, 1); pattern.Children.Add(blue); pattern.Children.Add(sand);
        var backdrop = new Window { WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false, ShowActivated = false, Topmost = true, Content = pattern };
        backdrop.Show();
        Native.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(backdrop).Handle, new IntPtr(-1), bounds.Left - 12, bounds.Top - 10, bounds.Right - bounds.Left + 24, bounds.Bottom - bounds.Top + 20, 0x0010);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        int phase = 0;
        ContextMenu? testMenu = null;
        double detachedLeft = 0, detachedTop = 0, detachedWidth = 0, detachedHeight = 0;
        void Check(bool result, string name)
        {
            report.Add((result ? "PASS: " : "FAIL: ") + name);
            if (!result) throw new InvalidOperationException(name);
        }
        window.Reveal(true);
        timer.Tick += (_, _) =>
        {
            try
            {
                if (phase == 0 && watch.Elapsed.TotalSeconds >= .6)
                {
                    Check(window.IsVisible && Native.IsWindowVisible(hwnd), "Native window is actually visible on the desktop");
                    Check(Native.GetForegroundWindow() != hwnd && !window.IsActive, "Automatic reveal does not take keyboard focus");
                    Check(window.ActualWidth > 400 && window.ActualHeight > 400 && double.IsFinite(window.Top), "Native window has usable bounds");
                    var visibleRoot = (FrameworkElement)window.Content;
                    var headerDrag = (System.Windows.Controls.Primitives.Thumb)window.FindName("HeaderDragThumb");
                    var topPoints = new[] { new Point(visibleRoot.ActualWidth / 2, 10), new Point(12, 55),
                        new Point(visibleRoot.ActualWidth - 12, 55), new Point(visibleRoot.ActualWidth / 2, 72) };
                    Check(topPoints.All(point => visibleRoot.InputHitTest(point) is DependencyObject hit && (hit == headerDrag || headerDrag.IsAncestorOf(hit))),
                        "The top edge, both side margins, and the blank strip below the title all route mouse input to dragging");
                    var settingsButton = (Button)window.FindName("AppearanceButton");
                    var settingsPoint = settingsButton.TransformToAncestor(visibleRoot).Transform(new Point(settingsButton.ActualWidth / 2, settingsButton.ActualHeight / 2));
                    Check(visibleRoot.InputHitTest(settingsPoint) is DependencyObject settingsHit && (settingsHit == settingsButton || settingsButton.IsAncestorOf(settingsHit)),
                        "The expanded drag area keeps the top-right settings button clickable");
                    var actual = Native.Bounds(hwnd);
                    var expected = Native.Placement(System.Windows.Forms.Screen.FromHandle(hwnd)).bounds;
                    Check(actual.Left == expected.Left && actual.Width == expected.Width,
                        "Actual native window width and position match the top-center trigger");
                    var pointerState = new RevealTiming(DateTime.Now);
                    pointerState.BeginPointerWatch(true);
                    var sampleTime = DateTime.Now;
                    var pointerToggle = (CheckBox)window.FindName("AutoHideToggle");
                    Check(!window.ShouldHideForPointer(pointerState, sampleTime, actual.Right + 20, actual.Top + 100)
                        && !window.ShouldHideForPointer(pointerState, sampleTime.AddSeconds(1), actual.Right + 20, actual.Top + 100),
                        "The disabled auto-hide switch also prevents pointer-leave hiding after reveal");
                    pointerToggle.IsChecked = true;
                    Check(!window.ShouldHideForPointer(pointerState, sampleTime, actual.Left + actual.Width / 2, Native.ScreenTop(window)),
                        "The entry path from the top strip keeps the real window open");
                    window.ShouldHideForPointer(pointerState, sampleTime, actual.Left + actual.Width / 2, actual.Top + 100);
                    window.ShouldHideForPointer(pointerState, sampleTime, actual.Right + 20, actual.Top + 100);
                    Check(window.ShouldHideForPointer(pointerState, sampleTime.AddMilliseconds(121), actual.Right + 20, actual.Top + 100),
                        "Moving out of the real panel bounds requests hiding without losing focus");
                    pointerToggle.IsChecked = false;
                    Check(Native.BackdropEnabled, "Native blur / acrylic composition is enabled");
                    if (checkBackdrop)
                    {
                        using var screenshot = CaptureDesktopWindow(hwnd, Path.Combine(directory, "native-glass.png"));
                        var left = screenshot.GetPixel(screenshot.Width / 5, screenshot.Height * 2 / 3);
                        var right = screenshot.GetPixel(screenshot.Width * 4 / 5, screenshot.Height * 2 / 3);
                        Check(Math.Abs(left.R - right.R) + Math.Abs(left.G - right.G) + Math.Abs(left.B - right.B) > 65,
                            $"Desktop colors remain visible through the panel ({left.R},{left.G},{left.B} / {right.R},{right.G},{right.B})");
                    }
                    else report.Add("NOTE: Interaction-only run omits the desktop backdrop color comparison");
                    if (Native.RegisterHotKey(hwnd, 77, 0x0001 | 0x0002 | 0x4000, 0x44))
                    {
                        report.Add("PASS: Optional Ctrl+Alt+D hotkey can be registered");
                        Native.UnregisterHotKey(hwnd, 77);
                    }
                    else report.Add("NOTE: Ctrl+Alt+D is already occupied; edge dwell and tray remain available");
                    phase = 1;
                }
                if (phase == 1 && watch.Elapsed.TotalSeconds >= 10) { window.Conceal(); phase = 2; }
                if (phase == 2 && watch.Elapsed.TotalSeconds >= 10.5)
                {
                    Check(!window.IsVisible, "Window retracts after the ten-second preview");
                    window.Reveal(true); phase = 3;
                }
                if (phase == 3 && watch.Elapsed.TotalSeconds >= 11)
                {
                    Check(window.IsVisible && window.Opacity > .99, "Hidden window can reveal again");
                    window.Reveal(false);
                    window.ShowAppearance();
                    Native.GetWindowRect(hwnd, out bounds);
                    Native.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(backdrop).Handle, hwnd, bounds.Left - 12, bounds.Top - 40, bounds.Right - bounds.Left + 24, bounds.Bottom - bounds.Top + 80, 0x0010);
                    phase = 4;
                }
                if (phase == 4 && watch.Elapsed.TotalSeconds >= 11.5)
                {
                    Check(window.IsActive && window.ShowingAppearance, "Appearance controls remain inside the active panel");
                    ((Slider)window.FindName("WidthSlider")).Value = 640;
                    ((Slider)window.FindName("HeightSlider")).Value = 620;
                    var resized = Native.Bounds(hwnd);
                    var configured = Native.Placement(System.Windows.Forms.Screen.FromHandle(hwnd), notebook.Appearance).bounds;
                    Check(resized.Left == configured.Left && resized.Width == configured.Width && resized.Height == configured.Height,
                        "Settings change the real window dimensions and keep the top trigger centered on its new width");
                    using var screenshot = CaptureDesktopWindow(hwnd, Path.Combine(directory, "native-appearance.png"));
                    var autoHide = (CheckBox)window.FindName("AutoHideToggle");
                    Check(autoHide.IsChecked == false && !notebook.Appearance.HideOnBlur,
                        "Focus-loss auto-hide is unchecked by default in the real settings window");
                    autoHide.IsChecked = true;
                    backdrop.Activate(); phase = 5;
                }
                if (phase == 5 && watch.Elapsed.TotalSeconds >= 12)
                {
                    Check(!window.IsVisible, "Switching focus to another window automatically retracts the panel");
                    ((CheckBox)window.FindName("AutoHideToggle")).IsChecked = false;
                    window.Reveal(false); phase = 6;
                }
                if (phase == 6 && watch.Elapsed.TotalSeconds >= 12.4) { backdrop.Activate(); phase = 7; }
                if (phase == 7 && watch.Elapsed.TotalSeconds >= 12.9)
                {
                    Check(window.IsVisible, "Auto-hide can be disabled in preferences");
                    var outsideTiming = new RevealTiming(DateTime.Now);
                    outsideTiming.BeginPointerWatch(true);
                    var outsideBounds = Native.Bounds(hwnd);
                    var outsideTime = DateTime.Now;
                    Check(!window.ShouldHideForPointer(outsideTiming, outsideTime, outsideBounds.Right + 50, outsideBounds.Bottom + 50)
                        && !window.ShouldHideForPointer(outsideTiming, outsideTime.AddSeconds(1), outsideBounds.Right + 50, outsideBounds.Bottom + 50),
                        "A disabled panel stays open when focus and the pointer both leave it");
                    window.Reveal(false);
                    ((CheckBox)window.FindName("AutoHideToggle")).IsChecked = true;
                    window.ShowDay(DateTime.Today);
                    var rows = (StackPanel)window.FindName("GoalList");
                    testMenu = ((Border)rows.Children[0]).ContextMenu;
                    testMenu.PlacementTarget = (Border)rows.Children[0];
                    testMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
                    testMenu.IsOpen = true;
                    phase = 8;
                }
                if (phase == 8 && watch.Elapsed.TotalSeconds >= 13.4)
                {
                    var menuSource = (System.Windows.Interop.HwndSource)PresentationSource.FromVisual(testMenu!)!;
                    var menuBounds = Native.Bounds(menuSource.Handle);
                    var menuState = new RevealTiming(DateTime.Now);
                    menuState.BeginPointerWatch(true);
                    var sampleTime = DateTime.Now;
                    Check(!window.ShouldHideForPointer(menuState, sampleTime, menuBounds.Left + menuBounds.Width / 2, menuBounds.Top + menuBounds.Height / 2),
                        "The popup menu is treated as part of the software's mouse area");
                    var panelBounds = Native.Bounds(hwnd);
                    int outsideX = Math.Max(panelBounds.Right, menuBounds.Right) + 50;
                    window.ShouldHideForPointer(menuState, sampleTime, outsideX, panelBounds.Bottom + 30);
                    Check(window.ShouldHideForPointer(menuState, sampleTime.AddMilliseconds(121), outsideX, panelBounds.Bottom + 30),
                        "Leaving both the panel and its captured context menu still requests hiding");
                    window.Conceal(); phase = 9;
                }
                if (phase == 9 && watch.Elapsed.TotalSeconds >= 13.9)
                {
                    Check(!window.IsVisible && testMenu?.IsOpen == false, "Hiding the panel also dismisses its context menu");
                    window.Reveal(false);
                    phase = 10;
                }
                if (phase == 10 && watch.Elapsed.TotalSeconds >= 14.3)
                {
                    ((CheckBox)window.FindName("AutoHideToggle")).IsChecked = true;
                    var detachedTiming = new RevealTiming(DateTime.Now);
                    detachedTiming.BeginReminder(DateTime.Now); detachedTiming.BeginPointerWatch(true);
                    window.Detached += () => { detachedTiming.CancelReminder(); detachedTiming.BeginPointerWatch(false); };
                    var header = (System.Windows.Controls.Primitives.Thumb)window.FindName("HeaderDragThumb");
                    double beforeLeft = window.Left, beforeTop = window.Top;
                    header.RaiseEvent(new System.Windows.Controls.Primitives.DragStartedEventArgs(0, 0)
                        { RoutedEvent = System.Windows.Controls.Primitives.Thumb.DragStartedEvent });
                    header.RaiseEvent(new System.Windows.Controls.Primitives.DragDeltaEventArgs(24, 30)
                        { RoutedEvent = System.Windows.Controls.Primitives.Thumb.DragDeltaEvent });
                    header.RaiseEvent(new System.Windows.Controls.Primitives.DragCompletedEventArgs(24, 30, false)
                        { RoutedEvent = System.Windows.Controls.Primitives.Thumb.DragCompletedEvent });
                    Check(Math.Abs(window.Left - beforeLeft - 24) < 1 && Math.Abs(window.Top - beforeTop - 30) < 1 && !window.IsInteracting,
                        "The header drag events move the native window and clear interaction on release");
                    Check(window.IsDetached && detachedTiming.ReminderDeadline == null,
                        "Dragging into detached mode cancels the reminder countdown");
                    var thumb = (System.Windows.Controls.Primitives.Thumb)window.FindName("ResizeThumb");
                    Check(thumb.Visibility == Visibility.Visible && window.ResizeMode == ResizeMode.CanResize,
                        "Detached mode exposes the resize handle");
                    detachedWidth = Math.Clamp(window.Width + 60, window.MinWidth, window.MaxWidth);
                    detachedHeight = Math.Clamp(window.Height + 10, window.MinHeight, window.MaxHeight);
                    thumb.RaiseEvent(new System.Windows.Controls.Primitives.DragDeltaEventArgs(60, 10)
                        { RoutedEvent = System.Windows.Controls.Primitives.Thumb.DragDeltaEvent });
                    thumb.RaiseEvent(new System.Windows.Controls.Primitives.DragCompletedEventArgs(60, 10, false)
                        { RoutedEvent = System.Windows.Controls.Primitives.Thumb.DragCompletedEvent });
                    Check(notebook.Appearance.PanelWidth == Math.Round(detachedWidth) && notebook.Appearance.PanelHeight == Math.Round(detachedHeight),
                        "Dragging the resize handle updates the saved width and height settings");
                    window.Left += 24; window.Top += 4;
                    detachedLeft = window.Left; detachedTop = window.Top;
                    window.Reveal(false);
                    Check(window.Left == detachedLeft && window.Top == detachedTop,
                        "Revealing a detached panel preserves its new position");
                    var panelBounds = Native.Bounds(hwnd);
                    Check(!window.ShouldHideForPointer(detachedTiming, DateTime.Now.AddSeconds(20), panelBounds.Right + 40, panelBounds.Bottom + 40),
                        "Leaving the old anchor and the detached panel never requests automatic hiding");
                    backdrop.Activate(); phase = 11;
                }
                if (phase == 11 && watch.Elapsed.TotalSeconds >= 14.9)
                {
                    Check(window.IsVisible && window.IsDetached && Math.Abs(window.ActualWidth - detachedWidth) < 1
                        && Math.Abs(window.ActualHeight - detachedHeight) < 1,
                        "The real detached window remains visible after focus loss and resizes to the requested bounds");
                    using var screenshot = CaptureDesktopWindow(hwnd, Path.Combine(directory, "native-detached.png"));
                    ((Button)window.FindName("HideButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Check(!window.IsVisible && !window.IsDetached,
                        "The hide button explicitly dismisses and unpins the detached window");
                    window.Reveal(false); phase = 12;
                }
                if (phase == 12 && watch.Elapsed.TotalSeconds >= 15.3)
                {
                    var expected = Native.Placement(System.Windows.Forms.Screen.FromHandle(hwnd), notebook.Appearance).bounds;
                    var actual = Native.Bounds(hwnd);
                    Check(Math.Abs(actual.Top - expected.Top) <= 1 && actual.Width == expected.Width && actual.Height == expected.Height,
                        "The next reveal restores the top anchor while retaining the user's configured panel size");
                    timer.Stop(); window.Close(); backdrop.Close();
                    File.WriteAllLines(Path.Combine(directory, "native-checks.txt"), report);
                    app.Shutdown(0);
                }
            }
            catch (Exception error)
            {
                timer.Stop(); window.Close(); backdrop.Close();
                File.WriteAllLines(Path.Combine(directory, "native-checks.txt"), report.Append(error.ToString()));
                app.Shutdown(1);
            }
        };
        timer.Start();
    }

    private static System.Drawing.Bitmap CaptureDesktopWindow(IntPtr hwnd, string path)
    {
        Native.GetWindowRect(hwnd, out var bounds);
        var bitmap = new System.Drawing.Bitmap(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bitmap.Size);
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return bitmap;
    }

    internal static void Run(string directory)
    {
        Directory.CreateDirectory(directory);
        var report = new List<string>();
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            report.Add("PASS: " + name);
        }
        var book = new Notebook(); int saves = 0;
        var window = new MainWindow(book, () => { saves++; return true; });
        var root = (FrameworkElement)window.Content;
        window.Content = null;
        Layout(root);
        Capture(root, Path.Combine(directory, "empty.png"));
        var input = (TextBox)window.FindName("GoalInput");
        var add = (Button)window.FindName("AddButton");
        input.Text = "完成产品首页的设计稿";
        add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(book.Find(DateTime.Today)?.Goals.Count == 1, "Add button commits a goal through the real WPF event");
        Check(input.Text.Length == 0 && saves == 1, "Adding saves and clears the input");
        add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(book.Find(DateTime.Today)?.Goals.Count == 1, "Empty input cannot create an empty goal");
        var list = (StackPanel)window.FindName("GoalList");
        var row = (Border)list.Children[0];
        ((MenuItem)row.ContextMenu.Items[0]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Check(book.Find(DateTime.Today)!.Goals[0].Completed, "Context menu marks a goal complete");
        row = (Border)list.Children[0];
        ((MenuItem)row.ContextMenu.Items[0]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Check(!book.Find(DateTime.Today)!.Goals[0].Completed && book.Find(DateTime.Today)!.Goals[0].Strokes.Count == 0, "Undo clears completion and ink");
        input.Text = "未提交的今日草稿";
        window.ShowDay(DateTime.Today.AddDays(-1));
        input.Text = "昨天补记的目标";
        add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.ShowDay(DateTime.Today);
        Check(input.Text == "未提交的今日草稿", "Draft survives switching dates");
        Check(book.Find(DateTime.Today.AddDays(-1))?.Goals[0].Text == "昨天补记的目标", "Calendar editing writes to the chosen date");
        window.ShowCalendar();
        var cells = (System.Windows.Controls.Primitives.UniformGrid)window.FindName("CalendarCells");
        Check(cells.Children.Count == 42 && window.ShowingCalendar, "Calendar renders all six weeks");
        ((Button)cells.Children[10]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(window.ShowingCalendar && window.ShowingDayDetails, "Clicking a calendar thumbnail opens details over the current calendar");
        var originalMonth = ((TextBlock)window.FindName("MonthLabel")).Text;
        input.Text = "日期浮窗的未提交草稿";
        var detailDay = window.SelectedDay;
        ((Button)window.FindName("CloseDayDetailButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(window.ShowingCalendar && !window.ShowingDayDetails && ((TextBlock)window.FindName("MonthLabel")).Text == originalMonth
            && book.Drafts[Notebook.Key(detailDay)] == "日期浮窗的未提交草稿",
            "Closing calendar details preserves the displayed month and the selected date's draft");
        var searchInput = (TextBox)window.FindName("CalendarSearchInput");
        var searchResults = (ListBox)window.FindName("CalendarSearchResults");
        var locateButton = (Button)window.FindName("LocateCalendarSearchButton");
        var distantDay = new DateTime(DateTime.Today.Year + 1, 1, 12);
        book.Get(distantDay).Goals.Add(new Goal { Text = "跨年项目搜索验证", Completed = true });
        window.ShowDay(DateTime.Today);
        input.Text = "搜索定位前未提交的草稿";
        window.ShowCalendar();
        searchInput.Text = "项目 搜索";
        Check(searchResults.Items.Count == 1 && searchResults.SelectedIndex == 0 && locateButton.IsEnabled,
            "Calendar input searches all dates and selects the first matching task");
        Check(((Grid)window.FindName("CalendarSearchView")).Visibility == Visibility.Visible
            && ((Grid)window.FindName("CalendarGrid")).Visibility == Visibility.Collapsed,
            "Search results replace the month grid without opening a separate popup");
        Layout(root);
        Capture(root, Path.Combine(directory, "calendar-search.png"));
        locateButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var locatedCell = cells.Children.OfType<Button>().Single(button => Equals(button.Tag, distantDay));
        Check(window.ShowingCalendar && window.SelectedDay == distantDay
            && ((TextBlock)window.FindName("MonthLabel")).Text == distantDay.ToString("yyyy 年 M 月"),
            "Confirming a search result stays in the calendar and moves to its month across years");
        Check(((Border)locatedCell.Content).BorderThickness.Top == 2
            && ((TextBlock)window.FindName("CalendarHint")).Text.Contains(distantDay.ToString("yyyy / MM / dd")),
            "The result date is visibly highlighted with an explicit location hint");
        Check(searchInput.Text.Length == 0 && ((Grid)window.FindName("CalendarGrid")).Visibility == Visibility.Visible,
            "Confirming restores the month grid and clears the search input");
        Layout(root);
        Capture(root, Path.Combine(directory, "calendar-located.png"));
        locatedCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(window.ShowingCalendar && window.ShowingDayDetails && list.Children.Count == 1 && window.SelectedDay == distantDay,
            "Clicking the highlighted calendar date opens matching tasks inside the calendar overlay");
        input.Text = "日历浮窗新增任务";
        add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(window.ShowingDayDetails && book.Find(distantDay)!.Goals.Count == 2 && list.Children.Count == 2,
            "Adding a task in calendar details stays in the overlay and writes only to the selected date");
        window.ShowDay(DateTime.Today);
        Check(input.Text == "搜索定位前未提交的草稿", "Global search navigation preserves the original day's unsubmitted draft");
        window.ShowCalendar();
        searchInput.Text = "不存在的任务关键词";
        Check(searchResults.Items.Count == 0 && !locateButton.IsEnabled
            && ((TextBlock)window.FindName("CalendarSearchEmpty")).Visibility == Visibility.Visible,
            "A missing search result displays the empty state and disables date confirmation");
        ((Button)window.FindName("ClearCalendarSearchButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(searchResults.Items.Count == 0 && ((Grid)window.FindName("CalendarGrid")).Visibility == Visibility.Visible,
            "Clearing search restores normal calendar browsing");
        var keySource = System.Windows.Interop.HwndSource.FromHwnd(new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle())!;
        var anotherSearchDay = distantDay.AddMonths(-1);
        book.Get(anotherSearchDay).Goals.Add(new Goal { Text = "跨月项目搜索验证" });
        searchInput.Text = "项目 搜索";
        searchResults.SelectedIndex = 1;
        searchResults.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, keySource, 0, System.Windows.Input.Key.Enter)
            { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent });
        Check(window.ShowingCalendar && window.SelectedDay == anotherSearchDay
            && ((TextBlock)window.FindName("MonthLabel")).Text == anotherSearchDay.ToString("yyyy 年 M 月"),
            "Enter confirms the selected result instead of always jumping to the first match");
        int hides = 0;
        window.HideRequested += () => hides++;
        cells.Children.OfType<Button>().Single(button => Equals(button.Tag, anotherSearchDay)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, keySource, 0, System.Windows.Input.Key.Escape)
            { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent });
        Check(window.ShowingCalendar && !window.ShowingDayDetails && hides == 0,
            "Escape closes date details while keeping the calendar panel open");
        searchInput.Text = "找不到的内容";
        searchInput.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, keySource, 0, System.Windows.Input.Key.Enter)
            { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent });
        Check(window.SelectedDay == anotherSearchDay && window.ShowingCalendar,
            "Enter with no search matches does not navigate or close the calendar");
        window.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, keySource, 0, System.Windows.Input.Key.Escape)
            { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent });
        Check(searchInput.Text.Length == 0 && hides == 0 && window.ShowingCalendar,
            "Escape clears an active calendar search before hiding the panel");
        Layout(root, 500, 570);
        var calendarGrid = (Grid)window.FindName("CalendarGrid");
        var lastCell = (Button)cells.Children[cells.Children.Count - 1];
        Check(lastCell.TransformToAncestor(calendarGrid).Transform(new Point(0, lastCell.ActualHeight)).Y <= calendarGrid.ActualHeight + 1,
            "The calendar and search field fit the smaller supported panel without clipping the final week");
        Layout(root);
        window.ShowDay(DateTime.Today.AddDays(-1));
        row = (Border)list.Children[0];
        ((MenuItem)row.ContextMenu.Items[3]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Check(book.Find(DateTime.Today.AddDays(-1))!.Goals.Count == 0, "Delete removes only the selected goal");
        ((Button)window.FindName("AppearanceButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(window.ShowingAppearance, "Appearance button opens the settings view");
        ((Slider)window.FindName("WidthSlider")).Value = 720;
        ((Slider)window.FindName("HeightSlider")).Value = 740;
        Check(book.Appearance.PanelWidth == 720 && book.Appearance.PanelHeight == 740
            && ((TextBlock)window.FindName("WidthLabel")).Text == "720 px"
            && ((TextBlock)window.FindName("HeightLabel")).Text == "740 px",
            "Width and height controls update the persisted dimensions and visible labels");
        ((Slider)window.FindName("TransparencySlider")).Value = 80;
        ((Slider)window.FindName("CornerSlider")).Value = 32;
        ((Slider)window.FindName("FontSlider")).Value = 18;
        ((Slider)window.FindName("HoverSlider")).Value = .6;
        ((CheckBox)window.FindName("BlurToggle")).IsChecked = false;
        ((Button)window.FindName("SandTint")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(book.Appearance.Transparency == .8 && book.Appearance.CornerRadius == 32 && book.Appearance.FontSize == 18 &&
            book.Appearance.HoverSeconds == .6 && !book.Appearance.Blur && book.Appearance.Tint == "sand", "Appearance controls update the persisted settings");
        Check(((SolidColorBrush)((Border)root).Background).Color.A == 51 && ((Border)root).CornerRadius.TopLeft == 32,
            "Transparency changes the surface tint only, and the window radius updates live");
        var carryToggle = (CheckBox)window.FindName("CarryOverToggle");
        var autoHideToggle = (CheckBox)window.FindName("AutoHideToggle");
        Check(autoHideToggle.IsChecked == false && !book.Appearance.HideOnBlur, "The focus-loss auto-hide switch starts unchecked and disabled");
        autoHideToggle.IsChecked = true;
        Check(book.Appearance.HideOnBlur, "Selecting the focus-loss switch enables auto-hide");
        autoHideToggle.IsChecked = false;
        window.FlushPendingChanges();
        Check(!book.Appearance.HideOnBlur, "Clearing the focus-loss switch disables auto-hide");
        Check(carryToggle.IsChecked == false && !book.AutoCarryOver, "The system carryover switch starts unchecked and disabled");
        var savesBeforeCarryToggle = saves;
        carryToggle.IsChecked = true;
        Check(book.AutoCarryOver && saves == savesBeforeCarryToggle + 1, "Selecting the system switch enables carryover and saves immediately");
        carryToggle.IsChecked = false;
        Check(!book.AutoCarryOver && saves == savesBeforeCarryToggle + 2, "Clearing the system switch disables carryover and saves immediately");
        ((Button)window.FindName("NightTint")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(book.Appearance.Tint == "night" && book.Appearance.Transparency == .25
            && ((SolidColorBrush)((TextBlock)window.FindName("DateLabel")).Foreground).Color.R > 150,
            "Dark mode applies light text and a sufficiently dark surface immediately");
        ((Button)window.FindName("CharcoalTint")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(book.Appearance.Tint == "charcoal" && ((SolidColorBrush)((Border)root).Background).Color.R == 24
            && ((SolidColorBrush)((TextBlock)window.FindName("DateLabel")).Foreground).Color.R > 150,
            "The charcoal background uses its dark surface and light text through the real control");
        Layout(root); Capture(root, Path.Combine(directory, "dark-settings.png"));
        window.ShowDay(DateTime.Today);
        var textSurface = (Grid)((Grid)((Border)list.Children[0]).Child).Children[1];
        Check(((TextBlock)textSurface.Children[0]).FontSize == 18, "Custom font size is applied to existing goals");
        ((Button)window.FindName("ResetAppearanceButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(book.Appearance.Transparency == .65 && book.Appearance.HoverSeconds == .4 && !book.Appearance.HideOnBlur
            && book.Appearance.PanelWidth == 568 && book.Appearance.PanelHeight == 660,
            "Reset restores default appearance and interaction");
        Check(!book.AutoCarryOver && carryToggle.IsChecked == false, "Resetting appearance keeps the independent system carryover setting");
        carryToggle.IsChecked = true;
        window.ShowAppearance(); Layout(root);
        Capture(root, Path.Combine(directory, "appearance.png"));

        book.Days.Clear(); book.Drafts.Clear();
        string[] examples = { "完成产品首页的设计稿", "整理本周的项目进展", "留 30 分钟读一本书", "下班前，清空收件箱" };
        var month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        for (int i = 0; i < DateTime.Today.Day; i++)
        {
            if (i % 3 == 1) continue;
            var record = book.Get(month.AddDays(i));
            for (int j = 0; j < (i % 3) + 2; j++) record.Goals.Add(Example(examples[j], j % 2 == 0));
        }
        book.Get(DateTime.Today).Goals = examples.Select((text, i) => Example(text, i < 2)).ToList();
        input.Text = "";
        window.ShowDay(DateTime.Today);
        Layout(root);
        Check(((TextBlock)window.FindName("CountLabel")).Text == "02 / 04 已完成", "Daily progress reflects completed goals");
        Capture(root, Path.Combine(directory, "today.png"));
        window.ShowCalendar(); Layout(root);
        Capture(root, Path.Combine(directory, "calendar.png"));
        ((Button)window.FindName("NightTint")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.ShowCalendar(); Layout(root);
        Capture(root, Path.Combine(directory, "dark-calendar.png"));
        cells.Children.OfType<Button>().Single(button => Equals(button.Tag, DateTime.Today)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Layout(root); Capture(root, Path.Combine(directory, "dark-calendar-details.png"));
        Check(((ScrollViewer)window.FindName("GoalScroll")).ActualHeight > 60 && window.ShowingDayDetails,
            "Calendar overlay retains a usable scrolling task list");
        Check(root.ActualWidth == 568 && root.ActualHeight == 660, $"Views fit the intended panel bounds ({root.ActualWidth} x {root.ActualHeight})");
        // Inspect dense and long content at the smallest supported practical desktop height.
        window.ShowDay(DateTime.Today);
        input.Text = new string('长', 500); add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Layout(root, 500, 570);
        Check(((ScrollViewer)window.FindName("GoalScroll")).ScrollableHeight > 0, "Long goals scroll instead of pushing the input off-screen");

        var rolloverBook = new Notebook { AutoCarryOver = true };
        var sourceDate = new DateTime(2026, 10, 2);
        rolloverBook.Get(sourceDate).Goals.Add(new Goal { Text = "未完成任务顺延验证" });
        rolloverBook.CarryOverUnfinished(sourceDate.AddDays(3));
        var rolloverWindow = new MainWindow(rolloverBook, () => true);
        rolloverWindow.ShowDay(sourceDate.AddDays(3));
        var carriedRows = (StackPanel)rolloverWindow.FindName("GoalList");
        Check(carriedRows.Children.Count == 1 && ((Border)carriedRows.Children[0]).ToolTip.ToString()!.Contains("2026-10-02"),
            "Monday shows the carried goal and its original date in the tooltip");
        ((MenuItem)((Border)carriedRows.Children[0]).ContextMenu.Items[0]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Check(rolloverBook.Find(sourceDate.AddDays(3))!.Goals[0].Completed && !rolloverBook.Find(sourceDate)!.Goals[0].Completed,
            "Completing a carried goal preserves the original day's calendar history");
        Check(!rolloverBook.CarryOverUnfinished(sourceDate.AddDays(4)), "Completion through the real UI stops the next rollover");
        rolloverWindow.Close();
        var historicalBook = new Notebook { AutoCarryOver = true };
        var historicalWindow = new MainWindow(historicalBook, () => { historicalBook.CarryOverUnfinished(DateTime.Today); return true; });
        historicalWindow.ShowDay(DateTime.Today.AddDays(-7));
        ((TextBox)historicalWindow.FindName("GoalInput")).Text = "只补录这一天";
        ((Button)historicalWindow.FindName("AddButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        historicalWindow.FlushPendingChanges(); historicalBook.CarryOverUnfinished(DateTime.Today.AddDays(30));
        Check(historicalBook.Days.Count == 1 && historicalBook.Find(DateTime.Today.AddDays(-7))!.Goals.Single().Backfilled,
            "Real historical input does not carry forward on add, later saves, or future date changes");
        historicalWindow.Close();
        File.WriteAllLines(Path.Combine(directory, "ui-checks.txt"), report.Append($"{report.Count} UI checks passed."));
        window.FlushPendingChanges();
        window.Close();
    }

    private static Goal Example(string text, bool done) => new()
    {
        Text = text, Completed = done,
        Strokes = done ? new() { new() { new(0, .52), new(.13, .49), new(.30, .55), new(.48, .48), new(.64, .52) } } : new()
    };

    private static void Layout(FrameworkElement root, double width = 568, double height = 660)
    {
        root.Width = width; root.Height = height;
        root.Measure(new Size(width, height)); root.Arrange(new Rect(0, 0, width, height)); root.UpdateLayout();
    }

    private static void Capture(FrameworkElement root, string path)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            // Off-screen WPF cannot capture a DWM backdrop; use a soft neutral preview background.
            var background = new LinearGradientBrush(Color.FromRgb(224, 234, 221), Color.FromRgb(238, 237, 222), 35);
            dc.DrawRectangle(background, null, new Rect(0, 0, 648, 740));
            dc.PushTransform(new TranslateTransform(40, 40));
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(20, 35, 58, 41)), null, new Rect(-3, 6, 574, 660), 18, 18);
            dc.DrawRectangle(new VisualBrush(root) { AutoLayoutContent = false }, null, new Rect(0, 0, 568, 660));
            dc.Pop();
        }
        var bitmap = new RenderTargetBitmap(1296, 1480, 192, 192, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }
}
