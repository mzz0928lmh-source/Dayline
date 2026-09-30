using Dayline;

int checks = 0;
void Check(bool result, string name)
{
    if (!result) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name); checks++;
}

var time = new DateTime(2026, 9, 29, 9, 59, 59);
var timing = new RevealTiming(time);
Check(!timing.AtEdge(time, true), "Top edge does not reveal immediately");
Check(!timing.AtEdge(time.AddMilliseconds(350), true), "Top edge waits the configured dwell instead of opening by accident");
Check(timing.AtEdge(time.AddMilliseconds(400), true), "Top edge reveals after 0.4 seconds by default");
Check(!timing.AtEdge(time.AddSeconds(3), true), "Top edge reveals only once until pointer leaves");
timing.AtEdge(time.AddSeconds(4), false);
Check(!timing.AtEdge(time.AddSeconds(5), true), "Leaving the edge resets dwell");
Check(timing.AtEdge(time.AddSeconds(7), true), "Re-entry can reveal again");
var customTiming = new RevealTiming(time);
customTiming.AtEdge(time, true, 1.2);
Check(!customTiming.AtEdge(time.AddSeconds(1), true, 1.2) && customTiming.AtEdge(time.AddMilliseconds(1200), true, 1.2), "Custom hover timing is respected");
Check(timing.HourDue(time.AddSeconds(1)), "Reminder crosses hour boundary");
Check(!timing.HourDue(time.AddSeconds(1.1)), "No repeated hourly reminders");
Check(!timing.HourDue(time.AddHours(2).AddSeconds(1)), "Resume from sleep does not replay reminders");
Check(!timing.HourDue(time.AddHours(-1)), "Clock adjustment does not cause false reminder");
Check(!new RevealTiming(time.AddSeconds(1)).HourDue(time.AddSeconds(1.1)), "Starting at the hour does not double reveal");
var midnight = new RevealTiming(new DateTime(2026, 9, 30, 23, 59, 59));
Check(midnight.HourDue(new DateTime(2026, 10, 1, 0, 0, 0)), "Midnight also triggers once");
timing.BeginReminder(time);
Check(!timing.ReminderExpired(time.AddSeconds(9.9), false), "Reminder stays visible for the full ten seconds");
Check(timing.ReminderExpired(time.AddSeconds(10), false), "Reminder dismisses at ten seconds");
timing.BeginReminder(time);
Check(!timing.ReminderExpired(time.AddSeconds(11), true), "Drawing or editing prevents dismissal");
Check(!timing.ReminderExpired(time.AddSeconds(20.9), false), "Interaction postpones dismissal by ten seconds");
Check(timing.ReminderExpired(time.AddSeconds(21), false), "Reminder dismisses after interaction finishes");
timing.CancelReminder();
Check(!timing.ReminderExpired(time.AddDays(1), false), "Manual reveal has no automatic timeout");
Check(InkRules.Completes(new[] { new InkPoint(0, .5), new InkPoint(.9, .5) }, .1, .8, .2, .8), "A fast drag across text completes it");
Check(InkRules.Completes(new[] { new InkPoint(.9, .5), new InkPoint(0, .5) }, .1, .8, .2, .8), "Right-to-left strike works");
Check(!InkRules.Completes(new[] { new InkPoint(.4, .5) }, .1, .8, .2, .8), "A click is not a strike");
Check(!InkRules.Completes(new[] { new InkPoint(0, .05), new InkPoint(1, .05) }, .1, .8, .2, .8), "A drag above text does not complete");
Check(!InkRules.Completes(new[] { new InkPoint(.4, .1), new InkPoint(.4, .9) }, .1, .8, .2, .8), "Vertical drag does not complete");
Check(!InkRules.Completes(new[] { new InkPoint(.4, .5), new InkPoint(.41, .5) }, .1, .8, .2, .8), "Tiny accidental drag does not complete");

var area = PanelBounds.Calculate(0, 0, 1920, 1040, 1);
Check(area.Left == 676 && area.Width == 568 && area.Top == 12, "Trigger geometry uses the same centered placement as the panel");
Check(area.IsTopTrigger(960, 0, 0) && area.IsTopTrigger(area.Left, 2, 0)
    && !area.IsTopTrigger(area.Left - 1, 0, 0) && !area.IsTopTrigger(area.Right, 0, 0), "Only the top strip within panel width can trigger a reveal");
