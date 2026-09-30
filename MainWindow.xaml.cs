using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Dayline;

public partial class MainWindow : Window
{
    private readonly Notebook book;
    private readonly Func<bool> save;
    private DateTime selectedDay = DateTime.Today;
    private DateTime calendarMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime? locatedCalendarDay;
    private readonly Dictionary<string, string> drafts;
    private readonly System.Windows.Threading.DispatcherTimer draftTimer = new() { Interval = TimeSpan.FromMilliseconds(450) };
    private bool rendering;
    private bool drawing;
    private int openMenus;
    private TextBox? editing;
    private bool hiding;
    private bool appearanceReady;
    private bool syncingAppearance;
    private bool nativeReady;
    private bool revealing;
    private ContextMenu? activeMenu;
    private readonly System.Windows.Threading.DispatcherTimer blurHideTimer = new() { Interval = TimeSpan.FromMilliseconds(160) };
    public AppearanceSettings Appearance => book.Appearance;
    public bool IsBusy => drawing || editing != null || openMenus > 0 || !string.IsNullOrWhiteSpace(GoalInput.Text)
        || (ShowingCalendar && !string.IsNullOrWhiteSpace(CalendarSearchInput.Text));
    public bool IsInteracting => drawing || editing != null || openMenus > 0;
    public bool ShowingCalendar => CalendarView.Visibility == Visibility.Visible;
    public bool ShowingAppearance => AppearanceView.Visibility == Visibility.Visible;
    public DateTime SelectedDay => selectedDay;
    public event Action? Interaction;
    public event Action? HideRequested;

    public MainWindow(Notebook book, Func<bool> save)
    {
        this.book = book; this.save = save; drafts = book.Drafts;
        draftTimer.Tick += (_, _) => { draftTimer.Stop(); Save(); };
        InitializeComponent();
        appearanceReady = true;
        SyncAppearanceControls();
        ApplyAppearance();
        SourceInitialized += (_, _) => { nativeReady = true; Native.ApplyGlass(this, Appearance); };
        GlassSurface.SizeChanged += (_, _) => UpdateSurfaceClip();
        Deactivated += (_, _) => { if (Appearance.HideOnBlur) blurHideTimer.Start(); };
        Activated += (_, _) => blurHideTimer.Stop();
        blurHideTimer.Tick += (_, _) =>
        {
            if (openMenus > 0 || drawing) return;
            blurHideTimer.Stop();
            if (Appearance.HideOnBlur && IsVisible && !IsActive && !hiding) HideRequested?.Invoke();
        };
        Closed += (_, _) => { blurHideTimer.Stop(); draftTimer.Stop(); };
        PreviewMouseDown += (_, _) => Interaction?.Invoke();
        PreviewKeyDown += Window_KeyDown;
        SizeChanged += (_, _) => UpdateProgress();
        RenderDay();
    }

    public void ShowDay(DateTime date)
    {
        FinishEditing();
        drafts[Notebook.Key(selectedDay)] = GoalInput.Text;
        selectedDay = date.Date;
        DayView.Visibility = Visibility.Visible;
        CalendarView.Visibility = Visibility.Collapsed;
        AppearanceView.Visibility = Visibility.Collapsed;
        AppearanceButton.Background = Brushes.Transparent;
        CalendarButton.Background = Brushes.Transparent;
        RenderDay();
    }

    public void ShowCalendar()
    {
        FinishEditing();
        calendarMonth = new DateTime(selectedDay.Year, selectedDay.Month, 1);
        DayView.Visibility = Visibility.Collapsed;
        CalendarView.Visibility = Visibility.Visible;
        AppearanceView.Visibility = Visibility.Collapsed;
        AppearanceButton.Background = Brushes.Transparent;
        TodayButton.Visibility = Visibility.Visible;
        CalendarButton.Background = Brush("#2077917E");
        locatedCalendarDay = null;
        CalendarSearchInput.Clear();
        UpdateCalendarSearch();
        RenderCalendar();
    }

    public void ShowAppearance()
    {
        FinishEditing();
        DayView.Visibility = CalendarView.Visibility = Visibility.Collapsed;
        AppearanceView.Visibility = Visibility.Visible;
        TodayButton.Visibility = Visibility.Visible;
        CalendarButton.Background = Brushes.Transparent;
        AppearanceButton.Background = Brush("#2077917E");
    }

