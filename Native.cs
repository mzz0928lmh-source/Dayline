using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace Dayline;

internal static class Native
{
    internal static bool BackdropEnabled { get; private set; }
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct Accent { public int State, Flags, Color, Animation; }
    [StructLayout(LayoutKind.Sequential)] private struct Composition { public int Attribute; public IntPtr Data; public int Size; }
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(Point point, uint flags);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint x, out uint y);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    [DllImport("user32.dll")] private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref Composition data);
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int ellipseWidth, int ellipseHeight);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
    [DllImport("user32.dll")] private static extern int SetWindowRgn(IntPtr hwnd, IntPtr region, bool redraw);

    internal static void ApplyGlass(Window window, AppearanceSettings settings)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (HwndSource.FromHwnd(hwnd)?.CompositionTarget is { } target) target.BackgroundColor = Colors.Transparent;
        int rounded = 2, dark = settings.Tint == "night" ? 1 : 0, border = unchecked((int)0xFFFFFFFE);
        DwmSetWindowAttribute(hwnd, 33, ref rounded, 4);
        DwmSetWindowAttribute(hwnd, 20, ref dark, 4);
        DwmSetWindowAttribute(hwnd, 34, ref border, 4);
        // Use untinted live blur, then tint once in WPF. Acrylic's own opaque tint
        // plus the WPF surface previously made the panel nearly solid white.
        int noSystemTint = 1;
        DwmSetWindowAttribute(hwnd, 38, ref noSystemTint, 4);
        var accent = new Accent { State = settings.Blur ? 3 : 0, Flags = 0, Color = 0 };
        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Accent>());
        try
        {
            Marshal.StructureToPtr(accent, pointer, false);
            var data = new Composition { Attribute = 19, Data = pointer, Size = Marshal.SizeOf<Accent>() };
            BackdropEnabled = SetWindowCompositionAttribute(hwnd, ref data) != 0 && settings.Blur;
        }
        finally { Marshal.FreeHGlobal(pointer); }
        UpdateCorners(window, settings);
    }

    internal static Forms.Screen PointerScreen()
    {
        GetCursorPos(out var cursor);
        return Forms.Screen.FromPoint(new System.Drawing.Point(cursor.X, cursor.Y));
    }

    internal static bool IsAtTop(AppearanceSettings? settings = null)
    {
        GetCursorPos(out var cursor);
        var screen = Forms.Screen.FromPoint(new System.Drawing.Point(cursor.X, cursor.Y));
        if (cursor.Y < screen.Bounds.Top || cursor.Y > screen.Bounds.Top + 2) return false;
        return Placement(screen, settings).bounds.IsTopTrigger(cursor.X, cursor.Y, screen.Bounds.Top);
    }

    internal static (PanelBounds bounds, double scale) Placement(Forms.Screen screen, AppearanceSettings? settings = null)
    {
        var center = new Point { X = screen.Bounds.Left + screen.Bounds.Width / 2, Y = screen.Bounds.Top + 10 };
        GetDpiForMonitor(MonitorFromPoint(center, 2), 0, out uint dpi, out _);
        double scale = (dpi == 0 ? 96 : dpi) / 96.0;
        return (PanelBounds.Calculate(screen.WorkingArea.Left, screen.WorkingArea.Top,
            screen.WorkingArea.Width, screen.WorkingArea.Height, scale, settings?.PanelWidth ?? 568, settings?.PanelHeight ?? 660), scale);
    }

    internal static void Place(Window window, Forms.Screen screen, AppearanceSettings settings)
    {
        var (bounds, scale) = Placement(screen, settings);
        window.Width = bounds.Width / scale;
        window.Height = bounds.Height / scale;
        SetWindowPos(new WindowInteropHelper(window).Handle, new IntPtr(-1), bounds.Left, bounds.Top, bounds.Width, bounds.Height, 0x0010);
        UpdateCorners(window, settings);
    }

    internal static PanelBounds Bounds(IntPtr hwnd)
    {
        GetWindowRect(hwnd, out var rect);
        return new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    internal static bool ContainsPopup(Visual popup, int x, int y) =>
        PresentationSource.FromVisual(popup) is HwndSource source && Bounds(source.Handle).Contains(x, y);

    internal static int ScreenTop(Window window) => Forms.Screen.FromHandle(new WindowInteropHelper(window).Handle).Bounds.Top;

    internal static void UpdateCorners(Window window, AppearanceSettings settings)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (settings.Blur && GetWindowRect(hwnd, out var bounds))
        {
            var dpi = VisualTreeHelper.GetDpi(window);
            int corner = (int)Math.Round(settings.CornerRadius * 2 * dpi.DpiScaleX);
            var region = CreateRoundRectRgn(0, 0, bounds.Right - bounds.Left + 1, bounds.Bottom - bounds.Top + 1, corner, corner);
            if (SetWindowRgn(hwnd, region, true) == 0) DeleteObject(region);
        }
        else SetWindowRgn(hwnd, IntPtr.Zero, true);
    }
}