Check(!area.IsTopTrigger(960, 3, 0) && !area.IsTopTrigger(0, 0, 0) && !area.IsTopTrigger(1919, 0, 0),
    "Below the top strip and both screen corners never trigger");
var hidpi = PanelBounds.Calculate(-2560, -1440, 2560, 1380, 1.5);
Check(hidpi.Width == 852 && hidpi.IsTopTrigger(hidpi.Left + 426, -1440, -1440)
    && !hidpi.IsTopTrigger(hidpi.Left - 1, -1440, -1440), "High DPI and monitors above or left of the primary display keep matching boundaries");
var sidebar = PanelBounds.Calculate(60, 0, 1860, 1040, 1);
Check(sidebar.Left == 706 && sidebar.IsTopTrigger(990, 0, 0), "A side taskbar keeps the trigger aligned with the actual window");
var narrow = PanelBounds.Calculate(0, 0, 500, 700, 1);
Check(narrow.Width == 476 && narrow.Left == 12, "Small displays use the reduced panel width for the trigger");
Check(area.Contains(960, 300, 26) && !area.Contains(area.Left, area.Top, 26)
    && !area.Contains(960, area.Bottom, 26), "Panel hit testing includes its body and excludes transparent corners and outside pixels");
Check(area.IsEntryBridge(960, 6, 0, 26) && !area.IsEntryBridge(area.Left - 1, 6, 0, 26), "The entry bridge covers the gap above the panel, not the whole screen");
var pointerTiming = new RevealTiming(time);
pointerTiming.BeginPointerWatch(true);
Check(!pointerTiming.PointerLeft(time, false, true, false)
    && !pointerTiming.PointerLeft(time.AddSeconds(2), false, true, false), "A top-hover reveal stays open while the pointer moves through the entry gap");
pointerTiming.PointerLeft(time.AddSeconds(3), true, false, false);
Check(!pointerTiming.PointerLeft(time.AddSeconds(4), false, false, false)
    && pointerTiming.PointerLeft(time.AddMilliseconds(4120), false, false, false), "Leaving the panel retracts after a short 120 ms debounce");
Check(!pointerTiming.PointerLeft(time.AddSeconds(5), true, false, false), "Re-entering the panel cancels a pending hide");
pointerTiming.PointerLeft(time.AddSeconds(6), false, true, false);
Check(pointerTiming.PointerLeft(time.AddMilliseconds(6120), false, true, false), "After entering the panel, moving back into the top gap counts as leaving");
pointerTiming.BeginPointerWatch(true);
pointerTiming.PointerLeft(time, false, false, false);
Check(pointerTiming.PointerLeft(time.AddMilliseconds(120), false, false, false), "Moving away from the trigger without entering the panel also retracts it");
pointerTiming.BeginPointerWatch(false);
Check(!pointerTiming.PointerLeft(time, false, false, false)
    && !pointerTiming.PointerLeft(time.AddSeconds(9), false, false, false), "An untouched hourly reminder is not instantly dismissed by a pointer elsewhere");
pointerTiming.PointerLeft(time, true, false, false);
Check(!pointerTiming.PointerLeft(time.AddSeconds(1), false, false, true), "Dragging a stroke or slider beyond the panel can finish before hiding");
pointerTiming.PointerLeft(time.AddSeconds(2), false, false, false);
Check(pointerTiming.PointerLeft(time.AddMilliseconds(2120), false, false, false), "Releasing a drag outside the panel allows it to retract");

var friday = new DateTime(2026, 10, 2);
var monday = friday.AddDays(3);
Check(Notebook.NextWeekday(friday) == monday && Notebook.NextWeekday(friday.AddDays(1)) == monday
    && Notebook.NextWeekday(friday.AddDays(2)) == monday, "Friday, Saturday, and Sunday all roll to Monday");
Check(Notebook.NextWeekday(monday) == monday.AddDays(1), "A weekday rolls to the next weekday");
Check(Notebook.NextWeekday(new DateTime(2027, 12, 31)) == new DateTime(2028, 1, 3), "Weekend skipping works across a year boundary");
var carryBook = new Notebook();
var unfinished = new Goal { Text = "继续完成说明书翻译" };
var completed = new Goal { Text = "已上线的客服", Completed = true };
carryBook.Get(friday).Goals.AddRange(new[] { unfinished, completed });
carryBook.Drafts[Notebook.Key(friday)] = "还没提交的草稿";
Check(!carryBook.CarryOverUnfinished(friday.AddHours(23)), "Today's unfinished goals stay today until the date changes");
Check(carryBook.CarryOverUnfinished(friday.AddDays(1)), "Crossing into Saturday schedules unfinished work for Monday");
Check(carryBook.Find(friday.AddDays(1)) == null && carryBook.Find(friday.AddDays(2)) == null, "Rollover creates no Saturday or Sunday entries");
Check(carryBook.Find(monday)!.Goals.Count == 1 && carryBook.Find(monday)!.Goals[0].Text == unfinished.Text,
    "Only unfinished work is carried forward");