    private void SyncAppearanceControls()
    {
        syncingAppearance = true;
        Appearance.Normalize();
        TransparencySlider.Value = Appearance.Transparency * 100;
        CornerSlider.Value = Appearance.CornerRadius;
        FontSlider.Value = Appearance.FontSize;
        HoverSlider.Value = Appearance.HoverSeconds;
        BlurToggle.IsChecked = Appearance.Blur;
        AutoHideToggle.IsChecked = Appearance.HideOnBlur;
        syncingAppearance = false;
    }

    private void ApplyAppearance()
    {
        var tint = (Color)ColorConverter.ConvertFromString(Appearance.Tint switch { "white" => "#FFFFFF", "sand" => "#F6EAD6", _ => "#EEF4ED" });
        tint.A = (byte)Math.Round(255 * (1 - Appearance.Transparency));
        GlassSurface.Background = new SolidColorBrush(tint);
        GlassSurface.CornerRadius = new CornerRadius(Appearance.CornerRadius);
        UpdateSurfaceClip();
        TransparencyLabel.Text = $"{Appearance.Transparency:P0}";
        CornerLabel.Text = $"{Appearance.CornerRadius:0} px";
        FontLabel.Text = $"{Appearance.FontSize:0} px";
        HoverLabel.Text = $"{Appearance.HoverSeconds:0.0} 秒";
        SageTint.Content = Appearance.Tint == "sage" ? "✓ 鼠尾草" : "鼠尾草";
        WhiteTint.Content = Appearance.Tint == "white" ? "✓ 雾白" : "雾白";
        SandTint.Content = Appearance.Tint == "sand" ? "✓ 暖砂" : "暖砂";
        if (nativeReady) Native.ApplyGlass(this, Appearance);
    }

    private void UpdateSurfaceClip()
    {
        if (GlassSurface.ActualWidth > 0 && GlassSurface.ActualHeight > 0)
            GlassSurface.Clip = new RectangleGeometry(new Rect(0, 0, GlassSurface.ActualWidth, GlassSurface.ActualHeight), Appearance.CornerRadius, Appearance.CornerRadius);
    }

    private void Appearance_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateAppearanceFromControls();
    private void Appearance_Toggled(object sender, RoutedEventArgs e) => UpdateAppearanceFromControls();
    private void UpdateAppearanceFromControls()
    {
        if (!appearanceReady || syncingAppearance) return;
        Appearance.Transparency = TransparencySlider.Value / 100;
        Appearance.CornerRadius = CornerSlider.Value;
        Appearance.FontSize = FontSlider.Value;
        Appearance.HoverSeconds = HoverSlider.Value;
        Appearance.Blur = BlurToggle.IsChecked == true;
        Appearance.HideOnBlur = AutoHideToggle.IsChecked == true;
        ApplyAppearance();
        draftTimer.Stop(); draftTimer.Start();
        Interaction?.Invoke();
    }

    private void Tint_Click(object sender, RoutedEventArgs e)
    {
        Appearance.Tint = (string)((Button)sender).Tag;
        ApplyAppearance(); Save();
    }
    private void ResetAppearance_Click(object sender, RoutedEventArgs e)
    {
        book.Appearance = new AppearanceSettings();
        SyncAppearanceControls(); ApplyAppearance(); Save();
    }
    private void Appearance_Click(object sender, RoutedEventArgs e) { if (ShowingAppearance) ShowDay(selectedDay); else ShowAppearance(); }

    public void RefreshForNewDay()
    {
        if (ShowingCalendar) { RenderCalendar(); UpdateCalendarSearch(); }
        else if (!ShowingAppearance && !IsBusy) ShowDay(DateTime.Today);
    }

    public void FlushPendingChanges() { FinishEditing(); draftTimer.Stop(); Save(); }

