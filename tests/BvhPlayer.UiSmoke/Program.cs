using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Automation;

internal static class Program
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern bool MoveWindow(nint hwnd, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll")] private static extern bool PostMessage(nint hwnd, uint msg, nint wParam, nint lParam);
    private static readonly List<string> checks = [];
    private static AutomationElement window = null!;

    private static void Assert(bool value, string message)
    { if (!value) throw new Exception(message); checks.Add(message); Console.WriteLine("PASS " + message); }
    private static AutomationElement Find(string id)
        => window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, id)) ?? throw new Exception("Missing control " + id);
    private static AutomationElement Named(string name, ControlType? type = null)
    {
        Condition condition = new PropertyCondition(AutomationElement.NameProperty, name);
        if (type != null) condition = new AndCondition(condition, new PropertyCondition(AutomationElement.ControlTypeProperty, type));
        return window.FindFirst(TreeScope.Descendants, condition) ?? throw new Exception("Missing control " + name);
    }
    private static void Click(AutomationElement element) => ((InvokePattern)element.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
    private static RangeValuePattern Range(string id) => (RangeValuePattern)Find(id).GetCurrentPattern(RangeValuePattern.Pattern);
    private static double Frame() => Range("Timeline").Current.Value;
    private static void Pause()
    { if (Find("PlayPause").Current.Name == "暂停") Click(Find("PlayPause")); }
    private static void WaitFor(Func<bool> condition, string failure, int milliseconds = 12000)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < milliseconds) { if (condition()) return; Thread.Sleep(80); }
        throw new Exception(failure);
    }
    private static void ShowHierarchyAtMinimumSize(Process process, bool verify)
    {
        MoveWindow(process.MainWindowHandle, 50, 40, 1080, 760, true); Thread.Sleep(200);
        var scrollable = window.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.IsScrollPatternAvailableProperty, true));
        foreach (AutomationElement el in scrollable)
        {
            var pattern = (ScrollPattern)el.GetCurrentPattern(ScrollPattern.Pattern);
            if (pattern.Current.VerticallyScrollable && el.Current.ControlType == ControlType.Pane)
            { pattern.SetScrollPercent(ScrollPattern.NoScroll, 100); break; }
        }
        Thread.Sleep(150);
        var tree = Find("JointTree");
        var items = tree.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TreeItem));
        if (items.Count < 2) throw new Exception("Hierarchy has fewer than two visible tree nodes");
        var item = items[1];
        if (item.TryGetCurrentPattern(ScrollItemPattern.Pattern, out var scroll)) ((ScrollItemPattern)scroll).ScrollIntoView();
        ((SelectionItemPattern)item.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
        Thread.Sleep(150);
        if (verify)
        {
            Assert(!item.Current.IsOffscreen && !Find("JointInfo").Current.IsOffscreen, "Minimum window keeps hierarchy and joint info accessible by scrolling");
            Assert(Find("JointInfo").Current.Name.StartsWith("3 通道", StringComparison.Ordinal), "Selecting a joint at minimum size updates its channel information");
        }
    }

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 0) { Console.WriteLine("Usage: UiSmoke <exe> [bvh file] [existing PID]"); return 2; }
        if (args[0] == "--inspect")
        {
            int id = int.Parse(args[1], CultureInfo.InvariantCulture);
            var elements = AutomationElement.RootElement.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ProcessIdProperty, id));
            foreach (AutomationElement el in elements)
                Console.WriteLine($"{el.Current.ControlType.ProgrammaticName} id={el.Current.AutomationId} name={el.Current.Name}");
            return 0;
        }
        if (args[0] == "--resize")
        {
            var app = Process.GetProcessById(int.Parse(args[1], CultureInfo.InvariantCulture));
            MoveWindow(app.MainWindowHandle, 50, 40, int.Parse(args[2], CultureInfo.InvariantCulture), int.Parse(args[3], CultureInfo.InvariantCulture), true);
            SetForegroundWindow(app.MainWindowHandle);
            return 0;
        }
        if (args[0] == "--minimum")
        {
            var app = Process.GetProcessById(int.Parse(args[1], CultureInfo.InvariantCulture));
            window = AutomationElement.FromHandle(app.MainWindowHandle);
            ShowHierarchyAtMinimumSize(app, true); SetForegroundWindow(app.MainWindowHandle);
            return 0;
        }
        if (args[0] == "--openinspect")
        {
            var app = Process.GetProcessById(int.Parse(args[1], CultureInfo.InvariantCulture));
            window = AutomationElement.FromHandle(app.MainWindowHandle);
            Click(Find("OpenFile")); Thread.Sleep(700);
            var elements = AutomationElement.RootElement.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ProcessIdProperty, app.Id));
            foreach (AutomationElement el in elements)
                Console.WriteLine($"{el.Current.ControlType.ProgrammaticName} id={el.Current.AutomationId} name={el.Current.Name}");
            return 0;
        }
        Process? process = null;
        try
        {
            process = args.Length > 2 ? Process.GetProcessById(int.Parse(args[2], CultureInfo.InvariantCulture))
                : Process.Start(new ProcessStartInfo(Path.GetFullPath(args[0])) { UseShellExecute = true });
            if (process == null) throw new Exception("Could not start app");
            WaitFor(() => { process.Refresh(); return process.MainWindowHandle != 0; }, "No main window");
            window = AutomationElement.FromHandle(process.MainWindowHandle);
            SetForegroundWindow(process.MainWindowHandle);
            WaitFor(() => Find("PlayPause").Current.IsEnabled && Range("Timeline").Current.Maximum > 0, "Demo did not load");
            Pause(); Click(Named("停止并回到首帧", ControlType.Button));
            Assert(Frame() == 0, "Stop shows first frame");
            double initialSpeed = Range("PlaybackSpeed").Current.Value;
            Console.WriteLine("OBSERVED initial speed " + initialSpeed);
            Range("PlaybackSpeed").SetValue(1);
            Assert(Math.Abs(Range("PlaybackSpeed").Current.Value - 1) < .01, "Speed slider changes to 1x");
            double maximum = Range("Timeline").Current.Maximum;
            Range("Timeline").SetValue(maximum / 3);
            Assert(Math.Abs(Frame() - Math.Round(maximum / 3)) < 1, "Timeline seeks while paused");
            Click(Named("下一帧", ControlType.Button)); double frame = Frame();
            Click(Named("上一帧", ControlType.Button));
            Assert(Frame() == frame - 1, "Next and previous frame buttons step the pose");
            Click(Named("停止并回到首帧", ControlType.Button));
            Click(Find("PlayPause")); Thread.Sleep(550);
            Pause(); double advance1 = Frame();
            Assert(advance1 > 10 && advance1 < 80, "Playback at 1x advances frames");
            Thread.Sleep(250); Assert(Frame() == advance1, "Pause keeps the frame unchanged");
            Click(Named("停止并回到首帧", ControlType.Button));
            Range("PlaybackSpeed").SetValue(2); Click(Find("PlayPause")); Thread.Sleep(550);
            Pause(); double advance2 = Frame();
            Assert(advance2 > advance1 * 1.5 && advance2 < advance1 * 2.6, "2x playback advances about twice as far");
            Range("PlaybackSpeed").SetValue(.1); Assert(Math.Abs(Range("PlaybackSpeed").Current.Value - .1) < .01, "Minimum speed 0.1x");
            Range("PlaybackSpeed").SetValue(3); Assert(Math.Abs(Range("PlaybackSpeed").Current.Value - 3) < .01, "Maximum speed 3x");
            Click(Named("1×", ControlType.Button)); Assert(Math.Abs(Range("PlaybackSpeed").Current.Value - 1) < .01, "1x preset works");
            Click(Named("正面", ControlType.Button)); Click(Named("侧面", ControlType.Button)); Click(Named("复位视角", ControlType.Button));
            Assert(process.Responding, "Front, side, and reset camera controls respond");
            var loop = (TogglePattern)Find("LoopPlayback").GetCurrentPattern(TogglePattern.Pattern);
            if (loop.Current.ToggleState == ToggleState.On) loop.Toggle();
            Range("Timeline").SetValue(maximum); Click(Find("PlayPause")); Thread.Sleep(200);
            Assert(Find("PlayPause").Current.Name == "播放" && Frame() == maximum, "Non-loop playback stops at final frame");
            Click(Find("PlayPause")); Thread.Sleep(150); Pause(); Assert(Frame() < maximum / 2, "Play restarts after reaching end");
            loop.Toggle(); Click(Named("停止并回到首帧", ControlType.Button));

            if (args.Length > 1)
            {
                Click(Find("OpenFile"));
                AutomationElement? dialog = null;
                WaitFor(() =>
                {
                    dialog = AutomationElement.RootElement.FindFirst(TreeScope.Descendants,
                        new AndCondition(new PropertyCondition(AutomationElement.ProcessIdProperty, process.Id), new PropertyCondition(AutomationElement.NameProperty, "打开 BVH 运动捕捉文件")));
                    return dialog != null;
                }, "Open-file dialog not found");
                var file = dialog!.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "1148"));
                file ??= dialog.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
                if (file == null) throw new Exception("File name field not found");
                var childEdit = file.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
                if (childEdit != null) file = childEdit;
                file.SetFocus();
                ((ValuePattern)file.GetCurrentPattern(ValuePattern.Pattern)).SetValue(Path.GetFullPath(args[1]));
                Console.WriteLine("OBSERVED dialog filename " + ((ValuePattern)file.GetCurrentPattern(ValuePattern.Pattern)).Current.Value);
                Thread.Sleep(200);
                var open = dialog.FindFirst(TreeScope.Descendants, new AndCondition(new PropertyCondition(AutomationElement.AutomationIdProperty, "1"), new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))) ?? throw new Exception("Dialog Open button not found");
                Console.WriteLine("OBSERVED clicked " + open.Current.Name);
                Click(open);
                WaitFor(() => window.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, Path.GetFileName(args[1]))) != null, "Selected BVH did not open");
                Assert(Range("Timeline").Current.Maximum > 240, "File dialog imports supplied BVH data");
                Range("Timeline").SetValue(Range("Timeline").Current.Maximum / 2);
                Assert(Frame() > 0, "Imported file can seek");
            }

            var invalidPath = Path.Combine(Path.GetTempPath(), "bvh-player-invalid.bvh");
            File.WriteAllText(invalidPath, "not a BVH file");
            // A second process exercises the startup-file error path and should retain its demo.
            using (var invalid = Process.Start(new ProcessStartInfo(Path.GetFullPath(args[0])) { UseShellExecute = true, ArgumentList = { invalidPath } }))
            {
                if (invalid == null) throw new Exception("Could not launch malformed-file check");
                AutomationElement? error = null;
                WaitFor(() =>
                {
                    error = AutomationElement.RootElement.FindFirst(TreeScope.Descendants,
                        new AndCondition(new PropertyCondition(AutomationElement.ProcessIdProperty, invalid.Id), new PropertyCondition(AutomationElement.NameProperty, "BVH 载入失败")));
                    return error != null;
                }, "Malformed file did not produce an error");
                var okay = error!.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
                if (okay != null) Click(okay);
                invalid.Refresh();
                Assert(invalid.Responding, "Malformed input shows an error without crashing");
                PostMessage(invalid.MainWindowHandle, 0x0010, 0, 0);
                invalid.WaitForExit(5000);
            }
            Assert(process.Responding, "Main process remains responsive after all controls");
            ShowHierarchyAtMinimumSize(process, true);
            Pause(); Range("PlaybackSpeed").SetValue(1);
            Range("Timeline").SetValue(Math.Min(100, Range("Timeline").Current.Maximum / 4));
            MoveWindow(process.MainWindowHandle, 50, 40, 1320, 860, true); SetForegroundWindow(process.MainWindowHandle);
            Directory.CreateDirectory("TestResults");
            File.WriteAllText("TestResults/ui-results.json", JsonSerializer.Serialize(new { passed = checks.Count, failed = 0, executable = Path.GetFileName(args[0]), pid = process.Id, windowHandle = process.MainWindowHandle.ToInt64(), checks }, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"RESULT {checks.Count} UI checks passed. PID {process.Id}; HWND {process.MainWindowHandle}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Directory.CreateDirectory("TestResults");
            File.WriteAllText("TestResults/ui-results.json", JsonSerializer.Serialize(new { passed = checks.Count, failed = 1, error = ex.ToString(), checks }, new JsonSerializerOptions { WriteIndented = true }));
            return 1;
        }
    }
}