Check(carryBook.Find(friday)!.Goals.Count == 2 && !unfinished.Completed && completed.Completed,
    "Original calendar entries keep their completion states");
Check(carryBook.Drafts.Count == 1 && carryBook.Drafts[Notebook.Key(friday)] == "还没提交的草稿", "Input drafts are not silently promoted to goals");
Check(!carryBook.CarryOverUnfinished(monday) && carryBook.Find(monday)!.Goals.Count == 1,
    "Repeated startup or save does not duplicate carried goals");
Check(carryBook.CarryOverUnfinished(monday.AddDays(2)) && carryBook.Find(monday.AddDays(1))!.Goals.Count == 1
    && carryBook.Find(monday.AddDays(2))!.Goals.Count == 1, "Several days offline catch up through each missed weekday");
var latest = carryBook.Find(monday.AddDays(2))!.Goals[0];
Check(latest.Id == unfinished.Id && latest.CarriedFrom == Notebook.Key(monday.AddDays(1))
    && unfinished.CarriedTo == Notebook.Key(monday), "Rollover preserves task identity and history links");
latest.Completed = true;
Check(!carryBook.CarryOverUnfinished(monday.AddDays(10)) && carryBook.Find(monday.AddDays(3)) == null,
    "Completing the latest task stops all further rollover");
latest.Completed = false;
carryBook.Find(monday.AddDays(2))!.Goals.Remove(latest);
Check(!carryBook.CarryOverUnfinished(monday.AddDays(10)), "Deleting a carried task does not resurrect it from history");
var twins = new Notebook();
twins.Get(monday).Goals.AddRange(new[] { new Goal { Text = "跟进项目" }, new Goal { Text = "跟进项目" } });
twins.CarryOverUnfinished(monday.AddDays(1));
Check(twins.Find(monday.AddDays(1))!.Goals.Count == 2, "Separate tasks with the same text are not incorrectly merged");
var weekendTask = new Notebook();
weekendTask.Get(friday.AddDays(1)).Goals.Add(new Goal { Text = "周末手动添加的任务" });
weekendTask.CarryOverUnfinished(monday);
Check(weekendTask.Find(monday)?.Goals.Count == 1 && weekendTask.Find(friday.AddDays(2)) == null,
    "Manually added weekend tasks carry to Monday, not Sunday");

var searchBook = new Notebook();
var olderSearchDay = new DateTime(2025, 12, 31);
var newerSearchDay = new DateTime(2026, 10, 2);
var searchGoal = new Goal { Text = "完成 Dayline 日历搜索设计", Completed = true };
searchBook.Get(olderSearchDay).Goals.Add(searchGoal);
searchBook.Get(newerSearchDay).Goals.Add(new Goal { Text = "跟进日历搜索验收" });
searchBook.Drafts[Notebook.Key(newerSearchDay)] = "仅在草稿里的关键词";
Check(searchBook.SearchGoals("日历").Select(result => result.Day).SequenceEqual(new[] { newerSearchDay, olderSearchDay }),
    "Global search finds partial task content across months and years, newest date first");
Check(searchBook.SearchGoals(" DAYLINE\t设计 ").Single().Goal == searchGoal,
    "Search ignores English case and matches all whitespace-separated keywords");
Check(searchBook.SearchGoals("设计 dayline").Count == 1 && searchBook.SearchGoals("设计 不存在").Count == 0,
    "Keyword order is flexible and every keyword is required");
Check(searchBook.SearchGoals("日历").Count == 2 && searchBook.SearchGoals("仅在草稿").Count == 0,
    "Completed and unfinished tasks are searchable while unsubmitted drafts stay excluded");
Check(searchBook.SearchGoals(null).Count == 0 && searchBook.SearchGoals(" \t\n ").Count == 0,
    "Empty or whitespace searches return no matches");
