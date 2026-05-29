using System;
using System.Drawing;
using System.Windows.Forms;

namespace DisKlinigiYonetimSistemi.Controls;

public sealed class Animator : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly int _durationMs;
    private int _elapsedMs;
    private Action<float>? _onUpdate;
    private Action? _onComplete;
    private const int Interval = 16; // ~60 FPS

    public Animator(int durationMs)
    {
        _durationMs = durationMs;
        _timer = new System.Windows.Forms.Timer { Interval = Interval };
        _timer.Tick += OnTick;
    }

    public void Start(Action<float> onUpdate, Action? onComplete = null)
    {
        Stop();
        _onUpdate = onUpdate;
        _onComplete = onComplete;
        _elapsedMs = 0;
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _elapsedMs = 0;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _elapsedMs += Interval;
        if (_elapsedMs >= _durationMs)
        {
            Stop();
            _onUpdate?.Invoke(1f);
            _onComplete?.Invoke();
        }
        else
        {
            // Ease-out cubic
            float t = (float)_elapsedMs / _durationMs;
            t--;
            float eased = (t * t * t + 1);
            _onUpdate?.Invoke(eased);
        }
    }

    public void Dispose()
    {
        Stop();
        _timer.Dispose();
    }

    // Helpers
    public static Color Blend(Color start, Color end, float amount)
    {
        int r = (int)(start.R + (end.R - start.R) * amount);
        int g = (int)(start.G + (end.G - start.G) * amount);
        int b = (int)(start.B + (end.B - start.B) * amount);
        return Color.FromArgb(Math.Max(0, Math.Min(255, r)), Math.Max(0, Math.Min(255, g)), Math.Max(0, Math.Min(255, b)));
    }
}