    private void RenderDay()
    {
        rendering = true;
        bool today = selectedDay == DateTime.Today;
        DateLabel.Text = selectedDay.ToString("yyyy / MM / dd") + "     " + selectedDay.ToString("dddd", new System.Globalization.CultureInfo("zh-CN"));
        DayTitle.Text = today ? "把今天，慢慢完成。" : selectedDay.ToString("M 月 d 日") + "的小事。";
        DaySubtitle.Text = today ? "写下想做的事，完成后，轻轻划掉。" : "回头看看，也是一种向前。";
        GoalsLabel.Text = today ? "今天的目标" : "这一天的目标";
        HistoryBadge.Visibility = today ? Visibility.Collapsed : Visibility.Visible;
        TodayButton.Visibility = today ? Visibility.Collapsed : Visibility.Visible;
        InputPlaceholder.Text = today ? "今天，想完成什么？" : "为这一天补上一件小事";
        GoalInput.Text = drafts.GetValueOrDefault(Notebook.Key(selectedDay), "");
        GoalList.Children.Clear();
        var goals = book.Find(selectedDay)?.Goals ?? new List<Goal>();
        foreach (var goal in goals) GoalList.Children.Add(CreateGoalRow(goal, goals.IndexOf(goal)));
        EmptyState.Visibility = goals.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        CountLabel.Text = $"{goals.Count(g => g.Completed):00} / {goals.Count:00} 已完成";
        UpdateProgress();
        rendering = false;
    }

    private void UpdateProgress()
    {
        if (ProgressFill == null) return;
        var goals = book.Find(selectedDay)?.Goals;
        double ratio = goals?.Count > 0 ? (double)goals.Count(g => g.Completed) / goals.Count : 0;
        ProgressFill.Width = Math.Max(0, (DayView.ActualWidth > 0 ? DayView.ActualWidth : 506) * ratio);
    }