searchBook.Get(newerSearchDay).Goals.Add(new Goal { Id = searchGoal.Id, Text = searchGoal.Text });
Check(searchBook.SearchGoals("Dayline").Select(result => result.Day).SequenceEqual(new[] { newerSearchDay, olderSearchDay }),
    "The same carried task remains locatable on each recorded date");
Check(searchBook.Days.Count == 2 && searchGoal.Completed && searchBook.Drafts.Count == 1,
    "Searching does not create dates or mutate task states and drafts");

var directory = Path.Combine(Path.GetTempPath(), "Dayline-check-" + Guid.NewGuid().ToString("N"));
try
{
    var store = new NotebookStore(directory);
    var book = store.Load();
    Check(book.Days.Count == 0, "First launch has no demo records");
    book.Get(time).Goals.Add(new Goal { Text = "完成今天的设计", Completed = true, Strokes = new() { new() { new(0, .5), new(.8, .55) } } });
    book.Drafts[Notebook.Key(time)] = "还没按回车的草稿";
    book.Appearance = new AppearanceSettings { Transparency = .8, CornerRadius = 32, HoverSeconds = .3, Tint = "sand", FontSize = 18, Blur = false, HideOnBlur = false };
    store.Save(book);
    var reloaded = store.Load();
    Check(reloaded.Find(time)!.Goals[0].Text == "完成今天的设计" && reloaded.Find(time)!.Goals[0].Completed, "Chinese goals and completion survive restart");
    Check(reloaded.Find(time)!.Goals[0].Strokes[0][1].Y == .55, "Freehand ink survives restart");
    Check(reloaded.Drafts[Notebook.Key(time)] == "还没按回车的草稿", "Unsubmitted drafts survive restart");
    Check(reloaded.Appearance.Transparency == .8 && reloaded.Appearance.CornerRadius == 32 && reloaded.Appearance.Tint == "sand"
        && reloaded.Appearance.FontSize == 18 && !reloaded.Appearance.Blur && !reloaded.Appearance.HideOnBlur && reloaded.Appearance.HoverSeconds == .3,
        "Appearance and interaction choices survive restart");
    book.Get(time.AddDays(1)).Goals.Add(new Goal { Text = "明天的事" });
    store.Save(book);
    Check(store.Load().Days.Count == 2, "Different dates keep independent records");
    Check(File.Exists(store.FilePath + ".bak"), "Atomic replacement preserves previous backup");
    File.WriteAllText(store.FilePath, "broken JSON");
    var recovered = store.Load();
    Check(recovered.Find(time)!.Goals[0].Text == "完成今天的设计" && store.RecoveryMessage != null, "Corruption recovers previous backup");
    Check(Directory.GetFiles(directory, "*.damaged-*").Length > 0, "Damaged original is preserved");
    File.Delete(store.FilePath + ".bak");
    bool refused = false;
    try { store.Load(); } catch (IOException) { refused = true; }
    Check(refused, "Unrecoverable corruption is never silently overwritten");
    File.WriteAllText(store.FilePath, "{\"Days\":{},\"Drafts\":{}}");
    Check(store.Load().Appearance.Transparency == .65 && store.Load().Appearance.HideOnBlur, "Old journals receive new appearance defaults without migration");
    var invalid = new AppearanceSettings { Transparency = 99, CornerRadius = -5, FontSize = double.NaN, HoverSeconds = double.PositiveInfinity, Tint = "unknown" };
    invalid.Normalize();
    Check(invalid.Transparency == .85 && invalid.CornerRadius == 12 && invalid.FontSize == 15 && invalid.HoverSeconds == .4 && invalid.Tint == "sage", "Invalid settings are constrained to usable values");
    store.Save(twins);
    var afterRestart = store.Load();
    Check(!afterRestart.CarryOverUnfinished(monday.AddDays(1)) && afterRestart.Find(monday.AddDays(1))!.Goals.Count == 2,
        "Saved rollover markers prevent duplicates after restarting");
    afterRestart.CarryOverUnfinished(monday.AddDays(2));
    Check(afterRestart.Find(monday.AddDays(2))!.Goals.Count == 2, "A restarted task continues rolling on the next weekday");
    store.Save(carryBook);
    Check(!store.Load().CarryOverUnfinished(monday.AddDays(20)), "Deletion remains respected after restart");
}
finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
Console.WriteLine($"\n{checks} checks passed.");
