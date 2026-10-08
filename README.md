# 日迹 · Dayline

[**中文**](#中文) ｜ [**English**](#english)

---

<a id="中文"></a>

## 中文

日迹是一款 Windows 本地目标记录与提醒工具。写下每天的目标，用鼠标划线标记完成，按日期查看或搜索历史记录。支持整点提醒、可选任务顺延、深色背景，以及可拖动、调整大小的面板。记录与设置保存在本机，无需账号或联网。

版本更新记录：[CHANGELOG.md](CHANGELOG.md)。

### 开始使用

从 [GitHub Releases](https://github.com/mzz0928lmh-source/Dayline/releases/latest) 下载 Windows x64 版本。推荐下载安装程序 `Dayline-0.6.1-Setup-x64.exe`，双击后按向导安装；也可以下载 ZIP 压缩包，解压后运行 `Dayline.exe`。两种方式均无需另行安装 .NET。启动后先常驻系统托盘。升级前先在系统托盘退出旧版，已有目标、草稿和外观设置会继续读取。安装版支持开始菜单快捷方式、可选桌面快捷方式和卸载；卸载保留本地记录。

<img width="500"  alt="image" src="https://github.com/user-attachments/assets/9a5f4fdb-244f-4654-88ef-3a37f15d7cca" /><img width="500" alt="image" src="https://github.com/user-attachments/assets/f853e28e-438a-4f26-8550-e9fe4693fa5a" />


- 鼠标停在显示器**顶部中央、面板宽度内**的最上沿 **0.4 秒**，面板快速滑出；顶部两侧不会触发。范围与面板实际宽度一致，支持不同屏幕缩放。等待时间可在设置中调整为 0.2～2 秒。
- 勾选“失焦时自动收起”时，鼠标移出面板后自动收起，输入会先保存。顶部到面板之间的小间隙可直接移入；右键菜单算作软件区域，离开面板和菜单后一起收起。关闭该开关后，鼠标移出也不会自动收起。正在拖动画线、窗口或滑块时，等松开鼠标再收起。
- 点击右上角**画笔按钮**或托盘的“自定义外观”，调整浅色 / 深夜 / 墨黑底色、背景透明度、毛玻璃、圆角、目标字号和唤出速度；在系统设置中勾选“未完成任务自动顺延”后开启顺延。深夜与墨黑使用浅色文字，首次从高透明度切换时会自动降低透明度，确保文字清晰。设置页可调整界面宽度和高度，实时生效并在重启后保留，尺寸自动适应当前屏幕的可用空间。设置实时生效并自动保存。
- 在底部输入目标，按 **Enter** 添加。目标和未提交的输入草稿都会自动保存在本机。
- 在目标文字上按住鼠标左键，横向拖动画线。覆盖足够文字后标记为完成，保留手写轨迹。
- **双击**文字编辑；**右键**或点击行末 `⋯`，可以撤销完成、编辑或删除。面板中 `Ctrl+Z` 可撤销列表最后一条已完成的目标。
- 点击右上角日历，查看每天的内容缩略图；点击日期，在日历内浮窗查看、编辑或补记当天的目标，不离开当前月份；点击浮窗 ×、空白遮罩或按 Esc 关闭详情。
- 日历上方可全局搜索所有日期的任务，部分文字即可匹配，英文不区分大小写；多个关键词用空格分隔，匹配同时包含这些词的任务。结果按日期从新到旧排列，包含已完成和未完成任务，每一天的顺延记录均可定位。
- 选中搜索结果，点击“定位日期”、按 Enter 或双击，即切换到对应月份并高亮那一天；点击高亮日期可查看当天任务。Ctrl+F 聚焦搜索，↑↓ 选择结果，Esc 或 × 清空搜索。
- “未完成任务自动顺延”为系统配置，默认不勾选，只有勾选后才开启，取消勾选立即关闭，重启后仍保持选择。没有此配置的旧记录默认关闭，已有明确配置保留原选择。关闭时不产生新的顺延记录，已有记录保留；开启会补齐普通未完成任务的顺延。恢复默认外观不会更改此系统开关。
- 过去日期手动添加或编辑的任务视为补录，只保留在该日期；保存、重启、跨日都不会顺延。当天新增任务仍按开关设置处理。
- 开启顺延时，当天未完成的目标会在跨日时自动顺延到下一个工作日：周一至周四顺延一天，周五、周六、周日顺延到下周一。之后仍未完成则继续顺延，跳过周六、周日，不另行判断节假日或调休。
- 软件关闭期间的顺延在下次启动时补齐；周末打开时，未完成任务会安排到周一。原日期保留当时的记录，悬停目标可查看顺延日期。已完成的任务停止顺延，删除顺延后的任务不会从旧记录再次生成。未提交的输入草稿仍留在原日期。
- 每到整点自动显示 **10 秒**，不抢走当前软件的键盘焦点。未移入面板时按 10 秒倒计时；移入后再移出会提前收起。输入会重新开始倒计时，画线或编辑过程中不会因倒计时结束而收起。
- “失焦时自动收起”位于系统设置中，默认不勾选，勾选后切换到其他窗口或鼠标移出时自动收起，取消勾选后两种自动收起均关闭；编辑内容和草稿会先保存。选择自动保存并在重启后保留，已有明确配置保留原选择。
- 唤出后整个顶部区域都可以拖动，包括标题、左右留白和上边缘；右上角按钮保持原本的点击功能。拖动后进入固定模式，右下角出现缩放手柄，缩放后的宽度和高度也会保存。固定后，鼠标移出、失焦、整点倒计时和 Esc 都不会收起整个窗口；仍可用 Esc 关闭日期详情或清空搜索。点击右上角隐藏按钮收起后，下次展开恢复顶部位置并保留已设置的大小。
- 顶部停靠模式中，点击右上角上箭头或按 **Esc** 也可以收起。整点提醒未取得焦点时，仍按 10 秒倒计时自动结束。
- 软件常驻系统托盘。双击托盘图标打开今天，右键托盘菜单可查看日历、打开记录文件夹或退出。
- `Ctrl+Alt+D` 可切换顶部停靠面板，固定模式中用于激活窗口；如果快捷键已被其他软件占用，请使用顶部悬停或托盘。

运行期间才会检测顶部悬停和整点提醒。休眠恢复后不会补播错过的提醒；跨日后记录按日期保存。首次运行是空白内容，预览图中的示例不会写入你的记录。

### 数据

记录位置：`%LOCALAPPDATA%\Dayline\journal.json`。

每次保存先写临时文件，再原子替换主文件，上一次记录保存在 `journal.json.bak`。如果主文件损坏，会尝试恢复备份并保留损坏原文件。保存失败会在面板底部提示，退出时会阻止丢失仍在内存中的记录。

没有联网、账号或云同步功能。备份时复制整个 `Dayline` 数据目录即可。默认不会设置开机自启。

### 系统要求

- Windows 10 1809 或更高版本、Windows 11，x64。
- 推荐工作区至少 800 × 600 逻辑像素。支持高 DPI，并根据鼠标所在显示器定位。
- Windows 10 / 11 使用无底色的原生实时模糊，面板单独叠加可调透明底色，避免两层底色叠加造成发白。窗口使用逐像素透明、圆角裁剪，文字不会随背景一起变淡。透明效果受系统“透明效果”设置、远程桌面和显卡环境影响。

### 开发与验证

使用 .NET 10 SDK。WPF 负责界面，Win32 / DWM 负责顶部检测、窗口定位与背景模糊；WinForms 仅用于系统托盘。没有第三方 NuGet 依赖。

生成安装版 EXE：先安装 [Inno Setup 6 或 7](https://jrsoftware.org/isdl.php)，然后在项目根目录运行以下命令。脚本先发布包含 .NET 运行时的应用，再编译安装程序；输出位于 `artifacts/installer/`，文件名自动使用项目版本号。

```powershell
.\packaging\Build-Installer.ps1
# 编译器安装在其他位置时：
.\packaging\Build-Installer.ps1 -CompilerPath 'C:\Tools\Inno Setup\ISCC.exe'
```

```powershell
dotnet build Dayline.csproj -c Release
dotnet run --project checks/Checks.csproj -c Release
dotnet bin/Release/net10.0-windows/Dayline.dll --self-test preview
dotnet bin/Release/net10.0-windows/Dayline.dll --native-check preview
dotnet bin/Release/net10.0-windows/Dayline.dll --native-check preview --interaction-only
dotnet publish Dayline.csproj -c Release -r win-x64 --self-contained true -o artifacts/v0.6.1/win-x64
```

`--self-test` 离屏运行真实界面控件并输出测试报告、今日面板 / 日历 / 外观 / 空白状态 PNG。离屏预览图使用柔和的背景模拟合成。`--native-check` 显示约 16 秒的临时测试窗口，用双色底图验证真实背景透出，并验证实际窗口与触发范围对齐、鼠标区域判断、右键菜单、失焦收起、拖动固定、缩放、手动隐藏恢复停靠和再次展开，不访问正式记录。

项目文件：`Core.cs` 为记录与时间 / 笔迹规则，`MainWindow.xaml` 为布局，`MainWindow.xaml.cs` 为界面交互，`Native.cs` 为 Windows 系统集成，`App.xaml.cs` 为托盘与应用生命周期。

桌面截图无法获取有效背景像素时，可用 `--interaction-only` 单独运行窗口交互检查；完整背景透出验证仍使用不带该参数的 `--native-check`。

[↑ 回到顶部](#日迹--dayline)

---

<a id="english"></a>

## English

Dayline is a local Windows app for daily goals and reminders. Write down your goals, cross them out with mouse strokes, and browse or search past records by date. It includes hourly reminders, optional task carryover, dark themes, and a movable, resizable panel. Your records and settings stay on your computer; no account or internet connection is required.

Changelog: [CHANGELOG.md](CHANGELOG.md).

### Getting started

Download the Windows x64 build from [GitHub Releases](https://github.com/mzz0928lmh-source/Dayline/releases/latest). Run `Dayline-0.6.1-Setup-x64.exe` to install, or extract the ZIP and run `Dayline.exe`. No separate .NET installation is needed. The app starts in the system tray. Exit the previous version before upgrading; your records and settings are preserved. The installer adds a Start menu shortcut, offers an optional desktop shortcut, and supports uninstalling while keeping your records.

- Move the mouse to the very top edge of the display, **horizontally centered and within the panel width**, and hold for **0.4 s** to slide the panel out; the top corners do not trigger it. The trigger range matches the panel's width and follows display scaling. The delay is adjustable between 0.2 s and 2 s in Settings.
- With **Auto-hide on focus loss** enabled, the panel hides once the mouse leaves it, saving your input first. The gap between the top edge and the panel can be crossed directly. Context menus count as part of the app, so the panel hides only after you leave both. With the option off, the mouse leaving never hides the panel. While dragging a stroke, the window, or a slider, the panel waits until you release the mouse.
- Click the **brush button** in the top-right corner, or choose **Customize appearance** from the tray, to adjust the light / midnight / ink background, opacity, blur, corner radius, goal font size, and reveal delay. Enable task carryover by turning on **Carry over unfinished tasks** in system settings. Midnight and ink use light text, and switching from high opacity lowers it automatically for legibility. The settings page also adjusts panel width and height with live preview, and sizes persist across restarts while staying within the available screen space. Settings apply and save immediately.
- Type a goal at the bottom and press **Enter** to add it. Goals and unsubmitted drafts are saved locally.
- Hold the left mouse button on a goal and drag horizontally to draw a stroke. Covering enough text marks it complete and keeps the handwritten trail.
- **Double-click** text to edit it; **right-click** or use the `⋯` at the end of the row to undo completion, edit, or delete. `Ctrl+Z` in the panel undoes the last completed goal in the list.
- Click the calendar in the top-right corner to see a thumbnail of each day; click a date to view, edit, or backfill that day's goals in a popover without leaving the current month; close the details with the × in the popover, the blank overlay, or Esc.
- Search all dates from above the calendar. Partial text matches, English is case-insensitive, and space-separated keywords match goals containing all of them. Results run newest to oldest across completed and unfinished goals, and carryover records for any day can be located.
- With a result selected, choose **Go to date**, press Enter, or double-click to switch to that month and highlight the day; click the highlighted date to see that day's goals. `Ctrl+F` focuses search, ↑↓ moves through results, and Esc or × clears it.
- **Carry over unfinished tasks** is a system setting, off by default. It takes effect only when checked and turns off immediately when unchecked, and the choice survives restarts. Older records without the setting default to off, while explicit choices are kept. When off, no new carryover records are created and existing ones remain; when on, ordinary unfinished tasks are carried over. Restoring the default appearance does not change this switch.
- Goals added or edited on a past date count as backfill and stay on that date; they never carry over on save, restart, or rollover. Goals added today still follow the switch.
- With carryover on, unfinished goals move to the next working day at rollover: Monday through Thursday move one day, and Friday, Saturday, and Sunday move to the following Monday. Still-unfinished goals keep moving, skipping Saturday and Sunday, with no holiday or make-up workday awareness.
- Carryover while the app is closed is applied on the next launch; opening on a weekend schedules unfinished goals for Monday. The original date keeps its record, and hovering a goal shows its carryover date. Completed goals stop carrying over, and deleting a carried-over goal does not regenerate it from the old record. Unsubmitted drafts stay on their original date.
- The panel appears automatically for **10 seconds** on the hour without stealing keyboard focus from your current app. It counts down while the mouse is outside; moving in and back out hides it early. Typing restarts the countdown, and drawing or editing keeps it open until the countdown would otherwise end.
- **Auto-hide on focus loss** lives in system settings and is off by default. When checked, the panel hides on switching to another window or when the mouse leaves; when unchecked, both automatic behaviors are off. Edits and drafts are saved first, and the choice persists across restarts, with explicit choices kept.
- Once revealed, the whole top area can be dragged, including the title, the side margins, and the top edge, while the top-right buttons keep their click actions. Dragging switches to fixed mode and shows a resize handle in the bottom-right corner; the new width and height are saved too. In fixed mode, the mouse leaving, focus loss, the hourly countdown, and Esc no longer hide the window; Esc still closes date details or clears search. Clicking the hide button in the top-right corner collapses it, and the next reveal restores the top position with your saved size.
- In top-dock mode, click the up arrow in the top-right corner or press **Esc** to collapse the panel. When the hourly reminder has no focus, it still ends on its 10-second countdown.
- The app stays in the system tray. Double-click the tray icon to open today, or right-click it for the calendar, the records folder, or Exit.
- `Ctrl+Alt+D` toggles the top-docked panel and activates the window in fixed mode; if another app already uses the shortcut, use the top hover or the tray instead.

Top hover detection and hourly reminders run only while the app is running. Missed reminders are not replayed after sleep, and records are saved by date across rollovers. The first run is empty; the sample goals in the preview images are never written to your records.

### Data

Records live in `%LOCALAPPDATA%\Dayline\journal.json`.

Every save writes a temporary file and then atomically replaces the main file, with the previous version kept in `journal.json.bak`. If the main file is corrupted, the backup is restored and the corrupted file is kept. Save failures are reported at the bottom of the panel, and exiting prevents losing records still in memory.

There is no networking, account, or cloud sync. To back up, copy the whole `Dayline` data folder. The app does not run at startup by default.

### Requirements

- Windows 10 1809 or later, Windows 11, x64.
- A workspace of at least 800 × 600 logical pixels is recommended. High DPI is supported, and the panel follows the display under the mouse.
- Windows 10 / 11 uses native live blur with no base color, and the panel adds a separate adjustable tint so the two layers do not wash out. The window uses per-pixel transparency and rounded clipping so text does not fade with the background. Transparency depends on the system transparency setting, Remote Desktop, and the graphics environment.

### Development and verification

The project uses the .NET 10 SDK. WPF handles the UI, Win32 / DWM handles top-edge detection, window placement, and background blur, and WinForms is used only for the system tray. There are no third-party NuGet dependencies.

To build the installer EXE, install [Inno Setup 6 or 7](https://jrsoftware.org/isdl.php) and run this command from the project root. The script publishes a self-contained app and builds the installer in `artifacts/installer/`, using the project version in its filename.

```powershell
.\packaging\Build-Installer.ps1
# For a custom compiler location:
.\packaging\Build-Installer.ps1 -CompilerPath 'C:\Tools\Inno Setup\ISCC.exe'
```

```powershell
dotnet build Dayline.csproj -c Release
dotnet run --project checks/Checks.csproj -c Release
dotnet bin/Release/net10.0-windows/Dayline.dll --self-test preview
dotnet bin/Release/net10.0-windows/Dayline.dll --native-check preview
dotnet bin/Release/net10.0-windows/Dayline.dll --native-check preview --interaction-only
dotnet publish Dayline.csproj -c Release -r win-x64 --self-contained true -o artifacts/v0.6.1/win-x64
```

`--self-test` runs the real UI controls offscreen and writes a test report plus today panel, calendar, appearance, and empty-state PNGs. Offscreen previews are composited over a soft background. `--native-check` shows a temporary test window for about 16 seconds, uses a two-tone backdrop to verify real background bleed-through, and checks that the actual window aligns with the trigger range, along with mouse-region detection, context menus, auto-hide on focus loss, drag-to-fix, resizing, manual hide and re-dock, and reveal again, without touching your real records.

Project files: `Core.cs` holds records and the time / stroke rules, `MainWindow.xaml` the layout, `MainWindow.xaml.cs` the UI interaction, `Native.cs` the Windows integration, and `App.xaml.cs` the tray and app lifetime.

When a desktop screenshot cannot capture valid background pixels, run the window interaction check alone with `--interaction-only`; full background bleed-through verification still uses `--native-check` without that flag.

[↑ Back to top](#日迹--dayline)