    private Border CreateGoalRow(Goal goal, int index)
    {
        var outer = new Border { Background = Brush(goal.Completed ? "#347E967E" : "#44FFFFFF"), CornerRadius = new CornerRadius(10), Margin = new Thickness(0, 0, 0, 7) };
        if (goal.CarriedFrom != null || goal.CarriedTo != null)
            outer.ToolTip = string.Join("\n", new[]
            {
                goal.CarriedFrom == null ? null : $"从 {goal.CarriedFrom} 顺延",
                goal.CarriedTo == null ? null : $"已顺延至 {goal.CarriedTo}"
            }.Where(line => line != null));
        var row = new Grid { MinHeight = 58 };
        outer.Child = row;
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(39) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        var number = new TextBlock { Text = (index + 1).ToString("00"), FontSize = 10, Foreground = Brush(goal.Completed ? "#9AA79A" : "#A1ADA3"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        row.Children.Add(number);
        var surface = new Grid { Background = Brushes.Transparent, Cursor = Cursors.Pen, ClipToBounds = true, Margin = new Thickness(0, 10, 0, 10) };
        Grid.SetColumn(surface, 1); row.Children.Add(surface);
        var text = new TextBlock { Text = goal.Text, FontSize = Appearance.FontSize, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Foreground = Brush(goal.Completed ? "#7A8C7D" : "#283D30"), LineHeight = Appearance.FontSize + 10, Padding = new Thickness(0, 2, 0, 2) };
        surface.Children.Add(text);
        var ink = new Canvas { IsHitTestVisible = false, ClipToBounds = true };
        surface.Children.Add(ink);
        void PaintSaved()
        {
            ink.Children.Clear();
            foreach (var stroke in goal.Strokes)
                ink.Children.Add(Polyline(stroke.Select(p => new Point(p.X * surface.ActualWidth, p.Y * surface.ActualHeight))));
        }
        surface.SizeChanged += (_, _) => PaintSaved();
        var menu = new ContextMenu();
        var toggle = new MenuItem { Header = goal.Completed ? "撤销完成" : "标记完成" };
        toggle.Click += (_, _) => { ToggleComplete(goal); PersistAndRender(); };
        menu.Items.Add(toggle);
        var edit = new MenuItem { Header = "编辑目标" };
        edit.Click += (_, _) => StartEditing(goal, surface, text, ink);
        menu.Items.Add(edit);
        menu.Items.Add(new Separator());
        var delete = new MenuItem { Header = "删除目标" };
        delete.Click += (_, _) => { book.Get(selectedDay).Goals.Remove(goal); PersistAndRender(); };
        menu.Items.Add(delete);
        menu.Opened += (_, _) => { activeMenu = menu; openMenus++; Interaction?.Invoke(); };
        menu.Closed += (_, _) => { if (activeMenu == menu) activeMenu = null; openMenus = Math.Max(0, openMenus - 1); Interaction?.Invoke(); };
        outer.ContextMenu = menu;
        var more = new Button { Content = "⋯", Padding = new Thickness(3, 8, 5, 8), Foreground = Brush("#91A08F"), VerticalAlignment = VerticalAlignment.Center, ToolTip = "编辑 / 撤销 / 删除", FontSize = 15 };
        System.Windows.Automation.AutomationProperties.SetName(more, "目标操作：" + goal.Text);
        more.Click += (_, _) => { menu.PlacementTarget = more; menu.IsOpen = true; };
        Grid.SetColumn(more, 2); row.Children.Add(more);

        List<InkPoint>? current = null;
        System.Windows.Shapes.Polyline? live = null;
        void Append(Point p)
        {
            if (surface.ActualWidth <= 0 || surface.ActualHeight <= 0 || current == null || live == null) return;
            var x = Math.Clamp(p.X, 0, surface.ActualWidth); var y = Math.Clamp(p.Y, 0, surface.ActualHeight);
            if (live.Points.Count > 0 && (live.Points[^1] - new Point(x, y)).Length < 1.5) return;
            current.Add(new InkPoint(x / surface.ActualWidth, y / surface.ActualHeight));
            live.Points.Add(new Point(x, y));
        }
        surface.MouseLeftButtonDown += (_, e) =>
        {
            if (editing != null) return;
            if (e.ClickCount == 2) { StartEditing(goal, surface, text, ink); e.Handled = true; return; }
            if (goal.Completed) return;
            current = new List<InkPoint>(); live = Polyline(Array.Empty<Point>()); ink.Children.Add(live);
            drawing = true; surface.CaptureMouse(); Append(e.GetPosition(surface)); e.Handled = true;
        };
        surface.MouseMove += (_, e) => { if (drawing && current != null) Append(e.GetPosition(surface)); };
        surface.MouseLeftButtonUp += (_, e) =>
        {
            if (current == null) return;
            Append(e.GetPosition(surface));
            var finished = current; current = null; drawing = false; surface.ReleaseMouseCapture();
            var measured = new FormattedText(goal.Text, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch), text.FontSize, text.Foreground, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            var textRight = Math.Min(1, Math.Max(0.03, measured.Width / Math.Max(1, surface.ActualWidth)));
            if (InkRules.Completes(finished, 0, textRight, 0.18, 0.82))
            {
                goal.Strokes.Add(finished); goal.Completed = true; PersistAndRender();
            }
            else PaintSaved();
            Interaction?.Invoke(); e.Handled = true;
        };
        surface.LostMouseCapture += (_, _) => { if (current != null) { current = null; drawing = false; PaintSaved(); } };
        return outer;
    }

    private void StartEditing(Goal goal, Grid surface, TextBlock text, Canvas ink)
    {
        FinishEditing();
        text.Visibility = Visibility.Hidden; ink.Visibility = Visibility.Hidden;
        var editor = new TextBox { Text = goal.Text, FontSize = Appearance.FontSize, MaxLength = 2000, TextWrapping = TextWrapping.Wrap, VerticalContentAlignment = VerticalAlignment.Center };
        editing = editor;
        surface.Children.Add(editor);
        bool finished = false;
        void Commit(bool cancel)
        {
            if (finished) return;
            finished = true; editing = null;
            if (!cancel && !string.IsNullOrWhiteSpace(editor.Text) && goal.Text != editor.Text.Trim())
            {
                goal.Text = editor.Text.Trim(); goal.Strokes.Clear(); goal.Completed = false; Save();
            }
            Dispatcher.BeginInvoke(new Action(RenderDay));
        }
        editor.LostKeyboardFocus += (_, _) => Commit(false);
        editor.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape) { Commit(e.Key == Key.Escape); e.Handled = true; }
        };
        editor.Focus(); editor.SelectAll(); Interaction?.Invoke();
    }

    private void FinishEditing()
    {
        if (editing != null) Keyboard.ClearFocus();
    }

