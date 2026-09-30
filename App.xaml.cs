using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Dayline;

public partial class App : System.Windows.Application
{
    private Mutex? instance;
    private EventWaitHandle? wake;
    private bool ownsInstance;
    private Forms.NotifyIcon? tray;
    private MainWindow? panel;
    private NotebookStore? store;
    private Notebook? book;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private RevealTiming timing = new(DateTime.Now);
    private DateTime activeDay = DateTime.Today;
    private IntPtr hwnd;
    private bool exiting;
    private bool saveFailed;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length > 0 && e.Args[0] == "--native-check")
        {
            SelfChecks.RunNative(this, e.Args.Length > 1 ? e.Args[1] : Path.Combine(AppContext.BaseDirectory, "preview"));
            return;
        }
        if (e.Args.Length > 0 && e.Args[0] == "--self-test")
        {
            try { SelfChecks.Run(e.Args.Length > 1 ? e.Args[1] : Path.Combine(AppContext.BaseDirectory, "preview")); Shutdown(0); }
            catch (Exception error) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "self-test-error.txt"), error.ToString()); Shutdown(1); }
            return;
        }
        instance = new Mutex(true, "Local\\Dayline.Desktop.v1", out ownsInstance);
        wake = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\Dayline.Desktop.Wake.v1");
        if (!ownsInstance) { wake.Set(); Shutdown(); return; }
        store = new NotebookStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dayline"));
        try { book = store.Load(); }
        catch (Exception error)
        {
            MessageBox.Show(error.Message + "\n\n数据目录：" + store.DirectoryPath, "日迹 · 无法打开记录", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1); return;
        }
        if (book.CarryOverUnfinished(DateTime.Today)) TrySave();
        panel = new MainWindow(book, TrySave);
        MainWindow = panel;
        panel.HideRequested += HidePanel;
        panel.Interaction += () => timing.TouchReminder(DateTime.Now);
        panel.Closing += (_, args) => { if (!exiting) { args.Cancel = true; HidePanel(); } };
        hwnd = new WindowInteropHelper(panel).EnsureHandle();
        HwndSource.FromHwnd(hwnd)?.AddHook(WindowMessage);
        var hotkeyRegistered = Native.RegisterHotKey(hwnd, 1, 0x0001 | 0x0002 | 0x4000, 0x44);
        CreateTray(hotkeyRegistered);
        timer.Tick += Tick;
        timer.Start();
        if (saveFailed) panel.SetSaveState(false);
        else if (store.RecoveryMessage != null) panel.SetSaveState(true, store.RecoveryMessage);
    }

    private bool TrySave()
    {
        if (book == null || store == null) return true;
        try
        {
            if (panel?.IsInteracting != true) book.CarryOverUnfinished(DateTime.Today);
            store.Save(book);
            saveFailed = false;
            if (tray != null) tray.Text = "日迹 · 顶部停留片刻，写下今天的小事";
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            saveFailed = true;
            if (tray != null) tray.Text = "日迹 · 保存失败，请检查磁盘权限或空间";
            return false;
        }
    }

    private void CreateTray(bool hotkeyRegistered)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("打开今天" + (hotkeyRegistered ? "    Ctrl+Alt+D" : ""), null, (_, _) => Reveal(false, true));
        menu.Items.Add("回到日历", null, (_, _) => { Reveal(false, false); panel!.ShowCalendar(); });
        menu.Items.Add("自定义外观", null, (_, _) => { Reveal(false, false); panel!.ShowAppearance(); });
        if (!hotkeyRegistered) menu.Items.Add(new Forms.ToolStripMenuItem("Ctrl+Alt+D 已被占用，可使用顶部唤出") { Enabled = false });
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("打开记录文件夹", null, (_, _) =>
        {
            if (store == null) return;
            Directory.CreateDirectory(store.DirectoryPath);
            Process.Start(new ProcessStartInfo(store.DirectoryPath) { UseShellExecute = true });
        });
        menu.Items.Add("退出日迹", null, (_, _) => ExitApp());
        tray = new Forms.NotifyIcon { Text = "日迹 · 顶部停留片刻，写下今天的小事", ContextMenuStrip = menu, Visible = true };
        using var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Dayline.Assets.Dayline.ico");
        tray.Icon = iconStream != null ? new System.Drawing.Icon(iconStream) : System.Drawing.SystemIcons.Application;
        tray.DoubleClick += (_, _) => Reveal(false, true);
    }

    private void Tick(object? sender, EventArgs args)
    {
        if (panel == null) return;
        var now = DateTime.Now;
        if (wake?.WaitOne(0) == true) Reveal(false, true);
        if (activeDay != now.Date && !panel.IsInteracting)
        {
            activeDay = now.Date;
            panel.FlushPendingChanges();
            panel.RefreshForNewDay();
        }
        if (timing.AtEdge(now, Native.IsAtTop(), panel.Appearance.HoverSeconds) && !panel.IsVisible) Reveal(false, true, fromTop: true);
        if (timing.HourDue(now) && !panel.IsVisible) Reveal(true, true);
        if (panel.IsVisible && Native.GetCursorPos(out var pointer) && panel.ShouldHideForPointer(timing, now, pointer.X, pointer.Y)) HidePanel();
        if (timing.ReminderDeadline.HasValue && panel.IsVisible)
        {
            bool expired = timing.ReminderExpired(now, panel.IsInteracting);
            if (expired) HidePanel();
        }
    }

    private void Reveal(bool automatic, bool today, bool fromTop = false)
    {
        if (panel == null) return;
        if (today && !panel.IsBusy) panel.ShowDay(DateTime.Today);
        if (automatic) timing.BeginReminder(DateTime.Now); else timing.CancelReminder();
        timing.BeginPointerWatch(fromTop);
        panel.Reveal(automatic);
    }

    private void HidePanel()
    {
        timing.CancelReminder();
        timing.BeginPointerWatch(false);
        panel?.FlushPendingChanges();
        panel?.Conceal();
    }

    private IntPtr WindowMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == 0x0312 && wParam.ToInt32() == 1)
        {
            if (panel?.IsVisible == true) HidePanel(); else Reveal(false, true);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void ExitApp()
    {
        panel?.FlushPendingChanges();
        if (saveFailed)
        {
            MessageBox.Show("记录暂时无法写入磁盘，日迹将继续运行。请检查数据目录权限和磁盘空间后再退出。", "日迹 · 请先保存记录", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        exiting = true;
        Shutdown();
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        panel?.FlushPendingChanges();
        if (saveFailed) e.Cancel = true;
        base.OnSessionEnding(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        timer.Stop();
        if (hwnd != IntPtr.Zero) Native.UnregisterHotKey(hwnd, 1);
        if (tray != null) { tray.Visible = false; tray.Icon?.Dispose(); tray.Dispose(); }
        wake?.Dispose();
        if (ownsInstance) instance?.ReleaseMutex();
        instance?.Dispose();
        base.OnExit(e);
    }
}
