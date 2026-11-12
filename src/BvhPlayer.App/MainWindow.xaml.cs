using BvhPlayer.Core;
using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BvhPlayer.App;

public partial class MainWindow : Window
{
    private readonly PlaybackClock clock = new();
    private readonly Stopwatch timer = Stopwatch.StartNew();
    private BvhDocument? clip;
    private bool ready, loading, updating, scrubbing, resumeAfterSeek;
    private double lastTime;
    private double lastRender;

    public MainWindow(string? initialPath)
    {
        InitializeComponent();
        ready = true;
        Loaded += async (_, _) =>
        {
            await LoadDemo();
            if (initialPath != null) await LoadFile(initialPath);
            lastTime = timer.Elapsed.TotalSeconds;
            CompositionTarget.Rendering += OnRendering;
        };
        Closed += (_, _) => CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        double now = timer.Elapsed.TotalSeconds;
        double elapsed = now - lastTime; lastTime = now;
        if (clip == null || loading) return;
        bool wasPlaying = clock.IsPlaying;
        clock.Advance(elapsed);
        if (!wasPlaying || (clock.IsPlaying && now - lastRender < 1.0 / 60)) return;
        lastRender = now;
        RefreshPose();
        UpdateTransport();
    }

    private async Task LoadDemo()
    {
        if (loading) return;
        await LoadDocument(() =>
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BvhPlayer.App.Assets.studio-walk.bvh")
                ?? throw new FileNotFoundException("缺少内置示例资源。");
            using var reader = new StreamReader(stream);
            return BvhDocument.Parse(reader.ReadToEnd(), "studio-walk.bvh");
        }, "原创内置示例 · 可拖入课件提供的任意 BVH 文件");
    }
    private async Task LoadFile(string path)
    {
        if (loading) return;
        if (!Path.GetExtension(path).Equals(".bvh", StringComparison.OrdinalIgnoreCase))
        { MessageBox.Show(this, "请选择扩展名为 .bvh 的运动捕捉文件。", "文件类型不支持", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        await LoadDocument(() => BvhDocument.Load(path), Path.GetFullPath(path));
    }

    private async Task LoadDocument(Func<BvhDocument> loader, string description)
    {
        loading = true;
        bool wasPlaying = clock.IsPlaying;
        clock.Pause();
        LoadingOverlay.Visibility = Visibility.Visible;
        PlayButton.IsEnabled = false;
        try
        {
            BvhDocument loaded = await Task.Run(loader);
            // Configure the scene before replacing the current transport/document.
            Scene.LoadClip(loaded);
            clip = loaded; clock.Load(loaded);
            FileNameText.Text = ViewportFile.Text = loaded.Name;
            FileNameText.ToolTip = description;
            FrameCountText.Text = loaded.FrameCount.ToString("N0", CultureInfo.InvariantCulture);
            FpsText.Text = $"{loaded.FramesPerSecond:0.##} fps";
            JointsText.Text = $"{loaded.AnimatedJointCount} / {loaded.ChannelCount}";
            Timeline.Maximum = Math.Max(loaded.FrameCount - 1, 0);
            Timeline.LargeChange = Math.Max(1, Math.Round(loaded.FramesPerSecond));
            StatusText.Text = description; StatusText.ToolTip = description;
            BuildTree();
            ApplyOptions();
            RefreshPose();
            UpdateTransport();
        }
        catch (Exception ex) when (ex is IOException or FormatException or UnauthorizedAccessException or ArgumentException)
        {
            if (wasPlaying) clock.Play();
            StatusText.Text = "载入失败，保留上一动作：" + ex.Message;
            MessageBox.Show(this, "无法读取 BVH 文件：\n\n" + ex.Message + "\n\n请检查文件格式后重试。", "BVH 载入失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            loading = false; LoadingOverlay.Visibility = Visibility.Collapsed;
            PlayButton.IsEnabled = clip != null;
            lastTime = timer.Elapsed.TotalSeconds;
        }
    }

    private void BuildTree()
    {
        JointTree.Items.Clear();
        if (clip == null) return;
        var items = new TreeViewItem[clip.Joints.Count];
        for (int i = 0; i < clip.Joints.Count; i++)
        {
            var j = clip.Joints[i];
            var item = new TreeViewItem { Header = j.IsEndSite ? "End Site" : j.Name, Tag = i, IsExpanded = j.ParentIndex < 0, Foreground = new SolidColorBrush(Color.FromRgb(52, 70, 94)) };
            items[i] = item;
            if (j.ParentIndex < 0) JointTree.Items.Add(item); else items[j.ParentIndex].Items.Add(item);
        }
        if (items.Length > 0) items[0].IsSelected = true;
    }
    private void Joint_Selected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (clip == null || JointTree.SelectedItem is not TreeViewItem { Tag: int i }) return;
        var j = clip.Joints[i];
        JointInfo.Text = j.IsEndSite ? "末端节点 · 不含运动通道" : $"{j.Channels.Length} 通道：" + string.Join(" → ", j.Channels.Select(c => c.ToString().Replace("rotation", "旋转", StringComparison.Ordinal).Replace("position", "位移", StringComparison.Ordinal)));
        Scene.SelectJoint(i);
    }
    private void RefreshPose()
    {
        if (clip == null) return;
        double f = clock.Position >= clock.Duration ? clip.FrameCount - 1 : clock.FramePosition;
        Scene.Render(f, SmoothCheck.IsChecked == true, clock.Loop);
    }
    private void UpdateTransport()
    {
        updating = true;
        if (!scrubbing) Timeline.Value = clock.CurrentFrame;
        FrameLabel.Text = $"帧 {clock.CurrentFrame + 1:N0} / {clock.FrameCount:N0}";
        TimeLabel.Text = $"{FormatTime(clock.Position)} / {FormatTime(clock.Duration)}";
        PlayLabel.Text = clock.IsPlaying ? "暂停" : "播放";
        PlayIcon.Data = Geometry.Parse(clock.IsPlaying ? "M0,0 L4,0 L4,14 L0,14 Z M7,0 L11,0 L11,14 L7,14 Z" : "M0,0 L0,14 L11,7 Z");
        ViewportState.Text = clock.IsPlaying ? $"正在播放 · {clock.Speed:0.0}×" : "已暂停 · 空格键开始播放";
        StateDot.Fill = new SolidColorBrush(clock.IsPlaying ? Color.FromRgb(83, 216, 170) : Color.FromRgb(149, 166, 190));
        System.Windows.Automation.AutomationProperties.SetName(PlayButton, clock.IsPlaying ? "暂停" : "播放");
        updating = false;
    }
    private static string FormatTime(double seconds)
        => $"{(int)seconds / 60:00}:{seconds % 60:00.00}";

    private void TogglePlayback() { if (clip == null || loading) return; if (clock.IsPlaying) clock.Pause(); else clock.Play(); lastTime = timer.Elapsed.TotalSeconds; UpdateTransport(); }
    private void Step(int direction) { if (clip == null || loading) return; clock.Step(direction); RefreshPose(); UpdateTransport(); }
    private void Stop() { if (loading) return; clock.Stop(); RefreshPose(); UpdateTransport(); }
    private void ApplyOptions()
    { clock.Loop = LoopCheck.IsChecked == true; Scene.Configure(FollowCheck.IsChecked == true, GridCheck.IsChecked == true); }
    private void Play_Click(object sender, RoutedEventArgs e) => TogglePlayback();
    private void Stop_Click(object sender, RoutedEventArgs e) => Stop();
    private void Previous_Click(object sender, RoutedEventArgs e) => Step(-1);
    private void Next_Click(object sender, RoutedEventArgs e) => Step(1);
    private async void Demo_Click(object sender, RoutedEventArgs e) => await LoadDemo();
    private async void Open_Click(object sender, RoutedEventArgs e) => await OpenFile();
    private async Task OpenFile()
    {
        if (loading) return;
        var dialog = new OpenFileDialog { Title = "打开 BVH 运动捕捉文件", Filter = "BVH 运动捕捉文件 (*.bvh)|*.bvh", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) await LoadFile(dialog.FileName);
    }
    private void Speed_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    { if (!ready) return; clock.Speed = SpeedSlider.Value; SpeedText.Text = $"{clock.Speed:0.0}×"; UpdateTransport(); }
    private void SpeedPreset_Click(object sender, RoutedEventArgs e)
    { if (sender is Button { Tag: string value }) SpeedSlider.Value = double.Parse(value, CultureInfo.InvariantCulture); }
    private void Options_Changed(object sender, RoutedEventArgs e)
    { if (!ready) return; ApplyOptions(); RefreshPose(); }
    private void Front_Click(object sender, RoutedEventArgs e) => Scene.SetView(false);
    private void Side_Click(object sender, RoutedEventArgs e) => Scene.SetView(true);
    private void ResetView_Click(object sender, RoutedEventArgs e) => Scene.ResetCamera();
    private void Timeline_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    { if (!ready || updating || clip == null || loading) return; clock.SeekFrame((int)Math.Round(Timeline.Value)); RefreshPose(); UpdateTransport(); }
    private void Timeline_Down(object sender, MouseButtonEventArgs e)
    { if (loading) return; scrubbing = true; resumeAfterSeek = clock.IsPlaying; clock.Pause(); }
    private void EndSeek()
    { if (!scrubbing) return; scrubbing = false; if (resumeAfterSeek) clock.Play(); lastTime = timer.Elapsed.TotalSeconds; UpdateTransport(); }
    private void Timeline_Up(object sender, MouseButtonEventArgs e) => EndSeek();
    private void Timeline_LostCapture(object sender, MouseEventArgs e) => EndSeek();
    private void Window_DragOver(object sender, DragEventArgs e)
    { e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; }
    private async void Window_Drop(object sender, DragEventArgs e)
    { if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths) await LoadFile(paths[0]); }
    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.O && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) { e.Handled = true; await OpenFile(); return; }
        if (Keyboard.Modifiers != ModifierKeys.None) return;
        if (e.Key == Key.Space) { TogglePlayback(); e.Handled = true; }
        else if (e.Key == Key.Home) { Stop(); e.Handled = true; }
        else if (e.Key == Key.R) { Scene.ResetCamera(); e.Handled = true; }
        else if (e.Key is Key.Left or Key.Right && Keyboard.FocusedElement is not Slider && !JointTree.IsKeyboardFocusWithin)
        { Step(e.Key == Key.Left ? -1 : 1); e.Handled = true; }
    }
}