    private void ToggleComplete(Goal goal)
    {
        goal.Completed = !goal.Completed; goal.Strokes.Clear();
        if (goal.Completed)
            goal.Strokes.Add(new List<InkPoint> { new(0, 0.5), new(Math.Min(0.98, Math.Max(0.08, goal.Text.Length * Appearance.FontSize / 424)), 0.5) });
    }

    private void AddGoal()
    {
        var input = GoalInput.Text.Trim();
        if (input.Length == 0) return;
        foreach (var line in input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            book.Get(selectedDay).Goals.Add(new Goal { Text = line });
        drafts.Remove(Notebook.Key(selectedDay));
        PersistAndRender(); GoalScroll.ScrollToBottom(); GoalInput.Focus();
    }

    private void PersistAndRender() { Save(); RenderDay(); }
    private void Save() => SetSaveState(save());
    public void SetSaveState(bool success, string? message = null)
    {
        SaveLabel.Text = message ?? (success ? "已保存在本机" : "保存失败 · 记录暂留内存，请勿退出");
        SaveDot.Fill = Brush(success ? "#89A08C" : "#C57A62");
        SaveLabel.ToolTip = SaveLabel.Text;
    }

    private void RenderCalendar()
    {
        MonthLabel.Text = calendarMonth.ToString("yyyy 年 M 月");
        CalendarCells.Children.Clear();
        int offset = ((int)calendarMonth.DayOfWeek + 6) % 7;
        var start = calendarMonth.AddDays(-offset);
        for (int i = 0; i < 42; i++)
        {
            var day = start.AddDays(i); bool current = day.Month == calendarMonth.Month;
            bool today = day.Date == DateTime.Today;
            bool located = day.Date == locatedCalendarDay;
            var record = book.Find(day);
            var button = new Button { Tag = day.Date, Padding = new Thickness(0), Margin = new Thickness(2), HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch, Opacity = current ? 1 : 0.32 };
            System.Windows.Automation.AutomationProperties.SetName(button, day.ToString("yyyy年M月d日") + $"，{record?.Goals.Count ?? 0} 个目标" + (located ? "，搜索定位" : ""));
            var card = new Border { CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(located ? 2 : 1), BorderBrush = Brush(located ? "#426C50" : today ? "#73937A" : "#167F9781"), Background = Brush(located ? "#909FC9A0" : today ? "#50D6E3D4" : record?.Goals.Count > 0 ? "#75FFFFFF" : "#22FFFFFF"), Padding = new Thickness(6, 5, 6, 4), ClipToBounds = true, MinHeight = 32 };
            button.Content = card;
            var content = new Grid(); card.Child = content;
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var dateText = new TextBlock { Text = day.Day.ToString("00"), FontSize = 10, Foreground = Brush(located || today ? "#42644B" : "#78887C"), FontWeight = located || today ? FontWeights.SemiBold : FontWeights.Normal };
            content.Children.Add(dateText);
            if (record?.Goals.Count > 0)
            {
                var mini = new StackPanel { Margin = new Thickness(0, 4, 0, 0), ClipToBounds = true };
                foreach (var goal in record.Goals.Take(3))
                    mini.Children.Add(new TextBlock { Text = goal.Text, FontSize = 5.5, LineHeight = 9, Foreground = Brush(goal.Completed ? "#91A090" : "#758776"), TextTrimming = TextTrimming.CharacterEllipsis, TextDecorations = goal.Completed ? TextDecorations.Strikethrough : null });
                Grid.SetRow(mini, 1); content.Children.Add(mini);
                dateText.Text += "  ·";
                button.ToolTip = day.ToString("M 月 d 日") + $" · {record.Goals.Count(g => g.Completed)}/{record.Goals.Count} 已完成\n" + string.Join("\n", record.Goals.Take(8).Select(g => (g.Completed ? "✓ " : "· ") + g.Text));
            }
            button.Click += (_, _) => ShowDay(day);
            CalendarCells.Children.Add(button);
        }
    }

    private void UpdateCalendarSearch()
    {
        if (CalendarSearchResults == null) return;
        bool searching = !string.IsNullOrWhiteSpace(CalendarSearchInput.Text);
        CalendarSearchPlaceholder.Visibility = string.IsNullOrEmpty(CalendarSearchInput.Text) ? Visibility.Visible : Visibility.Collapsed;
        ClearCalendarSearchButton.Visibility = CalendarSearchInput.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        CalendarMonthHeader.Visibility = searching ? Visibility.Collapsed : Visibility.Visible;
        CalendarGrid.Visibility = searching ? Visibility.Collapsed : Visibility.Visible;
        CalendarSearchView.Visibility = searching ? Visibility.Visible : Visibility.Collapsed;
        CalendarHint.Text = searching ? "搜索范围：所有日期 · 已完成和未完成任务"
            : locatedCalendarDay is DateTime day ? $"已定位 {day:yyyy / MM / dd} · 点击高亮日期查看任务" : "点击日期，翻开那一天";
        CalendarSearchResults.Items.Clear();
        if (!searching) return;
        var results = book.SearchGoals(CalendarSearchInput.Text);
        foreach (var result in results)
        {
            var content = new StackPanel();
            content.Children.Add(new TextBlock { Text = result.Day.ToString("yyyy / MM / dd") + (result.Goal.Completed ? "   ·   已完成" : "   ·   未完成"), FontSize = 10, Foreground = Brush("#788580"), Margin = new Thickness(0, 0, 0, 5) });
            content.Children.Add(new TextBlock { Text = result.Goal.Text, FontSize = 13, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, MaxHeight = 40 });
            var item = new ListBoxItem { Content = content, Tag = result, ToolTip = result.Goal.Text };
            System.Windows.Automation.AutomationProperties.SetName(item, result.Day.ToString("yyyy年M月d日") + "，" + result.Goal.Text + (result.Goal.Completed ? "，已完成" : "，未完成"));
            CalendarSearchResults.Items.Add(item);
        }
        CalendarSearchSummary.Text = $"找到 {results.Count} 条任务 · 按日期从新到旧";
        CalendarSearchEmpty.Visibility = results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        CalendarSearchResults.SelectedIndex = results.Count > 0 ? 0 : -1;
    }

    private void LocateSelectedCalendarSearch()
    {
        if (CalendarSearchResults.SelectedItem is not ListBoxItem { Tag: GoalSearchResult result }) return;
        // Reuse date switching so the original day's unsubmitted draft stays with its date.
        ShowDay(result.Day);
        ShowCalendar();
        locatedCalendarDay = result.Day;
        RenderCalendar();
        CalendarHint.Text = $"已定位 {result.Day:yyyy / MM / dd} · 点击高亮日期查看任务";
        CalendarCells.Children.OfType<Button>().First(button => Equals(button.Tag, result.Day)).Focus();
        Interaction?.Invoke();
    }

    private void CalendarSearch_TextChanged(object sender, TextChangedEventArgs e) { UpdateCalendarSearch(); Interaction?.Invoke(); }
    private void ClearCalendarSearch_Click(object sender, RoutedEventArgs e) { CalendarSearchInput.Clear(); CalendarSearchInput.Focus(); }
    private void LocateCalendarSearch_Click(object sender, RoutedEventArgs e) => LocateSelectedCalendarSearch();
    private void CalendarSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LocateCalendarSearchButton != null) LocateCalendarSearchButton.IsEnabled = CalendarSearchResults.SelectedItem != null;
    }
    private void CalendarSearchResults_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && ItemsControl.ContainerFromElement(CalendarSearchResults, source) is ListBoxItem)
        { LocateSelectedCalendarSearch(); e.Handled = true; }
    }
    private void CalendarSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { LocateSelectedCalendarSearch(); e.Handled = true; }
        else if (sender == CalendarSearchInput && (e.Key == Key.Down || e.Key == Key.Up)
            && CalendarSearchResults.SelectedItem is ListBoxItem item)
        { item.Focus(); e.Handled = true; }
    }

    public void Reveal(bool automatic)
    {
        hiding = false;
        revealing = true;
        blurHideTimer.Stop();
        BeginAnimation(TopProperty, null);
        ShowActivated = !automatic;
        Native.Place(this, Native.PointerScreen(), Appearance);
        if (!IsVisible) Show();
        double destination = Top;
        if (SystemParameters.ClientAreaAnimation)
        {
            var slide = new DoubleAnimation(destination - 28, destination, TimeSpan.FromMilliseconds(130)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            slide.Completed += (_, _) => revealing = false;
            BeginAnimation(TopProperty, slide);
        }
        else revealing = false;
        if (!automatic) Activate();
    }

    public void Conceal()
    {
        if (!IsVisible || hiding) return;
        blurHideTimer.Stop();
        if (activeMenu != null) activeMenu.IsOpen = false;
        FinishEditing(); hiding = true; revealing = false;
        if (!SystemParameters.ClientAreaAnimation) { Hide(); hiding = false; return; }
        var slide = new DoubleAnimation(Top, Top - 28, TimeSpan.FromMilliseconds(100)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
        slide.Completed += (_, _) =>
        {
            if (!hiding) return;
            Hide(); BeginAnimation(TopProperty, null); hiding = false;
        };
        BeginAnimation(TopProperty, slide);
    }

    public bool ShouldHideForPointer(RevealTiming tracking, DateTime now, int x, int y)
    {
        if (!IsVisible || hiding || revealing) return false;
        var bounds = Native.Bounds(new System.Windows.Interop.WindowInteropHelper(this).Handle);
        double radius = Appearance.CornerRadius * VisualTreeHelper.GetDpi(this).DpiScaleX;
        bool inside = bounds.Contains(x, y, radius) || (activeMenu?.IsOpen == true && Native.ContainsPopup(activeMenu, x, y));
        bool bridge = bounds.IsEntryBridge(x, y, Native.ScreenTop(this), radius);
        bool dragging = drawing || (openMenus == 0 && Mouse.Captured != null);
        return tracking.PointerLeft(now, inside, bridge, dragging);
    }
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        Interaction?.Invoke();
        if (ShowingCalendar && e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        { CalendarSearchInput.Focus(); CalendarSearchInput.SelectAll(); e.Handled = true; return; }
        if (ShowingCalendar && e.Key == Key.Escape && CalendarSearchInput.Text.Length > 0)
        { CalendarSearchInput.Clear(); CalendarSearchInput.Focus(); e.Handled = true; return; }
        if (e.Key == Key.Escape && editing == null) { HideRequested?.Invoke(); e.Handled = true; }
        if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control && Keyboard.FocusedElement is not TextBox)
        {
            var goal = book.Find(selectedDay)?.Goals.LastOrDefault(g => g.Completed);
            if (goal != null) { ToggleComplete(goal); PersistAndRender(); e.Handled = true; }
        }
    }
    private void Input_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (InputPlaceholder == null) return;
        InputPlaceholder.Visibility = string.IsNullOrEmpty(GoalInput.Text) ? Visibility.Visible : Visibility.Collapsed;
        if (!rendering) { drafts[Notebook.Key(selectedDay)] = GoalInput.Text; draftTimer.Stop(); draftTimer.Start(); Interaction?.Invoke(); }
    }
    private void Input_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { AddGoal(); e.Handled = true; } }
    private void Add_Click(object sender, RoutedEventArgs e) => AddGoal();
    private void Calendar_Click(object sender, RoutedEventArgs e) { if (ShowingCalendar) ShowDay(selectedDay); else ShowCalendar(); }
    private void Today_Click(object sender, RoutedEventArgs e) => ShowDay(DateTime.Today);
    private void Hide_Click(object sender, RoutedEventArgs e) => HideRequested?.Invoke();
    private void PreviousMonth_Click(object sender, RoutedEventArgs e) { if (calendarMonth.Year > 1900) calendarMonth = calendarMonth.AddMonths(-1); RenderCalendar(); }
    private void NextMonth_Click(object sender, RoutedEventArgs e) { if (calendarMonth.Year < 9998) calendarMonth = calendarMonth.AddMonths(1); RenderCalendar(); }
    private void ThisMonth_Click(object sender, RoutedEventArgs e) { calendarMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); RenderCalendar(); }
    private static SolidColorBrush Brush(string color) => (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
    private static System.Windows.Shapes.Polyline Polyline(IEnumerable<Point> points) => new() { Points = new PointCollection(points), Stroke = Brush("#9B58735E"), StrokeThickness = 1.6, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round, IsHitTestVisible = false };
}
