using System.Diagnostics;
using SDL2;
using WheelBridge.Core;

namespace WheelBridge;

/// <summary>The main loop: read devices, apply the profile, send a gamepad report.</summary>
public sealed class Bridge
{
    private readonly Profile _profile;
    private readonly DeviceManager _devices;
    private readonly VirtualPad _pad;

    public Bridge(Profile profile, DeviceManager devices, VirtualPad pad)
    {
        _profile = profile;
        _devices = devices;
        _pad = pad;
    }

    public void Run(CancellationToken token)
    {
        var period = TimeSpan.FromSeconds(1.0 / Math.Clamp(_profile.UpdateRateHz, 30, 1000));
        var clock = Stopwatch.StartNew();
        var next = clock.Elapsed;
        var lastStatus = TimeSpan.Zero;

        while (!token.IsCancellationRequested)
        {
            _devices.Update();
            Tick(out var steer, out var throttle, out var brake);

            if (clock.Elapsed - lastStatus > TimeSpan.FromMilliseconds(100))
            {
                lastStatus = clock.Elapsed;
                Console.Write($"\rSteer {steer,7:+0.000;-0.000}  Throttle {throttle,5:0.00}  Brake {brake,5:0.00}   ");
            }

            next += period;
            var wait = next - clock.Elapsed;
            if (wait > TimeSpan.Zero) Thread.Sleep(wait);
            else next = clock.Elapsed; // fell behind; don't try to catch up
        }
        Console.WriteLine();
    }

    private void Tick(out double steer, out double throttle, out double brake)
    {
        steer = Read(_profile.Steering, raw => AxisMath.Steering(raw, _profile.Steering));
        throttle = Read(_profile.Throttle, raw => AxisMath.Pedal(raw, _profile.Throttle));
        brake = Read(_profile.Brake, raw => AxisMath.Pedal(raw, _profile.Brake));

        _pad.SetSteering(AxisMath.ToStick(steer));
        _pad.SetThrottle(AxisMath.ToTrigger(throttle));
        _pad.SetBrake(AxisMath.ToTrigger(brake));

        // Several physical buttons may map to one pad button, so OR them together.
        var pressed = new Dictionary<PadButton, bool>();
        foreach (var b in _profile.Buttons)
        {
            var dev = _devices.Find(b.Device);
            var down = dev is not null && DeviceManager.Button(dev, b.Button);
            pressed[b.Target] = pressed.GetValueOrDefault(b.Target) || down;
        }

        if (_profile.DPad is { } hat && _devices.Find(hat.Device) is { } hatDev)
        {
            var h = DeviceManager.Hat(hatDev, hat.Hat);
            pressed[PadButton.Up] = pressed.GetValueOrDefault(PadButton.Up) || (h & SDL.SDL_HAT_UP) != 0;
            pressed[PadButton.Down] = pressed.GetValueOrDefault(PadButton.Down) || (h & SDL.SDL_HAT_DOWN) != 0;
            pressed[PadButton.Left] = pressed.GetValueOrDefault(PadButton.Left) || (h & SDL.SDL_HAT_LEFT) != 0;
            pressed[PadButton.Right] = pressed.GetValueOrDefault(PadButton.Right) || (h & SDL.SDL_HAT_RIGHT) != 0;
        }

        foreach (var (button, down) in pressed) _pad.SetButton(button, down);

        _pad.Submit();
    }

    private double Read(AxisSource src, Func<int, double> convert)
    {
        var dev = _devices.Find(src.Device);
        return dev is null ? 0 : convert(DeviceManager.Axis(dev, src.Axis));
    }
}
