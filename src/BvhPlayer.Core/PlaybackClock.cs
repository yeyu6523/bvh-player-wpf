namespace BvhPlayer.Core;

/// <summary>Independent of rendering: time is seconds of real elapsed time, never a timer tick count.</summary>
public sealed class PlaybackClock
{
    private double speed = 1;
    public int FrameCount { get; private set; } = 1;
    public double FrameTime { get; private set; } = 1.0 / 30;
    public double Duration => FrameCount * FrameTime;
    public double Position { get; private set; }
    public double FramePosition => Position / FrameTime;
    public int CurrentFrame => Math.Clamp((int)Math.Floor(FramePosition + 1e-8), 0, FrameCount - 1);
    public bool IsPlaying { get; private set; }
    public bool Loop { get; set; } = true;
    public double Speed
    {
        get => speed;
        set
        {
            if (!double.IsFinite(value) || value < 0.1 || value > 3) throw new ArgumentOutOfRangeException(nameof(value));
            speed = value;
        }
    }
    public void Load(BvhDocument document)
    { FrameCount = document.FrameCount; FrameTime = document.FrameTime; Position = 0; IsPlaying = false; }
    public void Play() { if (Position >= Duration) Position = 0; IsPlaying = true; }
    public void Pause() => IsPlaying = false;
    public void Stop() { IsPlaying = false; Position = 0; }
    public void SeekFrame(int frame) => Position = Math.Clamp(frame, 0, FrameCount - 1) * FrameTime;
    public void Step(int direction) { Pause(); SeekFrame(CurrentFrame + direction); }
    public void Advance(double elapsedSeconds)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        if (!IsPlaying) return;
        Position += elapsedSeconds * Speed;
        if (Position < Duration) return;
        if (Loop) Position %= Duration;
        else { Position = Duration; Pause(); }
    }
}
