using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Dayline;

public record InkPoint(double X, double Y);
public sealed class Goal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = "";
    public bool Completed { get; set; }
    public bool Backfilled { get; set; }
    public List<List<InkPoint>> Strokes { get; set; } = new();
    public string? CarriedFrom { get; set; }
    public string? CarriedTo { get; set; }
}

public sealed class DayRecord
{
    public List<Goal> Goals { get; set; } = new();
}

public sealed record GoalSearchResult(DateTime Day, Goal Goal);

public sealed class Notebook
{
    public Dictionary<string, DayRecord> Days { get; set; } = new();
    public Dictionary<string, string> Drafts { get; set; } = new();
    public AppearanceSettings Appearance { get; set; } = new();
    public bool AutoCarryOver { get; set; }
    public static string Key(DateTime day) => day.ToString("yyyy-MM-dd");
    public DayRecord? Find(DateTime day) => Days.GetValueOrDefault(Key(day));
    public DayRecord Get(DateTime day)
    {
        if (!Days.TryGetValue(Key(day), out var record))
            Days[Key(day)] = record = new DayRecord();
        return record;
    }

    public IReadOnlyList<GoalSearchResult> SearchGoals(string? query)
    {
        var terms = (query ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (terms.Length == 0) return Array.Empty<GoalSearchResult>();
        return Days.OrderByDescending(entry => entry.Key, StringComparer.Ordinal)
            .SelectMany(entry => entry.Value.Goals
                .Where(goal => terms.All(term => goal.Text.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .Select(goal => new GoalSearchResult(DateTime.ParseExact(entry.Key, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture), goal)))
            .ToList();
    }

    public static DateTime NextWeekday(DateTime day)
    {
        do { day = day.Date.AddDays(1); }
        while (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
        return day;
    }

    public bool CarryOverUnfinished(DateTime today)
    {
        if (!AutoCarryOver) return false;
        today = today.Date;
        bool changed = false;
        foreach (var entry in Days.OrderBy(d => d.Key, StringComparer.Ordinal).ToArray())
        {
            var sourceDay = DateTime.ParseExact(entry.Key, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            if (sourceDay >= today) continue;
            foreach (var original in entry.Value.Goals.ToArray())
            {
                var goal = original;
                var day = sourceDay;
                while (day < today && !goal.Completed && !goal.Backfilled && goal.CarriedTo == null)
                {
                    var next = NextWeekday(day);
                    var destination = Get(next);
                    // Keep one logical task ID across daily snapshots; text equality is not task identity.
                    var carried = destination.Goals.FirstOrDefault(g => g.Id == goal.Id);
                    if (carried == null)
                    {
                        carried = new Goal { Id = goal.Id, Text = goal.Text, CarriedFrom = Key(day) };
                        destination.Goals.Add(carried);
                    }
                    goal.CarriedTo = Key(next);
                    goal = carried;
                    day = next;
                    changed = true;
                }
            }
        }
        return changed;
    }
}

public sealed class NotebookStore(string directory)
{
    public string DirectoryPath { get; } = directory;
    public string FilePath => Path.Combine(DirectoryPath, "journal.json");
    public string? RecoveryMessage { get; private set; }
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public Notebook Load()
    {
        if (!File.Exists(FilePath)) return new Notebook();
        try { return Read(FilePath); }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // Keep a damaged original before any subsequent save can replace it.
            File.Copy(FilePath, FilePath + ".damaged-" + DateTime.Now.ToString("yyyyMMdd-HHmmssfff"), true);
            if (File.Exists(FilePath + ".bak"))
            {
                try
                {
                    var recovered = Read(FilePath + ".bak");
                    RecoveryMessage = "已从上次备份恢复记录，原文件已保留。";
                    return recovered;
                }
                catch (Exception backupError) when (backupError is IOException or JsonException or UnauthorizedAccessException) { }
            }
            throw new IOException("记录文件无法读取。原文件已保留，请检查数据目录。", e);
        }
    }

    private static Notebook Read(string path)
    {
        var book = JsonSerializer.Deserialize<Notebook>(File.ReadAllText(path), JsonOptions)
            ?? throw new JsonException("Empty notebook");
        if (book.Drafts == null || book.Days == null || book.Days.Any(d => !DateTime.TryParseExact(d.Key, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out _) || d.Value?.Goals == null ||
                d.Value.Goals.Any(g => g == null || g.Text == null || g.Strokes == null ||
                    g.Strokes.Any(s => s == null || s.Any(p => p == null || !double.IsFinite(p.X) || !double.IsFinite(p.Y))))))
            throw new JsonException("Invalid notebook structure");
        book.Appearance ??= new AppearanceSettings();
        book.Appearance.Normalize();
        return book;
    }

    public void Save(Notebook book)
    {
        Directory.CreateDirectory(DirectoryPath);
        var temporary = FilePath + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, book, JsonOptions);
            stream.Flush(true);
        }
        if (File.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".bak");
        else File.Move(temporary, FilePath);
    }
}

public sealed class AppearanceSettings
{
    public double Transparency { get; set; } = 0.65;
    public bool Blur { get; set; } = true;
    public double CornerRadius { get; set; } = 26;
    public double FontSize { get; set; } = 15;
    public double HoverSeconds { get; set; } = 0.4;
    public bool HideOnBlur { get; set; }
    public double PanelWidth { get; set; } = 568;
    public double PanelHeight { get; set; } = 660;
    public string Tint { get; set; } = "sage";
    public void Normalize()
    {
        Transparency = Clamp(Transparency, 0.15, 0.85, 0.65);
        CornerRadius = Clamp(CornerRadius, 12, 36, 26);
        FontSize = Clamp(FontSize, 13, 20, 15);
        HoverSeconds = Clamp(HoverSeconds, 0.2, 2, 0.4);
        PanelWidth = Clamp(PanelWidth, 500, 1600, 568);
        PanelHeight = Clamp(PanelHeight, 570, 1400, 660);
        if (Tint is not ("sage" or "white" or "sand" or "night" or "charcoal")) Tint = "sage";
    }
    private static double Clamp(double value, double min, double max, double fallback) => double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
}

public static class InkRules
{
    // Intersect every segment with the text's central band, including fast two-point drags.
    public static bool Completes(IReadOnlyList<InkPoint> stroke, double left, double right, double top, double bottom)
    {
        if (stroke.Count < 2 || right <= left || bottom <= top) return false;
        double min = double.PositiveInfinity, max = double.NegativeInfinity;
        for (int i = 1; i < stroke.Count; i++)
        {
            var a = stroke[i - 1]; var b = stroke[i];
            double start = 0, end = 1;
            var dy = b.Y - a.Y;
            if (Math.Abs(dy) < 0.00001)
            {
                if (a.Y < top || a.Y > bottom) continue;
            }
            else
            {
                var t1 = (top - a.Y) / dy; var t2 = (bottom - a.Y) / dy;
                start = Math.Max(0, Math.Min(t1, t2));
                end = Math.Min(1, Math.Max(t1, t2));
                if (end < start) continue;
            }
            var x1 = a.X + (b.X - a.X) * start;
            var x2 = a.X + (b.X - a.X) * end;
            var lo = Math.Max(left, Math.Min(x1, x2));
            var hi = Math.Min(right, Math.Max(x1, x2));
            if (hi < lo) continue;
            min = Math.Min(min, lo); max = Math.Max(max, hi);
        }
        return max - min >= (right - left) * 0.35;
    }
}

public readonly record struct PanelBounds(int Left, int Top, int Width, int Height)
{
    public int Right => Left + Width;
    public int Bottom => Top + Height;
    public static PanelBounds Calculate(int workLeft, int workTop, int workWidth, int workHeight, double scale, double panelWidth = 568, double panelHeight = 660)
    {
        int width = (int)Math.Round(Math.Min(panelWidth, workWidth / scale - 24) * scale);
        int height = (int)Math.Round(Math.Min(panelHeight, workHeight / scale - 28) * scale);
        return new(workLeft + (workWidth - width) / 2, workTop + (int)(12 * scale), width, height);
    }
    public bool IsTopTrigger(int x, int y, int screenTop) => x >= Left && x < Right && y >= screenTop && y <= screenTop + 2;
    public bool IsEntryBridge(int x, int y, int screenTop, double radius) => x >= Left && x < Right && y >= screenTop && y < Top + radius;
    public bool Contains(int x, int y, double radius = 0)
    {
        if (x < Left || x >= Right || y < Top || y >= Bottom) return false;
        radius = Math.Clamp(radius, 0, Math.Min(Width, Height) / 2.0);
        double dx = x - Math.Clamp(x, Left + radius, Right - radius);
        double dy = y - Math.Clamp(y, Top + radius, Bottom - radius);
        return dx * dx + dy * dy <= radius * radius;
    }
}

public sealed class RevealTiming(DateTime now)
{
    private DateTime lastSeen = now;
    private DateTime lastHour = Hour(now);
    private DateTime? edgeSince;
    private bool edgeTriggered;
    private bool pointerArmed;
    private bool waitingForEntry;
    private DateTime? pointerOutsideSince;
    public void BeginPointerWatch(bool fromTop)
    {
        pointerArmed = waitingForEntry = fromTop;
        pointerOutsideSince = null;
    }
    public bool PointerLeft(DateTime time, bool inside, bool inEntryBridge, bool dragging)
    {
        if (inside)
        {
            pointerArmed = true; waitingForEntry = false; pointerOutsideSince = null;
            return false;
        }
        if (!pointerArmed || dragging || (waitingForEntry && inEntryBridge))
        {
            pointerOutsideSince = null;
            return false;
        }
        pointerOutsideSince ??= time;
        return time - pointerOutsideSince >= TimeSpan.FromMilliseconds(120);
    }
    public DateTime? ReminderDeadline { get; private set; }
    public void BeginReminder(DateTime time) => ReminderDeadline = time.AddSeconds(10);
    public void CancelReminder() => ReminderDeadline = null;
    public void TouchReminder(DateTime time) { if (ReminderDeadline.HasValue) ReminderDeadline = time.AddSeconds(10); }
    public bool ReminderExpired(DateTime time, bool interacting)
    {
        if (interacting) TouchReminder(time);
        return ReminderDeadline.HasValue && time >= ReminderDeadline.Value;
    }
    private static DateTime Hour(DateTime time) => time.Date.AddHours(time.Hour);

    public bool AtEdge(DateTime time, bool atEdge, double hoverSeconds = 0.4)
    {
        if (!atEdge) { edgeSince = null; edgeTriggered = false; return false; }
        edgeSince ??= time;
        if (edgeTriggered || time - edgeSince < TimeSpan.FromMilliseconds(Math.Round(hoverSeconds * 1000))) return false;
        edgeTriggered = true;
        return true;
    }

    public bool HourDue(DateTime time)
    {
        var hour = Hour(time);
        var elapsed = time - lastSeen;
        var due = hour > lastHour && elapsed >= TimeSpan.Zero && elapsed < TimeSpan.FromSeconds(30)
            && time - hour < TimeSpan.FromSeconds(10);
        lastHour = hour;
        lastSeen = time;
        return due;
    }
}
